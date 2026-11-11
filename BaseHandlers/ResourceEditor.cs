using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace BaseHandlers;

// Base editor combining object tree editor and optional 3D view with draft changes
public abstract class ResourceEditor : Form, IEntryEditor
{
    private readonly BundleEntry entry;
    private readonly string title, noun;
    private ObjectTreeEditor tree;
    private SceneView view;

    private readonly ListBox issues = new()
    {
        Dock = DockStyle.Bottom,
        Height = 85,
        HorizontalScrollbar = true,
        Visible = false
    };

    private readonly Label status = new()
    {
        Dock = DockStyle.Fill,
        AutoSize = true,
        Anchor = AnchorStyles.Left
    };

    private readonly Button apply = new()
    {
        Text = "&Apply",
        AutoSize = true
    };

    private readonly MenuStrip menu = new();
    private bool applied, closing, worldLoading;

    protected BundleEntry ResourceEntry => entry;

    protected ObjectTreeEditor Tree => tree;

    protected Label Status => status;

    protected ToolStripMenuItem EditMenu { get; private set; }

    protected ResourceEditor(BundleEntry initialEntry, string initialTitle, string initialNoun)
    {
        entry = initialEntry;
        title = initialTitle;
        noun = initialNoun;
    }

    protected abstract List<TrafficRules.Issue> FindIssues();

    protected abstract void WriteResource();

