using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BundleFormat;
using BundleUtilities;
using DebugHelper;
using PluginAPI;

namespace BundleManager;

public partial class MainForm : Form
{
    #region Variables and Properties

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool SubForm
    {
        get;
        set
        {
            field = value;

            tsbNew.Visible = !value;
            toolStripSeparator1.Visible = !value;
            tsbOpen.Visible = !value;
            toolStripSeparator2.Visible = !value;
            toolStripSeparator3.Visible = !value;
            tsbSwitchMode.Visible = !value;

            newToolStripMenuItem.Visible = !value;
            toolStripMenuItem3.Visible = !value;
            openToolStripMenuItem.Visible = !value;
            toolStripMenuItem1.Visible = !value;

            exitToolStripMenuItem.Visible = !value;
            closeToolStripMenuItem.Visible = value;
        }
    }

    // Only touched on the UI thread: loading and saving hand their work to LoadingDialog.RunAsync
    private BundleArchive CurrentArchive { get; set; }

    private string CurrentFileName { get; set; }

    public bool _console => CurrentArchive.Console;

    public bool ForceOnlySpecificEntry = false;

    private readonly Lock _saveLock = new();

    private const int NameColumn = 1;
    private readonly Font _inferredNameFont;

    #endregion

    public MainForm()
    {
        InitializeComponent();
        _inferredNameFont = new Font(lstEntries.Font, FontStyle.Italic);

        //doNew();
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        lstEntries.Items.Clear();

        UpdatePluginMenu();
        addResourcesToBundleToolStripMenuItem.Enabled = CurrentArchive != null;
        fillMissingNamesToolStripMenuItem.Enabled = CurrentArchive != null;

        if (CurrentArchive == null)
        {
            lstEntries.Enabled = false;
            return;
        }
        else
        {
            lstEntries.Enabled = true;
        }

        lstEntries.BeginUpdate();
        IComparer sorter = lstEntries.ListViewItemSorter ?? new EntrySorter(0);
        lstEntries.ListViewItemSorter = null; // Disable sorting while adding

        foreach (BundleEntry entry in CurrentArchive.Entries)
        {
            byte[] primary = entry.EntryBlocks[0].Data ?? [];
            string[] values =
            [
                entry.Index.ToString("d3"),
                "", // Set by ShowName
                "0x" + entry.ID.ToString("X8"),
                entry.Type.ToString(),
                primary.Length.ToString(),
                primary.MakePreview(0, 16)
            ];

            // Sub item styles so the name alone can be italic
            ListViewItem item = new(values) { UseItemStyleForSubItems = false };
            Color color = BundleEntry.GetColor(entry.Type);
            foreach (ListViewItem.ListViewSubItem subItem in item.SubItems)
            {
                subItem.BackColor = color;
                if (color != Color.Transparent)
                    subItem.ForeColor = Color.Black; // Readable on the light type colours in dark mode too
            }
            ShowName(item, entry);
            lstEntries.Items.Add(item);
        }

        lstEntries.ListViewItemSorter = sorter; // Also calls Sort, keeping the column chosen before
        lstEntries.EndUpdate();
    }

    private void UpdatePluginMenu()
    {
        int index = toolsToolStripMenuItem.DropDownItems.IndexOf(pluginToolsSeparatorItem) + 1;

        while (true)
        {
            if (index >= toolsToolStripMenuItem.DropDownItems.Count)
                break;

            if (toolsToolStripMenuItem.DropDownItems[index] is ToolStripSeparator)
                break;

            toolsToolStripMenuItem.DropDownItems.RemoveAt(index);
        }

        PluginCommand[] commands = PluginCommandRegistry.Commands;

        if (commands.Length == 0)
        {
            ToolStripItem item = new ToolStripMenuItem("No Plugin Commands");
            item.Enabled = false;

            toolsToolStripMenuItem.DropDownItems.Insert(index, item);
        }
        else
        {
            for (int i = 0; i < commands.Length; i++)
            {
                PluginCommand command = commands[i];

                ToolStripItem item = new ToolStripMenuItem(command.Text);
                if (CurrentArchive == null)
                    item.Enabled = false;
                else if (command.CheckConditions == null)
                    item.Enabled = true;
                else
                    item.Enabled = command.CheckConditions(CurrentArchive);

                item.Click += (sender, args) => command.Use(this, CurrentArchive);

                toolsToolStripMenuItem.DropDownItems.Insert(index + i, item);
            }
        }
    }

