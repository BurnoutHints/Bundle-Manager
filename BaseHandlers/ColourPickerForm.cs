using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Numerics;
using System.Windows.Forms;

namespace BaseHandlers;

// Picks a float RGB colour that can be brighter than 1: hue, saturation and value pick the colour, intensity multiplies it
public class ColourPickerForm : Form
{
    private float hue, saturation, value, intensity;
    private bool updating;

    private readonly Vector3 original;
    private readonly PictureBox square = new() { Location = new Point(12, 12), Size = new Size(200, 200), BorderStyle = BorderStyle.FixedSingle };
    private readonly PictureBox hueBar = new() { Location = new Point(220, 12), Size = new Size(24, 200), BorderStyle = BorderStyle.FixedSingle };
    private readonly PictureBox preview = new() { Location = new Point(256, 12), Size = new Size(140, 60), BorderStyle = BorderStyle.FixedSingle };
    private readonly NumericUpDown red = Number(), green = Number(), blue = Number(), strength = Number(), alpha = Number();

    public Vector3 Colour { get; private set; }
    public float Alpha => (float)alpha.Value;

    public ColourPickerForm(Vector3 colour, float? alphaValue = null)
    {
        original = Colour = colour;

        Text = "Pick Colour";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(408, 262);

        int y = 84;
        void AddRow(string label, NumericUpDown box)
        {
            Controls.Add(new Label { Text = label, Location = new Point(256, y + 3), AutoSize = true });
            box.Location = new Point(326, y);
            Controls.Add(box);
            y += 26;
        }
        AddRow("Red", red);
        AddRow("Green", green);
        AddRow("Blue", blue);
        AddRow("Intensity", strength);
        if (alphaValue is float a)
        {
            AddRow("Alpha", alpha);
            alpha.Value = Clamp(alpha, a);
        }

        Button ok = new() { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(240, 226), Size = new Size(75, 25) };
        Button cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(321, 226), Size = new Size(75, 25) };
        AcceptButton = ok;
        CancelButton = cancel;
        Controls.AddRange([square, hueBar, preview, ok, cancel]);
        Controls.Add(new Label { Text = "Intensity scales the colour past 1.", Location = new Point(12, 226), AutoSize = true, ForeColor = SystemColors.GrayText });

        square.Paint += PaintSquare;
        hueBar.Paint += PaintHueBar;
        preview.Paint += PaintPreview;
        square.MouseDown += (_, e) => DragSquare(e);
        square.MouseMove += (_, e) => { if (e.Button == MouseButtons.Left) DragSquare(e); };
        hueBar.MouseDown += (_, e) => DragHue(e);
        hueBar.MouseMove += (_, e) => { if (e.Button == MouseButtons.Left) DragHue(e); };
        foreach (NumericUpDown box in (NumericUpDown[])[red, green, blue])
            box.ValueChanged += (_, _) => FromRgb();
        strength.ValueChanged += (_, _) => { if (!updating) { intensity = (float)strength.Value; FromHsv(); } };

