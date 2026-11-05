using System;
using System.Linq;
using System.Windows.Forms;
using BundleFormat;
using HexEditor;

namespace BundleManager;

public partial class EntryEditor
{
    private BundleEntry original;
    private bool loaded, closeAccepted, hasApplied;

    private readonly Button applyButton = new()
    {
        Text = "&Apply",
        AutoSize = true,
        Enabled = false
    };

    private readonly Label draftStatus = new()
    {
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Text = ""
    };

    private readonly ToolStrip hexTools = new()
    {
        GripStyle = ToolStripGripStyle.Hidden,
        Dock = DockStyle.Fill
    };

    private readonly ToolStripButton editButton = new("Edit bytes")
    {
        CheckOnClick = true
    };

    private readonly ToolStripComboBox byteMode = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        AutoSize = false,
        Width = 190
    };

    private readonly ToolStripButton inspectorButton = new("Value inspector")
    {
        CheckOnClick = true,
        Checked = true
    };

    private ToolStripMenuItem undoCommand, redoCommand, findCommand, nextCommand, previousCommand;
    private ToolStripMenuItem hexViewMenu;
    private ToolStripItem[] hexEditCommands = [];
    private SplitContainer primarySplit, secondarySplit;
    private string searchText = "";
    private int searchFormat;
    private byte[] searchPattern;

    private bool DraftChanged => loaded && (Entry.Dirty || hexData.Modified || hexExtraData.Modified);

    private void InitializeEditing()
    {
        MinimumSize = new(520, 400);
        primarySplit = SplitInspector(tabPrimary, hexData, valuesData);
        secondarySplit = SplitInspector(tabSecondary, hexExtraData, valuesExtraData);
        Controls.Clear();
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5
        };

        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.Percent, 100));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.Controls.Add(mnuBar, 0, 0);
        layout.Controls.Add(hexTools, 0, 1);
        Panel body = new()
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };

        body.Controls.AddRange([tabList, pboImage, lblLoading]);
        pboImage.Dock = tabList.Dock = DockStyle.Fill;
        layout.Controls.Add(body, 0, 2);
        pbMain.Dispose();
        TableLayoutPanel footer = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new(8, 4, 8, 4)
        };

        footer.ColumnStyles.Add(new(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new(SizeType.AutoSize));
        footer.Controls.Add(draftStatus, 0, 0);
        footer.SizeChanged += (_, _) => draftStatus.MaximumSize = new(Math.Max(100, footer.ClientSize.Width - 260), 0);
        FlowLayoutPanel buttons = new()
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
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
            if (ApplyDraft())
            {
                closeAccepted = true;
                DialogResult = DialogResult.OK;
                Close();
            }
        };
        cancel.Click += (_, _) =>
        {
            closeAccepted = true;
            DialogResult = DialogResult.Cancel;
            Close();
        };
        applyButton.Click += (_, _) => ApplyDraft();
        buttons.Controls.AddRange([ok, cancel, applyButton]);
        footer.Controls.Add(buttons, 1, 0);
        layout.Controls.Add(footer, 0, 3);
        layout.Controls.Add(stsMain, 0, 4);
        Controls.Add(layout);
        CancelButton = cancel;

        editButton.CheckedChanged += (_, _) => editModeToolStripMenuItem.Checked = editButton.Checked;
        byteMode.Items.AddRange(["Overwrite (fixed length)", "Insert / delete bytes"]);
        byteMode.SelectedIndex = 0;
        byteMode.SelectedIndexChanged += (_, _) =>
        {
            ActiveHex?.SetInsertMode(byteMode.SelectedIndex == 1);

            if (byteMode.SelectedIndex == 1)
                ShowFeedback("Insert / delete changes size. Review offsets and imports before applying.");
        };
        ToolStripButton find = new("Find…", null, (_, _) => FindBytes());
        ToolStripButton go = new("Go to…", null, (_, _) => goToOffsetToolStripMenuItem_Click(this, EventArgs.Empty));
        ToolStripButton column = new("Hex / Text", null, (_, _) =>
        {
            ActiveHex?.SwitchColumn();
            ActiveHex?.Focus();
        })
        {
            ToolTipText = "Switch the input column (F6). Tab moves to the next control."
        };

        inspectorButton.CheckedChanged += (_, _) =>
        {
            primarySplit.Panel2Collapsed = secondarySplit.Panel2Collapsed = !inspectorButton.Checked;
        };
        hexTools.Items.AddRange([editButton, byteMode, new ToolStripSeparator(), find, go, column, inspectorButton]);
        hexViewMenu = new("&View");
        ToolStripMenuItem inspectorMenu = new("&Value inspector")
        {
            CheckOnClick = true,
            Checked = true
        };

        inspectorMenu.CheckedChanged += (_, _) => inspectorButton.Checked = inspectorMenu.Checked;
        inspectorButton.CheckedChanged += (_, _) => inspectorMenu.Checked = inspectorButton.Checked;
        hexViewMenu.DropDownItems.Add(inspectorMenu);
        hexViewMenu.DropDownItems.Add(new ToolStripMenuItem("Switch &hex / text", null, (_, _) =>
        {
            ActiveHex?.SwitchColumn();
            ActiveHex?.Focus();
        })
        {
            ShortcutKeyDisplayString = "F6"
        });
        mnuBar.Items.Add(hexViewMenu);

        undoCommand = new("&Undo", null, (_, _) => RunHex(h => h.Undo()))
        {
            ShortcutKeys = Keys.Control | Keys.Z
        };
        redoCommand = new("&Redo", null, (_, _) => RunHex(h => h.Redo()))
        {
            ShortcutKeys = Keys.Control | Keys.Y
        };
        findCommand = new("&Find…", null, (_, _) => FindBytes())
        {
            ShortcutKeys = Keys.Control | Keys.F
        };
        nextCommand = new("Find &next", null, (_, _) => FindNext(false))
        {
            ShortcutKeys = Keys.F3
        };
        previousCommand = new("Find &previous", null, (_, _) => FindNext(true))
        {
            ShortcutKeys = Keys.Shift | Keys.F3
        };
        ToolStripMenuItem copy = new("&Copy", null, (_, _) => RunHex(h => h.Copy()));
        ToolStripMenuItem paste = new("&Paste", null, (_, _) => RunHex(h => h.Paste()));
        ToolStripMenuItem cut = new("C&ut / clear", null, (_, _) => RunHex(h => h.Cut()));
        ToolStripMenuItem clear = new("Clear / delete bytes", null, (_, _) => RunHex(h => h.DeleteSelection()));
        ToolStripMenuItem currentSelect = new("Select &all bytes", null, (_, _) => RunHex(h => h.SelectAll()));

        // Canvas handles Ctrl+C/X/V/A so text boxes retain their usual editing shortcuts.
        copy.ShortcutKeyDisplayString = "Ctrl+C";
        paste.ShortcutKeyDisplayString = "Ctrl+V";
        cut.ShortcutKeyDisplayString = "Ctrl+X";
        currentSelect.ShortcutKeyDisplayString = "Ctrl+A";
        ToolStripItem[] commands = [undoCommand, redoCommand, new ToolStripSeparator(), copy, paste, cut, clear, currentSelect,
            new ToolStripSeparator(), findCommand, nextCommand, previousCommand, new ToolStripSeparator()];

        for (int i = 0; i < commands.Length; i++)
            editStripMenuItem.DropDownItems.Insert(i, commands[i]);

        hexEditCommands = [.. commands, editSeparator, editModeToolStripMenuItem, goToOffsetToolStripMenuItem];
        editStripMenuItem.DropDownOpening += (_, _) =>
        {
            copy.Enabled = ActiveHex?.SelectionLength > 0;
            paste.Enabled = cut.Enabled = clear.Enabled = ActiveHex is { ReadOnly: false };
            currentSelect.Enabled = ActiveHex != null;
        };
        hexData.Feedback += ShowFeedback;
        hexExtraData.Feedback += ShowFeedback;
        importsView.Changed += UpdateDraftStatus;
        FormClosing += ConfirmDraftClose;
        UpdateHexStatus();
    }

    private static SplitContainer SplitInspector(TabPage tab, HexView hex, ValueView values)
    {
        tab.Controls.Clear();
        SplitContainer split = new()
        {
            Size = new(960, 400),
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel2,
            Panel1MinSize = 80,
            Panel2MinSize = 160,
            SplitterDistance = 620
        };

        hex.AutoScroll = false;
        values.Dock = DockStyle.Fill;
        split.Panel1.Controls.Add(hex);
        split.Panel2.Controls.Add(values);
        tab.Controls.Add(split);
        return split;
    }

    private bool FinishCellEdit() => valuesData.CommitEdit() && valuesExtraData.CommitEdit() && importsView.CommitEdit();

    private void RunHex(Action<HexView> command)
    {
        if (ActiveHex is HexView hex && FinishCellEdit())
        {
            command(hex);
            hex.Focus();
        }
    }

    // Synchronize only this window's draft. Navigation never changes the original bundle entry.
    private void FlushHexToDraft()
    {
        if (!loaded || showingImage)
            return;

        bool dirty = Entry.Dirty;

        if (!Entry.GetPrimaryWithoutImports().AsSpan().SequenceEqual(hexData.HexData))
            Entry.SetPrimary([.. hexData.HexData], Entry.GetImports());

        if (Entry.EntryBlocks[Entry.SecondaryBlock] is EntryBlock secondary &&
                    !(secondary.Data ?? []).AsSpan().SequenceEqual(hexExtraData.HexData))
            secondary.Data = [.. hexExtraData.HexData];

        Entry.Dirty = dirty;
    }

    private bool ApplyDraft()
    {
        if (!loaded || !FinishCellEdit())
            return false;

        if (!DraftChanged)
            return true;

        FlushHexToDraft();
        int size = Entry.GetPrimaryWithoutImports().Length;
        int pointerSize = Entry.Platform == BundlePlatform.PCx64
            ? 8
            : 4;

        if (Entry.GetImports().Any(import => (ulong)import.EntryPointerOffset + (uint)pointerSize > (ulong)size))
        {
            ShowFeedback("An import pointer is outside the primary data. Correct it on the Imports tab before applying.");
            tabList.SelectedTab = tabImports;
            return false;
        }

        EntryDraft.Assign(Entry, original);
        original.Dirty = true;
        Entry.Dirty = false;
        hasApplied = true;
        hexData.MarkApplied();
        hexExtraData.MarkApplied();
        UpdateDraftStatus();
        return true;
    }

    private void ConfirmDraftClose(object sender, FormClosingEventArgs e)
    {
        if (closeAccepted || !DraftChanged)
            return;

        DialogResult choice = MessageBox.Show(this, "Apply these changes to the bundle before closing?\n\n Save the bundle afterward to write changes to disk.",
                    "Unapplied resource changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        e.Cancel = choice == DialogResult.Cancel || choice == DialogResult.Yes && !ApplyDraft();
    }

    private void UpdateDraftStatus()
    {
        if (original == null)
            return;

        bool changed = DraftChanged;
        applyButton.Enabled = changed;
        Text = $"{(changed
            ? "* "
            : "")}Resource {Entry.ID:X8} — {Entry.Type}";
        draftStatus.Text = changed
            ? "Unapplied changes."
            : hasApplied
                ? "Applied to bundle. Save the bundle to write changes to disk."
                : "";
    }

    private void ShowFeedback(string message)
    {
        draftStatus.Text = message;
        draftStatus.AccessibleDescription = message;
    }

    private void FindBytes()
    {
        if (ActiveHex == null || !FinishCellEdit())
            return;

        using HexNavigationDialog dialog = new(true, ActiveHex.HexData.Length, searchText, searchFormat);

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        searchText = dialog.InputText;
        searchFormat = dialog.FormatIndex;
        searchPattern = dialog.Pattern;
        SearchAt(ActiveHex.SelectionStart, false);
    }

    private void FindNext(bool backwards)
    {
        if (ActiveHex == null)
            return;

        if (searchPattern == null)
        {
            FindBytes();
            return;
        }

        SearchAt(ActiveHex.SelectionStart + (backwards
            ? -1
            : 1), backwards);
    }

    private void SearchAt(int start, bool backwards)
    {
        HexView hex = ActiveHex;
        int found = HexNavigationDialog.Find(hex.HexData, searchPattern, start, backwards, out bool wrapped);

        if (found < 0)
        {
            ShowFeedback("No matching bytes in this data block.");
            return;
        }

        hex.Select(found, searchPattern.Length);
        hex.Focus();
        ShowFeedback($"Found at 0x{found:X}.{(wrapped
            ? " Search wrapped around the data block."
            : "")}");
    }
}
