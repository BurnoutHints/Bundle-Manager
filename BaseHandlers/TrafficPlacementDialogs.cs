using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using static BaseHandlers.Traffic;

namespace BaseHandlers;

public sealed class TrafficCellPicker : UserControl
{
    private sealed record Cell(int Index, string Label)
    {
        public override string ToString() => Label;
    }

    private readonly Traffic data;

    private readonly CheckedListBox list = new()
    {
        Dock = DockStyle.Fill,
        CheckOnClick = true,
        HorizontalScrollbar = true,
        IntegralHeight = false
    };

    private readonly TextBox filter = new()
    {
        Dock = DockStyle.Top,
        PlaceholderText = "Find a cell by coordinates or index",
        AccessibleName = "Find visibility cell"
    };

    private readonly Label summary = new()
    {
        Dock = DockStyle.Bottom,
        AutoSize = true
    };

    private readonly HashSet<int> selected = [];
    private bool updating;

    public int[] SelectedCells => [.. selected.Order()];

    public event Action SelectionChanged;

    public TrafficCellPicker(Traffic initialData)
    {
        data = initialData;
        Dock = DockStyle.Fill;
        Controls.Add(list);
        Panel search = new()
        {
            Dock = DockStyle.Top,
            Height = filter.PreferredHeight + 4
        };

        filter.Dock = DockStyle.Fill;
        search.Controls.Add(filter);
        search.Controls.Add(new Label()
        {
            Text = "&Find:",
            Dock = DockStyle.Left,
            Width = 40,
            TextAlign = ContentAlignment.MiddleLeft
        });
        Controls.Add(search);
        Controls.Add(summary);
        FlowLayoutPanel selection = new()
        {
            Dock = DockStyle.Bottom,
            AutoSize = true
        };

        Button select = new()
        {
            Text = "Select shown",
            AutoSize = true
        };

        Button clear = new()
        {
            Text = "Clear",
            AutoSize = true
        };

        select.Click += (_, _) => SetSelection([.. selected.Union(list.Items.Cast<Cell>().Select(cell => cell.Index))]);
        clear.Click += (_, _) => SetSelection([]);
        selection.Controls.AddRange([select, clear]);
        Controls.Add(selection);
        filter.TextChanged += (_, _) => RefreshCells();
        list.ItemCheck += (_, e) =>
        {
            if (updating)
                return;

            int index = ((Cell)list.Items[e.Index]).Index;

            if (e.NewValue == CheckState.Checked)
                selected.Add(index);
            else
                selected.Remove(index);

            UpdateSummary();
            SelectionChanged?.Invoke();
        };
        RefreshCells();
    }

    public void SetSelection(IEnumerable<int> cells)
    {
        selected.Clear();
        selected.UnionWith(cells.Where(index => index >= 0 && index < data.mPvs.maHullPvs.Count));
        RefreshCells();
        SelectionChanged?.Invoke();
    }

    private void RefreshCells()
    {
        updating = true;
        list.BeginUpdate();
        list.Items.Clear();

        for (int index = 0; index < data.mPvs.maHullPvs.Count; index++)
        {
            string label = TrafficVisibility.Label(data, index) + $" (index {index})";
            string query = filter.Text.Trim().Replace(" ", "");
            uint width = data.mPvs.muNumCells_X;
            string coordinates = width == 0
                ? index.ToString()
                : $"{index % width},{index / width}";
            bool matches = query.Length == 0 || (int.TryParse(query, out int target)
                ? target == index
                : coordinates.StartsWith(query, StringComparison.OrdinalIgnoreCase));

            if (matches)
                list.Items.Add(new Cell(index, label), selected.Contains(index));
        }

        list.EndUpdate();
        updating = false;
        UpdateSummary();
    }

    private void UpdateSummary() => summary.Text = selected.Count == 1
        ? "1 cell selected."
        : $"{selected.Count} cells selected.";
}

public sealed class TrafficPlacementDialog : Form
{
    private sealed record Area(Hull Value, string Label)
    {
        public override string ToString() => Label;
    }

    private readonly ComboBox destination = new()
    {
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList
    };

    private readonly NumericUpDown x = Number(), y = Number(), z = Number();

    private readonly CheckBox connections = new()
    {
        Text = "Keep connections to existing roads",
        AutoSize = true
    };

    private readonly Label error = new()
    {
        AutoSize = true,
        Dock = DockStyle.Fill
    };

    private readonly Traffic data;
    private readonly TrafficCopy copy;
    private readonly TrafficCellPicker cells;
    private readonly Action<Vector3> preview;