        SetColour(colour);

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
    }

    private static NumericUpDown Number() => new() { DecimalPlaces = 3, Increment = 0.05m, Maximum = 1000, Width = 70 };

    private static decimal Clamp(NumericUpDown box, float v) => Math.Clamp((decimal)v, box.Minimum, box.Maximum);

    // Colours up to 1 have an intensity of 1, brighter ones are split into a colour up to 1 and the intensity
    private void SetColour(Vector3 colour)
    {
        intensity = Math.Max(1, Math.Max(colour.X, Math.Max(colour.Y, colour.Z)));
        (hue, saturation, value) = ToHsv(colour / intensity, hue);
        Colour = colour;
        ShowValues();
    }

    private void FromRgb()
    {
        if (!updating)
            SetColour(new Vector3((float)red.Value, (float)green.Value, (float)blue.Value));
    }

    private void FromHsv()
    {
        Colour = FromHsv(hue, saturation, value) * intensity;
        ShowValues();
    }

    private void ShowValues()
    {
        updating = true;
        red.Value = Clamp(red, Colour.X);
        green.Value = Clamp(green, Colour.Y);
        blue.Value = Clamp(blue, Colour.Z);
        strength.Value = Clamp(strength, intensity);
        updating = false;
        square.Invalidate();
        hueBar.Invalidate();
        preview.Invalidate();
    }

    private void DragSquare(MouseEventArgs e)
    {
        saturation = Math.Clamp(e.X / (float)(square.ClientSize.Width - 1), 0, 1);
        value = 1 - Math.Clamp(e.Y / (float)(square.ClientSize.Height - 1), 0, 1);
        FromHsv();
    }

    private void DragHue(MouseEventArgs e)
    {
        hue = Math.Clamp(e.Y / (float)(hueBar.ClientSize.Height - 1), 0, 1) * 360;
        FromHsv();
    }

    private void PaintSquare(object sender, PaintEventArgs e)
    {
        Rectangle r = square.ClientRectangle;
        e.Graphics.Clear(ToColor(FromHsv(hue, 1, 1)));
        using (LinearGradientBrush white = new(r, Color.White, Color.FromArgb(0, Color.White), LinearGradientMode.Horizontal))
            e.Graphics.FillRectangle(white, r);
        using (LinearGradientBrush black = new(r, Color.FromArgb(0, Color.Black), Color.Black, LinearGradientMode.Vertical))
            e.Graphics.FillRectangle(black, r);

        Point p = new((int)(saturation * (r.Width - 1)), (int)((1 - value) * (r.Height - 1)));
        e.Graphics.DrawEllipse(value > 0.5f ? Pens.Black : Pens.White, p.X - 5, p.Y - 5, 10, 10);
    }

    private void PaintHueBar(object sender, PaintEventArgs e)
    {
        Rectangle r = hueBar.ClientRectangle;
        using LinearGradientBrush brush = new(r, Color.Red, Color.Red, LinearGradientMode.Vertical);
        brush.InterpolationColors = new ColorBlend
        {
            Colors = [Color.Red, Color.Yellow, Color.Lime, Color.Cyan, Color.Blue, Color.Magenta, Color.Red],
            Positions = [0, 1 / 6f, 2 / 6f, 3 / 6f, 4 / 6f, 5 / 6f, 1]
        };
        e.Graphics.FillRectangle(brush, r);

        int y = (int)(hue / 360 * (r.Height - 1));
        e.Graphics.FillRectangle(Brushes.White, 0, y - 1, r.Width, 3);
        e.Graphics.DrawRectangle(Pens.Black, 0, y - 2, r.Width - 1, 4);
    }

    // Old colour on top, new below, both at most full brightness
    private void PaintPreview(object sender, PaintEventArgs e)
    {
        Rectangle r = preview.ClientRectangle;
        using SolidBrush before = new(ToColor(Normalise(original)));
        using SolidBrush after = new(ToColor(Normalise(Colour)));
        e.Graphics.FillRectangle(before, 0, 0, r.Width, r.Height / 2);
        e.Graphics.FillRectangle(after, 0, r.Height / 2, r.Width, r.Height - r.Height / 2);
    }

    public static Vector3 Normalise(Vector3 colour) => Vector3.Clamp(colour / Math.Max(1, Math.Max(colour.X, Math.Max(colour.Y, colour.Z))), Vector3.Zero, Vector3.One);

    public static Color ToColor(Vector3 colour)
    {
        Vector3 c = Vector3.Clamp(colour, Vector3.Zero, Vector3.One) * 255;
        return Color.FromArgb((int)MathF.Round(c.X), (int)MathF.Round(c.Y), (int)MathF.Round(c.Z));
    }

    private static Vector3 FromHsv(float h, float s, float v)
    {
        float c = v * s;
        float x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        Vector3 rgb = (int)(h / 60 % 6) switch
        {
            0 => new(c, x, 0),
            1 => new(x, c, 0),
            2 => new(0, c, x),
            3 => new(0, x, c),
            4 => new(x, 0, c),
            _ => new(c, 0, x)
        };
        return rgb + new Vector3(v - c);
    }

    // Grey and black have no hue, so the previous one is kept
    private static (float Hue, float Saturation, float Value) ToHsv(Vector3 rgb, float previousHue)
    {
        float max = Math.Max(rgb.X, Math.Max(rgb.Y, rgb.Z));
        float delta = max - Math.Min(rgb.X, Math.Min(rgb.Y, rgb.Z));
        if (delta <= 0 || max <= 0)
            return (previousHue, 0, max);

        float h = max == rgb.X ? (rgb.Y - rgb.Z) / delta % 6 : max == rgb.Y ? (rgb.Z - rgb.X) / delta + 2 : (rgb.X - rgb.Y) / delta + 4;
        return ((h * 60 + 360) % 360, delta / max, max);
    }
}
