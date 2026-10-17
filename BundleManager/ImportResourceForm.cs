using BundleFormat;
using BundleUtilities;
using BurnoutImage;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BundleManager
{
    public partial class ImportResourceForm : Form
    {
        private const string ResourceNamePrefix = "bundlemanager://";

        private const string BinaryFilter = "Binary Files|*.dat;*.bin|All files (*.*)|*.*";
        private const string PrimaryFileFilter = "Binary Files|*.dat;*.bin|" + GameImage.FileFilter + "|All files (*.*)|*.*";
        private static readonly string[] ImageExtensions = [".dds", ".bmp", ".gif", ".jpg", ".jpeg", ".png", ".tif", ".tiff", ".tga", ".webp"];

        private readonly BundleArchive _workingArchive;

        private string _primaryPath;
        private byte[] _primaryData;
        private List<Dependency> _imports = [];
        private byte[] _imageBody;

        private EntryType SelectedType => resourceTypeComboBox.SelectedItem is EntryType type ? type : EntryType.Invalid;

        public ImportResourceForm(BundleArchive destination, string primaryDataPath)
        {
            InitializeComponent();

            _workingArchive = destination;

            resourceTypeComboBox.DataSource = Enum.GetValues(typeof(EntryType));
            resourceTypeComboBox.SelectedIndexChanged += (_, _) => OnTypeChanged();
            primaryResourcePathTextBox.Validated += primaryResourcePathTextBox_Validated;
            importCountUpDown.ValueChanged += (_, _) => RefreshImportsList();

            LoadPrimary(primaryDataPath);
        }

        // Asks for the primary data, then shows the dialog. Returns whether a resource was imported.
        public static bool ShowImport(IWin32Window owner, BundleArchive destination)
        {
            if (PickFile(owner, "primary", PrimaryFileFilter) is not string path)
                return false;

            ImportResourceForm form;
            try
            {
                form = new(destination, path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, $"Unable to import {path}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            using (form)
                return form.ShowDialog(owner) == DialogResult.OK;
        }

        #region Event Handlers

        private void autoUpdateIDCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            resourceIDTextBox.ReadOnly = autoUpdateIDCheckBox.Checked;

            if (autoUpdateIDCheckBox.Checked)
                RecalculateResourceIDTextBox();
        }

        private void resourceNameTextBox_TextChanged(object sender, EventArgs e)
        {
            if (autoUpdateIDCheckBox.Checked)
                RecalculateResourceIDTextBox();
        }

        private void primaryResourcePathTextBox_Validated(object sender, EventArgs e)
        {
            if (primaryResourcePathTextBox.Text != _primaryPath)
                TryLoadPrimary(primaryResourcePathTextBox.Text);
        }

        private void primaryDataPathSelectorButton_Click(object sender, EventArgs e)
        {
            if (PickFile(this, "primary", PrimaryFileFilter) is string path)
                TryLoadPrimary(path);
        }

        private void secondaryDataPathSelectorButton_Click(object sender, EventArgs e)
        {
            if (PickFile(this, "secondary", BinaryFilter) is string path)
                secondaryResourcePathTextBox.Text = path;
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

                type = InferEntryType(primary[..^(imports.Count * Dependency.Size)], imports.Count);
            }

            _primaryPath = path;
            _primaryData = primary;
            _imageBody = imageBody;
            _imports = imports;

            primaryResourcePathTextBox.Text = path;
            secondaryResourcePathTextBox.Text = string.Empty; // Belonged to the previous primary
            resourceNameTextBox.Text = ResourceNamePrefix + GetResourceBaseName(path);
            resourceNameTextBox.SelectionStart = resourceNameTextBox.TextLength;

            resourceTypeComboBox.SelectedItem = type;
            resourceTypeComboBox.Enabled = imageBody == null;
            OnTypeChanged();

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

            // Exports are named after the resource ID, e.g. 0A7B302C_primary.dat
            Match idMatch = ResourceIDPattern().Match(Path.GetFileNameWithoutExtension(path));
            autoUpdateIDCheckBox.Checked = !idMatch.Success;
            if (idMatch.Success)
                resourceIDTextBox.Text = idMatch.Value.ToUpperInvariant();
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
                primaryResourcePathTextBox.Text = _primaryPath; // Put the old path back in the text box
            }
        }

        private void OnTypeChanged()
        {
            bool needsSecondary = _imageBody == null && BundleEntry.GetMemoryChunkCount(SelectedType) > 1;
            secondaryDataPathSelectorButton.Visible = secondaryResourcePathTextBox.Visible = secondaryImportPathLabel.Visible = needsSecondary;

            string secondary = Path.Combine(Path.GetDirectoryName(_primaryPath), GetResourceBaseName(_primaryPath) + "_secondary" + Path.GetExtension(_primaryPath));
            
            if (needsSecondary && secondaryResourcePathTextBox.Text.Length == 0 && File.Exists(secondary))
                secondaryResourcePathTextBox.Text = secondary;

            if (autoUpdateIDCheckBox.Checked)
                RecalculateResourceIDTextBox();
        }

        private static PlatformType? TexturePlatform(BundlePlatform platform) => platform switch
        {
            BundlePlatform.PC => PlatformType.PC,
            BundlePlatform.PCx64 => PlatformType.PCx64,
            BundlePlatform.X360 => PlatformType.X360,
            BundlePlatform.PS3 => PlatformType.PS3,
            _ => null
        };

        private static bool TextureMatchesBundle(PlatformType texture, BundlePlatform bundle) =>
            texture == TexturePlatform(bundle) || (texture == PlatformType.Remastered && bundle == BundlePlatform.PC);

        PlatformType? GetImageTexturePlatform()
        {
            BundleEntry sample = _workingArchive.Entries.FirstOrDefault(e => e.Type == EntryType.Texture && e.HasHeader);
            PlatformType? platform = sample != null
                ? GameImage.DetectPlatform(sample.EntryBlocks[0].Data)
                : _workingArchive.Platform == BundlePlatform.PC
                    ? AskPCTexturePlatform()
                    : TexturePlatform(_workingArchive.Platform);

            // GameImage.SetImage can only write these
            return platform is PlatformType.PC or PlatformType.PCx64 or PlatformType.Remastered ? platform : null;
        }

        private static PlatformType AskPCTexturePlatform()
        {
            TaskDialogButton original = new ("PC (2009, The Ultimate Box)");
            TaskDialogButton remastered = new("Remastered");

            TaskDialogPage page = new()
            {
                Caption = "Import Resource into Bundle",
                Heading = "Which version of Burnout Paradise is this bundle for?",
                Text = "Bundle Manager was unable to infer the platform for this bundle. Please select the version of Burnout Paradise that this bundle is for.\n This will determine how this texture and others are imported.",
                Buttons = { original, remastered }
            };

            return TaskDialog.ShowDialog(page) == remastered ? PlatformType.Remastered : PlatformType.PC;
        }

        // This really needs to be put in a better spot
        EntryType InferEntryType(byte[] data, int importCount)
        {
            bool Has(ReadOnlySpan<byte> magic) => data.AsSpan().IndexOf(magic) >= 0;

            // AttribSysVault
            if (_workingArchive.Console
                ? Has("StrN"u8) && Has("Vers"u8)
                : Has("NrtS"u8) && Has("sreV"u8))
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
                ReadOnlySpan<byte> version = data.AsSpan(0x10); // mu16VersionNumber
                if ((_workingArchive.Console ? BinaryPrimitives.ReadUInt16BigEndian(version) : BinaryPrimitives.ReadUInt16LittleEndian(version)) == 11)
                    type = EntryType.Renderable;
            }

            return type;
        }

        private void RecalculateResourceIDTextBox()
        {
            resourceIDTextBox.Text = Crc32.HashEntryID($"{resourceNameTextBox.Text}.{SelectedType}").ToString("X8");
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

        private static string PickFile(IWin32Window owner, string chunkName, string filter)
        {
            using OpenFileDialog ofd = new()
            {
                Title = $"Please select the {chunkName} resource to import",
                Filter = filter
            };

            return ofd.ShowDialog(owner) == DialogResult.OK ? ofd.FileName : null;
        }

        private void RunImportOperation()
        {
            EntryType type = SelectedType;
            string name = resourceNameTextBox.Text;
            if (name.StartsWith(ResourceNamePrefix, StringComparison.OrdinalIgnoreCase))
                name = name[ResourceNamePrefix.Length..];

            if (string.IsNullOrWhiteSpace(name))
            {
                Warn("Please enter a name for the resource.");
                return;
            }

            if (type == EntryType.Invalid)
            {
                Warn("Please select the resource type to import as.");
                return;
            }

            if (!TryParseHex(resourceIDTextBox.Text, out uint id))
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
                if (BundleEntry.GetMemoryChunkCount(type) > 1)
                {
                    if (!File.Exists(secondaryResourcePathTextBox.Text))
                    {
                        Warn("Please select valid secondary data for the resource you are trying to import.");
                        return;
                    }

                    dataBlocks[secondaryBlock] = File.ReadAllBytes(secondaryResourcePathTextBox.Text);
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
                DependenciesListOffset = importCount > 0 ? _primaryData.Length - importCount * Dependency.Size : 0,
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

            _workingArchive.AddEntry(entry);

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

        [GeneratedRegex(@"^[0-9A-Fa-f]{8}(?![0-9A-Fa-f])")]
        private static partial Regex ResourceIDPattern();

        private static bool TryParseHex(string s, out uint value)
        {
            return uint.TryParse(s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? s[2..] : s,
                NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
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
            List<Dependency> list = [];

            foreach (string line in File.ReadLines(yamlPath))
            {
                string[] parts = line.Trim().TrimStart('-').Split(':');
                if (parts.Length == 2 && TryParseHex(parts[0].Trim(), out uint offset) && TryParseHex(parts[1].Trim(), out uint id))
                    list.Add(new Dependency { EntryPointerOffset = offset, ID = id });
            }

            return list;
        }

        private static byte[] AppendImportTable(byte[] primary, List<Dependency> imports, bool bigEndian)
        {
            MemoryStream ms = new();
            BinaryWriter2 bw = new(ms, bigEndian);
            foreach (Dependency dep in imports)
                dep.Write(bw);
            byte[] table = ms.ToArray();

            // Compare id and offset only, padding isn't always zero on console
            int start = primary.Length - table.Length;
            bool present = start >= 0;
            for (int i = 0; present && i < table.Length; i++)
                present = i % Dependency.Size >= 12 || primary[start + i] == table[i];
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

            if (data.Length % Dependency.Size != 0)
                return imports;

            using BinaryReader2 br = new(new MemoryStream(data), bigEndian);

            for (int start = data.Length - Dependency.Size; start >= 0; start -= Dependency.Size)
            {
                br.BaseStream.Position = start;
                Dependency dep = Dependency.Read(br);

                if (dep.ID == 0 || dep.ID > uint.MaxValue || dep.EntryPointerOffset % 4 != 0
                    || (imports.Count > 0 && dep.EntryPointerOffset >= imports[^1].EntryPointerOffset))
                    break;

                imports.Add(dep);
            }

            imports.Reverse();

            // Drop leading records until the last pointer slot fits before the table
            while (imports.Count > 0
                && imports[^1].EntryPointerOffset + pointerSize > data.Length - imports.Count * Dependency.Size)
                imports.RemoveAt(0);

            return imports;
        }

        #endregion
    }
}
