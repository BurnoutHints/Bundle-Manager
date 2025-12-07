using BundleFormat;
using BurnoutImage;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BundleManager
{
    public partial class ImportResourceForm : Form
    {
        MemoryStream ms;
        DataBlockImportProperties Properties = new();
        public bool AutoUpdateID;

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

        public ImportResourceForm(string primaryDataPath, uint? resourceID = null)
        {
            InitializeComponent();

            if (!Path.Exists(primaryDataPath))
                throw new InvalidDataException("Selected file does not exist or is corrupt!");

            ms = new(File.ReadAllBytes(primaryDataPath));

            ImportName = Path.GetFileNameWithoutExtension(primaryDataPath);

            RegisterDataBlockProperties();
            Properties.Entries[0].Path = primaryDataPath;
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
            if (!IsValidHex(resourceIDTextBox.Text))
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
                BundleUtilities.Crc32.HashEntryID
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

        EntryType InferEntryType(ref MemoryStream memoryStream)
        {
            long originalPos = memoryStream.Position;
            EntryType type = EntryType.Invalid;

            ms.Seek(0x0, SeekOrigin.Begin);

            if (GameImage.DetectPlatform(ms.ToArray()) is PlatformType p && p != PlatformType.Invalid)
            {
                type = EntryType.Texture;
            }

            memoryStream.Position = originalPos;
            return type;
        }

        void OnAnyDataBlockPathsUpdated()
        {
            importPathsLabel.Text =
                $"Primary data path:\r\n{Properties.Entries[0].Path}" +
                (Properties.DataBlockCount > 1 ? $"\r\n\r\nSecondary data path:\r\n{Properties.Entries[1].Path}" : string.Empty) +
                (Properties.DataBlockCount > 2 ? $"\r\n\r\nTertiary data path:\r\n{Properties.Entries[2].Path}" : string.Empty);
        }

        private static bool IsValidHex(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return false;

            foreach (char c in s)
                if (!Uri.IsHexDigit(c))
                    return false;

            return true;
        }

        private void secondaryDataPathSelectorButton_Click(object sender, EventArgs e)
        {
            SelectNewDataBlockDialog(1);
        }

        private void tertiaryDataPathSelectorButton_Click(object sender, EventArgs e)
        {
            SelectNewDataBlockDialog(2);
        }

        void SelectNewDataBlockDialog(byte index)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = $"Please select the {GetDataBlockName(index)} resource to import";
                ofd.Filter = "Binary Files|*.dat;*.bin|Image Files(*.dds,*.bmp;*.gif;*.jpg;*.png;*.tif;*.tga;*.webp)|*.dds;*.bmp;*.gif;*.jpg;*.png;*.tif;*.tga;*.webp|All files (*.*)|*.*";

                if (ofd.ShowDialog(this) != DialogResult.OK)
                    return;

                var path = ofd.FileName;

                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return;

                Properties.Entries[index].Path = path;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            for (int i = 1; i < Properties.DataBlockCount; i++)
            {
                if (!File.Exists(Properties.Entries[i].Path))
                {
                    MessageBox.Show($"Please select valid {GetDataBlockName(i)} data for the resource you are trying to import.");
                    return;
                }
            }

            byte[][] dataBlocks = new byte[Properties.DataBlockCount][];

            for (int i = 0; i < Properties.DataBlockCount; i++)
            {
                dataBlocks[i] = File.ReadAllBytes(Properties.Entries[i].Path);
            }

            
        }

        private static string GetDataBlockName(int index)
        {
            return index >= 0 && index < DataBlockNames.Length
                ? DataBlockNames[index]
                : "additional";
        }

        private static readonly string[] DataBlockNames =
        {
            "primary",
            "secondary",
            "tertiary"
        };

        struct DataBlockImportProperties
        {
            public byte DataBlockCount { get; private set; }
            public DataBlockImportEntry[] Entries;

            public EntryType EntryType
            {
                get => _currentEntryType;
                set
                {
                    _currentEntryType = value;
                    UpdateDataBlockCount();
                    UpdateEntryVisibilities();
                }
            }
            private EntryType _currentEntryType = EntryType.Invalid;

            public DataBlockImportProperties()
            {
                UpdateDataBlockCount();
                Entries = new DataBlockImportEntry[3];
            }

            void UpdateDataBlockCount()
            {
                DataBlockCount = _currentEntryType switch
                {
                    EntryType.AptData => 1,
                    EntryType.Texture => 2,
                    EntryType.Renderable => 3,
                    _ => 1,
                };
            }

            void UpdateEntryVisibilities()
            {
                for (byte i = 0; i < Entries.Length; i++)
                    Entries[i].Visible = DataBlockCount > i;
            }
        }

        struct DataBlockImportEntry
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
