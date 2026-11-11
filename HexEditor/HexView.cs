using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HexEditor;

public class HexView : UserControl
{
    private const int BytesPerRow = 16;
    private const int Inset = 5;
    private const int HexColumn = 10, TextColumn = HexColumn + BytesPerRow * 3 + 2;

    private static readonly string[] hexStrings = [.. Enumerable.Range(0, 256).Select(b => b.ToString("X2"))];

    private record Edit(int Start, byte[] Removed, byte[] Inserted, long Before, long After);

    private readonly VScrollBar vScroll;
    private readonly HScrollBar hScroll;
    private readonly Stack<Edit> undo = new(), redo = new();
    private byte[] data = [];
    private int caret, anchor; // The selection is between them. The caret can be after the last byte, to add bytes.
    private bool lowNibble; // Typing the caret byte's second digit
    private bool textColumn;
    private float charWidth = 1;
    private int rowHeight = 1;
    private long revision, appliedRevision, nextRevision;
    private int interpretationLength;

    public event EventHandler SelectionChanged;

    public event Action<string> Feedback;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public byte[] HexData
    {
        get => data;
        set
        {
            data = value?.ToArray() ?? [];
            caret = anchor = 0;
            lowNibble = false;
            undo.Clear();
            redo.Clear();
            revision = appliedRevision = ++nextRevision;
            vScroll.Value = 0;
            hScroll.Value = 0;
            UpdateScroll();
            Changed();
        }
    }

    [DefaultValue(true)]
    public bool ReadOnly
    {
        get;
        set
        {
            field = value;
            lowNibble = false;
            Changed();
        }
    } = true;

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Modified => revision != appliedRevision;

    public void MarkApplied()
    {
        lowNibble = false;
        appliedRevision = revision;
        Changed();
    }

    [Browsable(false)] public bool CanUndo => !ReadOnly && undo.Count > 0;

    [Browsable(false)] public bool CanRedo => !ReadOnly && redo.Count > 0;

    [Browsable(false)] public bool TextColumnActive => textColumn;

    public void SetInsertMode(bool insert)
    {
        InsertMode = insert;
        lowNibble = false;
        Changed();
    }

    public void SwitchColumn()
    {
        textColumn = !textColumn;
        lowNibble = false;
        EnsureCaretVisible();
        Changed();
    }

    public void ShowInterpretation(int length)
    {
        interpretationLength = Math.Min(length, data.Length - SelectionStart);
        Invalidate();
    }

    [Browsable(false)]
    public bool InsertMode { get; private set; }

    [Browsable(false)]
    public int SelectionStart => Math.Min(caret, anchor);

    [Browsable(false)]
    public int SelectionLength => Math.Abs(caret - anchor);

    public HexView()
    {
        // First, as resizing and changing font update it
        vScroll = new()
        {
            TabStop = false
        };
        vScroll.Scroll += (_, _) => Invalidate();
        Controls.Add(vScroll);
        hScroll = new()
        {
            TabStop = false
        };
        hScroll.Scroll += (_, _) => Invalidate();
        Controls.Add(hScroll);
        AutoScroll = false;
        TabStop = true;
        AccessibleName = "Resource bytes";
        AccessibleDescription = "Use arrow keys to move, Shift to select, F6 to switch hex and text columns, and Tab to leave the byte view.";

        DoubleBuffered = true;

        BorderStyle = BorderStyle.FixedSingle;

        BackColor = SystemColors.Window; // Follows the light or dark colour mode

        Width = 100;
        Height = 100;

        Font = new(FontFamily.GenericMonospace, 10, FontStyle.Regular, GraphicsUnit.Point);

        ContextMenuStrip menu = new();
        ToolStripMenuItem undoItem = new("Undo", null, (_, _) => Undo())
        {
            ShortcutKeyDisplayString = "Ctrl+Z"
        };

        ToolStripMenuItem redoItem = new("Redo", null, (_, _) => Redo())
        {
            ShortcutKeyDisplayString = "Ctrl+Y"
        };

        ToolStripMenuItem cutItem = new("Cut", null, (_, _) => Cut())
        {
            ShortcutKeyDisplayString = "Ctrl+X"
        };

        ToolStripMenuItem copyItem = new("Copy", null, (_, _) => Copy())
        {
            ShortcutKeyDisplayString = "Ctrl+C"
        };

        ToolStripMenuItem pasteItem = new("Paste", null, (_, _) => Paste())
        {
            ShortcutKeyDisplayString = "Ctrl+V"
        };

        ToolStripMenuItem deleteItem = new("Delete", null, (_, _) => Delete(false))
        {
            ShortcutKeyDisplayString = "Del"
        };

        menu.Items.AddRange([undoItem, redoItem, new ToolStripSeparator(), cutItem, copyItem, pasteItem, deleteItem, new ToolStripSeparator(),
            new ToolStripMenuItem("Select All", null, (_, _) => SelectAll())
            {
                ShortcutKeyDisplayString = "Ctrl+A"
            },
            new ToolStripMenuItem("Switch Hex / Text", null, (_, _) => SwitchColumn())
            {
                ShortcutKeyDisplayString = "F6"
            }]);
        menu.Opening += (_, _) =>
        {
            bool selected = SelectionLength > 0;
            undoItem.Enabled = !ReadOnly && undo.Count > 0;
            redoItem.Enabled = !ReadOnly && redo.Count > 0;
            cutItem.Enabled = deleteItem.Enabled = !ReadOnly && selected;
            copyItem.Enabled = selected;
            pasteItem.Enabled = !ReadOnly && Clipboard.ContainsText();
            cutItem.Text = InsertMode
                ? "Cut bytes"
                : "Copy and clear bytes";
            deleteItem.Text = InsertMode
                ? "Delete bytes"
                : "Clear bytes (00)";
        };
        ContextMenuStrip = menu;

        Measure();
    }

