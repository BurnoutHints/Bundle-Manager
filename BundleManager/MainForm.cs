using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BundleFormat;
using BundleUtilities;
using DebugHelper;
using PluginAPI;

namespace BundleManager
{
    public partial class MainForm : Form
    {
        #region Variables and Properties

        private bool _subForm;
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

        #endregion

        public MainForm()
        {
            InitializeComponent();

            //doNew();
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            lstEntries.Items.Clear();

            UpdatePluginMenu();

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
            lstEntries.ListViewItemSorter = null; // Disable sorting while adding

            foreach (BundleEntry entry in CurrentArchive.Entries)
            {
                if (entry.EntryBlocks[0].Data == null) // Exception occurred
                {
                    lstEntries.Items.Clear();
                    lstEntries.Enabled = false;
                    CurrentArchive = null;
                    CurrentFileName = null;
                    lstEntries.EndUpdate();
                    UpdatePluginMenu();
                    return;
                }
                string[] values = new string[]
                {
                    entry.Index.ToString("d3"),
                    entry.DetectName(),
                    "0x" + entry.ID.ToString("X8"),
                    entry.Type.ToString(),
                    entry.EntryBlocks[0].Data.Length.ToString(),
                    entry.EntryBlocks[0].Data.MakePreview(0, 16)
                };

                ListViewItem item = new ListViewItem(values);
                item.BackColor = BundleEntry.GetColor(entry.Type);
                lstEntries.Items.Add(item);
            }

            lstEntries.ListViewItemSorter = new EntrySorter(0); // Also calls Sort
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
            OpenFileDialog ofd = new OpenFileDialog();
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
            LoadingDialog loader = new LoadingDialog();
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
                    MessageBox.Show(this, "There was an error opening archive: " + (string)values[1], "Error",
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
            BundleArchive archive = BundleArchive.Read(path);

            loader.Value = new object[] { archive, path };
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

            LoadingDialog loader = new LoadingDialog();
            loader.Status = "Saving: " + CurrentFileName;
            loader.Done += Loader_SaveDone;
            _openSaveThread = new Thread(() => DoSaveBundle(loader, CurrentFileName));
            _openSaveThread.Start();
            loader.ShowDialog(this);
            return true;
        }

        public bool SaveAs()
        {
            if (CurrentArchive == null)
            {
                MessageBox.Show(this, "Nothing to save!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return true;
            }
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Supported Files|*.BUNDLE;*.BIN;*.BNDL;*.DAT;*.TEX|All Files|*.*";
            DialogResult result = sfd.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                if (!Utilities.IsValidPath(sfd.FileName))
                    return false;

                LoadingDialog loader = new LoadingDialog();
                loader.Status = "Saving: " + sfd.FileName;
                loader.Done += Loader_SaveDone;
                _openSaveThread = new Thread(() => DoSaveBundle(loader, sfd.FileName));
                _openSaveThread.Start();
                loader.ShowDialog(this);

                CurrentFileName = sfd.FileName;
                Text = "Bundle Manager - " + CurrentFileName;
                UpdateDisplay();

                return true;
            }
            return false;
        }

        private void Loader_SaveDone(bool cancelled, object value)
        {
            if (cancelled)
            {
                _openSaveThread?.Interrupt();
            }
            else
            {
                CurrentArchive.Dirty = false;
            }
        }

        public void DoSaveBundle(LoadingDialog loader, string path)
        {
            CurrentArchive.Write(path);

            loader.IsDone = true;
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

            int index;
            if (!int.TryParse(lstEntries.SelectedItems[0].Text, out index))
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
                LoadingDialog loader = new LoadingDialog();
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
                                editor.ShowDialog(this);
                            else
                                DebugUtil.ShowDebug(this, data);
                            if (ForceOnlySpecificEntry)
                                Environment.Exit(0);
                        }
                    }
                    TextureCache.ResetCache();
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
                            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            failure = true;

                            throw;
                        }
                    }
                    catch (Exception)
                    {
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
                EntryEditor editor = new EntryEditor();
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

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!CheckSave())
            {
                e.Cancel = true;
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
                DialogResult result = MessageBox.Show(this, "There are unsaved changes!\nWould you like to save?",
                    "Save", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    return Save();
                }
                else if (result == DialogResult.No)
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

            SearchDialog search = new SearchDialog();
            search.Search += id =>
            {
                BundleEntry entry = CurrentArchive.GetEntryByID(id);
                if (entry == null)
                {
                    MessageBox.Show(this, "Entry not found!", "Information", MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                string indexText = CurrentArchive.Entries.IndexOf(entry).ToString("D3");
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

                /*
                if (int.TryParse(iX, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out var iXint))
                {
                    if (int.TryParse(iY, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out var iYint))
                    {
                        int val2 = iXint.CompareTo(iYint);
                        if (this.Direction)
                            return val2 * -1;
                        return val2;
                    }
                }
                */

                int val = Math.Sign(string.Compare(iX, iY, StringComparison.CurrentCultureIgnoreCase));
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
}
