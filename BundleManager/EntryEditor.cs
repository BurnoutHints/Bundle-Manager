using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BundleFormat;
using BundleUtilities;
using BurnoutImage;
using LangEditor;

namespace BundleManager;

public partial class EntryEditor : Form
{
    private const string DataFileFilter = "Binary Files (Bundle Manager & YAP)|*.dat|Binary Files (Legacy BM & bnd2-manager)|*.bin;*.dat|Binary Files (DGI)|*.dat";
    private const int YAPFilterIndex = 1; // Legacy and DGI keep the import table at the end of the primary data, without a yaml

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public BundleEntry Entry
    {
        get;
        set
        {
            field = value;
            UpdateDisplay();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool ForceHex
    {
        get;
        set;
    }

    public EntryEditor()
    {
        InitializeComponent();
        Disposed += (_, _) => pboImage.Image?.Dispose(); // A PictureBox doesn't dispose its image

        ImageMenuVisible = false;
        BinaryMenuVisible = false;
        ImageVisible = false;
        ImageStatusPanelVisible = false;
        TabsVisible = false;
        MenuVisible = false; // Until UpdateDisplay has the entry

        //txtData.MaxLength = int.MaxValue;
        //txtExtraData.MaxLength = int.MaxValue;
    }

    private void FocusTab()
    {
        if (tabList.SelectedTab != null)
        {
            if (tabList.SelectedTab.Controls.Count > 0)
            {
                tabList.SelectedTab.Controls[0].Focus();
            }
        }
    }

    private byte[] DataHex
    {
        get => hexData.HexData;
        set => hexData.HexData = value;
    }

    private byte[] ExtraDataHex
    {
        get => hexExtraData.HexData;
        set => hexExtraData.HexData = value;
    }

    // The previous image is this editor's own decode, so nothing else holds it
    private Image Image
    {
        get => pboImage.Image;
        set
        {
            Image old = pboImage.Image;
            pboImage.Image = value;
            if (old != value)
                old?.Dispose();
        }
    }

    private bool ImageVisible { set => pboImage.Visible = value; }
    private bool TabsVisible { set => tabList.Visible = value; }
    private bool ImageStatusPanelVisible { set => imageStatusLabel.Visible = value; }
    private bool ImageMenuVisible { set => imageToolStripMenuItem.Visible = value; }
    private bool BinaryMenuVisible { set => binaryToolStripMenuItem.Visible = value; }
    private bool MenuVisible { set => mnuBar.Visible = value; }

    private static string GetInfo(BundleEntry entry)
    {
        StringBuilder sb = new();

        sb.AppendLine("ID: 0x" + entry.ID.ToString("X8"));
        sb.AppendLine("Type: " + entry.Type);
        sb.AppendLine("Has Header: " + entry.HasHeader);
        sb.AppendLine("Has Body: " + entry.HasBody);
        List<BundleDependency> dependencies = entry.GetDependencies();
        if (dependencies.Count > 0)
        {
            sb.AppendLine("Dependencies[" + dependencies.Count + "] = {");
            for (int i = 0; i < dependencies.Count; i++)
            {
                BundleDependency bundleDependency = dependencies[i];
                sb.AppendLine("    " + bundleDependency.ToString());
            }
            sb.AppendLine("}");
        }
        else
        {
            sb.AppendLine("Dependencies[" + dependencies.Count + "];");
        }

        return sb.ToString();
    }

    // Counts calls, so an older display that finishes decoding late doesn't replace a newer one
    private int _displayVersion;

    // A texture decodes, and the info resolves its dependencies, off the UI thread
    private async void UpdateDisplay()
    {
        int version = ++_displayVersion;
        BundleEntry entry = Entry;
        bool texture = !ForceHex && entry.Type == EntryType.Texture;
        Text = "Edit Entry: " + entry.Index.ToString("d3");

        (Image image, ImageHeader header, string info) = await Task.Run(() =>
        {
            Image decoded = texture ? GameImage.GetImage(entry.EntryBlocks[0].Data, entry.EntryBlocks[1].Data, entry.Platform) : null;
            return (decoded, decoded != null ? GameImage.GetImageHeader(entry.EntryBlocks[0].Data, entry.Platform) : null, GetInfo(entry));
        });
        if (IsDisposed || version != _displayVersion)
        {
            image?.Dispose();
            return;
        }

        bool showImage = image != null;
        Image = image;
        ImageVisible = showImage;
        TabsVisible = !showImage;
        ImageMenuVisible = showImage;
        ImageStatusPanelVisible = showImage;
        BinaryMenuVisible = !showImage;

        if (showImage)
        {
            imageStatusLabel.Text =
                $"Platform: {header.Platform}    " +
                $"Size: {header.Width}x{header.Height}    " +
                $"Format: {header.CompressionType}";
        }
        else
        {
            DataHex = entry.EntryBlocks[0].Data;
            ExtraDataHex = entry.EntryBlocks[entry.SecondaryBlock].Data;
        }

        MenuVisible = true;

        txtInfo.Text = info;
    }

    private void exportToolStripMenuItem_Click(object sender, EventArgs e)
    {
        SaveFileDialog sfd = new();
        sfd.Filter = "PNG Images|*.png";
        sfd.FileOk += Sfd_FileOk;
        sfd.ShowDialog(this);
    }

    private void Sfd_FileOk(object sender, CancelEventArgs e)
    {
        SaveFileDialog sfd = (SaveFileDialog)sender;

        if (sfd.FileName == null || sfd.FileName.Length <= 0)
            return;

        string path = sfd.FileName;

        Image.Save(path, ImageFormat.Png);
    }

    private async void importToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using OpenFileDialog ofd = new() { Filter = GameImage.FileFilter + "|All files (*.*)|*.*" };
        if (ofd.ShowDialog(this) != DialogResult.OK)
            return;

        string path = ofd.FileName;
        byte[] oldHeader = Entry.EntryBlocks[0].Data;
        ImageInfo info;
        try
        {
            ImageHeader header = GameImage.GetImageHeader(oldHeader, Entry.Platform);
            CompressionType? compression = null; // DDS files keep their own format
            if (!string.Equals(Path.GetExtension(path), ".dds", StringComparison.OrdinalIgnoreCase)
                && (compression = ImageImportDialog.AskCompression(this, path, header.Platform)) == null)
                return;

            info = await Task.Run(() => GameImage.SetImage(path, compression ?? header.CompressionType, header.Platform, oldHeader));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Unable to import {path}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        Entry.Dirty = true;
        Entry.EntryBlocks[0].Data = info.Header;
        Entry.EntryBlocks[1].Data = info.Data;

        ImageVisible = false;
        TabsVisible = false;
        ImageMenuVisible = false;
        BinaryMenuVisible = false;
        UpdateDisplay();
    }

    private void importDataToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using OpenFileDialog ofd = new()
        {
            FileName = ExportNames(secondary: false)[0],
            DefaultExt = "dat",
            Filter = DataFileFilter
        };

        if (ofd.ShowDialog(this) == DialogResult.OK)
            ImportPrimary(ofd.FileName);
    }

    private void ImportPrimary(string path)
    {
        byte[] data = ResourceImport.ReadPrimary(path, Entry.Console, out List<Dependency> imports);

        if (imports == null)
        {
            List<Dependency> current = [.. Entry.GetDependencies().Select(d => new Dependency { ID = d.EntryID, EntryPointerOffset = (uint)d.EntryPointerOffset })];
            if (current.Count > 0 && Dependency.EndsWithTable(data, current, Entry.Console))
            {
                imports = current;
            }
            else
            {
                List<Dependency> found = ResourceImport.ScanImports(data, Entry.Platform);
                imports = found.Count == 0 && current.Count == 0 ? [] : AskImports(found, current);
                if (imports == null)
                    return;
            }
            data = Dependency.AppendTable(data, imports, Entry.Console);
        }

        Entry.EntryBlocks[0].Data = data;
        Entry.Dependencies.Clear(); // A BND1 import list would hide the table
        Entry.DependencyCount = (short)imports.Count;
        Entry.DependenciesListOffset = imports.Count > 0 ? data.Length - imports.Count * Dependency.Size : 0;
        Entry.Dirty = true;

        UpdateDisplay();
    }

    private List<Dependency> AskImports(List<Dependency> found, List<Dependency> current)
    {
        static string Imports(int count) => count == 1 ? "1 import" : $"{count} imports";

        TaskDialogCommandLinkButton useFound = new($"Use the {Imports(found.Count)} at the end of the file", "The file ends with its own import table.");
        TaskDialogCommandLinkButton keep = new($"Keep the resource's {Imports(current.Count)}", "The file is the resource without its import table.");
        TaskDialogCommandLinkButton none = new("No imports", "The new resource doesn't import anything.");

        TaskDialogPage page = new()
        {
            Caption = "Import Data",
            Heading = found.Count > 0
                ? $"This file seems to end with {Imports(found.Count)}"
                : $"This file doesn't end with the resource's {Imports(current.Count)}",
            Text = "There's no _imports.yaml next to it to say which imports it has.",
            Icon = TaskDialogIcon.Warning,
            AllowCancel = true
        };

        if (found.Count > 0)
        {
            page.Buttons.Add(useFound);
            page.Expander = new TaskDialogExpander(string.Join("\n", found.Select(d =>
                $"0x{d.EntryPointerOffset:X8} -> {d.ID:X8}" + (Entry.Archive.GetEntryByID(d.ID) is BundleEntry target ? $" ({target.Type} in this bundle)" : ""))))
            {
                CollapsedButtonText = "Show imports",
                ExpandedButtonText = "Hide imports"
            };
        }
        if (current.Count > 0)
            page.Buttons.Add(keep);
        page.Buttons.Add(none);
        page.Buttons.Add(TaskDialogButton.Cancel);

        TaskDialogButton result = TaskDialog.ShowDialog(this, page);
        return result == useFound ? found : result == keep ? current : result == none ? [] : null;
    }

    private void exportDataToolStripMenuItem_Click(object sender, EventArgs e)
    {
        string[] names = ExportNames(secondary: false);

        // The native dialog so the name can follow the type
        string path = NativeSaveDialog.Show(this, names[0], "dat", DataFileFilter,
            (type, name) => NameForType(type, name, names), out int typeIndex);
        if (path == null)
            return;

        if (typeIndex == YAPFilterIndex)
            ResourceImport.WritePrimary(path, Entry);
        else
            ResourceImport.WritePrimaryWithTable(path, Entry);
    }

    private string[] ExportNames(bool secondary)
    {
        string id = Entry.ID.ToString("X8");
        byte[] bytes = BitConverter.GetBytes((uint)Entry.ID);
        if (Entry.Console)
            Array.Reverse(bytes);
        string dgi = string.Join("_", bytes.Select(b => b.ToString("X2")));

        if (secondary)
        {
            string dgiSuffix = Entry.Type switch
            {
                EntryType.Texture => "_texture",
                EntryType.Renderable => "_model",
                _ => "_secondary"
            };
            return [$"{id}_secondary", $"{id}_secondary", dgi + dgiSuffix];
        }

        bool hasSecondary = Entry.HasSection(1) || Entry.HasSection(2);
        return [hasSecondary ? $"{id}_primary" : id, id, dgi];
    }

    // Swaps only the default names, keeping any extension in the box
    private static string NameForType(int type, string name, string[] names)
    {
        string extension = Path.GetExtension(name);
        string stem = name[..^extension.Length];
        return (names.Contains(stem) ? names[type - 1] : stem) + extension;
    }

    private void importExtraToolStripMenuItem_Click(object sender, EventArgs e)
    {
        OpenFileDialog ofd = new();
        ofd.FileName = $"{Entry.ID.ToString("X8")}_secondary";
        ofd.DefaultExt = "dat";
        ofd.Filter = DataFileFilter;
        ofd.FileOk += Ofd_FileOk2;
        ofd.ShowDialog(this);
    }

    private void Ofd_FileOk2(object sender, CancelEventArgs e)
    {
        OpenFileDialog ofd = (OpenFileDialog)sender;

        if (ofd.FileName == null || ofd.FileName.Length <= 0 || !File.Exists(ofd.FileName))
            return;

        Stream s = ofd.OpenFile();
        MemoryStream ms = new();
        s.CopyTo(ms);
        s.Close();
        byte[] data = ms.ToArray();
        ms.Close();

        Entry.EntryBlocks[Entry.SecondaryBlock].Data = data;
        Entry.Dirty = true;

        UpdateDisplay();
    }

    private void exportExtraToolStripMenuItem_Click(object sender, EventArgs e)
    {
        string[] names = ExportNames(secondary: true);

        string path = NativeSaveDialog.Show(this, names[0], "dat", DataFileFilter,
            (type, name) => NameForType(type, name, names), out _);
        if (path != null)
            File.WriteAllBytes(path, Entry.EntryBlocks[Entry.SecondaryBlock].Data);
    }

    private void editId_Click(object sender, EventArgs e)
    {
        string value = InputDialog.ShowInput(this, "Please enter the value to hash.");
        if (value == null)
            return;
        ulong result = Crc32.HashCrc32B(value.ToLower());
        Entry.Dirty = true;
        Entry.ID = result;
        UpdateDisplay();
        MessageBox.Show(this, "The new hashed value is: " + result.ToString("X8"), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void calcLookup8_Click(object sender, EventArgs e)
    {
        string value = InputDialog.ShowInput(this, "Please enter the value to get the lookup 8.");
        if (value == null)
            return;
        ulong result = Utilities.CalcLookup8(value);
        UpdateDisplay();
        MessageBox.Show(this, "The lookup 8 hashed value is: " + result.ToString("X16"), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        FocusTab();
    }

    private void tabList_SelectedIndexChanged(object sender, EventArgs e)
    {
        FocusTab();
    }

    private void EntryEditor_Shown(object sender, EventArgs e)
    {
        FocusTab();
    }

    private struct Vertex
    {
        public float X, Y, Z;
    }

    private void infoToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (Entry.Type != EntryType.Renderable)
            return;

        string modelInfo = "";

        byte[] header = Entry.EntryBlocks[0].Data;

        MemoryStream ms = new(header);
        BinaryReader2 br = new(ms);
        br.BigEndian = Entry.Console;

        br.BaseStream.Position = 0x24;
        int offset = br.ReadInt32();
        br.BaseStream.Position = offset;

        int vertexBlockOff = br.ReadInt32();
        int dummy = br.ReadInt32();
        int vertexBlockSize = br.ReadInt32();

        long paddingCount = 16 - (br.BaseStream.Position % 16);
        br.BaseStream.Position += paddingCount;

        long blablaOff = br.BaseStream.Position;

        br.BaseStream.Position = blablaOff + 0x4C;
        int vertexCount = br.ReadInt32();
        br.BaseStream.Position = blablaOff + 0x54;
        int polyCount = br.ReadInt32();
        int indexCount = polyCount * 3;

        br.Close();
        ms.Close();

        modelInfo = "Model Info:\n";
        modelInfo += "Vertex Block Offset: " + vertexBlockOff.ToString("X8") + "\n";
        modelInfo += "Vertex Block Size: " + vertexBlockSize.ToString("X8") + "\n";
        modelInfo += "Vertex Count: " + vertexCount.ToString() + "\n";
        modelInfo += "Polygon Count: " + polyCount.ToString() + "\n";
        modelInfo += "Index Count: " + indexCount.ToString() + "\n";

        MessageBox.Show(this, modelInfo, "DEBUG", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
