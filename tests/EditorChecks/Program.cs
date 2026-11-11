using System.ComponentModel;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using BaseHandlers;
using BundleFormat;
using BundleManager;
using BundleUtilities;
using HexEditor;
using static BaseHandlers.Traffic;
using OpenTK.GLControl;

internal static partial class Program
{
    private static int checks;
    private static string output;
    private static bool nativeSplitters = true;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private static FieldInfo FindField(Type type, string field) => type == null
        ? null
        : type.GetField(field, Private) ?? FindField(type.BaseType, field);

    private static MethodInfo FindMethod(Type type, string method) => type == null
        ? null
        : type.GetMethod(method, Private) ?? FindMethod(type.BaseType, method);

    private static T Field<T>(object obj, string field) => (T)FindField(obj.GetType(), field).GetValue(obj);

    private static object Call(object obj, string method, params object[] args) => FindMethod(obj.GetType(), method).Invoke(obj, args);

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new(message);

        checks++;
        Console.WriteLine("PASS " + message);
    }

    private static void Equal(byte[] actual, byte[] expected, string message) => Check(actual.SequenceEqual(expected), message);

    private sealed class Bytes : HexView
    {
        public void Type(char value) => OnKeyPress(new(value));

        public void Key(Keys value) => OnKeyDown(new(value));

        public bool Input(Keys value) => IsInputKey(value);
    }

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            output = args.FirstOrDefault(arg => !arg.StartsWith("--")) ?? Path.GetFullPath("artifacts/editor-checks");
            Directory.CreateDirectory(output);
            nativeSplitters = !args.Contains("--skip-native-splitters");

            if (args.Contains("--progression-only"))
            {
                CheckProgression();
                CheckProgressionFiles(args);
                Console.WriteLine($"All {checks} progression checks passed.");
                return 0;
            }

            if (args.Contains("--particles-only"))
            {
                CheckParticles(args);
                Console.WriteLine($"All {checks} particle checks passed.");
                return 0;
            }

            if (args.Contains("--ai-props-only"))
            {
                CheckAIAndProps();
                Console.WriteLine($"All {checks} AI and prop checks passed.");
                return 0;
            }

            if (args.Contains("--traffic-workflows-only"))
            {
                CheckWorldVisualization();
                CheckTrafficWorkflows();
                Console.WriteLine($"All {checks} checks passed.");
                File.WriteAllText(Path.Combine(output, "result.txt"), $"All {checks} traffic workflow checks passed.\n");
                return 0;
            }

            CheckHex();
            CheckResources();

            if (!args.Contains("--resources-only"))
            {
                CheckProgression();
                CheckTraffic();
                CheckDialogs();
                CheckLargeTraffic();
                CheckAIAndProps();
                CheckParticles(args);
                CheckWorldVisualization();
                CheckTrafficWorkflows();
            }

            Console.WriteLine($"All {checks} checks passed.");
            File.WriteAllText(Path.Combine(output, "result.txt"), $"All {checks} checks passed. Native Windows Forms controls, synthetic resource data.\n");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void CheckHex()
    {
        using Bytes hex = new();
        hex.HexData = [0x12, 0x34, 0x56, 0x78];
        hex.Type('A');
        Equal(hex.HexData, [0x12, 0x34, 0x56, 0x78], "Read-only typing cannot alter bytes");
        hex.ReadOnly = false;
        hex.Type('A');
        hex.Type('B');
        Equal(hex.HexData, [0xAB, 0x34, 0x56, 0x78], "Hex digit pair writes one byte");
        hex.Undo();
        Equal(hex.HexData, [0x12, 0x34, 0x56, 0x78], "One undo restores the complete byte");
        Check(!hex.Modified, "Undo to the loaded baseline clears modified state");
        hex.Redo();
        hex.MarkApplied();
        hex.Undo();
        Check(hex.Modified, "Undo after Apply creates unapplied changes");
        hex.Redo();
        Check(!hex.Modified, "Redo back to Apply clears modified state");
        hex.Select(1, 2);
        hex.DeleteSelection();
        Equal(hex.HexData, [0xAB, 0, 0, 0x78], "Delete in overwrite clears bytes without resizing");
        hex.Select(1, 2);
        Check(!hex.PasteBytes([1]), "Overwrite rejects unequal selection and paste lengths");
        hex.Select(4, 0);
        hex.Type('F');
        Check(hex.HexData.Length == 4, "Typing at EOF cannot extend fixed-length data");
        Check(!hex.PasteBytes([1]), "Pasting at EOF cannot extend fixed-length data");
        hex.SetInsertMode(true);
        hex.Select(1, 0);
        hex.Type('C');
        hex.Type('D');
        Equal(hex.HexData, [0xAB, 0xCD, 0, 0, 0x78], "Explicit insert writes a complete byte");
        hex.Undo();
        Equal(hex.HexData, [0xAB, 0, 0, 0x78], "One undo removes a newly inserted byte");
        Check(!hex.Input(Keys.Tab) && !hex.Input(Keys.Shift | Keys.Tab), "Tab and Shift+Tab are available for focus navigation");
        hex.Key(Keys.F6);
        Check(hex.TextColumnActive, "F6 switches to text without trapping Tab");
        hex.Width = 260;
        Check(hex.Controls.OfType<HScrollBar>().Single().Visible, "Narrow byte views provide horizontal scrolling");
        hex.SetInsertMode(false);
        hex.HexData = [0x12, 0x34, 0x56, 0x78, 0x9A];
        hex.Select(0, 1);
        using ValueView values = new()
        {
            Source = hex
        };

        DataGridView grid = Field<DataGridView>(values, "grid");
        grid.CurrentCell = grid.Rows[4].Cells[1];
        Check(Field<Label>(values, "range").Text.Contains("4 bytes at 0x0–0x3"), "Inspector states the full Int32 write span");
        Check((int)typeof(HexView).GetField("interpretationLength", Private).GetValue(hex) == 4, "Inspector outlines all four bytes despite a one-byte selection");
        grid.Rows[4].Cells[1].Value = "1";
        Call(values, "Apply", 4);
        Equal(hex.HexData, [1, 0, 0, 0, 0x9A], "Inspector writes only its advertised span");
        hex.Undo();
        Equal(hex.HexData, [0x12, 0x34, 0x56, 0x78, 0x9A], "Inspector write undoes as one edit");
        byte[] bytes = [1, 2, 1, 2, 3];
        Check(HexNavigationDialog.Find(bytes, [1, 2], 1, false, out bool wrapped) == 2 && !wrapped, "Find Next advances to the next byte pattern");
        Check(HexNavigationDialog.Find(bytes, [1, 2], 3, false, out wrapped) == 0 && wrapped, "Find Next wraps with explicit wrap state");
        Check(HexNavigationDialog.Find(bytes, [1, 2], -1, true, out wrapped) == 2 && wrapped, "Find Previous wraps backwards");
        Check(HexNavigationDialog.Find(bytes, [9], 0, false, out _) == -1, "Search reports missing patterns");
        Equal(HexNavigationDialog.ParsePattern("0A ff\n20", 0), [10, 255, 32], "Hex search accepts spaced byte pairs");
        Equal(HexNavigationDialog.ParsePattern("é", 1), [0xC3, 0xA9], "Text search uses the selected UTF-8 encoding");
        hex.HexData = [0, 0, 0, 0];
        values.BigEndian = true;
        grid.CurrentCell = grid.Rows[4].Cells[1];
        grid.Rows[4].Cells[1].Value = "16909060";
        Call(values, "Apply", 4);
        Equal(hex.HexData, [1, 2, 3, 4], "Big-endian inspector writes the displayed byte order");
        hex.Undo();
        Equal(hex.HexData, [0, 0, 0, 0], "Big-endian inspector edits remain undoable");
    }

    private static BundleEntry Entry(byte[] bytes)
    {
        BundleArchive archive = new()
        {
            Platform = BundlePlatform.PC
        };

        BundleEntry entry = new(archive)
        {
            ID = 0x12345678,
            Type = EntryType.TrafficData,
            Platform = BundlePlatform.PC,
            EntryBlocks = [new()
            {
                Data = bytes
            }, new()
            {
                Data = []
            }, new()
            {
                Data = []
            }]
        };

        archive.Entries.Add(entry);
        return entry;
    }

    private static void Hidden(Form form)
    {
        form.ShowInTaskbar = false;
        form.StartPosition = FormStartPosition.Manual;
        form.Location = Point.Empty;
        form.Opacity = 0;
        form.Show();
        Application.DoEvents();
    }

    private static void Capture(Form form, string name)
    {
        using Bitmap image = new(form.Width, form.Height);
        form.DrawToBitmap(image, new(Point.Empty, image.Size));
        image.Save(Path.Combine(output, name));
    }

    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(c => (Control[])[c, .. Descendants(c)]);

    private static void CheckResources()
    {
        byte[] initial = [.. Enumerable.Range(0, 256).Select(i => (byte)i)];
        BundleEntry entry = Entry(initial);
        using EntryEditor editor = new()
        {
            ForceHex = true,
            Entry = entry
        };

        Hidden(editor);
        HexView hex = Field<HexView>(editor, "hexData");
        TabControl tabs = Field<TabControl>(editor, "tabList");
        Check(tabs.SelectedTab.Text == "info", "Resource editor opens on Info");
        Capture(editor, "resource-info.png");
        Check(!ReferenceEquals(editor.Entry, entry) && !ReferenceEquals(editor.Entry.EntryBlocks[0], entry.EntryBlocks[0]), "Resource draft owns separate buffers and blocks");
        tabs.SelectedTab = Field<TabPage>(editor, "tabPrimary");
        hex.Focus();
        Message tab = Message.Create(hex.Handle, 0x100, (nint)Keys.Tab, 0);
        hex.PreProcessMessage(ref tab);
        Check(!hex.ContainsFocus, "A real Tab preprocessing cycle leaves the byte canvas");
        Field<ToolStripMenuItem>(editor, "editModeToolStripMenuItem").Checked = true;
        hex.Overwrite(0, [0xFE]);
        tabs.SelectedTab = Field<TabPage>(editor, "tabImports");
        Capture(editor, "resource-imports.png");
        Equal(entry.GetPrimaryWithoutImports(), initial, "Switching tabs does not apply draft bytes to the bundle");
        tabs.SelectedTab = Field<TabPage>(editor, "tabPrimary");
        Check((bool)Call(editor, "ApplyDraft"), "Apply accepts a valid resource draft");
        Check(entry.GetPrimaryWithoutImports()[0] == 0xFE && entry.Dirty && !hex.Modified, "Apply updates the bundle and resets the editor baseline");
        hex.Undo();
        Check(hex.Modified && entry.GetPrimaryWithoutImports()[0] == 0xFE, "Undo after Apply does not mutate already applied bundle data");
        Check((bool)Call(editor, "ApplyDraft") && entry.GetPrimaryWithoutImports()[0] == 0, "Applying an undo restores the earlier bytes to the bundle");
        Field<ToolStripButton>(editor, "inspectorButton").Checked = false;
        Check(Field<SplitContainer>(editor, "primarySplit").Panel2Collapsed, "Value inspector can be collapsed to give bytes more space");
        Field<ToolStripButton>(editor, "inspectorButton").Checked = true;

        foreach (int width in (int[])[1000, 800, 600])
        {
            editor.ClientSize = new(width, 600);
            Application.DoEvents();

            if (width < 1000)
                Check(hex.Controls.OfType<HScrollBar>().Single().Visible, $"Resource bytes remain scrollable at {width}px");

            Capture(editor, $"hex-{width}.png");
        }

        editor.Font = new(SystemFonts.MessageBoxFont.FontFamily, 12);
        editor.ClientSize = new(800, 650);
        Application.DoEvents();
        Check(Field<Button>(editor, "applyButton").Bounds.Width > 0 && Field<Button>(editor, "applyButton").Visible,
            "Apply remains available with enlarged system-style fonts");
        Capture(editor, "hex-large-font.png");
        hex.Overwrite(1, [0xEE]);
        Descendants(editor).OfType<Button>().Single(b => b.Text == "Cancel").PerformClick();
        Check(entry.GetPrimaryWithoutImports()[1] == 1, "Cancel discards bytes edited after Apply");
    }

    private static Traffic Sample()
    {
        Traffic data = new()
        {
            muDataVersion = 45
        };

        Hull hull = new();
        SectionSpan span = new()
        {
            muMaxVehicles = 10
        };

        VehicleAsset asset = new()
        {
            mVehicleId = 0xABCD
        };

        VehicleType car = new()
        {
            mAsset = asset,
            mTraits = new()
        };

        FlowType flow = new()
        {
            maVehicleTypes = [new()
            {
                mVehicleType = car,
                muCumulativeProb = 255
            }]
        };

        data.maVehicleAssets.Add(asset);
        data.maVehicleTraits.Add(car.mTraits);
        data.maVehicleTypes.Add(car);
        data.maFlowTypes.Add(flow);
        hull.maVehicleAssets.Add(asset);
        Section section = new()
        {
            mSpan = span,
            mFlowType = flow,
            mfSpeed = 20,
            mfLength = 100,
            muVehiclesPerMinute = 15
        };

        foreach (int x in (int[])[0, 50, 100])
            section.maRungs.Add(new()
            {
                maPoints = [new(x, 0, 0, 1), new(x, 0, 4, 1)],
                mfCumulativeLength = x
            });

        hull.maSections.Add(section);
        hull.maSectionSpans.Add(span);
        data.maHulls.Add(hull);
        return data;
    }

    private static void CheckTraffic()
    {
        TrafficLabels.Install();
        Traffic data = Sample();
        TrafficReferences.Data = data;
        BundleEntry entry = Entry(data.Serialize(false, 4));
        Check(TrafficRules.Validate(data, entry).Count == 0, "Representative traffic graph validates");
        Section section = data.maHulls[0].maSections[0];
        SectionSpan span = section.mSpan;
        using ObjectTreeEditor tree = new(data, "Traffic");
        tree.SelectObject(span);
        Check(!tree.TryDeleteSelected(out List<ObjectGraph.Usage> references) && references.Count == 1, "Deleting a referenced traffic capacity is blocked before mutation");
        Check(data.maHulls[0].maSectionSpans.Contains(span) && !tree.Modified, "Blocked deletion leaves data and history unchanged");
        section.mfSpeed = 30;
        tree.RecordChange();
        tree.Undo();
        Check(section.mfSpeed == 20 && !tree.Modified, "Traffic property undo restores the original value and clean state");
        tree.Redo();
        tree.MarkApplied();
        tree.Undo();
        Check(tree.Modified && section.mfSpeed == 20, "Traffic undo after Apply creates an unapplied change");
        tree.Redo();
        Check(!tree.Modified, "Traffic redo to Apply clears dirty state");
        tree.SelectObject(section);
        Call(tree, "Duplicate");
        Check(data.maHulls[0].maSections.Count == 2, "Duplicate adds a traffic object");
        tree.Undo();
        Check(data.maHulls[0].maSections.Count == 1 && ReferenceEquals(section.mSpan, span), "Undo duplicate retains original object relationships");
        tree.Redo();
        Check(data.maHulls[0].maSections.Count == 2, "Redo restores duplicated traffic object");
        tree.SelectObject(data.maHulls[0].maSections[1]);
        Check(tree.TryDeleteSelected(out _) && data.maHulls[0].maSections.Count == 1, "Unreferenced traffic items can be deleted");
        tree.Undo();
        Check(data.maHulls[0].maSections.Count == 2, "Traffic deletion is undoable");
        Vector4 originalPoint = section.maRungs[0].maPoints[0];
        section.maRungs[0].maPoints[0] = Vector4.One;
        tree.RecordChange();
        tree.Undo();
        Check(section.maRungs[0].maPoints[0] == originalPoint, "Undo restores in-place geometry array edits");
        section.mForwardStraightOn = section;
        Traffic copy = (Traffic)ObjectTreeEditor.Clone(data);
        Check(ReferenceEquals(copy.maHulls[0].maSections[0].mForwardStraightOn, copy.maHulls[0].maSections[0]), "Draft clone relinks cyclic references within the copied graph");
        Hull otherHull = new();
        Section external = new()
        {
            mForwardLeft = section
        };

        otherHull.maSections.Add(external);
        data.maHulls.Add(otherHull);
        Check(ObjectGraph.Incoming(data, data.maHulls[0]).Any(u => u.Node.Value == external), "Deleting a road area detects references to its descendants from other areas");
        data.maHulls.Remove(otherHull);
        PropertyDescriptor speed = TypeDescriptor.GetProperties(section)["mfSpeed"];
        Check(speed.DisplayName == "Speed (game units)" && speed.Category == "Traffic flow", "Traffic properties use task labels and categories");
        Check(TypeDescriptor.GetProperties(data.maHulls[0])["maVehicleAssets"].GetEditor(typeof(System.Drawing.Design.UITypeEditor)) is TrafficReferenceEditor, "Multiple references use the named picker");
        VehicleType a = new(), currentB = new();
        List<FlowVehicleType> mix = TrafficMixDialog.Compile([(a: a, 25m), (b: currentB, 75m)]);
        Check(mix[0].muCumulativeProb == 64 && mix[1].muCumulativeProb == 255, "Individual vehicle chances convert to cumulative game encoding");
        data.maVehicleAssets.Add(new()
        {
            mVehicleId = 0x1234
        });
        data.maVehicleTraits.Add(new());
        a.mAsset = currentB.mAsset = data.maVehicleAssets[0];
        a.mTraits = currentB.mTraits = data.maVehicleTraits[0];
        data.maVehicleTypes.AddRange([a, currentB]);
        FlowType flow = new()
        {
            maVehicleTypes = mix
        };

        data.maFlowTypes.Add(flow);
        ObjectHistory mixHistory = new(data);
        List<FlowVehicleType> changedMix = TrafficMixDialog.Compile([(a: a, 60m), (b: currentB, 40m)]);
        TypeDescriptor.GetProperties(flow)["VehicleMix"].SetValue(flow, changedMix);
        mixHistory.Record();
        mixHistory.Undo();
        Check(ReferenceEquals(flow.maVehicleTypes, mix) && flow.maVehicleTypes[0].muCumulativeProb == 64, "Vehicle mix changes undo as one operation with original references");
        mixHistory.Redo();
        Check(flow.maVehicleTypes[0].muCumulativeProb == 153, "Vehicle mix redo restores the accepted chances");
        BundleEntry mixEntry = Entry(data.Serialize(false, 4));
        Traffic mixRead = new();
        mixRead.Read(mixEntry);
        int mixIndex = data.maFlowTypes.IndexOf(flow);
        Check(mixRead.maFlowTypes[mixIndex].maVehicleTypes[0].muCumulativeProb == 153 && mixRead.maFlowTypes[mixIndex].maVehicleTypes[1].muCumulativeProb == 255,
            "Vehicle chances survive serialization and reopening");
        using TrafficView view = new(data);
        object beforeTarget = FindField(typeof(TrafficView), "target").GetValue(view);
        float beforeDistance = Field<float>(view, "distance");
        view.Highlight(section);
        section.mfSpeed = 40;
        view.Rebuild();
        view.Highlight(section);
        Check(Equals(beforeTarget, FindField(typeof(TrafficView), "target").GetValue(view)) && beforeDistance == Field<float>(view, "distance"), "Traffic selection and property updates preserve the camera");
        GLControl glForPick = Field<OpenTK.GLControl.GLControl>(view, "gl");
        glForPick.Size = new(800, 500);
        OpenTK.Mathematics.Matrix4 matrix = (OpenTK.Mathematics.Matrix4)Call(view, "ViewProjection");
        OpenTK.Mathematics.Vector4 projected = new OpenTK.Mathematics.Vector4(25, 0, 0, 1) * matrix;
        object[] picked = null;
        view.Picked += path => picked = path;
        Call(view, "Pick", new Point((int)((projected.X / projected.W + 1) * 400), (int)((1 - projected.Y / projected.W) * 250)));
        Check(picked?.Last() is Section, "Clicking a visible lane edge between its points selects a section");
        using Form host = new()
        {
            Text = "Traffic properties",
            ClientSize = new(650, 800)
        };

        tree.SelectObject(section);

        if (nativeSplitters)
        {
            host.Controls.Add(tree);
            Hidden(host);
            Capture(host, "traffic-properties.png");
            host.Controls.Remove(tree);
        }

        TextBox search = Field<TextBox>(tree, "search");
        search.Text = "Road sections";
        Check(Field<ListBox>(tree, "results").Items.Count > 0, "Traffic search finds sections using readable navigation labels");
        host.Dispose();

        Traffic source = Sample();
        byte[] baseline = source.Serialize(false, 4);
        BundleEntry target = Entry(baseline);
        using TrafficEditor editor = new(source, target, false);
        Traffic draft = Field<Traffic>(editor, "data");
        ObjectTreeEditor editTree = Field<ObjectTreeEditor>(editor, "tree");
        draft.maHulls[0].maSections[0].mfSpeed = 55;
        editTree.RecordChange();
        Equal(target.GetPrimaryWithoutImports(), baseline, "Traffic edits remain isolated until Apply");
        Check(source.maHulls[0].maSections[0].mfSpeed == 20, "Traffic draft does not mutate the original handler model");
        Check((bool)Call(editor, "ApplyChanges"), "Traffic Apply accepts valid edits");
        Traffic reread = new();
        reread.Read(target);
        Check(reread.maHulls[0].maSections[0].mfSpeed == 55 && target.Dirty && !editTree.Modified, "Traffic Apply serializes the new value and marks the bundle dirty");
        draft.maHulls[0].maSections[0].mSpan = null;
        editTree.RecordChange();
        Check(!(bool)Call(editor, "ApplyChanges"), "Traffic Apply blocks incomplete required references");
        Check(Field<ListBox>(editor, "issues").Items.Count > 0, "Traffic reports repairable validation issues before close");
        editTree.Undo();
        Check(TrafficRules.Validate(draft, target).Count == 0, "Undo repairs an invalid traffic draft");
        editTree.SelectObject(draft.maHulls[0].maSections[0]);
        TrafficView shownView = Field<TrafficView>(editor, "view");

        if (nativeSplitters)
        {
            Hidden(editor);
            Application.DoEvents();
            Capture(editor, "traffic-full.png");

            if (Field<object>(shownView, "shader") != null)
            {
                Check(true, "Traffic viewport renders with a real OpenGL context");
                CaptureViewport(shownView);
                File.WriteAllText(Path.Combine(output, "viewport-state.txt"), "OpenGL context and visible GPU geometry verified.");
            }
            else
            {
                Check(Field<FlowLayoutPanel>(shownView, "unavailable").Visible && OpenTK.Windowing.Desktop.Monitors.GetMonitors().Count == 0,
                    "Disconnected displays show a usable traffic editor instead of creating an invalid GL window");
                File.WriteAllText(Path.Combine(output, "viewport-state.txt"), "No connected GLFW monitor. Disconnected-display fallback verified; GPU capture unavailable in this run.");
            }

            editor.ClientSize = new(800, 600);
            Application.DoEvents();
            Capture(editor, "traffic-800.png");
        }
        else
        {
            Console.WriteLine("SKIP Traffic splitter-window and GPU captures (--skip-native-splitters).");
            File.WriteAllText(Path.Combine(output, "viewport-state.txt"), "Native SplitContainer window captures and GPU rendering were skipped explicitly. Data, commands and drag math still tested.");
        }

        draft.maHulls[0].maSections[0].mfSpeed = 66;
        editTree.RecordChange();
        Call(Descendants(editor).OfType<Button>().Single(b => b.Text == "Cancel"), "OnClick", EventArgs.Empty);
        Traffic afterCancel = new();
        afterCancel.Read(target);
        Check(afterCancel.maHulls[0].maSections[0].mfSpeed == 55, "Traffic Cancel discards changes after Apply");
    }

    private static void CaptureViewport(TrafficView view)
    {
        GLControl gl = Field<OpenTK.GLControl.GLControl>(view, "gl");
        gl.MakeCurrent();
        Call(view, "Render");
        OpenTK.Graphics.OpenGL.GL.ReadBuffer(OpenTK.Graphics.OpenGL.ReadBufferMode.Front);
        byte[] pixels = new byte[gl.Width * gl.Height * 4];
        OpenTK.Graphics.OpenGL.GL.ReadPixels(0, 0, gl.Width, gl.Height, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, OpenTK.Graphics.OpenGL.PixelType.UnsignedByte, pixels);
        Check(pixels.Where((_, i) => i % 4 != 3).Any(b => b > 180), "Traffic GPU framebuffer contains visible lane geometry");
        using Bitmap image = new(gl.Width, gl.Height, PixelFormat.Format32bppArgb);
        BitmapData target = image.LockBits(new(Point.Empty, image.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

        for (int y = 0; y < image.Height; y++)
            Marshal.Copy(pixels, (image.Height - 1 - y) * image.Width * 4, target.Scan0 + y * target.Stride, image.Width * 4);

        image.UnlockBits(target);
        image.Save(Path.Combine(output, "traffic-viewport.png"));
    }

    private static void CheckDialogs()
    {
        using HexNavigationDialog find = new(true, 20);
        Hidden(find);
        Field<TextBox>(find, "input").Text = "0A F";
        Call(find, "Submit");
        Check(find.DialogResult != DialogResult.OK, "Find preserves invalid hex input for correction");
        Capture(find, "find-validation.png");
        using HexNavigationDialog offset = new(false, 20);
        Field<TextBox>(offset, "input").Text = "100";
        Call(offset, "Submit");
        Check(offset.DialogResult != DialogResult.OK, "Go to offset rejects an out-of-range address instead of clamping");
        Field<ComboBox>(offset, "format").SelectedIndex = 1;
        Field<TextBox>(offset, "input").Text = "10";
        Call(offset, "Submit");
        Check(offset.Offset == 10 && offset.DialogResult == DialogResult.OK, "Go to offset honors explicit decimal input");

        Traffic data = Sample();
        TrafficReferences.Data = data;

        data.maVehicleAssets.Clear();
        data.maHulls[0].maVehicleAssets.Clear();
        data.maVehicleAssets.Add(new()
        {
            mVehicleId = 0x1234
        });
        data.maVehicleAssets.Add(new()
        {
            mVehicleId = 0xABCD
        });
        Hull hull = data.maHulls[0];
        hull.maVehicleAssets.Add(data.maVehicleAssets[1]);
        using TrafficReferenceDialog picker = new(hull, "maVehicleAssets", typeof(VehicleAsset), true, hull.maVehicleAssets);
        Hidden(picker);
        ListBox candidates = Field<ListBox>(picker, "candidates");
        candidates.SelectedIndex = 0;
        Call(picker, "Add");
        Check(picker.SelectedValues.SequenceEqual([data.maVehicleAssets[1], data.maVehicleAssets[0]]), "Reference picker preserves existing order when adding named items");
        Check(hull.maVehicleAssets.Count == 1, "Reference picker leaves the model untouched until accepted");
        Capture(picker, "traffic-reference-picker.png");
        picker.Dispose();
        VehicleType a = new()
        {
            mAsset = data.maVehicleAssets[0]
        }, b = new()
        {
            mAsset = data.maVehicleAssets[1]
        };

        using TrafficMixDialog mix = new(TrafficMixDialog.Compile([(a: a, 25m), (b: b, 75m)]), [a, b]);
        Hidden(mix);
        Capture(mix, "traffic-vehicle-mix.png");
        mix.Dispose();
    }

    private static void CheckTrafficWorkflows()
    {
        TrafficLabels.Install();
        EncryptedString cgsID = new("CAR_A01");
        Check(cgsID.Value == "CAR_A01", "CgsID text encodes and decodes without losing the assigned value");
        cgsID.Value = "VAN_B02";
        Check(new EncryptedString(cgsID.Encrypted).Value == "VAN_B02", "Changing a CgsID updates its stored value");
        VehicleAsset asset = new()
        {
            mVehicleId = cgsID.Encrypted
        };

        PropertyDescriptor assetID = TypeDescriptor.GetProperties(asset)[nameof(VehicleAsset.mVehicleId)];
        Check(asset.ToString() == "VAN_B02" && assetID.Converter.ConvertToString(asset.mVehicleId) == "VAN_B02", "Vehicle asset labels and property values show decoded CgsIDs");
        Check((ulong)assetID.Converter.ConvertFromString("car_a01") == new EncryptedString("CAR_A01").Encrypted, "Vehicle CgsID editing accepts text and normalizes letter case");
        Check((ulong)assetID.Converter.ConvertFromString("0xFEDCBA9876543210") == 0xFEDCBA9876543210UL, "Raw vehicle IDs remain editable without numeric precision loss");
        Check(TrafficCgsIDConverter.Label(ulong.MaxValue) == "0xFFFFFFFFFFFFFFFF", "Vehicle IDs that cannot round trip as CgsIDs retain their exact hexadecimal representation");
        Check(TypeDescriptor.GetProperties(asset)["RawVehicleID"].IsReadOnly, "The raw vehicle ID is available as a read-only inspection field");
        bool rejected = false;

        try
        {
            assetID.Converter.ConvertFromString("CAR[A]");
        }
        catch (FormatException)
        {
            rejected = true;
        }

        Check(rejected, "Unsupported CgsID characters are rejected instead of being encoded as another name");

        Traffic data = Sample();
        assetID.SetValue(asset, assetID.Converter.ConvertFromString("car_a01"));
        data.maVehicleAssets.Add(asset);
        Traffic assetRead = new();
        assetRead.Read(Entry(data.Serialize(false, 4)));
        Check(assetRead.maVehicleAssets[data.maVehicleAssets.IndexOf(asset)].mVehicleId == new EncryptedString("CAR_A01").Encrypted, "Edited vehicle CgsIDs survive serialization and reopening unchanged");
        Hull source = data.maHulls[0], destination = new();
        data.maHulls.Add(destination);
        data.mPvs.muNumCells_X = 2;
        data.mPvs.muNumCells_Z = 1;
        data.mPvs.mCellSize = new(200, 0, 200, 0);
        data.mPvs.maHullPvs.Add(new()
        {
            maHulls = [source]
        });
        data.mPvs.maHullPvs.Add(new());
        Section original = source.maSections[0];
        Section next = new()
        {
            mSpan = original.mSpan,
            mFlowType = original.mFlowType,
            maRungs = [new()
            {
                maPoints = [new(150, 0, 0, 1), new(150, 0, 4, 1)]
            }, new()
            {
                maPoints = [new(160, 0, 0, 1), new(160, 0, 4, 1)],
                mfCumulativeLength = 10
            }]
        };

        source.maSections.Add(next);
        original.mForwardStraightOn = next;
        original.maRightNeighbours.Add(new()
        {
            mSection = next,
            mOurStartRung = original.maRungs[0],
            mTheirStartRung = next.maRungs[0]
        });

        Vector4 originalPoint = original.maRungs[0].maPoints[0];
        ObjectHistory history = new(data);
        TrafficCopy plan = new(data, original);
        plan.Translation.Apply(new(250, 4, 10));
        Check(original.maRungs[0].maPoints[0] == originalPoint && destination.maSections.Count == 0, "Copy preview leaves the source and destination untouched");
        Section copied = (Section)plan.Commit(destination, [1], false);
        Check(destination.maSections.Contains(copied) && !ReferenceEquals(copied, original), "A section can be duplicated into another road area");
        Check(destination.maSectionSpans.Contains(copied.mSpan) && !ReferenceEquals(copied.mSpan, original.mSpan), "Cross-area copies include an independent local traffic capacity");
        Check(copied.mForwardStraightOn == null && copied.maRightNeighbours.Count == 0, "Independent copies remove connections back to uncopied roads");
        Check(copied.maRungs[0].maPoints[0] == originalPoint + new Vector4(250, 4, 10, 0), "Copy offsets move XYZ while preserving the endpoint W component");
        Check(data.mPvs.maHullPvs[1].maHulls.Contains(destination), "The destination road area is assigned to the chosen visibility cell");
        Check(TrafficRules.Validate(data, Entry([])).Count == 0, "A copy into another road area passes reference validation and serialization");
        history.Record();
        history.Undo();
        Check(destination.maSections.Count == 0 && destination.maSectionSpans.Count == 0 && data.mPvs.maHullPvs[1].maHulls.Count == 0, "One undo removes copied geometry, dependencies and visibility changes");
        history.Redo();
        Check(ReferenceEquals(destination.maSections[0], copied), "Redo restores the same copied objects and their references");

        LightTriggerStartData layout = new()
        {
            maStartingPositions = [new()
            {
                mPosition = new(20, 2, 30, 1),
                mDirection = new(0, 0, 1, 0)
            }]
        };

        source.maLightTriggerStartData.Add(layout);
        TrafficLightType lightType = new();
        data.maTrafficLightTypes.Add(lightType);
        TrafficLight light = new()
        {
            muInstanceID = 100,
            mType = lightType,
            mPosAndYRotation = new(12, 3, 20, 0.75f)
        };

        source.maTrafficLights.Add(light);
        StopLine stop = new();
        original.maStopLines.Add(stop);
        JunctionLogicBox junction = new()
        {
            muID = 42,
            muEventJunctionID = 777,
            mOfflineStartData = layout,
            maStates = [new()
            {
                muStateTiming = 10
            }],
            maTrafficLightControllers = [new()
            {
                maTrafficLights = [light],
                maStopLines = [stop]
            }]
        };

        source.maJunctions.Add(junction);
        LightTrigger trigger = new()
        {
            mJunction = junction,
            mPosPlusYRot = new(10, 1, 20, 0.5f)
        };

        source.maLightTriggers.Add(trigger);
        StaticTrafficVehicle vehicle = new()
        {
            mFlowType = original.mFlowType
        };

        source.maStaticTrafficVehicles.Add(vehicle);
        Vector4 direction = layout.maStartingPositions[0].mDirection;
        Vector4 basis = vehicle.mTransform[0];
        TrafficTranslation movement = new(source);
        movement.Apply(new(5, 3, 7));
        movement.Apply(new(10, -2, 0));
        Check(original.maRungs[0].maPoints[0] == originalPoint + new Vector4(10, -2, 0, 0), "Repeated placement previews always use the original positions rather than accumulating offsets");
        Check(trigger.mPosPlusYRot.W == 0.5f && light.mPosAndYRotation.W == 0.75f && layout.maStartingPositions[0].mDirection == direction && vehicle.mTransform[0] == basis, "Moving an area preserves rotations, directions and transform basis rows");
        movement.Restore();
        Check(original.maRungs[0].maPoints[0] == originalPoint, "Cancel restores every previewed position");
        rejected = false;

        try
        {
            movement.Apply(new(float.NaN, 0, 0));
        }
        catch (ArgumentException)
        {
            rejected = true;
        }

        Check(rejected && original.maRungs[0].maPoints[0] == originalPoint, "Invalid movement is rejected before any position changes");
        ObjectHistory movementHistory = new(data);
        movement.Apply(new(10, 0, 0));
        movementHistory.Record();
        movementHistory.Undo();
        Check(original.maRungs[0].maPoints[0] == originalPoint && trigger.mPosPlusYRot.X == 10, "A placement gesture undoes as one edit across all moved objects");

        TrafficCopy areaPlan = new(data, source);
        areaPlan.Translation.Apply(new(600, 0, 0));
        Hull areaCopy = (Hull)areaPlan.Commit(null, [], false);
        Check(!ReferenceEquals(areaCopy.maJunctions[0], junction) && ReferenceEquals(areaCopy.maLightTriggers[0].mJunction, areaCopy.maJunctions[0]), "Whole-area copies reconnect triggers to the copied junctions");
        Check(ReferenceEquals(areaCopy.maJunctions[0].maTrafficLightControllers[0].maTrafficLights[0], areaCopy.maTrafficLights[0]) && ReferenceEquals(areaCopy.maJunctions[0].maTrafficLightControllers[0].maStopLines[0], areaCopy.maSections[0].maStopLines[0]), "Whole-area copies retain internal light and stop-line relationships");
        Check(areaCopy.maJunctions[0].muID != junction.muID && areaCopy.maTrafficLights[0].muInstanceID != light.muInstanceID && areaCopy.maJunctions[0].muEventJunctionID == junction.muEventJunctionID, "Copies receive unique local IDs while retaining their existing event links");
        TrafficCopy triggerPlan = new(data, trigger);
        LightTrigger triggerCopy = (LightTrigger)triggerPlan.Commit(destination, [1], false);
        Check(destination.maJunctions.Contains(triggerCopy.mJunction) && destination.maTrafficLights.Count == 1 && destination.maLightTriggerStartData.Count == 1, "Copying a trigger includes its local junction, lights and start layout");
        Check(destination.maSections.Contains(destination.maSections.First(section => section.maStopLines.Contains(triggerCopy.mJunction.maTrafficLightControllers[0].maStopLines[0]))), "Copied controllers refer to copied stop-line geometry");
        Check(TrafficRules.Validate(data, Entry([])).Count == 0, "Copied trigger dependencies and generated IDs serialize correctly");
        Traffic reopened = new();
        reopened.Read(Entry(data.Serialize(false, 4)));
        Check(reopened.maHulls.Count == data.maHulls.Count && reopened.maHulls[1].maLightTriggers.Count == 1, "Reopening the serialized traffic retains copies and their local relationships");
        Check(TrafficVisibility.CellsAt(data, new(250, 0, 10), new(350, 4, 20)).SequenceEqual([1]) && TrafficVisibility.CellCenter(data, 1).X == 300, "Cell placement uses the grid origin, dimensions and world coordinates");

        for (int index = 0; index < 7; index++)
        {
            Hull filler = new();
            data.maHulls.Add(filler);
            data.mPvs.maHullPvs[1].maHulls.Add(filler);
        }

        data.mPvs.muNumCells_X = 3;
        data.mPvs.maHullPvs.Add(new());
        int areaCount = data.maHulls.Count;
        rejected = false;

        try
        {
            new TrafficCopy(data, original).Commit(null, [2, 1], false);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        Check(rejected && data.maHulls.Count == areaCount && data.mPvs.maHullPvs[2].maHulls.Count == 0, "A full visibility cell blocks the entire duplication before any data is inserted");
        rejected = false;

        try
        {
            TrafficVisibility.CopyCells(data, 0, [2, 1], false);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        Check(rejected && data.mPvs.maHullPvs[2].maHulls.Count == 0, "Merging visibility cells validates every destination before changing any cell");
        ObjectHistory visibilityHistory = new(data);
        TrafficVisibility.CopyCells(data, 0, [2], false);
        visibilityHistory.Record();
        Check(ReferenceEquals(data.mPvs.maHullPvs[2].maHulls.Single(), source), "Copying visibility settings shares the existing road area without duplicating geometry");
        visibilityHistory.Undo();
        Check(data.mPvs.maHullPvs[2].maHulls.Count == 0, "Visibility-cell copies support undo");
        TrafficVisibility.CopyCells(data, 0, [1], true);
        Check(data.mPvs.maHullPvs[1].maHulls.SequenceEqual([source]), "Explicit replacement copies visibility settings into an occupied cell");
        ObjectHistory assignmentHistory = new(data);
        TrafficVisibility.SetArea(data, destination, [0, 2]);
        assignmentHistory.Record();
        Check(data.mPvs.maHullPvs[0].maHulls.Contains(destination) && data.mPvs.maHullPvs[2].maHulls.Contains(destination), "Road-area visibility can be assigned to several cells");
        assignmentHistory.Undo();
        Check(!data.mPvs.maHullPvs[0].maHulls.Contains(destination) && !data.mPvs.maHullPvs[2].maHulls.Contains(destination), "Visibility assignment changes undo together");

        Traffic dragSource = Sample();
        BundleEntry dragEntry = Entry(dragSource.Serialize(false, 4));
        using TrafficEditor dragEditor = new(dragSource, dragEntry, false);
        ObjectTreeEditor dragTree = Field<ObjectTreeEditor>(dragEditor, "tree");
        Traffic dragDraft = Field<Traffic>(dragEditor, "data");
        Section dragSection = dragDraft.maHulls[0].maSections[0];
        TrafficView dragView = Field<TrafficView>(dragEditor, "view");
        GLControl canvas = Field<GLControl>(dragView, "gl");
        canvas.Size = new(800, 500);
        dragTree.SelectObject(dragDraft.maHulls[0].maSections);
        Check(dragTree.CanDuplicate, "The Duplicate command remains available when a whole road-section collection is selected");
        dragTree.SelectObject(dragSection);
        OpenTK.Mathematics.Vector3 anchor = (OpenTK.Mathematics.Vector3)Call(dragView, "GroundPoint", new Point(400, 250), 0f);
        Check(Math.Abs(anchor.Y) < 0.001f, "Viewport dragging projects the pointer onto the selection's horizontal plane");
        Vector4 dragOriginal = dragSection.maRungs[0].maPoints[0];
        beginDrag();
        Call(dragView, "OnDrag", canvas, new MouseEventArgs(MouseButtons.Left, 0, 450, 250, 0));
        Check(dragSection.maRungs[0].maPoints[0] != dragOriginal && dragSection.maRungs[0].maPoints[0].Y == dragOriginal.Y && !dragTree.Modified, "Dragging previews a horizontal move before creating a history entry");
        Message escape = Message.Create(dragEditor.Handle, 0x100, (nint)Keys.Escape, 0);
        Check((bool)Call(dragEditor, "ProcessCmdKey", escape, Keys.Escape) && dragSection.maRungs[0].maPoints[0] == dragOriginal && !dragTree.Modified, "Esc cancels a drag and restores the original positions without closing the editor");
        beginDrag();
        Call(dragView, "OnDrag", canvas, new MouseEventArgs(MouseButtons.Left, 0, 460, 250, 0));
        Call(dragView, "FinishMove", true);
        Check(dragTree.Modified && dragSection.maRungs[0].maPoints[0] != dragOriginal, "Finishing a drag commits one editor history step");
        dragTree.Undo();
        Check(!dragTree.Modified && dragSection.maRungs[0].maPoints[0] == dragOriginal && dragSource.maHulls[0].maSections[0].maRungs[0].maPoints[0] == dragOriginal, "One Undo restores a drag while the original traffic handler remains unchanged");

        void beginDrag()
        {
            Check(Field<Func<bool>>(dragView, "MoveStarting")(), "The viewport begins a movement transaction for its selected geometry");
            FindField(typeof(TrafficView), "moveOrigin").SetValue(dragView, anchor);
            FindField(typeof(TrafficView), "moveHeight").SetValue(dragView, 0f);
            FindField(typeof(TrafficView), "moving").SetValue(dragView, true);
            FindField(typeof(TrafficView), "moved").SetValue(dragView, false);
            FindField(typeof(TrafficView), "downMouse").SetValue(dragView, new Point(400, 250));
            FindField(typeof(TrafficView), "lastMouse").SetValue(dragView, new Point(400, 250));
        }

        using TrafficPlacementDialog copyDialog = new(data, new(data, source), _ => { }, () => { });
        Hidden(copyDialog);
        Check(copyDialog.Destination == null, "Duplicating a whole road area defaults to a new road area");
        Capture(copyDialog, "traffic-duplicate.png");
        copyDialog.ClientSize = new(540, 500);
        Application.DoEvents();
        Capture(copyDialog, "traffic-duplicate-small.png");
        TrafficCellPicker cellPicker = Field<TrafficCellPicker>(copyDialog, "cells");
        cellPicker.SetSelection([0, 2]);
        Field<TextBox>(cellPicker, "filter").Text = "1";
        Check(cellPicker.SelectedCells.SequenceEqual([0, 2]) && Field<CheckedListBox>(cellPicker, "list").Items.Count == 1,
            "Filtering visibility cells preserves selections outside the search results");
        Field<TextBox>(cellPicker, "filter").Clear();
        copyDialog.Close();

        using TrafficPlacementDialog moveDialog = new(data, null, _ => { }, () => { });
        Hidden(moveDialog);
        Capture(moveDialog, "traffic-move.png");
        moveDialog.Close();

        using TrafficVisibilityDialog visibilityDialog = new(data, destination);
        Hidden(visibilityDialog);
        Capture(visibilityDialog, "traffic-visibility.png");
        visibilityDialog.Close();
    }

    private static void CheckLargeTraffic()
    {
        Traffic data = Sample();
        data.maHulls.Clear();
        FlowType flow = data.maFlowTypes[0];

        for (int area = 0; area < 8; area++)
        {
            Hull hull = new();
            SectionSpan span = new()
            {
                muMaxVehicles = 30
            };

            hull.maSectionSpans.Add(span);
            hull.maVehicleAssets.Add(data.maVehicleAssets[0]);

            for (int lane = 0; lane < 200; lane++)
            {
                Section section = new()
                {
                    mSpan = span,
                    mFlowType = flow,
                    mfSpeed = 25,
                    mfLength = 150
                };

                for (int point = 0; point < 16; point++)
                    section.maRungs.Add(new()
                    {
                        maPoints = [new(point * 10, 0, lane * 5, 1), new(point * 10, 0, lane * 5 + 3, 1)],
                        mfCumulativeLength = point * 10
                    });

                hull.maSections.Add(section);
            }

            for (int i = 0; i < 199; i++)
            {
                hull.maSections[i].mForwardStraightOn = hull.maSections[i + 1];
                hull.maSections[i + 1].mBackwardStraightOn = hull.maSections[i];
            }

            data.maHulls.Add(hull);
        }

        TrafficReferences.Data = data;
        BundleEntry entry = Entry([]);
        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
        Check(TrafficRules.Validate(data, entry).Count == 0, "Validation handles 1,600 linked sections and 25,600 lane points");
        ObjectHistory history = new(data);
        data.maHulls[0].maSections[0].mfSpeed = 30;
        history.Record();
        history.Undo();
        Check(data.maHulls[0].maSections[0].mfSpeed == 25, "Large traffic networks retain working property undo");
        watch.Stop();
        File.WriteAllText(Path.Combine(output, "large-network.txt"), $"1,600 linked sections; 25,600 lane points. Validation, history capture, edit and undo: {watch.ElapsedMilliseconds} ms.\n");
        Console.WriteLine($"Large-network validation and undo: {watch.ElapsedMilliseconds} ms.");
    }
}
