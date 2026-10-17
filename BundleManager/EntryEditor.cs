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

namespace BundleManager
{
    public partial class EntryEditor : Form
    {
        private const string DataFileFilter = "Binary Files (Bundle Manager & YAP)|*.dat|Binary Files (Legacy BM & bnd2-manager)|*.bin;*.dat|Binary Files (DGI)|*.dat";
        private const int YAPFilterIndex = 1; // Legacy and DGI keep the import table at the end of the primary data, without a yaml

        private delegate BundleEntry GetEntry();
        private delegate void SetEntry(BundleEntry entry);

        private BundleEntry _entry;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public BundleEntry Entry
        {
            get
            {
                if (InvokeRequired)
                {
                    GetEntry method = () =>
                    {
                        return _entry;
                    };
                    return (BundleEntry)Invoke(method);
                }
                else
                {
                    return _entry;
                }
            }
            set
            {
                if (InvokeRequired)
                {
                    SetEntry method = (BundleEntry entry) =>
                    {
                        _entry = entry;
                        Task.Run(() => UpdateDisplay());
                    };
                    Invoke(method, value);
                }
                else
                {
                    _entry = value;
                    UpdateDisplay();
                }
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

            ImageMenuVisible = false;
            BinaryMenuVisible = false;
            ImageVisible = false;
            ImageStatusPanelVisible = false;
            TabsVisible = false;

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

        private delegate byte[] GetDataHex();
        private delegate void SetDataHex(byte[] hex);
        private byte[] DataHex
        {
            get
            {
                if (hexData.InvokeRequired)
                {
                    GetDataHex method = () =>
                    {
                        return hexData.HexData;
                    };
                    return (byte[])Invoke(method);
                }
                else
                {
                    return hexData.HexData;
                }
            }
            set
            {
                if (hexData.InvokeRequired)
                {
                    SetDataHex method = (byte[] hex) =>
                    {
                        hexData.HexData = hex;
                    };
                    Invoke(method, value);
                }
                else
                {
                    hexData.HexData = value;
                }
            }
        }

        private byte[] ExtraDataHex
        {
            get
            {
                if (hexExtraData.InvokeRequired)
                {
                    GetDataHex method = () =>
                    {
                        return hexExtraData.HexData;
                    };
                    return (byte[])Invoke(method);
                }
                else
                {
                    return hexExtraData.HexData;
                }
            }
            set
            {
                if (hexExtraData.InvokeRequired)
                {
                    SetDataHex method = (byte[] hex) =>
                    {
                        hexExtraData.HexData = hex;
                    };
                    Invoke(method, value);
                }
                else
                {
                    hexExtraData.HexData = value;
                }
            }
        }

        private delegate Image GetImage();
        private delegate void SetImage(Image img);
        private Image Image
        {
            get
            {
                if (pboImage.InvokeRequired)
                {
                    GetImage method = () =>
                    {
                        return pboImage.Image;
                    };
                    return (Image)Invoke(method);
                }
                else
                {
                    return pboImage.Image;
                }
            }
            set
            {
                if (pboImage.InvokeRequired)
                {
                    SetImage method = (Image img) =>
                    {
                        pboImage.Image = img;
                    };
                    Invoke(method, value);
                }
                else
                {
                    pboImage.Image = value;
                }
            }
        }

        private delegate bool GetBool();
        private delegate void SetBool(bool visible);
        private delegate string GetString();
        private delegate void SetString(string text);
        private delegate TabControl.TabPageCollection GetTabPages();
        private delegate TabControl GetTabControl();
        private delegate TabPage GetTabPage();
        private bool ImageVisible
        {
            get
            {
                if (pboImage.InvokeRequired)
                {
                    GetBool method = () =>
                    {
                        return pboImage.Visible;
                    };
                    return (bool)Invoke(method);
                }
                else
                {
                    return pboImage.Visible;
                }
            }
            set
            {
                if (pboImage.InvokeRequired)
                {
                    SetBool method = (bool visible) =>
                    {
                        pboImage.Visible = visible;
                    };
                    Invoke(method, value);
                }
                else
                {
                    pboImage.Visible = value;
                }
            }
        }

        private delegate void SetTitleText(string val);
        private string Title
        {
            set
            {
                if (InvokeRequired)
                {
                    SetTitleText method = (string val) =>
                    {
                        Text = val;
                    };
                    Invoke(method, value);
                }
                else
                {
                    Text = value;
                }
            }
        }

        private bool TabsVisible
        {
            get
            {
                if (tabList.InvokeRequired)
                {
                    GetBool method = () =>
                    {
                        return tabList.Visible;
                    };
                    return (bool)Invoke(method);
                }
                else
                {
                    return tabList.Visible;
                }
            }
            set
            {
                if (tabList.InvokeRequired)
                {
                    SetBool method = (bool visible) =>
                    {
                        tabList.Visible = visible;
                    };
                    Invoke(method, value);
                }
                else
                {
                    tabList.Visible = value;
                }
            }
        }

        private bool ImageStatusPanelVisible
        {
            get
            {
                if (mnuBar.InvokeRequired)
                {
                    GetBool method = () =>
                    {
                        return imageStatusLabel.Visible;
                    };
                    return (bool)Invoke(method);
                }
                else
                {
                    return imageStatusLabel.Visible;
                }
            }
            set
            {
                if (mnuBar.InvokeRequired)
                {
                    SetBool method = (bool enabled) =>
                    {
                        imageStatusLabel.Visible = enabled;
                    };
                    Invoke(method, value);
                }
                else
                {
                    imageStatusLabel.Visible = value;
                }
            }
        }

        private bool ImageMenuVisible
        {
            get
            {
                if (mnuBar.InvokeRequired)
                {
                    GetBool method = () =>
                    {
                        return imageToolStripMenuItem.Visible;
                    };
                    return (bool)Invoke(method);
                }
                else
                {
                    return imageToolStripMenuItem.Visible;
                }
            }
            set
            {
                if (mnuBar.InvokeRequired)
                {
                    SetBool method = (bool enabled) =>
                    {
                        imageToolStripMenuItem.Visible = enabled;
                    };
                    Invoke(method, value);
                }
                else
                {
                    imageToolStripMenuItem.Visible = value;
                }
            }
        }

        private bool BinaryMenuVisible
        {
            get
            {
                if (mnuBar.InvokeRequired)
                {
                    GetBool method = () =>
                    {
                        return binaryToolStripMenuItem.Visible;
                    };
                    return (bool)Invoke(method);
                }
                else
                {
                    return mnuBar.Visible;
                }
            }
            set
            {
                if (mnuBar.InvokeRequired)
                {
                    SetBool method = (bool visible) =>
                    {
                        binaryToolStripMenuItem.Visible = visible;
                    };
                    Invoke(method, value);
                }
                else
                {
                    mnuBar.Visible = value;
                }
            }
        }

        private bool MenuVisible
        {
            get
            {
                if (mnuBar.InvokeRequired)
                {
                    GetBool method = () =>
                    {
                        return mnuBar.Visible;
                    };
                    return (bool)Invoke(method);
                }
                else
                {
                    return mnuBar.Visible;
                }
            }
            set
            {
                if (mnuBar.InvokeRequired)
                {
                    SetBool method = (bool visible) =>
                    {
                        mnuBar.Visible = visible;
                    };
                    Invoke(method, value);
                }
                else
                {
                    mnuBar.Visible = value;
                }
            }
        }

        private string InfoText
        {
            get
            {
                if (mnuBar.InvokeRequired)
                {
                    GetString method = () =>
                    {
                        return txtInfo.Text;
                    };
                    return (string)Invoke(method);
                }
                else
                {
                    return txtInfo.Text;
                }
            }
            set
            {
                if (mnuBar.InvokeRequired)
                {
                    SetString method = (string text) =>
                    {
                        txtInfo.Text = text;
                    };
                    Invoke(method, value);
                }
                else
                {
                    txtInfo.Text = value;
                }
            }
        }

        private string GetInfo()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("ID: 0x" + _entry.ID.ToString("X8"));
            sb.AppendLine("Type: " + _entry.Type);
            sb.AppendLine("Has Header: " + _entry.HasHeader);
            sb.AppendLine("Has Body: " + _entry.HasBody);
            List<BundleDependency> dependencies = _entry.GetDependencies();
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

        private void UpdateDisplay()
        {
            Title = "Edit Entry: " + _entry.Index.ToString("d3");

            if (ForceHex)
            {
                ImageVisible = false;
            }
            else if (_entry.Type == EntryType.Texture)
            {
                Image = GameImage.GetImage(_entry.EntryBlocks[0].Data, _entry.EntryBlocks[1].Data);

                ImageVisible = Image != null;

            }
            TabsVisible = !ImageVisible;
            ImageMenuVisible = ImageVisible;
            ImageStatusPanelVisible = ImageVisible;
            BinaryMenuVisible = TabsVisible;

            if (ImageStatusPanelVisible)
            {
                ImageHeader header = GameImage.GetImageHeader(_entry.EntryBlocks[0].Data);
                imageStatusLabel.Text = 
                    $"Platform: {header.Platform}    " +
                    $"Size: {header.Width}x{header.Height}    " +
                    $"Format: {header.CompressionType}";
            }

            if (TabsVisible)
            {
                DataHex = _entry.EntryBlocks[0].Data;
                ExtraDataHex = _entry.EntryBlocks[_entry.SecondaryBlock].Data;
            }

            MenuVisible = true;

            InfoText = GetInfo();
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
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

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = GameImage.FileFilter + "|All files (*.*)|*.*";
            ofd.FileOk += Ofd_FileOk;
            ofd.ShowDialog(this);
        }

        private void Ofd_FileOk(object sender, CancelEventArgs e)
        {
            OpenFileDialog ofd = (OpenFileDialog)sender;

            if (ofd.FileName == null || ofd.FileName.Length <= 0 || !File.Exists(ofd.FileName))
                return;

            string path = ofd.FileName;
            Task.Run(() => ImportImage(path));
        }

        private void ImportImage(string path)
        {
            _entry.Dirty = true;

            ImageVisible = false;
            TabsVisible = false;
            ImageMenuVisible = false;
            BinaryMenuVisible = false;

            ImageHeader header = GameImage.GetImageHeader(_entry.EntryBlocks[0].Data);

            ImageInfo info = GameImage.SetImage(path, header.CompressionType, header.Platform);

            Entry.EntryBlocks[0].Data = info.Header;
            Entry.EntryBlocks[1].Data = info.Data;

            ImageVisible = false;
            TabsVisible = false;
            ImageMenuVisible = false;
            BinaryMenuVisible = false;
            Task.Run(() => UpdateDisplay());
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
                List<Dependency> current = Entry.GetDependencies().Select(d => new Dependency { ID = d.EntryID, EntryPointerOffset = (uint)d.EntryPointerOffset }).ToList();
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

            Task.Run(() => UpdateDisplay());
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
                File.WriteAllBytes(path, Entry.EntryBlocks[0].Data);
        }

        private string[] ExportNames(bool secondary)
        {
            string id = _entry.ID.ToString("X8");
            byte[] bytes = BitConverter.GetBytes((uint)_entry.ID);
            if (_entry.Console)
                Array.Reverse(bytes);
            string dgi = string.Join("_", bytes.Select(b => b.ToString("X2")));

            if (secondary)
            {
                string dgiSuffix = _entry.Type switch
                {
                    EntryType.Texture => "_texture",
                    EntryType.Renderable => "_model",
                    _ => "_secondary"
                };
                return [$"{id}_secondary", $"{id}_secondary", dgi + dgiSuffix];
            }

            bool hasSecondary = _entry.HasSection(1) || _entry.HasSection(2);
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
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.FileName = $"{_entry.ID.ToString("X8")}_secondary";
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
            MemoryStream ms = new MemoryStream();
            s.CopyTo(ms);
            s.Close();
            byte[] data = ms.ToArray();
            ms.Close();

            Entry.EntryBlocks[Entry.SecondaryBlock].Data = data;
            Entry.Dirty = true;

            Task.Run(() => UpdateDisplay());
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
            _entry.Dirty = true;
            Entry.ID = result;
            Task.Run(() => UpdateDisplay());
            MessageBox.Show(this, "The new hashed value is: " + result.ToString("X8"), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void calcLookup8_Click(object sender, EventArgs e)
        {
            string value = InputDialog.ShowInput(this, "Please enter the value to get the lookup 8.");
            if (value == null)
                return;
            ulong result = Utilities.CalcLookup8(value);
            Task.Run(() => UpdateDisplay());
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

            MemoryStream ms = new MemoryStream(header);
            BinaryReader2 br = new BinaryReader2(ms);
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
}