    private async Task DoNew()
    {
        if (!await CheckSave() || AskNewBundle() is not (BundlePlatform platform, bool import))
            return;

        // Retail bundles are compressed, the decomp's x64 ones aren't
        CurrentArchive = new BundleArchive
        {
            BNDVersion = 2,
            Version = 2,
            Platform = platform,
            Flags = Flags.UnusedFlag1 | Flags.UnusedFlag2 | (platform == BundlePlatform.PCx64 ? 0 : Flags.Compressed)
        };
        CurrentFileName = null;
        Text = "Bundle Manager - Untitled";

        if (import)
            ImportResourceForm.ShowImport(this, CurrentArchive);
        UpdateDisplay();
    }

    // Asks the platform, then whether to import a first resource. Null when cancelled
    private (BundlePlatform Platform, bool Import)? AskNewBundle()
    {
        TaskDialogCommandLinkButton import = new("Import a resource", "Choose the first resource to add now.");
        TaskDialogCommandLinkButton empty = new("Keep it empty", "Add resources later from Tools > Add resource(s) to bundle.");
        TaskDialogPage contents = new()
        {
            Caption = "New Bundle",
            Heading = "Add a first resource?",
            Buttons = { import, empty, TaskDialogButton.Cancel },
            AllowCancel = true
        };

        TaskDialogPage platforms = new()
        {
            Caption = "New Bundle",
            Heading = "Which platform is this bundle for?",
            AllowCancel = true
        };
        BundlePlatform platform = default;
        foreach (var (value, text, note) in new (BundlePlatform, string, string)[]
        {
            (BundlePlatform.PC, "PC", "Burnout Paradise Remastered, or The Ultimate Box (2009)"),
            (BundlePlatform.X360, "Xbox 360", null),
            (BundlePlatform.PS3, "PlayStation 3", null),
            (BundlePlatform.PCx64, "PC x64", "Builds of the Burnout Paradise decompilation")
        })
        {
            TaskDialogCommandLinkButton button = new(text, note) { AllowCloseDialog = false };
            button.Click += (_, _) =>
            {
                platform = value;
                contents.Text = $"The new bundle is for {text}.";
                platforms.Navigate(contents);
            };
            platforms.Buttons.Add(button);
        }
        platforms.Buttons.Add(TaskDialogButton.Cancel);

        TaskDialogButton result = TaskDialog.ShowDialog(this, platforms);
        return result == import ? (platform, true) : result == empty ? (platform, false) : null;
    }

    private async Task Open()
    {
        if (!await CheckSave())
            return;
        OpenFileDialog ofd = new();
        ofd.Filter = "Supported Files|*.BUNDLE;*.BIN;*.BNDL;*.DAT;*.TEX|All Files|*.*";
        DialogResult result = ofd.ShowDialog(this);
        if (result == DialogResult.OK)
        {
            if (!Utilities.FileExists(ofd.FileName))
                return;

            await Open(ofd.FileName);
        }
    }

    public async Task Open(string path)
    {
        try
        {
            CurrentArchive = await LoadingDialog.RunAsync(this, "Loading: " + path, (_, _) => BundleArchive.Open(path));
            CurrentFileName = path;
            Text = "Bundle Manager - " + CurrentFileName;
        }
        catch (OperationCanceledException)
        {
            CurrentArchive = null;
            CurrentFileName = null;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"There was an error opening {path}:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            CurrentArchive = null;
            CurrentFileName = null;
            Text = "Bundle Manager";
        }
        UpdateDisplay();
    }