    public Vector3 Offset => new((float)x.Value, (float)y.Value, (float)z.Value);

    public Hull Destination => (destination.SelectedItem as Area)?.Value;

    public int[] VisibleCells => cells.SelectedCells;

    public bool KeepConnections => connections.Enabled && connections.Checked;

    public TrafficPlacementDialog(Traffic initialData, TrafficCopy initialCopy, Action<Vector3> previewChange, Action frame)
    {
        data = initialData;
        copy = initialCopy;
        preview = previewChange;
        cells = new(initialData);
        Text = copy == null
            ? "Move traffic"
            : "Duplicate traffic";
        AutoScaleMode = AutoScaleMode.Font;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = MaximizeBox = false;
        ClientSize = copy == null
            ? new(460, 155)
            : new(720, 630);
        MinimumSize = copy == null
            ? new(400, 185)
            : new(540, 500);

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            Padding = new(10),
            ColumnCount = 1,
            RowCount = 8
        };

        for (int index = 0; index < 8; index++)
            layout.RowStyles.Add(new(index == 4
                ? SizeType.Percent
                : SizeType.AutoSize, index == 4
                ? 100
                : 0));

        Label explanation = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Text = copy == null
                ? "Preview an offset for the selection. X and Z move across the map; Y changes height."
                : "Copy the selection and its related road data. Preview its position before adding it."
        };

        layout.Controls.Add(explanation, 0, 0);
        TableLayoutPanel areaRow = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Visible = copy != null
        };

        areaRow.ColumnStyles.Add(new(SizeType.AutoSize));
        areaRow.ColumnStyles.Add(new(SizeType.Percent, 100));
        areaRow.Controls.Add(new Label()
        {
            Text = "Road &area:",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 0);
        areaRow.Controls.Add(destination, 1, 0);
        layout.Controls.Add(areaRow, 0, 1);

        TableLayoutPanel offsets = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ColumnCount = 6
        };

        AddOffset("&X:", x, 0);
        AddOffset("&Y:", y, 2);
        AddOffset("&Z:", z, 4);
        layout.Controls.Add(offsets, 0, 2);
        connections.Visible = copy != null;
        layout.Controls.Add(connections, 0, 3);
        Panel visibility = new()
        {
            Dock = DockStyle.Fill,
            Visible = copy != null
        };

        visibility.Controls.Add(cells);
        visibility.Controls.Add(new Label()
        {
            Text = "Visibility cells: where this road area can be visible.",
            Dock = DockStyle.Top,
            AutoSize = true
        });
        layout.Controls.Add(visibility, 0, 4);

        FlowLayoutPanel cellActions = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Visible = copy != null
        };

        Button center = new()
        {
            Text = "Center in selected cell",
            AutoSize = true,
            Enabled = false
        };

        Button selectPosition = new()
        {
            Text = "Select cells at new position",
            AutoSize = true
        };

        center.Click += (_, _) =>
        {
            if (cells.SelectedCells.Length != 1 || copy.Translation.Count == 0)
                return;

            try
            {
                Vector3 target = TrafficVisibility.CellCenter(data, cells.SelectedCells[0]);
                x.Value = (decimal)Math.Clamp(target.X - copy.Translation.Center.X, (float)x.Minimum, (float)x.Maximum);
                z.Value = (decimal)Math.Clamp(target.Z - copy.Translation.Center.Z, (float)z.Minimum, (float)z.Maximum);
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                error.Text = exception.Message;
            }
        };
        selectPosition.Click += (_, _) => cells.SetSelection(TrafficVisibility.CellsAt(data, copy.Translation.Minimum + Offset, copy.Translation.Maximum + Offset));
        cells.SelectionChanged += () => center.Enabled = cells.SelectedCells.Length == 1 && copy?.Translation.Count > 0;
        cellActions.Controls.AddRange([center, selectPosition]);
        layout.Controls.Add(cellActions, 0, 5);
        layout.Controls.Add(error, 0, 6);

        FlowLayoutPanel buttons = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };

        Button cancel = new()
        {
            Text = "Cancel",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };

        Button accept = new()
        {
            Text = copy == null
                ? "Move"
                : "Duplicate",
            AutoSize = true
        };

        Button show = new()
        {
            Text = "Show selection",
            AutoSize = true
        };

        show.Click += (_, _) => frame();
        accept.Click += (_, _) => Submit();
        buttons.Controls.AddRange([cancel, accept, show]);
        layout.Controls.Add(buttons, 0, 7);
        Controls.Add(layout);
        AcceptButton = accept;
        CancelButton = cancel;

        foreach (NumericUpDown number in (NumericUpDown[])[x, y, z])
            number.ValueChanged += (_, _) => Preview();

        if (copy != null)
        {
            destination.Items.Add(new Area(null, "New road area"));

            for (int index = 0; index < data.maHulls.Count; index++)
                destination.Items.Add(new Area(data.maHulls[index], $"{index}: {data.maHulls[index]}"));

            destination.SelectedIndexChanged += (_, _) =>
            {
                connections.Enabled = ReferenceEquals(Destination, copy.SourceArea);
                cells.SetSelection(TrafficVisibility.CellsForArea(data, Destination ?? copy.SourceArea));
            };
            destination.SelectedIndex = copy.IsAreaCopy
                ? 0
                : data.maHulls.IndexOf(copy.SourceArea) + 1;
        }

        Shown += (_, _) => Preview();

        void AddOffset(string label, NumericUpDown number, int column)
        {
            offsets.ColumnStyles.Add(new(SizeType.AutoSize));
            offsets.ColumnStyles.Add(new(SizeType.Percent, 33));
            offsets.Controls.Add(new Label()
            {
                Text = label,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            }, column, 0);
            number.Dock = DockStyle.Fill;
            offsets.Controls.Add(number, column + 1, 0);
        }
    }

    private static NumericUpDown Number() => new()
    {
        DecimalPlaces = 3,
        Minimum = -10000000,
        Maximum = 10000000,
        Increment = 1
    };

    private void Preview()
    {
        try
        {
            preview(Offset);
            error.Text = "";
        }
        catch (ArgumentException exception)
        {
            error.Text = exception.Message;
        }
    }

    private void Submit()
    {
        try
        {
            preview(Offset);
            copy?.Validate(Destination, cells.SelectedCells, KeepConnections);
            DialogResult = DialogResult.OK;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            error.Text = exception.Message;
        }
    }
}