    // Selects bytes and scrolls them to the middle of the view
    public void Select(int start, int length)
    {
        anchor = Math.Clamp(start, 0, data.Length);
        caret = Math.Clamp(start + length, 0, data.Length);
        lowNibble = false;
        TopRow = anchor / BytesPerRow - VisibleRows / 2;
        EnsureCaretVisible();
        Changed();
    }

    // Writes over bytes as an edit that can be undone, keeping the selection
    public void Overwrite(int offset, byte[] bytes)
    {
        if (ReadOnly || offset < 0 || offset + bytes.Length > data.Length)
            return;

        lowNibble = false;
        (int keepAnchor, int keepCaret) = (anchor, caret);
        Replace(offset, bytes.Length, bytes, caret);
        (anchor, caret) = (keepAnchor: keepAnchor, keepCaret: keepCaret);
        Changed();
    }

    private int RowCount => data.Length / BytesPerRow + 1; // The last row can be just the end, to add bytes there

    private int VisibleRows => Math.Max(1, (Viewport.Height - Inset) / rowHeight);

    private Rectangle Viewport => new(0, 0, Math.Max(1, ClientSize.Width - vScroll.Width), Math.Max(1, ClientSize.Height - (hScroll.Visible
        ? hScroll.Height
        : 0)));

    private int MaxTopRow => Math.Max(0, RowCount - VisibleRows);

    private int TopRow
    {
        get => vScroll.Value;
        set
        {
            vScroll.Value = Math.Clamp(value, 0, MaxTopRow);
            Invalidate();
        }
    }

    private void Measure()
    {
        charWidth = TextRenderer.MeasureText("0000000000", Font, Size.Empty, TextFormatFlags.NoPadding).Width / 10f;
        rowHeight = Font.Height;
        UpdateScroll();
    }

    private void UpdateScroll()
    {
        if (vScroll == null || hScroll == null)
            return; // The base constructor can resize before ours runs

        int contentWidth = (int)Math.Ceiling(Inset * 2 + (TextColumn + BytesPerRow) * charWidth);
        hScroll.Visible = contentWidth > ClientSize.Width - vScroll.Width;
        hScroll.SetBounds(0, ClientSize.Height - hScroll.Height, Math.Max(0, ClientSize.Width - vScroll.Width), hScroll.Height);
        vScroll.SetBounds(ClientSize.Width - vScroll.Width, 0, vScroll.Width, Viewport.Height);
        hScroll.LargeChange = Viewport.Width;
        hScroll.SmallChange = Math.Max(1, (int)charWidth * 3);
        hScroll.Maximum = Math.Max(0, contentWidth - 1);
        hScroll.Value = Math.Clamp(hScroll.Value, 0, Math.Max(0, contentWidth - Viewport.Width));
        vScroll.LargeChange = VisibleRows;
        vScroll.Maximum = Math.Max(0, RowCount - 1);
        vScroll.Value = Math.Clamp(vScroll.Value, 0, MaxTopRow);
    }

