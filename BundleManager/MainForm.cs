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

    private bool _subForm;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool SubForm
    {
        get { return _subForm; }
        set
        {
            _subForm = value;

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

    private BundleArchive _archive;

    private delegate BundleArchive GetArchive();

    private delegate void SetArchive(BundleArchive archive);

    private BundleArchive CurrentArchive
    {
        get
        {
            if (InvokeRequired)
            {
                GetArchive method = () =>
                {
                    return _archive;
                };
                return (BundleArchive)Invoke(method);
            }
            else
            {
                return _archive;
            }
        }
        set
        {
            if (InvokeRequired)
            {
                SetArchive method = (BundleArchive archive) =>
                {
                    _archive = archive;
                };
                Invoke(method, value);
            }
            else
            {
                _archive = value;
            }
        }
    }

    private string _currentFileName;

    private delegate string GetString();

    private delegate void SetString(string archive);

    private string CurrentFileName
    {
        get
        {
            if (InvokeRequired)
            {
                GetString method = () =>
                {
                    return _currentFileName;
                };
                return (string)Invoke(method);
            }
            else
            {
                return _currentFileName;
            }
        }
        set
        {
            if (InvokeRequired)
            {
                SetString method = (string filename) =>
                {
                    _currentFileName = filename;
                };
                Invoke(method, value);
            }
            else
            {
                _currentFileName = value;
            }
        }
    }

    private delegate bool GetBool();

    private delegate void SetBool(bool value);

    public bool _console => CurrentArchive.Console;

    public bool ForceOnlySpecificEntry = false;

    private Thread _openSaveThread;
    private Thread _saveThread;

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
                subItem.BackColor = color;
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

    private void DoNew()
    {
        if (!CheckSave())
            return;
        //CurrentArchive = new BundleArchive();
        //UpdateDisplay();

        MessageBox.Show(this,
            "This feature is currently not avaliable because we don't know enough about the Bundle format to create them from scratch yet.",
            "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void Open()
    {
        if (!CheckSave())
            return;
        OpenFileDialog ofd = new();
        ofd.Filter = "Supported Files|*.BUNDLE;*.BIN;*.BNDL;*.DAT;*.TEX|All Files|*.*";
        DialogResult result = ofd.ShowDialog(this);
        if (result == DialogResult.OK)
        {
            if (!Utilities.FileExists(ofd.FileName))
                return;

            Open(ofd.FileName);
        }
    }

    public void Open(string path)
    {
        LoadingDialog loader = new();
        loader.Status = "Loading: " + path;
        loader.Done += Loader_OpenDone;
        _openSaveThread = new Thread(() => DoOpenBundle(loader, path));
        _openSaveThread.Start();
        loader.ShowDialog(this);
    }

    private void Loader_OpenDone(bool cancelled, object value)
    {
        if (cancelled)
        {
            _openSaveThread?.Interrupt();
            CurrentArchive = null;
            CurrentFileName = null;
        }
        else
        {
            object[] values = (object[])value;
            CurrentArchive = (BundleArchive)values[0];

            if (CurrentArchive == null)
            {
                MessageBox.Show(this, $"There was an error opening {values[1]}:\n{values[2]}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                CurrentFileName = null;
                Text = "Bundle Manager";
            }
            else
            {
                CurrentFileName = (string)values[1];
                Text = "Bundle Manager - " + CurrentFileName;
            }
        }
        UpdateDisplay();
    }

    public void DoOpenBundle(LoadingDialog loader, string path)
    {
        BundleArchive archive = null;
        string error = null;
        try
        {
            archive = BundleArchive.Open(path);
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }

        loader.Value = new object[] { archive, path, error };
        loader.IsDone = true;
    }

    public bool Save()
    {
        if (CurrentArchive == null)
        {
            MessageBox.Show(this, "Nothing to save!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return true;
        }
        if (string.IsNullOrEmpty(CurrentFileName))
        {
            return SaveAs();
        }

        return SaveTo(CurrentFileName);
    }

    public bool SaveAs()
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
            if (!Utilities.IsValidPath(sfd.FileName) || !SaveTo(sfd.FileName))
                return false;

            CurrentFileName = sfd.FileName;
            Text = "Bundle Manager - " + CurrentFileName;

            return true;
        }
        return false;
    }

    // True only once the bundle is written. Cancelling, or an error, leaves the file as it was
    private bool SaveTo(string path)
    {
        _saveThread?.Join(); // A cancelled save may still be stopping. It never waits on the UI thread, so this can't deadlock

        BundleArchive archive = CurrentArchive;
        CancellationTokenSource cancel = new();
        Exception error = null;
        bool cancelled = false;

        LoadingDialog loader = new();
        loader.Status = "Saving: " + path;
        loader.Done += (wasCancelled, _) =>
        {
            cancelled = wasCancelled;
            if (wasCancelled)
                cancel.Cancel();
        };
        _saveThread = new Thread(() =>
        {
            try
            {
                archive.Write(path, cancel.Token);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            loader.IsDone = true;
        });
        _saveThread.Start();
        loader.ShowDialog(this);

        if (cancelled)
            return false;
        if (error != null)
        {
            MessageBox.Show(this, $"{path} wasn't saved, and is unchanged:\n{error.Message}", "Error",
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

    private delegate BundleEntry GetEntryDelegate(int index);
    public BundleEntry GetEntry(int index)
    {
        if (InvokeRequired)
        {
            GetEntryDelegate del = GetEntry;
            return (BundleEntry)Invoke(del, index);
        }
        else
        {
            return CurrentArchive.Entries[index];
        }
    }

    public void EditSelectedEntry(bool forceHex = false, bool forceDebug = false)
    {
        int count = lstEntries.SelectedIndices.Count;
        if (count <= 0)
            return;

        if (!int.TryParse(lstEntries.SelectedItems[0].Text, out int index))
            return;

        EditEntry(index, forceHex, forceDebug);
    }

    public void EditEntry(int index, bool forceHex = false, bool forceDebug = false)
    {
        BundleEntry entry = GetEntry(index);

        if (EntryTypeRegistry.IsRegistered(entry.Type) && !forceHex)
        {
            IEntryData data = EntryTypeRegistry.GetHandler(entry.Type);

            TextureCache.ResetCache();
            LoadingDialog loader = new();
            loader.Status = "Loading: " + entry.ID.ToString("X8");

            Thread loadInstanceThread = null;
            bool failure = false;

            loader.Done += (cancelled, value) =>
            {
                if (cancelled)
                    loadInstanceThread?.Interrupt();
                else
                {
                    if (failure)
                    {
                        MessageBox.Show(this, "Failed to load Entry", "Error", MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    else
                    {
                        loader.Hide();
                        if (forceDebug)
                        {
                            DebugUtil.ShowDebug(this, data);
                        }
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
                }
                TextureCache.ResetCache();
                BundleCache.ClearArchives();
            };

            loadInstanceThread = new Thread(() =>
            {
                try
                {
                    try
                    {
                        failure = !data.Read(entry, loader);
                    }
                    catch (ReadFailedError ex)
                    {

                        Console.Out.WriteLine(ex.ToString());
                        MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        failure = true;

                        throw;
                    }
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(e.ToString());
                    MessageBox.Show("Failed to load Entry", "Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    failure = true;
                }
                loader.IsDone = true;
            });
            loadInstanceThread.Start();
            loader.ShowDialog(this);
        }
        else
        {
            using EntryEditor editor = new();
            editor.ForceHex = forceHex;
            Task.Run(() => openEditor(editor, index));
            editor.ShowDialog(this);
        }
    }


    public void openEditor(EntryEditor editor, int index)
    {
        editor.Entry = GetEntry(index);
    }

    #region Event Handlers

    private void newToolStripMenuItem_Click(object sender, EventArgs e)
    {
        DoNew();
    }

    private void openToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Open();
    }

    private void saveToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Save();
    }

    private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        SaveAs();
    }

    private void exitToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Exit();
    }

    private void tsbNew_Click(object sender, EventArgs e)
    {
        DoNew();
    }

    private void tsbOpen_Click(object sender, EventArgs e)
    {
        Open();
    }

    private void tsbSave_Click(object sender, EventArgs e)
    {
        Save();
    }

    private void lstEntries_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        EditSelectedEntry(false);
    }

    private void previewToolStripMenuItem_Click(object sender, EventArgs e)
    {
        EditSelectedEntry(false);
    }

    private void viewDataToolStripMenuItem_Click(object sender, EventArgs e)
    {
        EditSelectedEntry(true);
    }

    private void viewDebugMenuStripMenuItem_Click(object sender, EventArgs e)
    {
        EditSelectedEntry(false, true);
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

    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (!CheckSave())
        {
            e.Cancel = true;
            return; // Application.Exit would close again and ask a second time
        }

        if (!SubForm)
            Application.Exit();
    }

    private void tsbSwitchMode_Click(object sender, EventArgs e)
    {
        SwitchMode();
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

    private bool CheckSave()
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
                return Save();
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

    private void SwitchMode()
    {
        if (!CheckSave())
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
