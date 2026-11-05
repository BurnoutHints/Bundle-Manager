using System;
using System.Windows.Forms;
using static BaseHandlers.Traffic;

namespace BaseHandlers;

public sealed partial class TrafficEditor
{
    private readonly ToolStripButton moveCommand = new("Move…");
    private readonly ToolStripButton visibilityCommand = new("Visibility…");
    private TrafficTranslation movement;
    private Hull selectedArea;

    private void InitializePlacement(ToolStripMenuItem editMenu)
    {
        tree.DuplicateRequested = selection =>
        {
            if (selection is PVSCell cell)
            {
                CopyVisibilityCell(cell);
                return true;
            }

            if (!TrafficCopy.CanCopy(selection))
                return false;

            DuplicateTraffic(selection);
            return true;
        };
        moveCommand.ToolTipText = "Move the selected geometry with a preview (Ctrl+M). Move selection in the 3D view also supports dragging.";
        visibilityCommand.ToolTipText = "Choose which visibility cells include this road area.";
        moveCommand.Click += (_, _) => MoveTraffic();
        visibilityCommand.Click += (_, _) => EditVisibility();
        tree.AddPlacementCommands(moveCommand, visibilityCommand);
        tree.SelectionChanged += selection =>
        {
            selectedArea = TrafficCopy.AreaOf(data, selection);
            moveCommand.Enabled = TrafficTranslation.CanMove(selection);
            visibilityCommand.Enabled = selectedArea != null || selection is PVSCell;
        };
        moveCommand.Enabled = visibilityCommand.Enabled = false;

        ToolStripMenuItem duplicate = new("&Duplicate…", null, (_, _) => tree.DuplicateSelection())
        {
            ShortcutKeys = Keys.Control | Keys.D
        };

        ToolStripMenuItem move = new("&Move…", null, (_, _) => MoveTraffic())
        {
            ShortcutKeys = Keys.Control | Keys.M
        };

        ToolStripMenuItem visibility = new("&Visibility cells…", null, (_, _) => EditVisibility());
        editMenu.DropDownItems.AddRange([new ToolStripSeparator(), duplicate, move, visibility]);
        editMenu.DropDownOpening += (_, _) =>
        {
            duplicate.Enabled = tree.CanDuplicate;
            move.Enabled = moveCommand.Enabled;
            visibility.Enabled = visibilityCommand.Enabled;
        };
        view.MoveStarting += BeginMovement;
        view.MovePreview += offset =>
        {
            try
            {
                movement?.Apply(offset);
                view.Rebuild();
            }
            catch (ArgumentException exception)
            {
                view.CancelMove();
                status.Text = exception.Message;
            }
        };
        view.MoveFinished += accept =>
        {
            TrafficTranslation preview = movement;
            movement = null;

            if (preview == null)
                return;

            if (accept)
                tree.CompleteEdit();
            else
            {
                preview.Restore();
                tree.RefreshProperties();
                view.Rebuild();
            }
        };
        Deactivate += (_, _) => view.CancelMove();
    }

    private bool BeginMovement()
    {
        movement = new(tree.SelectedObject);

        if (movement.Count > 0)
            return true;

        movement = null;
        return false;
    }

    private void MoveTraffic()
    {
        view.CancelMove();

        if (!TrafficTranslation.CanMove(tree.SelectedObject) || !ValidateChildren())
            return;

        TrafficTranslation preview = new(tree.SelectedObject);
        bool accepted = false;

        try
        {
            using TrafficPlacementDialog dialog = new(data, null, offset =>
            {
                preview.Apply(offset);
                view.Rebuild();
            }, view.FrameSelection);
            accepted = dialog.ShowDialog(this) == DialogResult.OK;
        }
        finally
        {
            if (!accepted)
                preview.Restore();

            view.Rebuild();
            tree.RefreshProperties();
        }

        if (accepted)
            tree.CompleteEdit();
    }

    private void DuplicateTraffic(object selection)
    {
        view.CancelMove();

        if (!ValidateChildren())
            return;

        try
        {
            TrafficCopy copy = new(data, selection);

            try
            {
                using TrafficPlacementDialog dialog = new(data, copy, offset =>
                {
                    copy.Translation.Apply(offset);
                    view.Highlight(copy.Contents);
                }, view.FrameSelection);

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                object result = copy.Commit(dialog.Destination, dialog.VisibleCells, dialog.KeepConnections);
                tree.CompleteEdit(result);
            }
            finally
            {
                view.Highlight(tree.SelectedObject);
            }
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            MessageBox.Show(this, exception.Message, "Cannot duplicate traffic", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void EditVisibility()
    {
        view.CancelMove();

        if (!ValidateChildren())
            return;

        if (tree.SelectedObject is PVSCell cell)
        {
            using TrafficReferenceDialog dialog = new(cell, nameof(PVSCell.maHulls), typeof(Hull), true, cell.maHulls);

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                cell.maHulls = [.. Array.ConvertAll(dialog.SelectedValues, value => (Hull)value)];
                tree.CompleteEdit();
            }

            return;
        }

        if (selectedArea == null)
            return;

        using TrafficVisibilityDialog visibility = new(data, selectedArea);

        if (visibility.ShowDialog(this) != DialogResult.OK)
            return;

        TrafficVisibility.SetArea(data, selectedArea, visibility.SelectedCells);
        tree.CompleteEdit();
    }

    private void CopyVisibilityCell(PVSCell cell)
    {
        int source = data.mPvs.maHullPvs.IndexOf(cell);

        if (source < 0)
            return;

        using TrafficVisibilityDialog dialog = new(data, null, source);

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        TrafficVisibility.CopyCells(data, source, dialog.SelectedCells, dialog.Replace);
        tree.CompleteEdit();
    }

    protected override bool ProcessCmdKey(ref Message message, Keys keyData)
    {
        if (keyData == Keys.Escape && view?.ExitMoveMode() == true)
            return true;

        view?.CancelMove();
        return base.ProcessCmdKey(ref message, keyData);
    }

    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (keyData == Keys.Escape && view?.ExitMoveMode() == true)
            return true;

        return base.ProcessDialogKey(keyData);
    }
}