public sealed class TrafficVisibilityDialog : Form
{
    private readonly Traffic data;
    private readonly Hull area;
    private readonly int source;
    private readonly TrafficCellPicker cells;

    private readonly CheckBox replace = new()
    {
        Text = "Replace existing road areas in destination cells",
        AutoSize = true
    };

    private readonly Label error = new()
    {
        AutoSize = true,
        Dock = DockStyle.Fill
    };

    public int[] SelectedCells => cells.SelectedCells;

    public bool Replace => replace.Checked;

    public TrafficVisibilityDialog(Traffic initialData, Hull initialArea, int sourceCell = -1)
    {
        data = initialData;
        area = initialArea;
        source = sourceCell;
        cells = new(initialData);
        Text = source >= 0
            ? "Copy visibility settings"
            : "Road area visibility";
        AutoScaleMode = AutoScaleMode.Font;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = MaximizeBox = false;
        ClientSize = new(650, 490);
        MinimumSize = new(460, 350);

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            Padding = new(10),
            ColumnCount = 1,
            RowCount = 5
        };

        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.Percent, 100));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.Controls.Add(new Label()
        {
            Text = source >= 0
                ? $"Copy the road areas from {TrafficVisibility.Label(data, source)} to the selected cells. Geometry stays in place."
                : "Choose the visibility cells from which this road area can be visible. Each cell holds up to 8 road areas.",
            AutoSize = true,
            Dock = DockStyle.Fill
        }, 0, 0);
        layout.Controls.Add(cells, 0, 1);
        replace.Visible = source >= 0;
        layout.Controls.Add(replace, 0, 2);
        layout.Controls.Add(error, 0, 3);

        FlowLayoutPanel buttons = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };

        Button cancel = new()
        {
            Text = "Cancel",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };

        Button accept = new()
        {
            Text = source >= 0
                ? "Copy"
                : "OK",
            AutoSize = true
        };

        accept.Click += (_, _) => Submit();
        buttons.Controls.AddRange([cancel, accept]);
        layout.Controls.Add(buttons, 0, 4);
        Controls.Add(layout);
        AcceptButton = accept;
        CancelButton = cancel;

        if (area != null)
            cells.SetSelection(TrafficVisibility.CellsForArea(data, area));
    }

    private void Submit()
    {
        try
        {
            if (source >= 0)
                TrafficVisibility.ValidateCopyCells(data, source, cells.SelectedCells, replace.Checked);
            else
                TrafficVisibility.ValidateArea(data, area, cells.SelectedCells);

            DialogResult = DialogResult.OK;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            error.Text = exception.Message;
        }
    }
}
