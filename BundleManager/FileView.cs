using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using BundleFormat;
using BundleUtilities;

namespace BundleManager;

public partial class FileView : Form
{
    private static bool _loadMaterials;
    public static bool LoadMaterials
    {
        get => _loadMaterials;
        set
        {
            _loadMaterials = value;
            Config.LoadMaterials = _loadMaterials;
        }
    }

    private bool AlwaysIgnore => ignoreIDConflictsToolStripMenuItem.CheckState == CheckState.Checked;
    private string _currentPath
    {
        get => BundleCache.CurrentPath;
        set => BundleCache.CurrentPath = value;
    }

    private bool HasPath => Utilities.IsValidPath(_currentPath);

    private Thread _updateDisplayThread;

    public FileView()
    {
        InitializeComponent();

        LoadMaterials = true;

        UpdateRecentFiles();
    }

    private void Clear()
    {
        _currentPath = null;
        BundleCache.Files.Clear();
        BundleCache.Paths.Clear();
        BundleCache.EntryInfos.Clear();
        lstMain.Items.Clear();
    }

    private void UpdateRecentFiles()
    {
        SaveData.Load();

        int index = fileToolStripMenuItem.DropDownItems.IndexOf(recentSeparatorToolStripMenu) + 1;

        while (true)
        {
            if (index >= fileToolStripMenuItem.DropDownItems.Count)
                break;

            if (fileToolStripMenuItem.DropDownItems[index] is ToolStripSeparator)
                break;

            fileToolStripMenuItem.DropDownItems.RemoveAt(index);
        }

        if (SaveData.RecentPaths.Count == 0)
        {
            ToolStripItem item = new ToolStripMenuItem("No Recent Items");
            item.Enabled = false;

            fileToolStripMenuItem.DropDownItems.Insert(index, item);
        }
        else
        {
            List<string> paths = SaveData.RecentPaths;
            paths.Reverse();
            for (int i = 0; i < paths.Count; i++)
            {
                string path = paths[i];

                ToolStripItem item = new ToolStripMenuItem(path);
                item.Click += (sender, args) => Open(path);

                fileToolStripMenuItem.DropDownItems.Insert(index + i, item);
            }
        }
    }

    private void UpdateDisplay()
    {
        Invoke(new Action(UpdateRecentFiles));

        if (!Utilities.IsValidPath(_currentPath))
        {
            BundleCache.Files.Clear();
            BundleCache.Paths.Clear();
            BundleCache.EntryInfos.Clear();
            lstMain.Items.Clear();
        }
        lstMain.Enabled = Utilities.IsValidPath(_currentPath);

        foreach (string path in BundleCache.Paths)
        {
            string fixedPath = path.Replace('\\', '/');
            string fixedCurrentPath = _currentPath.Replace('\\', '/');

            fixedPath = fixedPath.Replace(fixedCurrentPath, "");

            if (fixedPath.StartsWith("/"))
                fixedPath = fixedPath[1..];

            string[] itemData =
            [
                fixedPath
            ];

            lstMain.Items.Add(new ListViewItem(itemData));
        }
    }

    private void LoadFileList()
    {
        lstMain.Items.Clear();
        if (HasPath)
        {
            LoadingDialog loader = new();
            loader.Status = "Loading: " + _currentPath;
            loader.Done += (cancelled, value) =>
            {
                if (cancelled)
                {
                    _updateDisplayThread?.Interrupt();
                    _currentPath = null;
                }
                UpdateDisplay();
            };
            _updateDisplayThread = new Thread(() =>
            {
                DoLoadFileList(loader);

                loader.Value = new object[] { _currentPath };
                loader.IsDone = true;
            });
            _updateDisplayThread.Start();
            loader.ShowDialog(this);
        }
    }

    private struct ConflictChoice
    {
        public bool Cancel { get; set; }
        public bool IgnoreAll { get; set; }

        public ConflictChoice(bool cancel, bool ignoreAll)
        {
            Cancel = cancel;
            IgnoreAll = ignoreAll;
        }
    }

