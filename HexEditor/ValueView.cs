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
    private static readonly (string Name, int Size, Func<byte[], string> Format, Func<string, byte[]> Parse)[] Types =
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

    public ValueView()
    {
        grid = new DataGridView
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
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Type", ReadOnly = true, FillWeight = 20, SortMode = DataGridViewColumnSortMode.NotSortable });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Value", ToolTipText = "Type a value to write it, in edit mode. Integers can be hexadecimal, starting with 0x.", FillWeight = 40, SortMode = DataGridViewColumnSortMode.NotSortable });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Hex", ReadOnly = true, FillWeight = 40, SortMode = DataGridViewColumnSortMode.NotSortable });
        foreach ((string name, _, _, _) in Types)
            grid.Rows.Add(name, "", "");
        grid.CellEndEdit += (_, e) => Apply(e.RowIndex);
        grid.DataError += (_, e) => e.ThrowException = false;

        // Radio buttons in one panel switch each other off
        littleEndian = new RadioButton { Text = "Little endian", AutoSize = true, Checked = true };
        bigEndian = new RadioButton { Text = "Big endian", AutoSize = true };
        bigEndian.CheckedChanged += (_, _) => UpdateValues();
        FlowLayoutPanel endian = new() { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(3, 0, 0, 0) };
        endian.Controls.AddRange([littleEndian, bigEndian]);

        Controls.Add(grid);
        Controls.Add(endian);
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
        set => (value ? bigEndian : littleEndian).Checked = true;
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
        byte[] data = Source?.HexData ?? [];
        int offset = Source?.SelectionStart ?? 0;
        for (int i = 0; i < Types.Length; i++)
        {
            (_, int size, Func<byte[], string> format, _) = Types[i];
            DataGridViewRow row = grid.Rows[i];
            bool fits = offset + size <= data.Length;
            row.Cells[ValueColumn].ReadOnly = !fits || Source.ReadOnly;
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
    }

    // A value that doesn't parse goes back to what it was, with the reason on the cell
    private void Apply(int index)
    {
        DataGridViewCell cell = grid.Rows[index].Cells[ValueColumn];
        (_, int size, _, Func<string, byte[]> parse) = Types[index];
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
