using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using System.Windows.Forms;

namespace HexEditor;

public class ValueView : UserControl
{
    private const int ValueColumn = 1, HexColumn = 2;

    // Bytes are least significant first, as BitConverter uses on Windows
    private static readonly (string Name, int Size, Func<byte[], string> Format, Func<string, byte[]> Parse)[] types =
        [
            ("Int8", 1, b => ((sbyte)b[0]).ToString(), Integer<sbyte>),
        ("UInt8", 1, b => b[0].ToString(), Integer<byte>),
        ("Int16", 2, b => BitConverter.ToInt16(b).ToString(), Integer<short>),
        ("UInt16", 2, b => BitConverter.ToUInt16(b).ToString(), Integer<ushort>),
        ("Int32", 4, b => BitConverter.ToInt32(b).ToString(), Integer<int>),
        ("UInt32", 4, b => BitConverter.ToUInt32(b).ToString(), Integer<uint>),
        ("Int64", 8, b => BitConverter.ToInt64(b).ToString(), Integer<long>),
        ("UInt64", 8, b => BitConverter.ToUInt64(b).ToString(), Integer<ulong>),
        ("Half", 2, b => ((float)BitConverter.ToHalf(b)).ToString(CultureInfo.InvariantCulture), // Exact, Half rounds to its own precision
            s => BitConverter.GetBytes(Half.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture))),
        ("Float", 4, b => BitConverter.ToSingle(b).ToString(CultureInfo.InvariantCulture),
            s => BitConverter.GetBytes(float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture))),
        ("Double", 8, b => BitConverter.ToDouble(b).ToString(CultureInfo.InvariantCulture),
            s => BitConverter.GetBytes(double.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture))),
        ("Binary", 1, b => Convert.ToString(b[0], 2).PadLeft(8, '0'), s => [Convert.ToByte(s, 2)])
        ];

    private readonly RadioButton littleEndian, bigEndian;
    private readonly DataGridView grid;

    private readonly Label range = new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        Padding = new(3, 4, 3, 4)
    };

    private bool updating;

    public ValueView()
    {
        grid = new()
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText,
            BackgroundColor = SystemColors.Window
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn()
        {
            HeaderText = "type",
            ReadOnly = true,
            FillWeight = 20,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn()
        {
            HeaderText = "Value",
            ToolTipText = "Type a value to write it, in edit mode. Integers can be hexadecimal, starting with 0x.",
            FillWeight = 40,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn()
        {
            HeaderText = "Hex",
            ReadOnly = true,
            FillWeight = 40,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        foreach ((string name, _, _, _) in types)
            grid.Rows.Add(name, "", "");

        grid.CellValidating += (_, e) =>
        {
            if (updating || e.ColumnIndex != ValueColumn || !grid.IsCurrentCellInEditMode)
                return;

            try
            {
                types[e.RowIndex].Parse(Convert.ToString(e.FormattedValue).Trim());
                grid.Rows[e.RowIndex].Cells[ValueColumn].ErrorText = "";
            }
            catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
            {
                e.Cancel = true;
                grid.Rows[e.RowIndex].Cells[ValueColumn].ErrorText = "Enter a value that fits " + types[e.RowIndex].Name + ". " + ex.Message;
                range.Text = "Invalid value. Correct it or press Esc to cancel this edit.";
            }
        };
        grid.CellEndEdit += (_, e) =>
        {
            if (!updating)
                Apply(e.RowIndex);
        };
        grid.CurrentCellChanged += (_, _) => ShowRange();
        grid.Enter += (_, _) => ShowRange();
        grid.DataError += (_, e) => e.ThrowException = false;

        // Radio buttons in one panel switch each other off
        littleEndian = new()
        {
            Text = "Little endian",
            AutoSize = true,
            Checked = true
        };
        bigEndian = new()
        {
            Text = "Big endian",
            AutoSize = true
        };
        bigEndian.CheckedChanged += (_, _) => UpdateValues();
        FlowLayoutPanel endian = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new(3, 0, 0, 0)
        };

        endian.Controls.AddRange([littleEndian, bigEndian]);

        Controls.Add(grid);
        Controls.Add(range);
        Controls.Add(endian);
    }

    public bool CommitEdit() => grid.EndEdit();

    private void ShowRange()
    {
        if (Source == null || grid.CurrentRow == null)
            return;

        (string name, int size, _, _) = types[grid.CurrentRow.Index];
        int offset = Source.SelectionStart;
        bool fits = offset + size <= Source.HexData.Length;
        Source.ShowInterpretation(fits
            ? size
            : 0);
        range.Text = fits
            ? $"{name}: {size} byte{(size == 1
                ? ""
                : "s")} at 0x{offset:X}–0x{offset + size - 1:X}\n{(BigEndian
                ? "Big"
                : "Little")} endian · {(Source.ReadOnly
                ? "Read only"
                : "Writes the outlined bytes")}"
            : $"{name} needs {size} bytes; {Source.HexData.Length - offset} remain.";
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public HexView Source
    {
        get;
        set
        {
            if (field != null)
                field.SelectionChanged -= Source_SelectionChanged;

            field = value;

            if (field != null)
                field.SelectionChanged += Source_SelectionChanged;

            UpdateValues();
        }
    }

    [DefaultValue(false)]
    public bool BigEndian
    {
        get => bigEndian.Checked;
        set => (value
            ? bigEndian
            : littleEndian).Checked = true;
    }

    // Decimal, or hexadecimal starting with 0x
    private static byte[] Integer<T>(string s) where T : IBinaryInteger<T>
    {
        T value = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? T.Parse(s.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture)
            : T.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
        byte[] bytes = new byte[value.GetByteCount()];
        value.WriteLittleEndian(bytes);
        return bytes;
    }

    private void Source_SelectionChanged(object sender, EventArgs e) => UpdateValues();

    private void UpdateValues()
    {
        updating = true;

        if (grid.IsCurrentCellInEditMode)
            grid.CancelEdit();

        byte[] data = Source?.HexData ?? [];
        int offset = Source?.SelectionStart ?? 0;

        for (int i = 0; i < types.Length; i++)
        {
            (_, int size, Func<byte[], string> format, _) = types[i];
            DataGridViewRow row = grid.Rows[i];
            bool fits = offset + size <= data.Length;
            row.Cells[ValueColumn].ReadOnly = !fits || Source == null || Source.ReadOnly;
            row.Cells[ValueColumn].ErrorText = "";

            if (!fits)
            {
                row.Cells[ValueColumn].Value = row.Cells[HexColumn].Value = "";
                continue;
            }

            byte[] value = data[offset..(offset + size)];

            if (!bigEndian.Checked)
                Array.Reverse(value);

            row.Cells[HexColumn].Value = "0x" + Convert.ToHexString(value); // Most significant first
            Array.Reverse(value);
            row.Cells[ValueColumn].Value = format(value);
        }

        updating = false;
        ShowRange();
    }

    // A value that doesn't parse goes back to what it was, with the reason on the cell
    private void Apply(int index)
    {
        DataGridViewCell cell = grid.Rows[index].Cells[ValueColumn];
        (_, int size, _, Func<string, byte[]> parse) = types[index];
        int offset = Source.SelectionStart;
        byte[] bytes;

        try
        {
            bytes = parse((cell.Value as string ?? "").Trim());
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
        {
            UpdateValues();
            cell.ErrorText = ex.Message;
            return;
        }

        if (bigEndian.Checked)
            Array.Reverse(bytes);

        if (bytes.AsSpan().SequenceEqual(Source.HexData.AsSpan(offset, size)))
            UpdateValues(); // Shows it as it's formatted
        else
            Source.Overwrite(offset, bytes); // Which updates the values
    }
}
