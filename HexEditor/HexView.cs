using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace HexEditor;

public class HexView : UserControl
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public byte[] HexData
    {
        get;
        set
        {
            field = value;
            if (field != null)
            {
                vScroll.Minimum = 0;
                vScroll.Maximum = field.Length / 16;
            }
            Invalidate();
        }
    }

    private VScrollBar vScroll;

    public HexView()
    {
        DoubleBuffered = true;

        BorderStyle = BorderStyle.FixedSingle;

        BackColor = SystemColors.Window; // Follows the light or dark colour mode

        Width = 100;
        Height = 100;

        Font = new Font(FontFamily.GenericMonospace, 10, FontStyle.Regular, GraphicsUnit.Point);

        vScroll = new VScrollBar();
        vScroll.Dock = DockStyle.Right;
        vScroll.Scroll += VScroll_Scroll;
        Controls.Add(vScroll);

        Click += HexView_Click;
    }

    private void HexView_Click(object sender, EventArgs e)
    {
        Focus();
    }

    private void VScroll_Scroll(object sender, ScrollEventArgs e)
    {
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        int min = vScroll.Minimum;
        int max = vScroll.Maximum;
        int val = vScroll.Value;
        int delta = e.Delta;
        if (delta < 0)
            val += 1;
        else if (delta > 0)
            val -= 1;
        if (val < 0)
            val = 0;
        else if (val > max)
            val = max;
        vScroll.Value = val;
        
        Invalidate();
        base.OnMouseWheel(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Graphics g = e.Graphics;

        if (HexData == null)
            return;

        int padding = 5;
        int separation = 5;

        for (int i = 0; i < HexData.Length; i++)
        {
            float yVal = ((i / 16) * 14 + padding) - vScroll.Value * 14;
            if (yVal > Height || yVal < 0)
                continue;

            byte b = HexData[i];

            string byteString = b.ToString("X2");
            SizeF stringSize = g.MeasureString(byteString, Font);
            float stringWidth = stringSize.Width + separation;
            float stringHeight = stringSize.Height;

            int x = i % 16;
            int y = i / 16;

            float xP = x * stringWidth + padding;
            float yP = (y * stringHeight + padding) - vScroll.Value * stringHeight;

            PointF point = new(xP, yP);

            g.DrawString(byteString, Font, SystemBrushes.WindowText, point);

            string ascii = Encoding.ASCII.GetString([b]);
            char aChar = ascii[0];
            if (char.IsControl(aChar))
                ascii = ".";

            SizeF stringSizeA = g.MeasureString(ascii, Font);
            float stringWidthA = stringSizeA.Width;
            float stringHeightA = stringSizeA.Height;

            float asciiXStart = 16 * stringWidth + padding;

            float xPAscii = x * stringWidthA * 0.65f;

            PointF pointA = new(xPAscii + asciiXStart, yP);

            g.DrawString(ascii, Font, SystemBrushes.WindowText, pointA);
        }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        base.OnPaintBackground(e);
    }
}
