using BundleFormat;
using BundleUtilities;
using BurnoutImage;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BundleManager
{
    public partial class ImportResourceForm : Form
    {
        private const string ResourceNamePrefix = "bundlemanager://";

        private const string BinaryFilter = "Binary Files|*.dat;*.bin|All files (*.*)|*.*";
        private const string PrimaryFileFilter = "Binary Files|*.dat;*.bin|" + GameImage.FileFilter + "|All files (*.*)|*.*";

        private readonly BundleArchive _workingArchive;

        private string _primaryPath;
        private PrimaryResource _primary;

        private EntryType SelectedType => resourceTypeComboBox.SelectedItem is EntryType type ? type : EntryType.Invalid;
        private string FullName => $"{resourceNameTextBox.Text}.{SelectedType}";

        public ImportResourceForm(BundleArchive destination, string primaryDataPath)
        {
            InitializeComponent();

            _workingArchive = destination;

            resourceTypeComboBox.DataSource = Enum.GetValues(typeof(EntryType));
            resourceTypeComboBox.SelectedIndexChanged += (_, _) => OnTypeChanged();
            primaryResourcePathTextBox.Validating += primaryResourcePathTextBox_Validating;
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

        // Validating instead of Validated so a bad path also cancels the click that moved focus away
        private void primaryResourcePathTextBox_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (primaryResourcePathTextBox.Text != _primaryPath)
                e.Cancel = !TryLoadPrimary(primaryResourcePathTextBox.Text);
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
            _primary = ResourceImport.LoadPrimary(path, _workingArchive, AskPCTexturePlatform);
            _primaryPath = path;

            primaryResourcePathTextBox.Text = path;
            secondaryResourcePathTextBox.Text = string.Empty; // Belonged to the previous primary
            resourceNameTextBox.Text = ResourceNamePrefix + ResourceImport.GetResourceBaseName(path);
            resourceNameTextBox.SelectionStart = resourceNameTextBox.TextLength;

            resourceTypeComboBox.SelectedItem = _primary.Type;
            resourceTypeComboBox.Enabled = _primary.ImageBody == null;
            OnTypeChanged();

            selectTypeLabel.Text = _primary.Type == EntryType.Invalid
                ? "Unable to auto detect resource type, please select it from the box below:"
                : "Select the resource type to import as:";

            int importCount = _primary.Imports.Count;
            importCountUpDown.Value = 0;
            importCountUpDown.Maximum = importCount;
            importCountUpDown.Value = importCount;
            importCountUpDown.Enabled = _primary.ImportsSource == null && importCount > 0;
            importsLabel.Text = _primary.ImageBody != null ? "Imports: none (texture built from the image)"
                : _primary.ImportsSource != null ? $"Imports from {_primary.ImportsSource}:"
                : importCount > 0 ? "Imports detected at the end of the primary data. Lower the count if the first rows are really resource data:"
                : "Imports: none detected in the primary data";
            RefreshImportsList();

            uint? fileID = ResourceImport.GetIDFromFileName(path);
            autoUpdateIDCheckBox.Checked = fileID == null;
            if (fileID is uint id)
                resourceIDTextBox.Text = id.ToString("X8");
        }

        private bool TryLoadPrimary(string path)
        {
            try
            {
                LoadPrimary(path);
                return true;
            }
            catch (Exception ex)
            {
                Warn($"Unable to use {path}: {ex.Message}");
                primaryResourcePathTextBox.Text = _primaryPath; // Put the old path back in the text box
                return false;
            }
        }

        private void OnTypeChanged()
        {
            bool needsSecondary = _primary.ImageBody == null && BundleEntry.GetMemoryChunkCount(SelectedType) > 1;
            secondaryDataPathSelectorButton.Visible = secondaryResourcePathTextBox.Visible = secondaryImportPathLabel.Visible = needsSecondary;

            if (needsSecondary && secondaryResourcePathTextBox.Text.Length == 0 && ResourceImport.FindSecondary(_primaryPath) is string secondary)
                secondaryResourcePathTextBox.Text = secondary;

            if (autoUpdateIDCheckBox.Checked)
                RecalculateResourceIDTextBox();
        }

        private static PlatformType AskPCTexturePlatform()
        {
            TaskDialogButton original = new ("PC (2009, The Ultimate Box)");
            TaskDialogButton remastered = new("Remastered");

            TaskDialogPage page = new()
            {
                Caption = "Import Resource into Bundle",
                Heading = "Which version of Burnout Paradise is this bundle for?",
                Text = "Bundle Manager was unable to infer the platform for this bundle. Please select the version of Burnout Paradise that this bundle is for. This will determine how this texture and others are imported.",
                Buttons = { original, remastered }
            };

            return TaskDialog.ShowDialog(page) == remastered ? PlatformType.Remastered : PlatformType.PC;
        }

        private void RecalculateResourceIDTextBox()
        {
            resourceIDTextBox.Text = Crc32.HashEntryID(FullName).ToString("X8");
        }

        private void RefreshImportsList()
        {
            int count = (int)importCountUpDown.Value;

            importsListBox.BeginUpdate();
            importsListBox.Items.Clear();
            foreach (Dependency dep in _primary.Imports.Skip(_primary.Imports.Count - count))
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

            if (!ResourceImport.TryParseHex(resourceIDTextBox.Text, out uint id))
            {
                Warn("Please enter a valid hexadecimal resource ID.");
                return;
            }

            byte[] secondary = _primary.ImageBody;
            if (secondary == null)
            {
                if (BundleEntry.GetMemoryChunkCount(type) > 1)
                {
                    if (!File.Exists(secondaryResourcePathTextBox.Text))
                    {
                        Warn("Please select valid secondary data for the resource you are trying to import.");
                        return;
                    }

                    secondary = File.ReadAllBytes(secondaryResourcePathTextBox.Text);
                }

                if (type == EntryType.Texture
                    && !ResourceImport.TextureMatchesBundle(_primary.Data, _workingArchive.Platform)
                    && !Confirm($"This texture header doesn't look like a {_workingArchive.Platform} texture.", "Import anyway"))
                    return;
            }

            BundleEntry existing = _workingArchive.GetEntryByID(id);
            if (existing != null && !Confirm($"This bundle already has a resource with ID {id:X8} ({existing.Type}). Replace it?", "Replace"))
                return;

            _workingArchive.AddEntry(_workingArchive.CreateEntry(id, type, FullName, _primary.Data, (int)importCountUpDown.Value, secondary));

            DialogResult = DialogResult.OK;
            Close();
        }

        private void Warn(string message)
        {
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private bool Confirm(string message, string action)
        {
            TaskDialogButton confirm = new(action);

            TaskDialogPage page = new()
            {
                Caption = Text,
                Text = message,
                Icon = TaskDialogIcon.Warning,
                Buttons = { confirm, TaskDialogButton.Cancel },
                AllowCancel = true
            };

            return TaskDialog.ShowDialog(this, page) == confirm;
        }
    }
}