    private void Changed()
    {
        Invalidate();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void MoveCaret(int to, bool extend)
    {
        caret = Math.Clamp(to, 0, data.Length);

        if (!extend)
            anchor = caret;

        lowNibble = false;

        int row = caret / BytesPerRow;

        if (row < TopRow)
            TopRow = row;
        else if (row >= TopRow + VisibleRows)
            TopRow = row - VisibleRows + 1;

        EnsureCaretVisible();
        Changed();
    }

    private void EnsureCaretVisible()
    {
        int column = textColumn
            ? TextColumn + caret % BytesPerRow
            : HexCharacter(caret % BytesPerRow);
        int left = (int)(Inset + column * charWidth), right = left + (int)(charWidth * (textColumn
            ? 1
            : 2));
        int scroll = hScroll.Value;

        if (left < scroll)
            scroll = left;

        if (right > scroll + Viewport.Width)
            scroll = right - Viewport.Width;

        hScroll.Value = Math.Clamp(scroll, 0, Math.Max(0, hScroll.Maximum - hScroll.LargeChange + 1));
    }

    // Every edit goes through here so it can be undone.
    private void Replace(int start, int count, byte[] bytes, int newCaret, bool mergeNibble = false)
    {
        byte[] removed = data[start..(start + count)];
        byte[] result = new byte[data.Length - count + bytes.Length];
        data.AsSpan(0, start).CopyTo(result);
        bytes.CopyTo(result, start);
        data.AsSpan(start + count).CopyTo(result.AsSpan(start + bytes.Length));
        data = result;

        long before = revision;

        if (mergeNibble && undo.TryPeek(out Edit previous) && previous.Start == start)
        {
            undo.Pop();
            removed = previous.Removed;
            before = previous.Before;
        }

        revision = ++nextRevision;
        undo.Push(new(start, removed, [.. bytes], before, revision));
        redo.Clear();
        UpdateScroll();
        MoveCaret(newCaret, false);
    }

    // Undoes from one stack onto the other, so it's also redo
    public void Undo() => RestoreEdit(undo, redo, false);

    public void Redo() => RestoreEdit(redo, undo, true);

    private void RestoreEdit(Stack<Edit> from, Stack<Edit> to, bool forward)
    {
        if (ReadOnly || !from.TryPop(out Edit edit))
            return;

        byte[] bytes = forward
            ? edit.Inserted
            : edit.Removed;
        int count = forward
            ? edit.Removed.Length
            : edit.Inserted.Length;
        data = [.. data.AsSpan(0, edit.Start), .. bytes, .. data.AsSpan(edit.Start + count)];
        revision = forward
            ? edit.After
            : edit.Before;
        to.Push(edit);
        lowNibble = false;
        UpdateScroll();
        Select(edit.Start, bytes.Length);
    }

    public void SelectAll()
    {
        anchor = 0;
        caret = data.Length;
        Changed();
    }

    public void Copy()
    {
        if (SelectionLength == 0)
            return;

        byte[] bytes = data[SelectionStart..(SelectionStart + SelectionLength)];
        Clipboard.SetText(textColumn
            ? Encoding.Latin1.GetString(bytes)
            : string.Join(" ", bytes.Select(b => hexStrings[b])));
    }

    public void Cut()
    {
        if (ReadOnly || SelectionLength == 0)
            return;

        Copy();
        Delete(false);
    }

    public void Paste()
    {
        if (ReadOnly || !Clipboard.ContainsText())
            return;

        string text = Clipboard.GetText();
        byte[] bytes;

        if (textColumn)
        {
            bytes = Encoding.Latin1.GetBytes(text);
        }
        else
        {
            try
            {
                bytes = Convert.FromHexString(string.Concat(text.Where(c => !char.IsWhiteSpace(c))));
            }
            catch (FormatException)
            {
                Feedback?.Invoke("Enter pairs of hexadecimal digits, for example: 0A FF 20.");
                return;
            }
        }

        PasteBytes(bytes);
    }

    public bool PasteBytes(byte[] bytes)
    {
        if (ReadOnly || bytes.Length == 0)
            return false;

        int start = SelectionLength > 0
            ? SelectionStart
            : caret;

        if (!InsertMode && ((SelectionLength > 0 && SelectionLength != bytes.Length) || start + bytes.Length > data.Length))
        {
            Feedback?.Invoke("Overwrite keeps the size fixed. Select the same number of bytes, or place the caret where the pasted bytes fit.");
            return false;
        }

        lowNibble = false;
        int count = InsertMode
            ? SelectionLength
            : bytes.Length;
        Replace(start, count, bytes, start + bytes.Length);
        return true;
    }

    public void DeleteSelection() => Delete(false);

    // The selection, or the byte at the caret (Delete) or before it (Backspace)
    private void Delete(bool before)
    {
        if (ReadOnly)
            return;

        int start = SelectionLength > 0
            ? SelectionStart
            : before
                ? caret - 1
                : caret;
        int count = SelectionLength > 0
            ? SelectionLength
            : 1;

        if (start < 0 || start + count > data.Length)
            return;

        lowNibble = false;
        Replace(start, count, InsertMode
            ? []
            : new byte[count], start);
    }

    protected override bool IsInputKey(Keys keyData)
    {
        return (keyData & ~Keys.Shift) is Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown
            || (keyData & ~(Keys.Shift | Keys.Control)) is Keys.Home or Keys.End
            || base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        int rowStart = caret - caret % BytesPerRow;
        int page = VisibleRows * BytesPerRow;

        switch (e.KeyCode)
        {
            case Keys.Left:
                MoveCaret(caret - 1, e.Shift);
                break;
            case Keys.Right:
                MoveCaret(caret + 1, e.Shift);
                break;
            case Keys.Up:
                MoveCaret(caret >= BytesPerRow
                    ? caret - BytesPerRow
                    : caret, e.Shift);
                break;
            case Keys.Down:
                MoveCaret(caret + BytesPerRow <= data.Length
                    ? caret + BytesPerRow
                    : caret, e.Shift);
                break;
            case Keys.PageUp:
                MoveCaret(caret - page, e.Shift);
                break;
            case Keys.PageDown:
                MoveCaret(caret + page, e.Shift);
                break;
            case Keys.Home:
                MoveCaret(e.Control
                    ? 0
                    : rowStart, e.Shift);
                break;

            // Selecting goes past the row's last byte so it's included
            case Keys.End:
                MoveCaret(e.Control
                    ? data.Length
                    : rowStart + (e.Shift
                        ? BytesPerRow
                        : BytesPerRow - 1), e.Shift);
                break;
            case Keys.F6:
                SwitchColumn();
                break;
            case Keys.Delete:
                Delete(false);
                break;
            case Keys.Back:
                Delete(true);
                break;
            case Keys.A when e.Control:
                SelectAll();
                break;
            case Keys.C when e.Control:
                Copy();
                break;
            case Keys.X when e.Control:
                Cut();
                break;
            case Keys.V when e.Control:
                Paste();
                break;
            case Keys.Z when e.Control && e.Shift:
                Redo();
                break;
            case Keys.Z when e.Control:
                Undo();
                break;
            case Keys.Y when e.Control:
                Redo();
                break;
            default:
                return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);

        if (ReadOnly || char.IsControl(e.KeyChar))
            return;

        e.Handled = true;

        if (SelectionLength > 0 && !lowNibble)
            MoveCaret(SelectionStart, false); // Typing starts at the selection

        bool currentAdd = InsertMode;

        if (!currentAdd && caret == data.Length)
        {
            Feedback?.Invoke("End of data. Choose Insert / delete bytes to change the resource size.");
            return;
        }

        if (textColumn)
        {
            if (e.KeyChar <= 0xFF)
                Replace(caret, currentAdd
                    ? 0
                    : 1, [(byte)e.KeyChar], caret + 1);
        }
        else if (Uri.IsHexDigit(e.KeyChar))
        {
            int digit = Uri.FromHex(e.KeyChar);

            if (!lowNibble)
            {
                int low = currentAdd
                    ? 0
                    : data[caret] & 0xF;
                Replace(caret, currentAdd
                    ? 0
                    : 1, [(byte)(digit << 4 | low)], caret);
                lowNibble = true;
                Invalidate();
            }
            else
            {
                Replace(caret, 1, [(byte)(data[caret] & 0xF0 | digit)], caret + 1, mergeNibble: true);
            }
        }
    }

    // Hit-tests coordinate to byte index and text column flag
    private (int Index, bool Text) HitTest(Point point)
    {
        int row = TopRow + Math.Max(0, (point.Y - Inset) / rowHeight);
        float column = (point.X + hScroll.Value - Inset) / charWidth;
        int index;
        bool text = textColumn;

        if (column >= TextColumn - 1)
        {
            index = (int)(column - TextColumn);
            text = true;
        }
        else if (column >= HexColumn - 1)
        {
            float hex = column - HexColumn;
            index = (int)((hex >= BytesPerRow / 2 * 3
                ? hex - 1
                : hex) / 3);
            text = false;
        }
        else
        {
            index = 0;
        }

        return (Math.Min(row * BytesPerRow + Math.Clamp(index, 0, BytesPerRow - 1), data.Length), text: text);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        if (e.Button != MouseButtons.Left)
            return;

        (int index, bool text) = HitTest(e.Location);
        textColumn = text;
        MoveCaret(index, ModifierKeys.HasFlag(Keys.Shift));
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (e.Button != MouseButtons.Left)
            return;

        int index = HitTest(e.Location).Index;
        MoveCaret(index >= anchor
            ? index + 1
            : index, true); // Include the byte under the mouse
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        TopRow -= Math.Sign(e.Delta) * 3;
        base.OnMouseWheel(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateScroll();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        Measure();
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        lowNibble = false;
        Invalidate();
    }

    private float X(int column) => Inset + column * charWidth - hScroll.Value;

    private static int HexCharacter(int index) => HexColumn + index * 3 + (index >= BytesPerRow / 2
        ? 1
        : 0);

    private void DrawText(Graphics g, string text, float x, int y, Color color)
    {
        TextRenderer.DrawText(g, text, Font, new Point((int)x, y), color, TextFormatFlags.NoPadding);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SetClip(Viewport);

        int selectionStart = SelectionStart, selectionEnd = SelectionStart + SelectionLength;

        for (int visible = 0; visible <= VisibleRows; visible++)
        {
            int rowStart = (TopRow + visible) * BytesPerRow;

            if (rowStart > data.Length || (rowStart == data.Length && rowStart > 0))
                break;

            int y = Inset + visible * rowHeight;
            DrawText(g, rowStart.ToString("X8"), X(0), y, SystemColors.GrayText);

            for (int i = rowStart; i < Math.Min(rowStart + BytesPerRow, data.Length); i++)
            {
                int index = i - rowStart;
                float hexX = X(HexCharacter(index)), textX = X(TextColumn + index);
                bool selected = i >= selectionStart && i < selectionEnd;

                if (selected)
                {
                    // Up to the next byte, so a selection is one block
                    float width = i + 1 < selectionEnd && index < BytesPerRow - 1
                        ? X(HexCharacter(index + 1)) - hexX
                        : 2 * charWidth;
                    g.FillRectangle(SystemBrushes.Highlight, hexX, y, width, rowHeight);
                    g.FillRectangle(SystemBrushes.Highlight, textX, y, charWidth, rowHeight);
                }

                Color color = selected
                    ? SystemColors.HighlightText
                    : SystemColors.WindowText;
                DrawText(g, hexStrings[data[i]], hexX, y, color);
                byte b = data[i];
                DrawText(g, b is >= 0x20 and < 0x7F
                    ? ((char)b).ToString()
                    : ".", textX, y, color);

                if (i >= selectionStart && i < selectionStart + interpretationLength)
                {
                    using Pen rangePen = new(SystemColors.HotTrack, 2);
                    g.DrawRectangle(rangePen, hexX, y + 1, charWidth * 2, rowHeight - 2);
                    g.DrawRectangle(rangePen, textX, y + 1, charWidth, rowHeight - 2);
                }
            }
        }

        if (!Focused)
            return;

        // The caret in the column being typed in, and an outline of the same byte in the other
        int caretRow = caret / BytesPerRow - TopRow;

        if (caretRow < 0 || caretRow > VisibleRows)
            return;

        int caretIndex = caret % BytesPerRow;
        int caretY = Inset + caretRow * rowHeight;
        float caretHex = X(HexCharacter(caretIndex)), caretText = X(TextColumn + caretIndex);
        float activeX = textColumn
            ? caretText
            : caretHex + (lowNibble
                ? charWidth
                : 0);

        if (InsertMode && !ReadOnly)
            g.FillRectangle(SystemBrushes.WindowText, activeX - 1, caretY, 2, rowHeight);
        else
            g.DrawRectangle(SystemPens.WindowText, activeX, caretY, charWidth, rowHeight - 1);

        using Pen outline = new(SystemColors.GrayText)
        {
            DashStyle = System.Drawing.Drawing2D.DashStyle.Dot
        };

        g.DrawRectangle(outline, textColumn
            ? caretHex
            : caretText, caretY, textColumn
            ? 2 * charWidth
            : charWidth, rowHeight - 1);
    }
}
