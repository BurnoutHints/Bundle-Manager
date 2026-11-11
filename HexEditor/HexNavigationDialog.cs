using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HexEditor;

public sealed class HexNavigationDialog : Form
{
    private readonly TextBox input = new()
    {
        Dock = DockStyle.Fill
    };

    private readonly ComboBox format = new()
    {
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList
    };

    private readonly Label error = new()
    {
        AutoSize = true,
        Dock = DockStyle.Fill
    };

    private readonly bool find;
    private readonly int limit;

    public byte[] Pattern { get; private set; }

    public int Offset { get; private set; }

    public string InputText => input.Text;

    public int FormatIndex => format.SelectedIndex;

    public HexNavigationDialog(bool initialFind, int initialLimit, string previous = "", int previousFormat = 0)
    {
        find = initialFind;
        limit = initialLimit;
        Text = initialFind
            ? "Find bytes or text"
            : "Go to offset";
        AutoScaleMode = AutoScaleMode.Font;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = MaximizeBox = false;
        ClientSize = new(470, 200);
        MinimumSize = new(360, 235);
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            Padding = new(12),
            ColumnCount = 2,
            RowCount = 4
        };

        layout.ColumnStyles.Add(new(SizeType.AutoSize));
        layout.ColumnStyles.Add(new(SizeType.Percent, 100));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.Percent, 100));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.Controls.Add(new Label()
        {
            Text = initialFind
                ? "&Find:"
                : "&Offset:",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 0);
        layout.Controls.Add(input, 1, 0);
        layout.Controls.Add(new Label()
        {
            Text = "&Format:",
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 1);
        layout.Controls.Add(format, 1, 1);
        format.Items.AddRange(initialFind
            ? ["Hex bytes", "Text (UTF-8)", "Text (Latin-1)"]
            : ["Hexadecimal", "Decimal"]);
        format.SelectedIndex = Math.Clamp(previousFormat, 0, format.Items.Count - 1);
        input.Text = previous;
        error.Text = initialFind
            ? "Hex example: 0A FF 20. Text searches are case-sensitive."
            : $"Offsets are relative to this data block. Range: 0–{initialLimit:N0} (0x{initialLimit:X}).";
        layout.Controls.Add(error, 0, 2);
        layout.SetColumnSpan(error, 2);
        FlowLayoutPanel actions = new()
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

        Button ok = new()
        {
            Text = initialFind
                ? "Find"
                : "Go",
            AutoSize = true
        };

        ok.Click += (_, _) => Submit();
        actions.Controls.AddRange([cancel, ok]);
        layout.Controls.Add(actions, 0, 3);
        layout.SetColumnSpan(actions, 2);
        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = cancel;
        Shown += (_, _) =>
        {
            input.Focus();
            input.SelectAll();
        };
    }

    private void Submit()
    {
        try
        {
            if (find)
            {
                Pattern = ParsePattern(input.Text, format.SelectedIndex);

                if (Pattern.Length == 0)
                    throw new FormatException("Enter bytes or text to find.");
            }
            else
            {
                string text = input.Text.Trim();

                if (format.SelectedIndex == 0 && text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    text = text[2..];

                if (!int.TryParse(text, format.SelectedIndex == 0
                    ? NumberStyles.AllowHexSpecifier
                    : NumberStyles.Integer,
                                    CultureInfo.InvariantCulture, out int offset) || offset < 0 || offset > limit)
                    throw new FormatException($"Enter an offset from 0 to {limit:N0} (0x{limit:X}).");

                Offset = offset;
            }

            DialogResult = DialogResult.OK;
        }
        catch (Exception ex) when (ex is FormatException or EncoderFallbackException)
        {
            error.Text = ex.Message;
            input.Focus();
        }
    }

    public static byte[] ParsePattern(string text, int format) => format switch
    {
        0 => Convert.FromHexString(string.Concat(text.Where(c => !char.IsWhiteSpace(c)))),
        1 => Encoding.UTF8.GetBytes(text),
        _ => Encoding.GetEncoding("iso-8859-1", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetBytes(text)
    };

    public static int Find(byte[] data, byte[] pattern, int start, bool backwards, out bool wrapped)
    {
        wrapped = false;

        if (pattern.Length == 0 || pattern.Length > data.Length)
            return -1;

        int last = data.Length - pattern.Length;
        start = Math.Clamp(start, -1, last + 1);

        if (!backwards)
        {
            int first = Math.Max(0, start);

            for (int i = first; i <= last; i++)
                if (data.AsSpan(i, pattern.Length).SequenceEqual(pattern))
                    return i;

            wrapped = true;

            for (int i = 0; i < first && i <= last; i++)
                if (data.AsSpan(i, pattern.Length).SequenceEqual(pattern))
                    return i;
        }
        else
        {
            int first = Math.Min(start, last);

            for (int i = first; i >= 0; i--)
                if (data.AsSpan(i, pattern.Length).SequenceEqual(pattern))
                    return i;

            wrapped = true;

            for (int i = last; i > first; i--)
                if (data.AsSpan(i, pattern.Length).SequenceEqual(pattern))
                    return i;
        }

        return -1;
    }
}
