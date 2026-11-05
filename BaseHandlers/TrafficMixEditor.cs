using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Windows.Forms;
using static BaseHandlers.Traffic;

namespace BaseHandlers;

public sealed class TrafficMixEditor : UITypeEditor
{
    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) => UITypeEditorEditStyle.Modal;

    public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
    {
        using TrafficMixDialog dialog = new((List<FlowVehicleType>)value, TrafficReferences.Data.maVehicleTypes);

        if (dialog.ShowDialog() != DialogResult.OK)
            return value;

        return ((List<FlowVehicleType>)value).Select(v => (v.mVehicleType, v.muCumulativeProb)).SequenceEqual(dialog.Result.Select(v => (v.mVehicleType, v.muCumulativeProb)))
            ? value
            : dialog.Result;
    }
}

public sealed class TrafficMixDialog : Form
{
    private sealed record Choice(VehicleType Value, string Label);

    private readonly DataGridView grid = new()
    {
        Dock = DockStyle.Fill,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        BackgroundColor = SystemColors.Window
    };

    private readonly Label message = new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        Padding = new(8)
    };

    public List<FlowVehicleType> Result { get; private set; }

    public TrafficMixDialog(List<FlowVehicleType> values, List<VehicleType> vehicles)
    {
        Text = "Vehicle mix";
        ClientSize = new(650, 420);
        MinimumSize = new(460, 300);
        AutoScaleMode = AutoScaleMode.Font;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = MaximizeBox = false;
        grid.Columns.Add(new DataGridViewComboBoxColumn()
        {
            HeaderText = "Vehicle type",
            DataSource = vehicles.Select((v, i) => new Choice(v, $"{i}: {v}")).ToArray(),
            DisplayMember = "Label",
            ValueMember = "Value",
            ValueType = typeof(VehicleType),
            FillWeight = 70
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn()
        {
            HeaderText = "Chance (%)",
            ValueType = typeof(decimal),
            FillWeight = 30,
            DefaultCellStyle = new()
            {
                Format = "0.##",
                Alignment = DataGridViewContentAlignment.MiddleRight
            }
        });
        grid.DataError += (_, e) =>
        {
            e.ThrowException = false;
            message.Text = "Choose a vehicle and enter a non-negative number for its chance.";
        };
        int previous = 0;

        foreach (FlowVehicleType item in values)
        {
            grid.Rows.Add(item.mVehicleType, (item.muCumulativeProb - previous) * 100m / 255);
            previous = item.muCumulativeProb;
        }

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            Padding = new(6),
            FlowDirection = FlowDirection.RightToLeft
        };

        Button cancel = new()
        {
            Text = "Cancel",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };

        Button ok = new()
        {
            Text = "OK",
            AutoSize = true
        };

        Button normalize = new()
        {
            Text = "Normalize to 100%",
            AutoSize = true
        };

        Button currentRemove = new()
        {
            Text = "Remove row",
            AutoSize = true
        };

        currentRemove.Click += (_, _) =>
        {
            if (grid.CurrentRow is { IsNewRow: false } row)
                grid.Rows.Remove(row);
        };
        normalize.Click += (_, _) =>
        {
            try
            {
                List<(VehicleType Vehicle, decimal Chance)> rows = ReadRows();
                decimal sum = rows.Sum(r => r.Chance);

                if (sum <= 0)
                    throw new ArgumentException("Enter at least one positive chance.");

                for (int currentI = 0; currentI < rows.Count; currentI++)
                    grid.Rows[currentI].Cells[1].Value = rows[currentI].Chance * 100 / sum;

                message.Text = "Total is 100%. The saved chances are rounded to the game's 255 steps.";
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException)
            {
                message.Text = ex.Message;
            }
        };
        ok.Click += (_, _) =>
        {
            try
            {
                List<(VehicleType Vehicle, decimal Chance)> rows = ReadRows();

                if (rows.Count > 0 && Math.Abs(rows.Sum(r => r.Chance) - 100m) > 0.01m)
                    throw new ArgumentException("Chances must total 100%. Adjust them or choose Normalize to 100%.");

                Result = Compile(rows);
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException)
            {
                message.Text = ex.Message;
            }
        };
        buttons.Controls.AddRange([cancel, ok, normalize, currentRemove]);
        message.Text = "Choose vehicles and their individual chances. Add a row at the bottom. Chances should total 100%.";
        Controls.Add(grid);
        Controls.Add(message);
        Controls.Add(buttons);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private List<(VehicleType Vehicle, decimal Chance)> ReadRows()
    {
        if (!grid.EndEdit())
            throw new ArgumentException("Finish editing the current value first.");

        List<(VehicleType, decimal)> values = [.. grid.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).Select(r => (
            r.Cells[0].Value as VehicleType ?? throw new ArgumentException($"Choose a vehicle in row {r.Index + 1}."),
            Convert.ToDecimal(r.Cells[1].Value ?? throw new ArgumentException($"Enter a chance in row {r.Index + 1}."))))];

        if (values.Any(v => v.Item2 < 0))
            throw new ArgumentException("Chances cannot be negative.");

        return values;
    }

    public static List<FlowVehicleType> Compile(List<(VehicleType Vehicle, decimal Chance)> rows)
    {
        if (rows.Count > 255 || rows.Any(r => r.Vehicle == null || r.Chance < 0))
            throw new ArgumentException("Choose up to 255 vehicles with non-negative chances.");

        if (rows.Count == 0)
            return [];

        decimal total = rows.Sum(r => r.Chance);

        if (total <= 0)
            throw new ArgumentException("Enter at least one positive chance.");

        decimal cumulative = 0;

        return [.. rows.Select(r =>
        {
            cumulative += r.Chance;
            return new FlowVehicleType()
            {
                mVehicleType = r.Vehicle,
                muCumulativeProb = (byte)Math.Round(cumulative * 255 / total, MidpointRounding.AwayFromZero)
            };
        })];
    }
}
