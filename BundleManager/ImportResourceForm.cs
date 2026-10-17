using BundleFormat;
using BundleUtilities;
using BurnoutImage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BundleManager
{
    public partial class ImportResourceForm : Form
    {
        private const string ResourceNamePrefix = "bundlemanager://";
        private const int ImportEntrySize = 0x10;

        private const string BinaryFilter = "Binary Files|*.dat;*.bin|All files (*.*)|*.*";
        private const string BinaryAndImageFilter = "Binary Files|*.dat;*.bin|Image Files(*.dds,*.bmp;*.gif;*.jpg;*.png;*.tif;*.tga;*.webp)|*.dds;*.bmp;*.gif;*.jpg;*.png;*.tif;*.tga;*.webp|All files (*.*)|*.*";
        private static readonly string[] ImageExtensions = [".dds", ".bmp", ".gif", ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".tga", ".webp"];

        DataChunkImportProperties Properties = new();
        public bool AutoUpdateID;
        public BundleArchive ResultArchive { get; private set; }

        private BundleArchive _workingArchive;

        private byte[] _primaryData;
        private List<Dependency> _imports = [];
        private byte[] _imageBody;

        string ImportName
        {
            get => _importName;
            set
            {
                _importName = value ?? string.Empty;
                SetResourceNameText(ResourceNamePrefix + _importName, ResourceNamePrefix.Length + _importName.Length);
            }
        }
        private string _importName;
        private bool _isUpdatingResourceNameText;

        public ImportResourceForm(BundleArchive destination, string primaryDataPath, uint? resourceID = null)
        {
            InitializeComponent();

            _workingArchive = destination;

            RegisterDataBlockProperties();

            resourceTypeComboBox.DataSource = Enum.GetValues(typeof(EntryType));
            resourceTypeComboBox.SelectedIndexChanged += ResourceTypeComboBox_SelectedIndexChanged;

            resourceIDTextBox.KeyPress += ResourceIDTextBox_KeyPress;
            resourceIDTextBox.Validating += ResourceIDTextBox_Validating;

            primaryResourcePathTextBox.Validated += UpdateDataPathsFromTextBoxes;
            secondaryResourcePathTextBox.Validated += UpdateDataPathsFromTextBoxes;
            tertiaryResourcePathTextBox.Validated += UpdateDataPathsFromTextBoxes;

            importCountUpDown.ValueChanged += (_, _) => RefreshImportsList();

            LoadPrimary(primaryDataPath);

            if (resourceID is uint id)
                resourceIDTextBox.Text = id.ToString("X8");
            autoUpdateIDCheckBox.Checked = resourceID == null;
        }

        #region Event Handlers

        private void ResourceIDTextBox_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!Utilities.IsValidHex(resourceIDTextBox.Text))
            {
                e.Cancel = true;
                MessageBox.Show("Please enter a valid hexadecimal value.");
            }
        }

        private void ResourceIDTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            bool isControl = char.IsControl(e.KeyChar);
            bool isHex = Uri.IsHexDigit(e.KeyChar);

            if (!isControl && !isHex)
                e.Handled = true;
        }

        private void ResourceTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (resourceTypeComboBox.SelectedItem is EntryType entryType)
            {
                Properties.EntryType = entryType;
                FillDefaultSecondary();

                if (AutoUpdateID)
                    RecalculateResourceIDTextBox();
            }
        }

        private void autoUpdateIDCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            AutoUpdateID = autoUpdateIDCheckBox.Checked;
            resourceIDTextBox.ReadOnly = autoUpdateIDCheckBox.Checked;

            if (AutoUpdateID)
                RecalculateResourceIDTextBox();
        }

        private void resourceNameTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_isUpdatingResourceNameText)
                return;

            string text = resourceNameTextBox.Text ?? string.Empty;

            _importName = text.StartsWith(ResourceNamePrefix, StringComparison.OrdinalIgnoreCase)
                ? text.Substring(ResourceNamePrefix.Length)
                : text;

            if (AutoUpdateID)
                RecalculateResourceIDTextBox();
        }

        private void primaryDataPathSelectorButton_Click(object sender, EventArgs e)
        {
            SelectNewDataChunkDialog(0);
        }

        private void secondaryDataPathSelectorButton_Click(object sender, EventArgs e)
        {
            SelectNewDataChunkDialog(1);
        }

        private void tertiaryDataPathSelectorButton_Click(object sender, EventArgs e)
        {
            SelectNewDataChunkDialog(2);
        }

        private void importButton_Click(object sender, EventArgs e)
        {
            try
            {
                RunImportOperation();
            }
            catch (Exception ex)
            {
                Warn("Failed to import the resource: " + ex.Message);
            }
        }