    private ConflictChoice ResolveConflict(uint entryID, string file1, string file2)
    {
        if (AlwaysIgnore)
            return new ConflictChoice(false, true);

        return (ConflictChoice)Invoke(new Func<ConflictChoice>(() =>
        {
            TaskDialogCommandLinkButton ignoreOnce = new("Ignore this conflict", "Keep the copy that was found first and continue loading.");
            TaskDialogCommandLinkButton ignoreAll = new("Ignore all conflicts", "Don't ask again while loading this folder.");
            TaskDialogCommandLinkButton stop = new("Stop loading");

            TaskDialogPage page = new()
            {
                Caption = "ID Conflict",
                Heading = $"Resource 0x{entryID:X8} is in more than one bundle",
                Text = $"{file2}\n{file1}\n\nIgnoring this may cause other problems.",
                Icon = TaskDialogIcon.Warning,
                Buttons = { ignoreOnce, ignoreAll, stop },
                DefaultButton = stop
            };

            TaskDialogButton result = TaskDialog.ShowDialog(this, page);
            return new ConflictChoice(result == stop, result == ignoreAll);
        }));
    }

    private void DoLoadFileList(ILoader loader)
    {
        BundleCache.Files.Clear();
        BundleCache.Paths.Clear();
        BundleCache.EntryInfos.Clear();
        string[] files = Directory.GetFiles(_currentPath, "*.*", SearchOption.AllDirectories);

        Dictionary<string, bool> fileTypes = [];

        string cachePath = _currentPath + "/files.bmcache";
        if (File.Exists(cachePath))
        {
            string[] lines = File.ReadAllLines(cachePath);
            foreach (string line in lines)
            {
                string[] data = line.Split('|');
                if (data.Length < 2)
                    continue;

                if (File.Exists(data[0]))
                    fileTypes.Add(data[0], data[1] == "bundle");
            }
        }

        int i = 0;
        int index = 0;
        bool ignoreAllConflicts = false;
        foreach (string file in files)
        {
            index++;
            try
            {
                if (!fileTypes.ContainsKey(file))
                {
                    bool isBundle = BundleArchive.IsBundle(file);
                    fileTypes.Add(file, isBundle);

                    if (!isBundle)
                        continue;
                }
                else if (!fileTypes[file])
                {
                    continue;
                }

                int progress = index * 100 / files.Length;
                loader.SetStatus("Loading(" + progress.ToString("D2") + "%): " + Path.GetFileName(file));
                loader.SetProgress(progress);

                string fixedPath = file.Replace('\\', '/');
                string fixedCurrentPath = _currentPath.Replace('\\', '/');

                fixedPath = fixedPath.Replace(fixedCurrentPath, "");

                if (fixedPath.StartsWith("/"))
                    fixedPath = fixedPath[1..];

                string[] itemData =
                [
                    fixedPath
                ];

                bool cancel = false;

                List<EntryInfo> entryIDs = BundleArchive.GetEntryInfos(file, false);
                foreach (EntryInfo info in entryIDs)
                {
                    uint entryID = info.ID;
                    if (BundleCache.Files.ContainsKey(entryID))
                    {
                        if (ignoreAllConflicts)
                            continue;
                        int index1 = BundleCache.Files[entryID];
                        string otherFile = BundleCache.Paths[index1];
                        ConflictChoice choice = ResolveConflict(entryID, file, otherFile);
                        if (choice.Cancel)
                        {
                            cancel = true;
                            break;
                        }
                        if (choice.IgnoreAll)
                            ignoreAllConflicts = true;
                        continue;
                    }
                    if (!BundleCache.EntryInfos.ContainsKey(entryID))
                        BundleCache.EntryInfos.Add(entryID, info);
                    BundleCache.Files.Add(entryID, i);
                }

                if (cancel)
                {
                    BundleCache.Files.Clear();
                    BundleCache.Paths.Clear();
                    BundleCache.EntryInfos.Clear();
                    lstMain.Items.Clear();
                    break;
                }

                BundleCache.Paths.Add(file);
            }
            catch (ThreadAbortException)
            {
                BundleCache.Files.Clear();
                BundleCache.Paths.Clear();
                BundleCache.EntryInfos.Clear();

                _currentPath = null;
                break;
            }

            i++;
        }

        try
        {
            Stream s = File.Open(cachePath, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new(s);

            foreach (string key in fileTypes.Keys)
                sw.WriteLine(key + "|" + (fileTypes[key] ? "bundle" : "other"));

            sw.Flush();
            sw.Close();
            s.Close();
        }
        catch (IOException ex)
        {
            Invoke(new Action(() => MessageBox.Show(this, "Failed to cache file types:\n" + ex.Message + "\n" + ex.StackTrace)));
        }
    }

    public void Open(string path = "")
    {
        if (string.IsNullOrEmpty(path))
        {
            FolderBrowserDialog fbd = new();
            SaveData.Load();
            if (SaveData.RecentPaths.Count > 0)
                fbd.SelectedPath = SaveData.RecentPaths[^1];
            DialogResult result = fbd.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                _currentPath = fbd.SelectedPath;
                SaveData.AddRecentPath(_currentPath);
                SaveData.Save();
                LoadFileList();
            }
        }
        else
        {
            _currentPath = path;
            SaveData.AddRecentPath(_currentPath);
            SaveData.Save();
            LoadFileList();
        }
    }

