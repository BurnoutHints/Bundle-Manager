using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using BundleFormat;

namespace BaseHandlers.EnvironmentSettings;

// One time line location as a table in time order: the time of day, the keyframe used then, and swatches of the keyframe's
// sky top, sky horizon and key light colours. Keyframes in the same bundle can be opened in their own editor.
public class TimeLineView : UserControl
{
    private record KeyframeChoice(ulong Id, string Name);

    private const int TimeColumn = 0, KeyframeColumn = 1, PreviewColumn = 2;

    private readonly TimeLineLocation location;
    private readonly BundleEntry entry;
    private readonly DataGridView grid;
    private readonly ToolStripButton removeButton;
    private readonly Dictionary<ulong, EnvironmentKeyframe> previews = [];

    public TimeLineView(TimeLineLocation location, BundleEntry entry)
    {
        this.location = location;
        this.entry = entry;
        Dock = DockStyle.Fill;

        // Every keyframe in the bundle, and any the time line uses from elsewhere
        List<KeyframeChoice> choices = [.. entry.Archive.Entries
            .Where(e => e.Type == EntryType.BrnEnvironmentKeyframe)
            .Select(e => new KeyframeChoice(e.ID, string.IsNullOrEmpty(e.DebugInfo.Name) ? $"0x{e.ID:X8}" : e.DebugInfo.Name))];
        foreach (ulong id in location.Keyframes.Select(k => k.mpKeyframe).Distinct().Where(id => choices.All(c => c.Id != id)))
            choices.Add(new KeyframeChoice(id, $"0x{id:X8} (not in this bundle)"));
        choices.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

        grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Window
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Time", ToolTipText = "Time of day, as HH:MM:SS. (mpfKeyframeTimes)", FillWeight = 20, SortMode = DataGridViewColumnSortMode.NotSortable });
        grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            HeaderText = "Keyframe",
            ToolTipText = "Environment keyframe used at this time. (mppKeyframes)",
            DataSource = choices,
            ValueMember = nameof(KeyframeChoice.Id),
            DisplayMember = nameof(KeyframeChoice.Name),
            ValueType = typeof(ulong),
            FlatStyle = FlatStyle.Flat,
            FillWeight = 55
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Sky top, horizon, key light", ToolTipText = "Double-click to edit the keyframe.", ReadOnly = true, FillWeight = 25, SortMode = DataGridViewColumnSortMode.NotSortable });

        // A time that doesn't parse goes back to what it was, keeping the cell would leave the grid unable to leave it
        grid.CellEndEdit += (_, e) =>
        {
            if (e.ColumnIndex != TimeColumn)
                return;
            DataGridViewRow row = grid.Rows[e.RowIndex];
            TimeLineKeyframe keyframe = Keyframe(e.RowIndex);
            if (!TryParseTime(row.Cells[TimeColumn].Value as string, out float time))
            {
                row.Cells[TimeColumn].Value = FormatTime(keyframe.mfTime);
                row.ErrorText = "Enter a time of day from 00:00:00 to 24:00:00.";
                return;
            }
            keyframe.mfTime = time;
            BeginInvoke(() => Fill(keyframe)); // Not while the grid is still ending the edit
        };
        // Apply a picked keyframe straight away rather than when the cell is left
        grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (grid.CurrentCell?.ColumnIndex == KeyframeColumn)
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        grid.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == KeyframeColumn && grid.Rows[e.RowIndex].Cells[KeyframeColumn].Value is ulong id)
            {
                Keyframe(e.RowIndex).mpKeyframe = id;
                grid.InvalidateCell(PreviewColumn, e.RowIndex);
            }
        };
        grid.CellPainting += PaintPreview;
        grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == PreviewColumn)
                EditKeyframe();
        };
        grid.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Delete && removeButton.Enabled)
                Run(Remove);
        };
        grid.DataError += (_, e) => e.ThrowException = false;

        removeButton = new ToolStripButton("Remove", null, (_, _) => Run(Remove));
        ToolStrip tools = new() { GripStyle = ToolStripGripStyle.Hidden, Dock = DockStyle.Top };
        tools.Items.AddRange([
            new ToolStripButton("Add", null, (_, _) => Run(Add)) { ToolTipText = "Add a keyframe an hour after the selected one." },
            removeButton,
            new ToolStripSeparator(),
            new ToolStripButton("Edit keyframe...", null, (_, _) => Run(EditKeyframe)) { ToolTipText = "Edit the selected keyframe, it's saved when its editor closes." }
        ]);

        Controls.Add(grid);
        Controls.Add(tools);
        Fill(null);
    }

    // Finishes a cell being edited first. Ending a time edit queues a refill, so the action is queued after it.
    private void Run(Action action)
    {
        grid.EndEdit();
        BeginInvoke(action);
    }

    private TimeLineKeyframe Keyframe(int row) => (TimeLineKeyframe)grid.Rows[row].Tag;

    private TimeLineKeyframe Selected => grid.CurrentRow is DataGridViewRow row ? (TimeLineKeyframe)row.Tag : null;

    // Sorts the keyframes by time and shows them, selecting the given one
    private void Fill(TimeLineKeyframe select)
    {
        List<TimeLineKeyframe> sorted = [.. location.Keyframes.OrderBy(k => k.mfTime)];
        location.Keyframes.Clear();
        location.Keyframes.AddRange(sorted);

        grid.Rows.Clear();
        foreach (TimeLineKeyframe keyframe in sorted)
        {
            int row = grid.Rows.Add(FormatTime(keyframe.mfTime), keyframe.mpKeyframe, "");
            grid.Rows[row].Tag = keyframe;
            if (keyframe == select)
                grid.CurrentCell = grid.Rows[row].Cells[TimeColumn];
        }
        removeButton.Enabled = sorted.Count > 1;
    }

    private void Add()
    {
        TimeLineKeyframe selected = Selected ?? location.Keyframes.LastOrDefault();
        TimeLineKeyframe added = new()
        {
            mfTime = Math.Min((selected?.mfTime ?? -3600) + 3600, TimeLineKeyframe.SecondsPerDay),
            mpKeyframe = selected?.mpKeyframe ?? (grid.Columns[KeyframeColumn] as DataGridViewComboBoxColumn).Items.Cast<KeyframeChoice>().FirstOrDefault()?.Id ?? 0
        };
        location.Keyframes.Add(added);
        Fill(added);
    }

    private void Remove()
    {
        if (Selected is TimeLineKeyframe selected && location.Keyframes.Count > 1)
        {
            int index = location.Keyframes.IndexOf(selected);
            location.Keyframes.Remove(selected);
            Fill(location.Keyframes[Math.Min(index, location.Keyframes.Count - 1)]);
        }
    }

    private BundleEntry KeyframeEntry(ulong id) => entry.Archive.Entries.FirstOrDefault(e => e.ID == id && e.Type == EntryType.BrnEnvironmentKeyframe);

    private void EditKeyframe()
    {
        if (Selected is not TimeLineKeyframe selected)
            return;
        if (KeyframeEntry(selected.mpKeyframe) is not BundleEntry keyframeEntry)
        {
            MessageBox.Show(this, "That keyframe isn't in this bundle.", "Edit Keyframe", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        EnvironmentKeyframe keyframe = new();
        keyframe.Read(keyframeEntry);
        keyframe.GetEditor(keyframeEntry).ShowDialog(this);
        previews.Remove(selected.mpKeyframe);
        grid.InvalidateColumn(PreviewColumn);
    }

    // Null for keyframes from other bundles
    private EnvironmentKeyframe Preview(ulong id)
    {
        if (!previews.TryGetValue(id, out EnvironmentKeyframe keyframe) && KeyframeEntry(id) is BundleEntry keyframeEntry)
        {
            keyframe = new EnvironmentKeyframe();
            keyframe.Read(keyframeEntry);
            previews[id] = keyframe;
        }
        return keyframe;
    }

    private void PaintPreview(object sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != PreviewColumn)
            return;

        e.PaintBackground(e.CellBounds, (e.State & DataGridViewElementStates.Selected) != 0);
        e.Handled = true;
        if (Preview(Keyframe(e.RowIndex).mpKeyframe) is not EnvironmentKeyframe keyframe)
            return;

        Vector3[] colours = [keyframe.mScatteringData.mv3SkyTopColour, keyframe.mScatteringData.mv3SkyHorColour, keyframe.mLightingData.mv3KeyLightColour];
        int size = e.CellBounds.Height - 6;
        for (int i = 0; i < colours.Length; i++)
        {
            Rectangle swatch = new(e.CellBounds.Left + 4 + i * (size + 4), e.CellBounds.Top + 3, size, size);
            using SolidBrush brush = new(ColourPickerForm.ToColor(ColourPickerForm.Normalise(colours[i])));
            e.Graphics.FillRectangle(brush, swatch);
            e.Graphics.DrawRectangle(SystemPens.ControlDark, swatch);
        }
    }

    public static string FormatTime(float seconds)
    {
        int total = (int)MathF.Round(seconds);
        return $"{total / 3600:00}:{total / 60 % 60:00}:{total % 60:00}";
    }

    // HH, HH:MM or HH:MM:SS
    public static bool TryParseTime(string text, out float seconds)
    {
        seconds = 0;
        string[] parts = (text ?? "").Trim().Split(':');
        if (parts.Length > 3)
            return false;
        for (int i = 0; i < parts.Length; i++)
        {
            if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float part) || part < 0 || (i > 0 && part >= 60))
                return false;
            seconds += part * (i == 0 ? 3600 : i == 1 ? 60 : 1);
        }
        return seconds <= TimeLineKeyframe.SecondsPerDay;
    }
}
