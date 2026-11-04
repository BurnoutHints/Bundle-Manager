using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Windows.Forms;

namespace HexEditor;

public class HexView : UserControl
{
    private const int BytesPerRow = 16;
    private const int Inset = 5;
    private const int HexColumn = 10, TextColumn = HexColumn + BytesPerRow * 3 + 2;

    private static readonly string[] HexStrings = [.. Enumerable.Range(0, 256).Select(b => b.ToString("X2"))];

    private record Edit(int Start, byte[] Removed, byte[] Inserted);

    private readonly VScrollBar vScroll;
    private readonly Stack<Edit> undo = new(), redo = new();
    private byte[] data = [];
    private int caret, anchor; // The selection is between them. The caret can be after the last byte, to add bytes.
    private bool lowNibble; // Typing the caret byte's second digit
    private bool textColumn;
    private float charWidth = 1;
    private int rowHeight = 1;

    public event EventHandler SelectionChanged;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public byte[] HexData
    {
        get => data;
        set
        {
            data = value ?? [];
            caret = anchor = 0;
            lowNibble = false;
            undo.Clear();
            redo.Clear();
            Modified = false;
            vScroll.Value = 0;
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

    // Set by edits, cleared by setting HexData
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Modified { get; set; }

    [Browsable(false)]
    public bool InsertMode { get; private set; }

    [Browsable(false)]
    public int SelectionStart => Math.Min(caret, anchor);

    [Browsable(false)]
    public int SelectionLength => Math.Abs(caret - anchor);

    public HexView()
    {
        // First, as resizing and changing font update it
        vScroll = new VScrollBar();
        vScroll.Dock = DockStyle.Right;
        vScroll.Scroll += (_, _) => Invalidate();
        Controls.Add(vScroll);

        DoubleBuffered = true;

        BorderStyle = BorderStyle.FixedSingle;

        BackColor = SystemColors.Window; // Follows the light or dark colour mode

        Width = 100;
        Height = 100;

        Font = new Font(FontFamily.GenericMonospace, 10, FontStyle.Regular, GraphicsUnit.Point);

        ContextMenuStrip menu = new();
        ToolStripMenuItem undoItem = new("Undo", null, (_, _) => Undo(undo, redo)) { ShortcutKeyDisplayString = "Ctrl+Z" };
        ToolStripMenuItem redoItem = new("Redo", null, (_, _) => Undo(redo, undo)) { ShortcutKeyDisplayString = "Ctrl+Y" };
        ToolStripMenuItem cutItem = new("Cut", null, (_, _) => Cut()) { ShortcutKeyDisplayString = "Ctrl+X" };
        ToolStripMenuItem copyItem = new("Copy", null, (_, _) => Copy()) { ShortcutKeyDisplayString = "Ctrl+C" };
        ToolStripMenuItem pasteItem = new("Paste", null, (_, _) => Paste()) { ShortcutKeyDisplayString = "Ctrl+V" };
        ToolStripMenuItem deleteItem = new("Delete", null, (_, _) => Delete(false)) { ShortcutKeyDisplayString = "Del" };
        menu.Items.AddRange([undoItem, redoItem, new ToolStripSeparator(), cutItem, copyItem, pasteItem, deleteItem, new ToolStripSeparator(),
            new ToolStripMenuItem("Select All", null, (_, _) => SelectAll()) { ShortcutKeyDisplayString = "Ctrl+A" }]);
        menu.Opening += (_, _) =>
        {
            bool selected = SelectionLength > 0;
            undoItem.Enabled = !ReadOnly && undo.Count > 0;
            redoItem.Enabled = !ReadOnly && redo.Count > 0;
            cutItem.Enabled = deleteItem.Enabled = !ReadOnly && selected;
            copyItem.Enabled = selected;
            pasteItem.Enabled = !ReadOnly && Clipboard.ContainsText();
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
        Changed();
    }

    // Writes over bytes as an edit that can be undone, keeping the selection
    public void Overwrite(int offset, byte[] bytes)
    {
        if (ReadOnly || offset < 0 || offset + bytes.Length > data.Length)
            return;
        (int keepAnchor, int keepCaret) = (anchor, caret);
        Replace(offset, bytes.Length, bytes, caret);
        (anchor, caret) = (keepAnchor, keepCaret);
        Changed();
    }

    private int RowCount => data.Length / BytesPerRow + 1; // The last row can be just the end, to add bytes there
    private int VisibleRows => Math.Max(1, (ClientSize.Height - Inset) / rowHeight);
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
        if (vScroll == null)
            return; // The base constructor can resize before ours runs
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
        Changed();
    }

    // Every edit goes through here so it can be undone.
    private void Replace(int start, int count, byte[] bytes, int newCaret, bool record = true)
    {
        byte[] removed = data[start..(start + count)];
        byte[] result = new byte[data.Length - count + bytes.Length];
        data.AsSpan(0, start).CopyTo(result);
        bytes.CopyTo(result, start);
        data.AsSpan(start + count).CopyTo(result.AsSpan(start + bytes.Length));
        data = result;

        if (record)
        {
            undo.Push(new Edit(start, removed, bytes));
            redo.Clear();
        }
        Modified = true;
        UpdateScroll();
        MoveCaret(newCaret, false);
    }

    // Undoes from one stack onto the other, so it's also redo
    private void Undo(Stack<Edit> from, Stack<Edit> to)
    {
        if (ReadOnly || !from.TryPop(out Edit edit))
            return;
        Replace(edit.Start, edit.Inserted.Length, edit.Removed, edit.Start, record: false);
        to.Push(new Edit(edit.Start, edit.Inserted, edit.Removed));
    }

    private void SelectAll()
    {
        anchor = 0;
        caret = data.Length;
        Changed();
    }

    private void Copy()
    {
        if (SelectionLength == 0)
            return;
        byte[] bytes = data[SelectionStart..(SelectionStart + SelectionLength)];
        Clipboard.SetText(textColumn ? Encoding.Latin1.GetString(bytes) : string.Join(" ", bytes.Select(b => HexStrings[b])));
    }

    private void Cut()
    {
        if (ReadOnly || SelectionLength == 0)
            return;
        Copy();
        Delete(false);
    }

    private void Paste()
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
                SystemSounds.Beep.Play();
                return;
            }
        }
        if (bytes.Length == 0)
            return;

