using System;
using System.Collections.Generic;
using System.Windows.Forms;
using BundleFormat;
using PluginAPI;

namespace BaseHandlers;

public sealed partial class TrafficEditor : Form, IEntryEditor
{
    private readonly Traffic data;
    private readonly BundleEntry entry;
    private readonly ObjectTreeEditor tree;
    private readonly TrafficView view;

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

    private bool applied, closing;

    public TrafficEditor(Traffic source, BundleEntry initialEntry, bool promptForWorld = true)
    {
        entry = initialEntry;
        data = (Traffic)ObjectTreeEditor.Clone(source);
        TrafficLabels.Install();
        Traffic previous = TrafficReferences.Data;
        TrafficReferences.Data = data;
        tree = new(data, "Traffic");
        view = new(data)
        {
            Dock = DockStyle.Fill
        };
        tree.SelectionChanged += view.Highlight;
        tree.Changed += () =>
        {
            view.Rebuild();
            ValidateState();
        };
        view.Picked += tree.Select;
        TrafficReferences.Preview = view.Highlight;
        TrafficReferences.RestorePreview = () => view.Highlight(tree.SelectedObject);
        Disposed += (_, _) =>
        {
            TrafficReferences.Data = previous;
            TrafficReferences.Preview = null;
            TrafficReferences.RestorePreview = null;
        };

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

        MenuStrip menu = new();
        ToolStripMenuItem edit = new("&Edit");
        ToolStripMenuItem undo = new("&Undo", null, (_, _) => tree.Undo())
        {
            ShortcutKeys = Keys.Control | Keys.Z
        };

        ToolStripMenuItem redo = new("&Redo", null, (_, _) => tree.Redo())
        {
            ShortcutKeys = Keys.Control | Keys.Y
        };

        edit.DropDownItems.AddRange([undo, redo, new ToolStripSeparator(), new ToolStripMenuItem("&Find…", null, (_, _) => tree.FocusSearch(), Keys.Control | Keys.F)]);
        edit.DropDownOpening += (_, _) =>
        {
            undo.Enabled = tree.CanUndo;
            redo.Enabled = tree.CanRedo;
        };
        menu.Items.Add(edit);
        Controls.Add(menu);
        MainMenuStrip = menu;
        InitializePlacement(edit);
        InitializeWorldVisualization(menu, promptForWorld);
        ClientSize = new(1200, 760);
        MinimumSize = new(680, 440);
        AutoScaleMode = AutoScaleMode.Font;
        StartPosition = FormStartPosition.CenterParent;
        FormClosing += (_, e) =>
        {
            view.CancelMove();

            if (closing || !tree.Modified)
                return;

            DialogResult choice = MessageBox.Show(this, "Apply these changes to the bundle before closing?\n\n If you select Yes, Save the bundle afterward to write changes to disk.", "Unapplied traffic changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            e.Cancel = choice == DialogResult.Cancel || choice == DialogResult.Yes && !ApplyChanges();
        };
        ValidateState();
    }

    private void GoToIssue()
    {
        if (issues.SelectedItem is TrafficRules.Issue issue)
            tree.SelectObject(issue.Item, issue.Property);
    }

    private bool ValidateState()
    {
        List<TrafficRules.Issue> errors = TrafficRules.Validate(data, entry);
        issues.Items.Clear();
        issues.Items.AddRange([.. errors]);
        issues.Visible = errors.Count > 0;
        apply.Enabled = tree.Modified && errors.Count == 0;
        Text = $"{(tree.Modified
            ? "* "
            : "")}Traffic — {entry.ID:X8}";
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

        data.Write(entry);
        tree.MarkApplied();
        applied = true;
        ValidateState();
        return true;
    }
}
