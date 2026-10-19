using BundleFormat;
using BundleUtilities;
using DebugHelper;
using ModelViewer;
using ModelViewer.SceneData;
using PluginAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BaseHandlers;

public partial class InstanceListEditor : Form, IEntryEditor
{
    public delegate void OnChanged();
    public event OnChanged Changed;


    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public InstanceList InstanceList
    {
        get;
        set
        {
            field = value;
            UpdateDisplay();
        }
    }

    private Dictionary<ulong, string> _nameCache;

    public InstanceListEditor()
    {
        InitializeComponent();

        _nameCache = [];
    }

    private void UpdateDisplay()
    {
        lstMain.Items.Clear();

        if (InstanceList == null)
            return;

        for (int i = 0; i < InstanceList.Instances.Count; i++)
        {
            ModelInstance model = InstanceList.Instances[i];

            string[] items =
            [
                GetModelName(model.ModelEntryID),
                model.Translation.ToString(),
                model.Rotation.ToString(),
                model.Scale.ToString()
            ];

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
            name = name[commonstring.Length..];

        commonstring = "Burnout/Content_World/";
        if (name.StartsWith(commonstring))
            name = name[commonstring.Length..];

        /*commonstring = "Scenes/";
        if (name.StartsWith(commonstring))
            name = name.Substring(commonstring.Length);

        commonstring = "Scenes_Final/";
        if (name.StartsWith(commonstring))
            name = name.Substring(commonstring.Length);*/

        int qIndex = name.IndexOf("?");
        if (qIndex > -1)
            name = name[..qIndex];
        return name;
    }

    private async Task ViewSelectedModel()
    {
        if (lstMain.SelectedIndices.Count <= 0)
            return;

        int selectedIndex = lstMain.SelectedIndices[0];
        ModelInstance model = InstanceList.Instances[selectedIndex];

        (Renderable renderable, BundleEntry renderableEntry) loaded;
        try
        {
            loaded = await LoadingDialog.RunAsync(this, "Loading model", (loader, _) =>
            {
                BundleEntry modelEntry = InstanceList.Entry.Archive.GetEntryByID(model.ModelEntryID)
                    ?? BundleCache.GetEntryByID(model.ModelEntryID);
                if (modelEntry == null)
                    return default;

                BundleEntry renderableEntry = modelEntry.GetDependencies()[0].Entry;
                Renderable renderable = new();
                renderable.Read(renderableEntry, loader);
                return (renderable, renderableEntry);
            });
        }
        catch (Exception) // Including cancelling
        {
            return;
        }
        if (loaded.renderable == null)
            return;

        IEntryEditor editor = loaded.renderable.GetEditor(loaded.renderableEntry);
        editor.ShowDialog(this);
        (editor as IDisposable)?.Dispose();
    }

    private void InstanceListEditor_Load(object sender, EventArgs e)
    {
        UpdateColumnWidth();
    }

    private async void RenderToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Scene scene;
        try
        {
            scene = await LoadingDialog.RunAsync(this, "Loading instances", (loader, _) => InstanceList.MakeScene(loader));
        }
        catch (Exception) // Including cancelling
        {
            return;
        }
        if (scene != null)
            ModelViewerForm.ShowModelViewer(this, scene);
    }

    private void DebugToolStripMenuItem_Click(object sender, EventArgs e)
    {
        DebugUtil.ShowDebug(this, InstanceList);
    }

    private void LstMain_SizeChanged(object sender, EventArgs e)
    {
        UpdateColumnWidth();
    }

    private async void LstMain_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        await ViewSelectedModel();
    }
}