        int start = SelectionLength > 0 ? SelectionStart : caret;
        int count = SelectionLength > 0 ? SelectionLength : InsertMode ? 0 : Math.Min(bytes.Length, data.Length - caret);
        Replace(start, count, bytes, start + bytes.Length);
    }

    // The selection, or the byte at the caret (Delete) or before it (Backspace)
    private void Delete(bool before)
    {
        if (ReadOnly)
            return;
        if (SelectionLength > 0)
            Replace(SelectionStart, SelectionLength, [], SelectionStart);
        else if (before && caret > 0)
            Replace(caret - 1, 1, [], caret - 1);
        else if (!before && caret < data.Length)
            Replace(caret, 1, [], caret);
    }

    protected override bool IsInputKey(Keys keyData)
    {
        return (keyData & ~Keys.Shift) is Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown or Keys.Tab
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
            case Keys.Left: MoveCaret(caret - 1, e.Shift); break;
            case Keys.Right: MoveCaret(caret + 1, e.Shift); break;
            case Keys.Up: MoveCaret(caret >= BytesPerRow ? caret - BytesPerRow : caret, e.Shift); break;
            case Keys.Down: MoveCaret(caret + BytesPerRow <= data.Length ? caret + BytesPerRow : caret, e.Shift); break;
            case Keys.PageUp: MoveCaret(caret - page, e.Shift); break;
            case Keys.PageDown: MoveCaret(caret + page, e.Shift); break;
            case Keys.Home: MoveCaret(e.Control ? 0 : rowStart, e.Shift); break;
            // Selecting goes past the row's last byte so it's included
            case Keys.End: MoveCaret(e.Control ? data.Length : rowStart + (e.Shift ? BytesPerRow : BytesPerRow - 1), e.Shift); break;
            case Keys.Tab:
                textColumn = !textColumn;
                lowNibble = false;
                Changed();
                break;
            case Keys.Insert when !ReadOnly:
                InsertMode = !InsertMode;
                Changed();
                break;
            case Keys.Delete: Delete(false); break;
            case Keys.Back: Delete(true); break;
            case Keys.A when e.Control: SelectAll(); break;
            case Keys.C when e.Control: Copy(); break;
            case Keys.X when e.Control: Cut(); break;
            case Keys.V when e.Control: Paste(); break;
            case Keys.Z when e.Control && e.Shift: Undo(redo, undo); break;
            case Keys.Z when e.Control: Undo(undo, redo); break;
            case Keys.Y when e.Control: Undo(redo, undo); break;
            default: return;
        }
        e.Handled = true;
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        if (ReadOnly || char.IsControl(e.KeyChar))
            return;
        e.Handled = true;

        if (SelectionLength > 0 && !lowNibble)
            MoveCaret(SelectionStart, false); // Typing starts at the selection
        bool add = InsertMode || caret == data.Length;

        if (textColumn)
        {
            if (e.KeyChar <= 0xFF)
                Replace(caret, add ? 0 : 1, [(byte)e.KeyChar], caret + 1);
        }
        else if (Uri.IsHexDigit(e.KeyChar))
        {
            int digit = Uri.FromHex(e.KeyChar);
            if (!lowNibble)
            {
                int low = add ? 0 : data[caret] & 0xF;
                Replace(caret, add ? 0 : 1, [(byte)(digit << 4 | low)], caret);
                lowNibble = true;
                Invalidate();
            }
            else
            {
                Replace(caret, 1, [(byte)(data[caret] & 0xF0 | digit)], caret + 1);
            }
        }
    }

    // The byte under a point and whether it's in the text column. The offset column gives the row's first byte.
    private (int Index, bool Text) HitTest(Point point)
    {
        int row = TopRow + Math.Max(0, (point.Y - Inset) / rowHeight);
        float column = (point.X - Inset) / charWidth;
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
            index = (int)((hex >= BytesPerRow / 2 * 3 ? hex - 1 : hex) / 3);
            text = false;
        }
        else
        {
            index = 0;
        }
        return (Math.Min(row * BytesPerRow + Math.Clamp(index, 0, BytesPerRow - 1), data.Length), text);
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
        MoveCaret(index >= anchor ? index + 1 : index, true); // Include the byte under the mouse
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
        Invalidate();
    }

    private float X(int column) => Inset + column * charWidth;
    private static int HexCharacter(int index) => HexColumn + index * 3 + (index >= BytesPerRow / 2 ? 1 : 0);

    private void DrawText(Graphics g, string text, float x, int y, Color color)
    {
        TextRenderer.DrawText(g, text, Font, new Point((int)x, y), color, TextFormatFlags.NoPadding);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;

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
                    float width = i + 1 < selectionEnd && index < BytesPerRow - 1 ? X(HexCharacter(index + 1)) - hexX : 2 * charWidth;
                    g.FillRectangle(SystemBrushes.Highlight, hexX, y, width, rowHeight);
                    g.FillRectangle(SystemBrushes.Highlight, textX, y, charWidth, rowHeight);
                }

                Color color = selected ? SystemColors.HighlightText : SystemColors.WindowText;
                DrawText(g, HexStrings[data[i]], hexX, y, color);
                byte b = data[i];
                DrawText(g, b is >= 0x20 and < 0x7F ? ((char)b).ToString() : ".", textX, y, color);
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
        float activeX = textColumn ? caretText : caretHex + (lowNibble ? charWidth : 0);
        if (InsertMode && !ReadOnly)
            g.FillRectangle(SystemBrushes.WindowText, activeX - 1, caretY, 2, rowHeight);
        else
            g.DrawRectangle(SystemPens.WindowText, activeX, caretY, charWidth, rowHeight - 1);
        using Pen outline = new(SystemColors.GrayText) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot };
        g.DrawRectangle(outline, textColumn ? caretHex : caretText, caretY, textColumn ? 2 * charWidth : charWidth, rowHeight - 1);
    }
}