    public async Task<bool> Save()
    {
        if (CurrentArchive == null)
        {
            MessageBox.Show(this, "Nothing to save!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return true;
        }
        if (string.IsNullOrEmpty(CurrentFileName))
        {
            return await SaveAs();
        }

        return await SaveTo(CurrentFileName);
    }

    public async Task<bool> SaveAs()
    {
        if (CurrentArchive == null)
        {
            MessageBox.Show(this, "Nothing to save!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return true;
        }
        SaveFileDialog sfd = new();
        sfd.Filter = "Supported Files|*.BUNDLE;*.BIN;*.BNDL;*.DAT;*.TEX|All Files|*.*";
        DialogResult result = sfd.ShowDialog(this);
        if (result == DialogResult.OK)
        {
            if (!Utilities.IsValidPath(sfd.FileName) || !await SaveTo(sfd.FileName))
                return false;

            CurrentFileName = sfd.FileName;
            Text = "Bundle Manager - " + CurrentFileName;

            return true;
        }
        return false;
    }

    // True only once the bundle is written. Cancelling, or an error, leaves the file as it was
    private async Task<bool> SaveTo(string path)
    {
        BundleArchive archive = CurrentArchive;
        try
        {
            await LoadingDialog.RunAsync(this, "Saving: " + path, (_, cancel) =>
            {
                lock (_saveLock)
                    archive.Write(path, cancel);
                return true;
            });
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"{path} wasn't saved, and is unchanged:\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        archive.Dirty = false;
        return true;
    }

    public void Exit()
    {
        Application.Exit();
    }

    public BundleEntry GetEntry(int index) => CurrentArchive.Entries[index];

    public async Task EditSelectedEntry(bool forceHex = false, bool forceDebug = false)
    {
        int count = lstEntries.SelectedIndices.Count;
        if (count <= 0)
            return;

        if (!int.TryParse(lstEntries.SelectedItems[0].Text, out int index))
            return;

        await EditEntry(index, forceHex, forceDebug);
    }

    public async Task EditEntry(int index, bool forceHex = false, bool forceDebug = false)
    {
        if (CurrentArchive == null || index < 0 || index >= CurrentArchive.Entries.Count) // From the command line
            return;
        BundleEntry entry = GetEntry(index);

        if (!EntryTypeRegistry.IsRegistered(entry.Type) || forceHex)
        {
            using EntryEditor editor = new() { ForceHex = forceHex };
            editor.Entry = entry;
            editor.ShowDialog(this);
            return;
        }

        IEntryData data = EntryTypeRegistry.GetHandler(entry.Type);
        TextureCache.ResetCache();
        try
        {
            bool loaded;
            try
            {
                loaded = await LoadingDialog.RunAsync(this, "Loading: " + entry.ID.ToString("X8"), (loader, _) => data.Read(entry, loader));
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.ToString());
                MessageBox.Show(this, ex is ReadFailedError ? ex.Message : "Failed to load Entry", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!loaded)
            {
                MessageBox.Show(this, "Failed to load Entry", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (forceDebug)
                DebugUtil.ShowDebug(this, data);
            IEntryEditor editor = data.GetEditor(entry);
            if (editor != null)
            {
                editor.ShowDialog(this);
                (editor as IDisposable)?.Dispose(); // Closing a dialog doesn't dispose it, and model viewers hold their scene and a running timer
            }
            else
                DebugUtil.ShowDebug(this, data);
            if (ForceOnlySpecificEntry)
                Environment.Exit(0);
        }
        finally
        {
            TextureCache.ResetCache();
            BundleCache.ClearArchives();
        }
    }

    #region Event Handlers

    private async void newToolStripMenuItem_Click(object sender, EventArgs e)
    {
        await DoNew();
    }

    private async void openToolStripMenuItem_Click(object sender, EventArgs e)
    {
        await Open();
    }

    private async void saveToolStripMenuItem_Click(object sender, EventArgs e)
    {
        await Save();
    }

    private async void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        await SaveAs();
    }

    private void exitToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Exit();
    }

    private async void tsbNew_Click(object sender, EventArgs e)
    {
        await DoNew();
    }

    private async void tsbOpen_Click(object sender, EventArgs e)
    {
        await Open();
    }

    private async void tsbSave_Click(object sender, EventArgs e)
    {
        await Save();
    }

    private async void lstEntries_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        await EditSelectedEntry(false);
    }

    private async void previewToolStripMenuItem_Click(object sender, EventArgs e)
    {
        await EditSelectedEntry(false);
    }

    private async void viewDataToolStripMenuItem_Click(object sender, EventArgs e)
    {
        await EditSelectedEntry(true);
    }

    private async void viewDebugMenuStripMenuItem_Click(object sender, EventArgs e)
    {
        await EditSelectedEntry(false, true);
    }

    private void renameToolStripMenuItem_Click(object sender, EventArgs e)
    {
        RenameSelectedEntry();
    }

    // Edits the name in a text box over its cell, as the list's own label editing only edits the first column.
    // Enter or clicking away keeps it, Escape cancels. An empty name removes it, leaving the detected one.
    private void RenameSelectedEntry()
    {
        // F2 reaches here from the text box too, which is the list's only child
        if (lstEntries.SelectedItems.Count == 0 || lstEntries.Controls.Count > 0)
            return;

        ListViewItem item = lstEntries.SelectedItems[0];
        if (!int.TryParse(item.Text, out int index))
            return;
        BundleEntry entry = GetEntry(index);

        item.EnsureVisible();
        string shown = item.SubItems[NameColumn].Text;
        TextBox box = new() { AutoSize = false, BorderStyle = BorderStyle.FixedSingle, Font = lstEntries.Font, Bounds = item.SubItems[NameColumn].Bounds, Text = shown };

        bool finished = false;
        void Finish(bool keep, bool enter = false)
        {
            if (finished)
                return;
            finished = true;

            // Enter also stores an italic name left as it is, clicking away only keeps edits
            string name = box.Text.Trim();
            if (keep && name != (enter ? entry.DebugInfo.Name ?? "" : shown))
            {
                entry.DebugInfo.Name = name.Length > 0 ? name : null;
                entry.Dirty = true;
                ShowName(item, entry);
            }

            lstEntries.Controls.Remove(box);
            box.Dispose();
            lstEntries.Focus();
        }

        box.KeyDown += (_, e) =>
        {
            if (e.KeyCode is not (Keys.Enter or Keys.Escape))
                return;
            e.SuppressKeyPress = true;
            bool enter = e.KeyCode == Keys.Enter;
            Finish(enter, enter);
        };
        box.LostFocus += (_, _) => Finish(true);

        lstEntries.Controls.Add(box);
        box.Focus();
        box.SelectAll();
    }

    // Names the bundle doesn't store are italic, as ResourceDB.json or detection only suggests them
    private void ShowName(ListViewItem item, BundleEntry entry)
    {
        ListViewItem.ListViewSubItem name = item.SubItems[NameColumn];
        name.Text = entry.DetectName();
        bool inferred = string.IsNullOrWhiteSpace(entry.DebugInfo.Name) && name.Text.Length > 0;
        name.Font = inferred ? _inferredNameFont : null;
        item.ToolTipText = inferred ? "Likely name, not stored in the bundle. Rename and press Enter, or Tools > Fill missing names, to store it." : "";
    }

    private void fillMissingNamesToolStripMenuItem_Click(object sender, EventArgs e)
    {
        int filled = 0;
        foreach (ListViewItem item in lstEntries.Items)
        {
            BundleEntry entry = GetEntry(int.Parse(item.Text));
            string name = item.SubItems[NameColumn].Text;
            if (!string.IsNullOrWhiteSpace(entry.DebugInfo.Name) || name.Length == 0)
                continue;

            entry.DebugInfo.Name = name;
            entry.Dirty = true;
            ShowName(item, entry);
            filled++;
        }

        MessageBox.Show(this, filled > 0 ? $"Filled {filled} missing names." : "No missing names were found.", "Fill missing names",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // Closing can't wait for a save, so with unsaved changes it stops, asks (and saves), then closes again
    private bool _closeConfirmed;

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (!_closeConfirmed && CurrentArchive?.Dirty == true)
        {
            e.Cancel = true;
            BeginInvoke(CloseAfterCheckingSave);
            return;
        }

        if (!SubForm)
            Application.Exit();
    }

    private async void CloseAfterCheckingSave()
    {
        if (!await CheckSave())
            return;
        _closeConfirmed = true;
        Close();
    }

    private async void tsbSwitchMode_Click(object sender, EventArgs e)
    {
        await SwitchMode();
    }

    private void closeToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void lstEntries_ColumnClick(object sender, ColumnClickEventArgs e)
    {
        SortColumn(e.Column);
    }

    private void searchForEntryToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Search();
    }

    private void addResourcesToBundleToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (CurrentArchive != null && ImportResourceForm.ShowImport(this, CurrentArchive))
            UpdateDisplay();
    }

    #endregion

    #region Utility

    private async Task<bool> CheckSave()
    {
        if (CurrentArchive == null)
        {
            return true;
        }
        else if (CurrentArchive.Dirty)
        {
            TaskDialogButton save = new("Save");
            TaskDialogButton dontSave = new("Don't Save");

            TaskDialogPage page = new()
            {
                Caption = "Save",
                Heading = "There are unsaved changes",
                Text = "Would you like to save them first?",
                Icon = TaskDialogIcon.Warning,
                Buttons = { save, dontSave, TaskDialogButton.Cancel },
                AllowCancel = true
            };

            TaskDialogButton result = TaskDialog.ShowDialog(this, page);
            if (result == save)
            {
                return await Save();
            }
            else if (result == dontSave)
            {
                CurrentArchive = null;
                CurrentFileName = null;
                UpdateDisplay();
                return true;
            }
            else
            {
                return false;
            }

        }
        else
        {
            return true;
        }
    }

    private void Search()
    {
        if (CurrentArchive == null)
            return;

        SearchDialog search = new();
        search.Search += id =>
        {
            BundleEntry entry = CurrentArchive.GetEntryByID(id);
            if (entry == null)
            {
                MessageBox.Show(this, "Entry not found!", "Information", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            string indexText = entry.Index.ToString("D3");
            int listIndex = -1;
            for (int i = 0; i < lstEntries.Items.Count; i++)
            {
                ListViewItem item = lstEntries.Items[i];
                if (item.Text == indexText)
                {
                    listIndex = i;
                    break;
                }
            }
            if (listIndex < 0)
                return;

            lstEntries.SelectedIndices.Clear();
            lstEntries.SelectedIndices.Add(listIndex);
            lstEntries.EnsureVisible(listIndex);
        };
        search.ShowDialog(this);
    }

    private async Task SwitchMode()
    {
        if (!await CheckSave())
            return;

        CurrentArchive = null;
        CurrentFileName = null;
        UpdateDisplay();

        Hide();
        Program.SavePreferredMode(true);
        Program.folderModeForm.Show();
    }

    private void SortColumn(int column)
    {
        EntrySorter sorter;

        if (lstEntries.ListViewItemSorter is EntrySorter)
        {
            // every other time
            sorter = lstEntries.ListViewItemSorter as EntrySorter;
            sorter.Column = column;
        }
        else
        {
            // first time
            sorter = new EntrySorter(column);
            lstEntries.ListViewItemSorter = sorter;
        }
        // if you're clicking the same column already being sorted
        //if (sorter.Column == column)
        //{
        // change the direction state from true to false or vice-versa
        sorter.Swap();
        // bop-it
        lstEntries.Sort();
        //}
    }

    private class EntrySorter : IComparer
    {
        public int Column;
        public bool Direction;

        public EntrySorter(int column)
        {
            this.Column = column;
            this.Direction = false;
        }

        public int Compare(object x, object y)
        {
            ListViewItem itemX = (ListViewItem)x;
            ListViewItem itemY = (ListViewItem)y;

            if (Column > itemX?.SubItems.Count || Column > itemY?.SubItems.Count)
            {
                if (this.Direction)
                    return -1;
                return 1;
            }

            string iX = itemX?.SubItems[Column].Text;
            string iY = itemY?.SubItems[Column].Text;

            // Index and size are numbers, so 1000 sorts after 20
            int val = long.TryParse(iX, out long nX) && long.TryParse(iY, out long nY)
                ? nX.CompareTo(nY)
                : Math.Sign(string.Compare(iX, iY, StringComparison.CurrentCultureIgnoreCase));
            if (this.Direction)
                return val * -1;
            return val;

        }

        public void Swap()
        {
            this.Direction = !this.Direction;
        }
    }

    #endregion
}