    private void OpenBundle(int index)
    {
        string file = BundleCache.Paths[index];
        MainForm form = new();
        form.SubForm = true;
        form.Open(file);
        form.ShowDialog(this);
    }

    private void Exit()
    {
        Application.Exit();
    }

    private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Open();
    }

    private void exitToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Exit();
    }

    private void tsbOpen_Click(object sender, EventArgs e)
    {
        Open();
    }

    private void tsbSwitchMode_Click(object sender, EventArgs e)
    {
        Clear();

        Hide();
        Program.SavePreferredMode(false);
        Program.fileModeForm.Show();
    }

    private void FileView_FormClosing(object sender, FormClosingEventArgs e)
    {
        Application.Exit();
    }

    private void lstMain_DoubleClick(object sender, EventArgs e)
    {
        if (lstMain.SelectedIndices.Count > 0)
        {
            OpenBundle(lstMain.SelectedIndices[0]);
        }
    }

    private void ignoreIDConflictsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (ignoreIDConflictsToolStripMenuItem.CheckState == CheckState.Checked)
            ignoreIDConflictsToolStripMenuItem.CheckState = CheckState.Unchecked;
        else
            ignoreIDConflictsToolStripMenuItem.CheckState = CheckState.Checked;
    }

    private void loadMaterialsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (loadMaterialsToolStripMenuItem.CheckState == CheckState.Checked)
            loadMaterialsToolStripMenuItem.CheckState = CheckState.Unchecked;
        else
            loadMaterialsToolStripMenuItem.CheckState = CheckState.Checked;

        LoadMaterials = loadMaterialsToolStripMenuItem.CheckState == CheckState.Checked;
    }

    private void dumpInfoToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (_currentPath == null)
        {
            MessageBox.Show("Please load a folder first!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SaveFileDialog sfd = new();
        sfd.Filter = "Text Files|*.txt|All Files|*.*";
        DialogResult result = sfd.ShowDialog(this);
        if (result == DialogResult.OK)
        {
            string path = sfd.FileName;

            Stream s = File.Open(path, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new(s);

            int maxLength = -1;

            Array types = Enum.GetValues(typeof(EntryType));

            foreach (EntryType type in types)
            {
                int len = type.ToString().Length;

                maxLength = Math.Max(len, maxLength);
            }

            foreach (uint key in BundleCache.EntryInfos.Keys)
            {
                EntryInfo info = BundleCache.EntryInfos[key];

                string thePath = info.Path;
                string fixedPath = thePath.Replace('\\', '/');
                string fixedCurrentPath = _currentPath.Replace('\\', '/');

                fixedPath = fixedPath.Replace(fixedCurrentPath, "");

                if (fixedPath.StartsWith("/"))
                    fixedPath = fixedPath[1..];

                string typeName = info.Type.ToString();

                int len = typeName.Length;

                int lenDiff = maxLength - len;

                string typeString = typeName;

                for (int i = 0; i < lenDiff; i++)
                    typeString += " ";

                sw.WriteLine("0x" + info.ID.ToString("X8") + "\t\t" + typeString + "\t\t" + fixedPath);
            }

            sw.Flush();
            sw.Close();
            s.Close();
        }
    }
}
