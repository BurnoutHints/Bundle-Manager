using BundleFormat;
using BurnoutImage;
using BundleUtilities;

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace BundleManager
{
    public partial class ImportResourceForm : Form
    {
        MemoryStream ms;
        DataChunkImportProperties Properties = new();
        public bool AutoUpdateID;
        public BundleArchive ResultArchive { get; private set; }

        private BundleArchive _workingArchive;

        private BundleEntry _newEntry;


        string ImportName
        {
            get => _importName;
            set
            {
                _importName = value;
                resourceNameTextBox.Text = ImportName;
            }
        }
        private string _importName;

        public ImportResourceForm(BundleArchive destination, string primaryDataPath, uint? resourceID = null)
        {
            InitializeComponent();

            _workingArchive = destination;

            if (!Path.Exists(primaryDataPath))
                throw new InvalidDataException("Selected file does not exist or is corrupt!");

            ms = new(File.ReadAllBytes(primaryDataPath));

            ImportName = Path.GetFileNameWithoutExtension(primaryDataPath);

            RegisterDataBlockProperties();
            Properties.Entries[0].Path = primaryDataPath;

            ResolveDependencies();

            Properties.EntryType = InferEntryType(ref ms);

            if (Properties.EntryType == EntryType.Invalid)
                selectTypeLabel.Text = "Unable to auto detect resource type, please select it from the box below:";

            autoUpdateIDCheckBox.Checked = resourceID == null ? true : false;

            resourceTypeComboBox.DataSource = Enum.GetValues(typeof(EntryType));
            resourceTypeComboBox.SelectedIndexChanged += ResourceTypeComboBox_SelectedIndexChanged;
            resourceTypeComboBox.SelectedItem = Properties.EntryType;

            resourceIDTextBox.KeyPress += ResourceIDTextBox_KeyPress;
            resourceIDTextBox.Validating += ResourceIDTextBox_Validating;

        }

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
            }
        }

        private void autoUpdateIDCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            AutoUpdateID = autoUpdateIDCheckBox.Checked;
            resourceIDTextBox.ReadOnly = autoUpdateIDCheckBox.Checked;
            if (AutoUpdateID)
                CalculateResourceIDFromName();
        }

        private void resourceNameTextBox_TextChanged(object sender, EventArgs e)
        {
            ImportName = resourceNameTextBox.Text;

            if (AutoUpdateID)
                CalculateResourceIDFromName();
        }

        void CalculateResourceIDFromName()
        {
            resourceIDTextBox.Text =
                Crc32.HashEntryID
                (
                    // example: bundlemanager://<selected name>.<selected type>
                    $"{resourceNamePrefixLabel.Text}{ImportName.ToLower()}.{Properties.EntryType.ToString()}"
                    .ToLower()
                )
                .ToString("X8");
        }

        void RegisterDataBlockProperties()
        {
            Properties.Entries[1].PathSelectorControl = secondaryDataPathSelectorButton;
            Properties.Entries[2].PathSelectorControl = tertiaryDataPathSelectorButton;

            for (int i = 0; i < Properties.Entries.Length; i++)
            {
                Properties.Entries[i].PathChanged = OnAnyDataBlockPathsUpdated;
            }

            OnAnyDataBlockPathsUpdated();
        }

        void ResolveDependencies()
        {
            byte[] primaryData = ms.ToArray();

            LoadDependencies(
                primaryData,
                _workingArchive.Platform > BundlePlatform.PC,
                Properties.Entries[0].Path,
                out List<Dependency> dependencies,
                out int dependencyBytes
                );

            int newPrimarySize = primaryData.Length - dependencyBytes;
            if (newPrimarySize < 0) newPrimarySize = 0;

            byte[] strippedPrimary = new byte[newPrimarySize];
            if (newPrimarySize > 0)
                Buffer.BlockCopy(primaryData, 0, strippedPrimary, 0, newPrimarySize);

            _newEntry = new(_workingArchive)
            {
                Type = Properties.EntryType
            };

            foreach (Dependency dep in dependencies)
                _newEntry.Dependencies.Add(dep);

            _newEntry.DependencyCount = (short)_newEntry.Dependencies.Count;

            ms.SetLength(strippedPrimary.Length);

            MessageBox.Show($"Resolved {_newEntry.Dependencies.Count} dependencies for the imported resource.");
        }

        EntryType InferEntryType(ref MemoryStream memoryStream)
        {
            long originalPos = memoryStream.Position;
            EntryType type = EntryType.Invalid;

            ms.Seek(0x0, SeekOrigin.Begin);

            if (GameImage.DetectPlatform(ms.ToArray()) is PlatformType p 
                && p != PlatformType.Invalid 
                && _newEntry.Dependencies.Count == 0)
            {
                type = EntryType.Texture;
            }
            else if (ms.Length <= 0x20) // Model
            {
                // 32 bit model
                ms.Seek(0x13, SeekOrigin.Begin);
                byte modelCandidate32 = (byte)ms.ReadByte();

                if (ms.Length > 0x14) // 64 bit model (32 bit is technically padded to nearest 0x10, so this would run anyway)
                {
                    // 64 bit model
                    ms.Seek(0x1F, SeekOrigin.Begin);

                    if (ms.ReadByte() == 0x2)
                        type = EntryType.Model;
                }

                if (modelCandidate32 == 0x2)
                    type = EntryType.Model;
            }
            else
            {
                // renderable
                if (_workingArchive.Console switch
                    {
                        true => Utilities.ReadUInt16BE(ms.ToArray(), 0x10),
                        false => Utilities.ReadUInt16LE(ms.ToArray(), 0x10)
                    } == 11) // mu16VersionNumber
                {
                    type = EntryType.Renderable;
                }
            }

            memoryStream.Position = originalPos;
            return type;
        }

        void OnAnyDataBlockPathsUpdated()
        {
            importPathsLabel.Text =
                $"Primary data path:\r\n{Properties.Entries[0].Path}" +
                (Properties.DataChunkCount > 1 ? $"\r\n\r\nSecondary data path:\r\n{Properties.Entries[1].Path}" : string.Empty) +
                (Properties.DataChunkCount > 2 ? $"\r\n\r\nTertiary data path:\r\n{Properties.Entries[2].Path}" : string.Empty);
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
            RunImportOperation();
        }

        void SelectNewDataChunkDialog(byte index)
        {
            using (OpenFileDialog ofd = new())
            {
                ofd.Title = $"Please select the {GetDataChunkName(index)} resource to import";
                ofd.Filter = "Binary Files|*.dat;*.bin|Image Files(*.dds,*.bmp;*.gif;*.jpg;*.png;*.tif;*.tga;*.webp)|*.dds;*.bmp;*.gif;*.jpg;*.png;*.tif;*.tga;*.webp|All files (*.*)|*.*";

                if (ofd.ShowDialog(this) != DialogResult.OK)
                    return;

                string path = ofd.FileName;

                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return;

                Properties.Entries[index].Path = path;
            }
        }

        private void RunImportOperation()
        {
            for (int i = 1; i < Properties.DataChunkCount; i++)
            {
                if (!File.Exists(Properties.Entries[i].Path))
                {
                    MessageBox.Show($"Please select valid {GetDataChunkName(i)} data for the resource you are trying to import.");
                    return;
                }
            }

            byte[][] dataBlocks = new byte[Properties.DataChunkCount][];

            dataBlocks[0] = ms.ToArray();
            if (Properties.DataChunkCount > 1)
            {
                for (int i = 1; i < Properties.DataChunkCount; i++)
                {
                    dataBlocks[i] = File.ReadAllBytes(Properties.Entries[i].Path);
                }
            }

            EntryBlock block1 = new()
            {
                Compressed = false,
                Data = dataBlocks[0]
            };

            EntryBlock block2 = new()
            {
                Compressed = false
            };

            EntryBlock block3 = new()
            {
                Compressed = false
            };

            if (Properties.DataChunkCount > 1)
            {
                if (Properties.EntryType == EntryType.Texture)
                    block2.Compressed = true;

                block2.Data = dataBlocks[1];
            }
            
            _newEntry.EntryBlocks =
            [
                block1,
                block2,
                block3
            ];
            
            _newEntry.Dirty = true;
            
            _newEntry.ID = Convert.ToUInt32(resourceIDTextBox.Text, 16);
            _newEntry.Index = _workingArchive.Entries.Count;
            _newEntry.DebugInfo = new()
            {
                Name = $"{resourceNamePrefixLabel.Text}{ImportName}.{Properties.EntryType.ToString()}",
                TypeName = Properties.EntryType.ToString()
            };

            _workingArchive.Entries.Add(_newEntry);

            ResultArchive = _workingArchive;
            DialogResult = DialogResult.OK;
            Close();
        }

        #region Dependency Handling

        private static void LoadDependencies(
            byte[] primaryData,
            bool isBigEndian,
            string primaryPath,
            out List<Dependency> dependencies,
            out int dependencyBytes)
        {
            string yamlPath = GetImportsYamlPath(primaryPath);

            if (yamlPath != null && File.Exists(yamlPath))
            {
                dependencies = LoadDependenciesFromYaml(yamlPath);
                dependencyBytes = dependencies.Count * 0x10;
            }
            else
            {
                ScanBinaryDependencies(primaryData, isBigEndian, out dependencies, out dependencyBytes);
            }
        }

        private static string GetImportsYamlPath(string primaryPath)
        {
            string dir = Path.GetDirectoryName(primaryPath);
            string name = Path.GetFileNameWithoutExtension(primaryPath);
            const string primarySuffix = "_primary";

            if (name.EndsWith(primarySuffix, StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - primarySuffix.Length);

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

        private static void ScanBinaryDependencies(
            byte[] primaryData,
            bool isBigEndian,
            out List<Dependency> dependencies,
            out int dependencyBytes)
        {
            dependencies = [];
            dependencyBytes = 0;

            if (primaryData.Length < 0x10)
                return;

            int pos = primaryData.Length - 0x10;

            while (pos >= 0)
            {
                if (!TryReadDependencyEntry(primaryData, pos, isBigEndian, out uint id, out uint location))
                    break;

                Dependency dep = new()
                {
                    ID = id,
                    EntryPointerOffset = location
                };

                dependencies.Add(dep);
                dependencyBytes += 0x10;
                pos -= 0x10;
            }

            if (dependencies.Count > 0)
                dependencies.Reverse();
        }

        private static bool TryReadDependencyEntry(
            byte[] data,
            int offset,
            bool isBigEndian,
            out uint id,
            out uint location)
        {
            id = 0;
            location = 0;

            if (offset < 0 || offset + 0x10 > data.Length)
                return false;

            if (isBigEndian)
            {
                if (data[offset + 0] != 0 || data[offset + 1] != 0 ||
                    data[offset + 2] != 0 || data[offset + 3] != 0)
                    return false;

                if (data[offset + 8] != 0 || data[offset + 9] != 0 ||
                    data[offset + 10] != 0 || data[offset + 11] != 0)
                    return false;

                id = Utilities.ReadUInt32BE(data, offset + 4);
                location = Utilities.ReadUInt32BE(data, offset + 12);
            }
            else
            {
                if (data[offset + 4] != 0 || data[offset + 5] != 0 ||
                    data[offset + 6] != 0 || data[offset + 7] != 0)
                    return false;

                if (data[offset + 12] != 0 || data[offset + 13] != 0 ||
                    data[offset + 14] != 0 || data[offset + 15] != 0)
                    return false;

                id = Utilities.ReadUInt32LE(data, offset + 0);
                location = Utilities.ReadUInt32LE(data, offset + 8);
            }

            if (id == 0)
                return false;

            return true;
        }

        #endregion

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
                    if (PathSelectorControl != null)
                        PathSelectorControl.Visible = value;
                    Path = value == false ? string.Empty : Path;
                }
            }
            private bool _visible;

            public Control? PathSelectorControl;
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
    }
}