#endregion

        void RegisterDataBlockProperties()
        {
            Properties.Entries[0].PathSelectorControls =
            [
                primaryDataPathSelectorButton,
                primaryResourcePathTextBox,
                primaryImportPathLabel
            ];
            Properties.Entries[1].PathSelectorControls =
            [
                secondaryDataPathSelectorButton,
                secondaryResourcePathTextBox,
                secondaryImportPathLabel
            ];
            Properties.Entries[2].PathSelectorControls =
            [
                tertiaryDataPathSelectorButton,
                tertiaryResourcePathTextBox,
                tertiaryImportPathLabel
            ];

            primaryResourcePathTextBox.Tag = 0;
            secondaryResourcePathTextBox.Tag = 1;
            tertiaryResourcePathTextBox.Tag = 2;

            for (int i = 0; i < Properties.Entries.Length; i++)
            {
                Properties.Entries[i].PathChanged = OnAnyDataBlockPathsUpdated;
            }

            OnAnyDataBlockPathsUpdated();
        }

        void LoadPrimary(string path)
        {
            byte[] primary;
            byte[] imageBody = null;
            List<Dependency> imports = [];
            string importsSource = null;
            EntryType type = EntryType.Texture;

            if (ImageExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            {
                PlatformType platform = GetImageTexturePlatform()
                    ?? throw new NotSupportedException($"Images can't be converted for {_workingArchive.Platform} bundles. Import the texture's header and body instead.");

                ImageInfo info = GameImage.SetImage(path, CompressionType.DXT5, platform);
                primary = info.Header;
                imageBody = info.Data;
            }
            else
            {
                primary = File.ReadAllBytes(path);

                string yamlPath = GetImportsYamlPath(path);
                if (yamlPath != null && File.Exists(yamlPath))
                {
                    imports = LoadDependenciesFromYaml(yamlPath);
                    importsSource = Path.GetFileName(yamlPath);
                    primary = AppendImportTable(primary, imports, _workingArchive.Console);
                }
                else
                {
                    imports = ScanImportTable(primary, _workingArchive.Console, _workingArchive.Platform == BundlePlatform.PCx64 ? 8 : 4);
                }

                type = InferEntryType(primary[..^(imports.Count * ImportEntrySize)], imports.Count);
            }

            _primaryData = primary;
            _imageBody = imageBody;
            _imports = imports;

            ImportName = GetResourceBaseName(path);
            Properties.Entries[0].Path = path;
            Properties.Entries[1].Path = string.Empty; // Belonged to the previous primary

            resourceTypeComboBox.SelectedItem = type;
            Properties.EntryType = type;
            resourceTypeComboBox.Enabled = imageBody == null;
            if (imageBody != null)
                Properties.Entries[1].Visible = false; // Body comes from the image
            FillDefaultSecondary();

            selectTypeLabel.Text = type == EntryType.Invalid
                ? "Unable to auto detect resource type, please select it from the box below:"
                : "Select the resource type to import as:";

            importCountUpDown.Value = 0;
            importCountUpDown.Maximum = imports.Count;
            importCountUpDown.Value = imports.Count;
            importCountUpDown.Enabled = importsSource == null && imports.Count > 0;
            importsLabel.Text = imageBody != null ? "Imports: none (texture built from the image)"
                : importsSource != null ? $"Imports from {importsSource}:"
                : imports.Count > 0 ? "Imports detected at the end of the primary data. Lower the count if the first rows are really resource data:"
                : "Imports: none detected in the primary data";
            RefreshImportsList();

            if (AutoUpdateID)
                RecalculateResourceIDTextBox();
        }

        private void TryLoadPrimary(string path)
        {
            try
            {
                LoadPrimary(path);
            }
            catch (Exception ex)
            {
                Warn($"Unable to use {path}: {ex.Message}");
                OnAnyDataBlockPathsUpdated(); // Put the old path back in the text box
            }
        }

        PlatformType? GetImageTexturePlatform()
        {
            BundleEntry sample = _workingArchive.Entries.FirstOrDefault(e => e.Type == EntryType.Texture && e.HasHeader);
            PlatformType? platform = sample != null
                ? GameImage.DetectPlatform(sample.EntryBlocks[0].Data)
                : _workingArchive.Platform switch
                {
                    BundlePlatform.PC => PlatformType.PC,
                    BundlePlatform.PCx64 => PlatformType.PCx64,
                    _ => null
                };

            // GameImage.SetImage can only write these
            return platform is PlatformType.PC or PlatformType.PCx64 or PlatformType.Remastered ? platform : null;
        }

        private static bool TextureMatchesBundle(PlatformType texture, BundlePlatform bundle) => bundle switch
        {
            BundlePlatform.PC => texture is PlatformType.PC or PlatformType.Remastered,
            BundlePlatform.PCx64 => texture == PlatformType.PCx64,
            BundlePlatform.X360 => texture == PlatformType.X360,
            BundlePlatform.PS3 => texture == PlatformType.PS3,
            _ => true
        };

        // This really needs to be put in a better spot
        EntryType InferEntryType(byte[] data, int importCount)
        {
            // AttribSysVault
            if (_workingArchive.Console switch
            {
                true => Utilities.ContainsASCII(data, "StrN")
                     && Utilities.ContainsASCII(data, "Vers"),
                false => Utilities.ContainsASCII(data, "NrtS")
                      && Utilities.ContainsASCII(data, "sreV")
            } == true)
            {
                return EntryType.AttribSysVault;
            }

            EntryType type = EntryType.Invalid;

            // Texture
            if (GameImage.DetectPlatform(data) is PlatformType p
                && p != PlatformType.Invalid
                && importCount == 0)
            {
                type = EntryType.Texture;
            }
            // Model
            else if (data.Length <= 0x20)
            {
                // 32 bit model, then 64 bit model (32 bit is technically padded to nearest 0x10, so this would run anyway)
                if ((data.Length > 0x13 && data[0x13] == 0x2)
                    || (data.Length > 0x1F && data[0x1F] == 0x2))
                    type = EntryType.Model;
            }
            // Renderable
            else
            {
                if (_workingArchive.Console switch
                {
                    true => Utilities.ReadUInt16BE(data, 0x10),
                    false => Utilities.ReadUInt16LE(data, 0x10)
                } == 11) // mu16VersionNumber
                {
                    type = EntryType.Renderable;
                }
            }

            return type;
        }

        void OnAnyDataBlockPathsUpdated()
        {
            primaryResourcePathTextBox.Text = Properties.Entries[0].Path;
            secondaryResourcePathTextBox.Text = Properties.Entries[1].Path;
            tertiaryResourcePathTextBox.Text = Properties.Entries[2].Path;
        }

        private void UpdateDataPathsFromTextBoxes(object sender, EventArgs e)
        {
            if (sender is not TextBox { Tag: int index } textBox || textBox.Text == Properties.Entries[index].Path)
                return;

            if (index == 0)
                TryLoadPrimary(textBox.Text);
            else
                Properties.Entries[index].Path = textBox.Text;
        }

        private void RecalculateResourceIDTextBox()
        {
            string nameText = resourceNameTextBox.Text ?? string.Empty;

            Utilities.CalculateResourceIDFromName
            (
                string.Concat
                (
                    nameText.ToLowerInvariant(),
                    ".",
                    Properties.EntryType.ToString()
                ),
                out string id
            );
            resourceIDTextBox.Text = id;
        }

        private void RefreshImportsList()
        {
            int count = (int)importCountUpDown.Value;

            importsListBox.BeginUpdate();
            importsListBox.Items.Clear();
            foreach (Dependency dep in _imports.Skip(_imports.Count - count))
            {
                BundleEntry target = _workingArchive.GetEntryByID(dep.ID);
                importsListBox.Items.Add($"0x{dep.EntryPointerOffset:X8} -> {dep.ID:X8}"
                    + (target == null ? string.Empty : $"  ({target.Type} in this bundle)"));
            }
            importsListBox.EndUpdate();
        }

        void SelectNewDataChunkDialog(byte index)
        {
            using (OpenFileDialog ofd = new())
            {
                ofd.Title = $"Please select the {GetDataChunkName(index)} resource to import";
                ofd.Filter = index == 0 ? BinaryAndImageFilter : BinaryFilter;

                if (ofd.ShowDialog(this) != DialogResult.OK)
                    return;

                string path = ofd.FileName;

                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return;

                if (index == 0)
                    TryLoadPrimary(path);
                else
                    Properties.Entries[index].Path = path;
            }
        }

        private void RunImportOperation()
        {
            EntryType type = Properties.EntryType;

            if (string.IsNullOrWhiteSpace(_importName))
            {
                Warn("Please enter a name for the resource.");
                return;
            }

            if (type == EntryType.Invalid)
            {
                Warn("Please select the resource type to import as.");
                return;
            }

            if (!uint.TryParse(resourceIDTextBox.Text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint id))
            {
                Warn("Please enter a valid hexadecimal resource ID.");
                return;
            }

            BundleEntry template = _workingArchive.Entries.FirstOrDefault(e => e.Type == type);

            // Secondary data is memory type 2 on PS3 and 1 elsewhere
            int secondaryBlock = template != null
                ? (template.HasSection(2) && !template.HasSection(1) ? 2 : 1)
                : (_workingArchive.Platform == BundlePlatform.PS3 ? 2 : 1);

            byte[][] dataBlocks = new byte[3][];
            dataBlocks[0] = _primaryData;

            if (_imageBody != null)
            {
                dataBlocks[secondaryBlock] = _imageBody;
            }
            else
            {
                for (int i = 1; i < Properties.DataChunkCount; i++)
                {
                    if (!File.Exists(Properties.Entries[i].Path))
                    {
                        Warn($"Please select valid {GetDataChunkName(i)} data for the resource you are trying to import.");
                        return;
                    }

                    dataBlocks[i == 1 ? secondaryBlock : i] = File.ReadAllBytes(Properties.Entries[i].Path);
                }

                if (type == EntryType.Texture
                    && !TextureMatchesBundle(GameImage.DetectPlatform(_primaryData), _workingArchive.Platform)
                    && !Confirm($"This texture header doesn't look like a {_workingArchive.Platform} texture. Import it anyway?"))
                    return;
            }

            BundleEntry existing = _workingArchive.GetEntryByID(id);
            if (existing != null && !Confirm($"This bundle already has a resource with ID {id:X8} ({existing.Type}). Replace it?"))
                return;

            int importCount = (int)importCountUpDown.Value;

            BundleEntry entry = new(_workingArchive)
            {
                ID = id,
                Type = type,
                Platform = _workingArchive.Platform,
                DependencyCount = (short)importCount,
                DependenciesListOffset = importCount > 0 ? _primaryData.Length - importCount * ImportEntrySize : 0,
                DebugInfo = new()
                {
                    Name = $"{resourceNameTextBox.Text}.{type}",
                    TypeName = type.ToString()
                },
                Dirty = true,
                EntryBlocks = new EntryBlock[3]
            };

            for (int i = 0; i < entry.EntryBlocks.Length; i++)
            {
                EntryBlock block = new() { Compressed = false };
                if (dataBlocks[i] != null)
                {
                    block.Data = dataBlocks[i];
                    block.UncompressedAlignment = template != null && template.HasSection(i) ? template.EntryBlocks[i].UncompressedAlignment : 16;
                }
                entry.EntryBlocks[i] = block;
            }

            if (existing != null)
                _workingArchive.Entries.Remove(existing);

            // Retain ID sorting like retail bundles
            int index = _workingArchive.Entries.FindIndex(e => e.ID > entry.ID);
            _workingArchive.Entries.Insert(index < 0 ? _workingArchive.Entries.Count : index, entry);
            for (int i = 0; i < _workingArchive.Entries.Count; i++)
                _workingArchive.Entries[i].Index = i;

            ResultArchive = _workingArchive;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Warn(string message)
        {
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private bool Confirm(string message)
        {
            return MessageBox.Show(this, message, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        #region Dependency Handling

        private void SetResourceNameText(string text, int caretPosition)
        {
            _isUpdatingResourceNameText = true;
            resourceNameTextBox.Text = text;
            resourceNameTextBox.SelectionStart = Math.Clamp(caretPosition, 0, resourceNameTextBox.TextLength);
            resourceNameTextBox.SelectionLength = 0;
            _isUpdatingResourceNameText = false;
        }

        // When the type needs secondary data, use "<ID>_secondary" next to "<ID>_primary" if it's there
        private void FillDefaultSecondary()
        {
            string primary = Properties.Entries[0].Path;
            if (_imageBody != null || Properties.DataChunkCount < 2
                || string.IsNullOrEmpty(primary) || !string.IsNullOrEmpty(Properties.Entries[1].Path))
                return;

            string secondary = Path.Combine(Path.GetDirectoryName(primary), GetResourceBaseName(primary) + "_secondary" + Path.GetExtension(primary));
            if (File.Exists(secondary))
                Properties.Entries[1].Path = secondary;
        }

        private static string GetResourceBaseName(string path)
        {
            const string primarySuffix = "_primary";
            string name = Path.GetFileNameWithoutExtension(path);

            return name.EndsWith(primarySuffix, StringComparison.OrdinalIgnoreCase)
                ? name[..^primarySuffix.Length]
                : name;
        }

        private static string GetImportsYamlPath(string primaryPath)
        {
            string dir = Path.GetDirectoryName(primaryPath);
            string name = GetResourceBaseName(primaryPath);

            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(name))
                return null;

            return Path.Combine(dir, name + "_imports.yaml");
        }

        private static List<Dependency> LoadDependenciesFromYaml(string yamlPath)
        {
            string[] lines = File.ReadAllLines(yamlPath);
            List<Dependency> list = [];

            foreach (string line in lines)
            {
                string trimmed = line.Trim();

                if (trimmed.Length == 0)
                    continue;

                if (trimmed.StartsWith("#"))
                    continue;

                if (trimmed[0] == '-')
                    trimmed = trimmed.Substring(1).TrimStart();

                int colonIndex = trimmed.IndexOf(':');
                if (colonIndex <= 0)
                    continue;

                string offsetPart = trimmed.Substring(0, colonIndex).Trim();
                string idPart = trimmed.Substring(colonIndex + 1).Trim();

                if (!Utilities.TryParseHexUInt(offsetPart, out uint offset))
                    continue;

                if (!Utilities.TryParseHexUInt(idPart, out uint id))
                    continue;

                Dependency dep = new()
                {
                    EntryPointerOffset = offset,
                    ID = id
                };

                list.Add(dep);
            }

            return list;
        }

        private static byte[] AppendImportTable(byte[] primary, List<Dependency> imports, bool bigEndian)
        {
            MemoryStream ms = new();
            BinaryWriter2 bw = new(ms, bigEndian);
            foreach (Dependency dep in imports)
            {
                bw.Write(dep.ID);
                bw.Write(dep.EntryPointerOffset);
                bw.Write(0u); // Padding
            }
            byte[] table = ms.ToArray();

            // Compare id and offset only, padding isn't always zero on console
            int start = primary.Length - table.Length;
            bool present = start >= 0;
            for (int i = 0; present && i < table.Length; i++)
                present = i % ImportEntrySize >= 12 || primary[start + i] == table[i];
            if (present)
                return primary;

            int tableStart = (primary.Length + 0xF) & ~0xF;
            byte[] result = new byte[tableStart + table.Length];
            Buffer.BlockCopy(primary, 0, result, 0, primary.Length);
            Buffer.BlockCopy(table, 0, result, tableStart, table.Length);
            return result;
        }

        private static List<Dependency> ScanImportTable(byte[] data, bool bigEndian, int pointerSize)
        {
            List<Dependency> imports = [];

            if (data.Length % ImportEntrySize != 0)
                return imports;

            using BinaryReader2 br = new(new MemoryStream(data), bigEndian);

            for (int start = data.Length - ImportEntrySize; start >= 0; start -= ImportEntrySize)
            {
                br.BaseStream.Position = start;
                ulong id = br.ReadUInt64();
                uint offset = br.ReadUInt32();

                if (id == 0 || id > uint.MaxValue || offset % 4 != 0
                    || (imports.Count > 0 && offset >= imports[^1].EntryPointerOffset))
                    break;

                imports.Add(new Dependency { ID = id, EntryPointerOffset = offset });
            }

            imports.Reverse();

            // Drop leading records until the last pointer slot fits before the table
            while (imports.Count > 0
                && imports[^1].EntryPointerOffset + pointerSize > data.Length - imports.Count * ImportEntrySize)
                imports.RemoveAt(0);

            return imports;
        }

        #endregion

        #region Data Chunks

        private static string GetDataChunkName(int index)
        {
            return index >= 0 && index < DataChunkNames.Length
                ? DataChunkNames[index]
                : "additional";
        }

        private static readonly string[] DataChunkNames =
        {
            "primary",
            "secondary",
            "tertiary"
        };

        struct DataChunkImportProperties
        {
            public byte DataChunkCount { get; private set; }
            public DataChunkImportEntry[] Entries;

            public EntryType EntryType
            {
                get => _currentEntryType;
                set
                {
                    _currentEntryType = value;
                    UpdateDataChunkCount();
                    UpdateEntryVisibilities();
                }
            }
            private EntryType _currentEntryType = EntryType.Invalid;

            public DataChunkImportProperties()
            {
                UpdateDataChunkCount();
                Entries = new DataChunkImportEntry[3];
            }

            void UpdateDataChunkCount()
            {
                DataChunkCount = BundleEntry.GetMemoryChunkCount(_currentEntryType);
            }

            void UpdateEntryVisibilities()
            {
                for (byte i = 0; i < Entries.Length; i++)
                    Entries[i].Visible = DataChunkCount > i;
            }
        }

        struct DataChunkImportEntry
        {
            public bool Visible
            {
                get => _visible;
                set
                {
                    _visible = value;
                    if (PathSelectorControls.Count > 0)
                        foreach (var control in PathSelectorControls)
                            control.Visible = value;
                    Path = value == false ? string.Empty : Path;
                }
            }
            private bool _visible;

            public List<Control> PathSelectorControls;
            public Action? PathChanged;
            public string Path
            {
                get => _path;
                set
                {
                    _path = value;
                    PathChanged?.Invoke();
                }
            }
            private string _path;
        }

        #endregion
    }
}