    // Initializes tree and view using derived class draft state
    protected void Initialize(object draft, string rootName, SceneView sceneView)
    {
        tree = new(draft, rootName);
        view = sceneView;
        tree.Changed += () =>
        {
            view?.Rebuild();
            ValidateState();
        };

        if (view == null)
            Controls.Add(tree);
        else
        {
            tree.SelectionChanged += view.Highlight;
            view.Picked += tree.Select;
            SplitContainer split = new()
            {
                Dock = DockStyle.Fill,
                Size = new(1200, 650),
                SplitterDistance = 460,
                Panel1MinSize = 280,
                Panel2MinSize = 180
            };

            split.Panel1.Controls.Add(tree);
            split.Panel2.Controls.Add(view);
            Controls.Add(split);
        }

        Controls.Add(issues);
        issues.DoubleClick += (_, _) => GoToIssue();
        issues.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                GoToIssue();
                e.Handled = true;
            }
        };

        TableLayoutPanel footer = new()
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            Padding = new(8),
            ColumnCount = 2
        };

        footer.ColumnStyles.Add(new(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new(SizeType.AutoSize));
        footer.Controls.Add(status, 0, 0);
        footer.SizeChanged += (_, _) => status.MaximumSize = new(Math.Max(120, footer.ClientSize.Width - 270), 0);
        FlowLayoutPanel actions = new()
        {
            AutoSize = true,
            WrapContents = false
        };

        Button ok = new()
        {
            Text = "OK",
            AutoSize = true
        };

        Button cancel = new()
        {
            Text = "Cancel",
            AutoSize = true,
            CausesValidation = false
        };

        ok.Click += (_, _) =>
        {
            if (ApplyChanges())
            {
                closing = true;
                DialogResult = DialogResult.OK;
                Close();
            }
        };
        cancel.Click += (_, _) =>
        {
            closing = true;
            DialogResult = DialogResult.Cancel;
            Close();
        };
        apply.Click += (_, _) => ApplyChanges();
        actions.Controls.AddRange([ok, cancel, apply]);
        footer.Controls.Add(actions, 1, 0);
        Controls.Add(footer);
        CancelButton = cancel;

        EditMenu = new("&Edit");
        ToolStripMenuItem undo = new("&Undo", null, (_, _) => tree.Undo())
        {
            ShortcutKeys = Keys.Control | Keys.Z
        };

        ToolStripMenuItem redo = new("&Redo", null, (_, _) => tree.Redo())
        {
            ShortcutKeys = Keys.Control | Keys.Y
        };

        EditMenu.DropDownItems.AddRange([undo, redo, new ToolStripSeparator(), new ToolStripMenuItem("&Find…", null, (_, _) => tree.FocusSearch(), Keys.Control | Keys.F)]);
        EditMenu.DropDownOpening += (_, _) =>
        {
            undo.Enabled = tree.CanUndo;
            redo.Enabled = tree.CanRedo;
        };
        menu.Items.Add(EditMenu);
        Controls.Add(menu);
        MainMenuStrip = menu;
        ClientSize = new(1200, 760);
        MinimumSize = new(680, 440);
        AutoScaleMode = AutoScaleMode.Font;
        StartPosition = FormStartPosition.CenterParent;
        FormClosing += (_, e) =>
        {
            view?.CancelMove();

            if (closing || !tree.Modified)
                return;

            DialogResult choice = MessageBox.Show(this, "Apply these changes to the bundle before closing?\n\n If you select Yes, Save the bundle afterward to write changes to disk.", $"Unapplied {noun} changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            e.Cancel = choice == DialogResult.Cancel || choice == DialogResult.Yes && !ApplyChanges();
        };
        ValidateState();
    }

    protected void InitializeWorldVisualization(bool promptForWorld)
    {
        ToolStripMenuItem openWorld = new("&Open worldcol.bin…");
        ToolStripMenuItem removeWorld = new("&Remove world geometry");
        ToolStripMenuItem viewMenu = new("&View");
        viewMenu.DropDownItems.AddRange([openWorld, removeWorld]);
        menu.Items.Add(viewMenu);
        openWorld.Click += async (_, _) => await OpenWorldVisualization();
        removeWorld.Click += (_, _) => view.SetWorldGeometry(null);
        viewMenu.DropDownOpening += (_, _) =>
        {
            openWorld.Enabled = !worldLoading;
            removeWorld.Enabled = !worldLoading && view.HasWorldGeometry;
        };

        if (promptForWorld)
            Shown += async (_, _) =>
            {
                DialogResult choice = MessageBox.Show(this,
                    $"Open worldcol.bin to show the world geometry behind the {noun}?\n\nYou can also open it later from the View menu.",
                    "World visualization", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (choice == DialogResult.Yes)
                    await OpenWorldVisualization();
            };
    }

    private async Task OpenWorldVisualization()
    {
        if (worldLoading)
            return;

        view.CancelMove();
        using OpenFileDialog dialog = new()
        {
            Title = $"Open world collision for {noun} visualization",
            FileName = "worldcol.bin",
            Filter = "World collision bundles (*.bin;*.bundle;*.bndl)|*.bin;*.bundle;*.bndl|All files (*.*)|*.*",
            CheckFileExists = true,
            InitialDirectory = Path.GetDirectoryName(entry.Archive?.Path) ?? ""
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        worldLoading = true;

        try
        {
            string path = dialog.FileName;
            TrafficWorldGeometry geometry = await LoadingDialog.RunAsync(this, "Loading world geometry…",
                (loader, cancel) => TrafficWorldGeometry.Load(path, loader, cancel));

            if (!IsDisposed)
                view.SetWorldGeometry(geometry);
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            MessageBox.Show(this, "Could not open the world geometry.\n\n" + exception.Message,
                "Cannot open world visualization", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            worldLoading = false;
        }
    }

    private void GoToIssue()
    {
        if (issues.SelectedItem is TrafficRules.Issue issue)
            tree.SelectObject(issue.Item, issue.Property);
    }

    private bool ValidateState()
    {
        List<TrafficRules.Issue> errors = FindIssues();
        issues.Items.Clear();
        issues.Items.AddRange([.. errors]);
        issues.Visible = errors.Count > 0;
        apply.Enabled = tree.Modified && errors.Count == 0;
        Text = $"{(tree.Modified
            ? "* "
            : "")}{title} — {entry.ID:X8}";
        status.Text = errors.Count > 0
            ? $"{errors.Count} issue(s). Double-click an issue to fix it, or Undo."
            : tree.Modified
                ? "Unapplied changes."
                : applied
                    ? "Applied changes. Save the bundle to write changes to disk."
                    : "";
        return errors.Count == 0;
    }

    private bool ApplyChanges()
    {
        if (!ValidateChildren() || !ValidateState())
            return false;

        if (!tree.Modified)
            return true;

        WriteResource();
        tree.MarkApplied();
        applied = true;
        ValidateState();
        return true;
    }
}
