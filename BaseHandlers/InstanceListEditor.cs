using BundleFormat;
using BundleUtilities;
using ModelViewer;
using ModelViewer.SceneData;
using PluginAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;

namespace BaseHandlers
{
    public partial class InstanceListEditor : Form, IEntryEditor
    {
        public delegate void OnChanged();
        public event OnChanged Changed;

        private InstanceList _instanceList;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public InstanceList InstanceList
        {
            get => _instanceList;
            set
            {
                _instanceList = value;
                UpdateDisplay();
            }
        }

        private Dictionary<ulong, string> _nameCache;

        public InstanceListEditor()
        {
            InitializeComponent();

            _nameCache = new Dictionary<ulong, string>();
        }

        private void UpdateDisplay()
        {
            lstMain.Items.Clear();

            if (InstanceList == null)
                return;

            for (int i = 0; i < InstanceList.Instances.Count; i++)
            {
                ModelInstance model = InstanceList.Instances[i];

                string[] items = new string[]
                {
                    GetModelName(model.ModelEntryID),
                    model.Translation.ToString(),
                    model.Rotation.ToString(),
                    model.Scale.ToString()
                };

                lstMain.Items.Add(new ListViewItem(items));
            }
        }

        private void UpdateColumnWidth()
        {
            if (lstMain.Columns.Count <= 0)
                return;

            int totalColumnWidth = 0;
            for (int i = 1; i < lstMain.Columns.Count; i++)
            {
                totalColumnWidth += lstMain.Columns[i].Width;
            }

            int remainingWidth = lstMain.Width - totalColumnWidth - 120;

            if (lstMain.Columns[0].Width != remainingWidth)
                lstMain.Columns[0].Width = remainingWidth;
        }

        private string GetModelName(ulong id)
        {
            if (_nameCache.ContainsKey(id))
                return _nameCache[id];

            string result;

            //BundleArchive archive = InstanceList.Entry.Archive;
            //bool external = !archive.ContainsEntry(id);
            //if (external)
                result = BundleCache.GetEntryNameByID(id);
            //else
            //    result = archive.GetEntryNameByID(id);

            if (string.IsNullOrWhiteSpace(result))
                result = "0x" + id.ToString("X8");
            else
                result = SimplifyModelName(result);

            //if (external)
            //    result += " (External)";

            _nameCache.Add(id, result);
            return result;
        }

        private string SimplifyModelName(string name)
        {
            string commonstring = "gamedb://burnout5/";
            if (name.StartsWith(commonstring))
                name = name.Substring(commonstring.Length);

            commonstring = "Burnout/Content_World/";
            if (name.StartsWith(commonstring))
                name = name.Substring(commonstring.Length);

            /*commonstring = "Scenes/";
            if (name.StartsWith(commonstring))
                name = name.Substring(commonstring.Length);

            commonstring = "Scenes_Final/";
            if (name.StartsWith(commonstring))
                name = name.Substring(commonstring.Length);*/

            int qIndex = name.IndexOf("?");
            if (qIndex > -1)
                name = name.Substring(0, qIndex);
            return name;
        }

        private void ViewSelectedModel()
        {
            if (lstMain.SelectedIndices.Count <= 0)
                return;

            int selectedIndex = lstMain.SelectedIndices[0];
            ModelInstance model = InstanceList.Instances[selectedIndex];

            LoadingDialog loader = new LoadingDialog();
            BundleEntry renderableEntry = null;
            Renderable renderable = null;

            Thread loadInstanceThread = null;
            bool failure = false;

            loader.Done += (cancelled, value) =>
            {
                if (cancelled)
                    loadInstanceThread?.Interrupt();
                else
                {
                    loader.Hide();
                    if (!failure)
                    {
                        IEntryEditor editor = renderable.GetEditor(renderableEntry);
                        editor.ShowDialog(this);
                    }
                }
            };

            loadInstanceThread = new Thread(() =>
            {
                try
                {
                    BundleEntry modelEntry = InstanceList.Entry.Archive.GetEntryByID(model.ModelEntryID);
                    if (modelEntry == null)
                    {
                        string file = BundleCache.GetFileByEntryID(model.ModelEntryID);
                        if (!string.IsNullOrEmpty(file))
                        {
                            BundleArchive archive = BundleArchive.Read(file);
                            modelEntry = archive.GetEntryByID(model.ModelEntryID);
                        }
                    }

                    if (modelEntry != null)
                    {
                        renderableEntry = modelEntry.GetDependencies()[0].Entry;
                        renderable = new Renderable();
                        renderable.Read(renderableEntry, loader);
                    }
                    if (renderable == null)
                        failure = true;
                }
                catch (Exception)
                {
                    failure = true;
                }
                loader.IsDone = true;
            });
            loadInstanceThread.Start();
            loader.ShowDialog(this);
        }

        private void InstanceListEditor_Load(object sender, EventArgs e)
        {
            UpdateColumnWidth();
        }

        private void RenderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadingDialog loader = new LoadingDialog();
            Scene scene = null;

            Thread loadInstanceThread = null;
            bool failure = false;

            loader.Done += (cancelled, value) =>
            {
                if (cancelled)
                    loadInstanceThread?.Interrupt();
                else
                {
                    loader.Hide();
                    if (!failure)
                        ModelViewerForm.ShowModelViewer(this, scene);
                }
            };

            loadInstanceThread = new Thread(() =>
            {
                try
                {
                    scene = InstanceList.MakeScene(loader);
                    if (scene == null)
                        failure = true;
                }
                catch (Exception)
                {
                    failure = true;
                }
                loader.IsDone = true;
            });
            loadInstanceThread.Start();
            loader.ShowDialog(this);
        }

        private void DebugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
        }

        private void LstMain_SizeChanged(object sender, EventArgs e)
        {
            UpdateColumnWidth();
        }

        private void LstMain_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ViewSelectedModel();
        }
    }
}
