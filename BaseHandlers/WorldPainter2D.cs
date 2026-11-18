using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BundleFormat;
using BundleUtilities;
using PluginAPI;

namespace BaseHandlers;

public class WorldPainter2D : IEntryData
{
    public const byte Outside = 255;

    public readonly record struct Region(string Name, RectangleF World, Rectangle Cells);

    private static readonly Region mainland = new("Mainland", new(-4208, -3846, 8270, 6101), new(0, 0, 256, 256));
    private static readonly Region islands14 = new("Islands", new(3661.9f, -2749.6f, 4220, 3790), new(256, 0, 128, 128));
    private static readonly Region island19 = new("Big Surf Island", new(3703, -1790, 2088, 2100), new(256, 0, 128, 128));

    // BrnWorld::EDistrict
    private static readonly string[] districts = ["Ocean View", "West Acres", "Twin Bridges", "Big Surf Beach", "Eastern Shore", "Hillside Pass", "Heartbreak Hills",
        "Rockridge Cliffs", "South Bay", "Park Vale", "Paradise Wharf", "Crystal Summit", "Lone Peaks", "Sunset Valley", "Downtown", "River City", "Motor City", "Waterfront",
        "Paradise Keys Bridge", "North Beach", "Midtown", "South Coast", "Perren's Point"];

    private static readonly string[] islands = [.. districts.Take(18), "Island 1", "Island 2", "Island 3"];

    // KA_AMBIENCE_NAMES
    private static readonly string[] ambiences = [.. districts.Take(18), "Downtown traffic", "Downtown heavy traffic", "Tunnel"];

    private byte[] data;
    private int cells;
    private string[] names;

    public string Name { get; private set; }

    public int Width { get; private set; }

    public int Height { get; private set; }

    // In lookup order
    public Region[] Regions { get; private set; }

    public byte this[int x, int y] => data[cells + y * Width + x];

    public string Label(byte value) => value == Outside
        ? "Outside"
        : names.ElementAtOrDefault(value) is string name
            ? $"{value}: {name}"
            : $"{value}";

    // WorldMap2D::GetValue's cell for a world position
    public Point? Cell(float x, float z)
    {
        foreach (Region region in Regions)
        {
            RectangleF world = region.World;

            if (x < world.Left || z < world.Top || x > world.Right || z > world.Bottom)
                continue;

            int column = (int)(region.Cells.Width * ((x - world.X) / world.Width) + region.Cells.X), row = (int)(region.Cells.Height * ((z - world.Y) / world.Height) + region.Cells.Y);
            return column >= 0 && column < Width && row >= 0 && row < Height
                ? new(column, row)
                : null;
        }

        return null;
    }

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        data = entry.GetPrimaryWithoutImports();
        bool big = entry.Console;

        int u16(int at) => big
            ? BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(at))
            : BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(at));

        int start = data.Length < 8
            ? -1
            : (int)(big
                ? BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(4))
                : BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(4)));

        if (start < 8 || start + 4 > data.Length)
            throw new ReadFailedError("The world painter's data offset is outside the resource.");

        Width = u16(start);
        Height = u16(start + 2);
        cells = start + 4;

        if (cells + Width * Height > data.Length)
            throw new ReadFailedError($"A {Width}x{Height} world painter grid doesn't fit in the resource.");

        Name = ResourceDB.Lookup(entry.ID);
        bool remastered = Name == "Ambiences" || data.AsSpan(cells, Width * Height).IndexOfAnyInRange((byte)21, (byte)22) >= 0;
        names = Name == "Ambiences"
            ? ambiences
            : remastered || Width <= 256
                ? districts
                : islands;
        Regions = Width <= 256
            ? [mainland]
            : [mainland, remastered
                ? island19
                : islands14];
        return true;
    }

    // The viewer doesn't change anything
    public bool Write(BundleEntry entry)
    {
        entry.SetPrimary(data, entry.GetImports());
        return true;
    }

    public EntryType GetEntryType(BundleEntry entry) => EntryType.WorldPainter2D;

    public IEntryEditor GetEditor(BundleEntry entry) => new WorldPainter2DView(this, entry);
}

// The map top-down with each region at its world rectangle, a legend, and the value under the cursor
public sealed class WorldPainter2DView : Form, IEntryEditor
{
    private readonly WorldPainter2D map;
    private readonly Bitmap image;
    private readonly RectangleF world;
    private readonly Dictionary<byte, PointF> centres = [];

    private readonly Label status = new()
    {
        Dock = DockStyle.Bottom,
        Height = 24,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private readonly CheckBox labels = new()
    {
        Text = "Show names",
        Checked = true,
        Dock = DockStyle.Bottom,
        Height = 24
    };

    private readonly Panel canvas = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Color.FromArgb(24, 24, 28)
    };

    public WorldPainter2DView(WorldPainter2D initialMap, BundleEntry entry)
    {
        map = initialMap;
        world = map.Regions.Select(r => r.World).Aggregate(RectangleF.Union);
        image = new(map.Width, map.Height, PixelFormat.Format32bppArgb);
        int[] pixels = new int[map.Width * map.Height];
        Dictionary<byte, int> counts = [];

        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                byte value = map[x, y];
                pixels[y * map.Width + x] = Colour(value).ToArgb();
                counts[value] = counts.GetValueOrDefault(value) + 1;
            }

        BitmapData bits = image.LockBits(new(0, 0, map.Width, map.Height), ImageLockMode.WriteOnly, image.PixelFormat);
        Marshal.Copy(pixels, 0, bits.Scan0, pixels.Length);
        image.UnlockBits(bits);

        // Name each value at the middle of the cells the game can reach
        Dictionary<byte, (int Count, double X, double Z)> sums = [];

        foreach (WorldPainter2D.Region region in map.Regions)
            for (int y = region.Cells.Top; y < Math.Min(region.Cells.Bottom, map.Height); y++)
                for (int x = region.Cells.Left; x < Math.Min(region.Cells.Right, map.Width); x++)
                {
                    float worldX = region.World.X + (x - region.Cells.X + 0.5f) / region.Cells.Width * region.World.Width;
                    float worldZ = region.World.Y + (y - region.Cells.Y + 0.5f) / region.Cells.Height * region.World.Height;

                    if (map.Cell(worldX, worldZ) != new Point(x, y) || map[x, y] == WorldPainter2D.Outside)
                        continue;

                    (int count, double sx, double sz) = sums.GetValueOrDefault(map[x, y]);
                    sums[map[x, y]] = (count + 1, sx + worldX, sz + worldZ);
                }

        foreach ((byte value, (int count, double x, double z)) in sums)
            centres[value] = new((float)(x / count), (float)(z / count));

        ListView legend = new()
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            SmallImageList = new()
            {
                ImageSize = new(14, 14)
            }
        };

        legend.Columns.Add("Value", 190);
        legend.Columns.Add("Cells", 60, HorizontalAlignment.Right);

        foreach ((byte value, int count) in counts.OrderBy(v => v.Key))
        {
            Bitmap swatch = new(14, 14);
            using (Graphics g = Graphics.FromImage(swatch))
                g.Clear(Colour(value));

            legend.SmallImageList.Images.Add(swatch);
            legend.Items.Add(new ListViewItem([map.Label(value), $"{count}"], legend.SmallImageList.Images.Count - 1));
        }

        Panel side = new()
        {
            Dock = DockStyle.Right,
            Width = 280
        };

        side.Controls.AddRange([legend, labels]);
        Controls.AddRange([canvas, side, status]);
        typeof(Panel).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(canvas, true);
        canvas.Paint += (_, e) => Draw(e.Graphics);
        canvas.Resize += (_, _) => canvas.Invalidate();
        canvas.MouseMove += (_, e) => Hover(e.Location);
        labels.CheckedChanged += (_, _) => canvas.Invalidate();
        status.Text = $"{map.Width}x{map.Height} cells. " + string.Join("; ", map.Regions.Select(r => $"{r.Name}: X {r.World.Left:0}..{r.World.Right:0}, Z {r.World.Top:0}..{r.World.Bottom:0}"))
            + ". Point at the map to read a cell.";
        Text = $"World painter — {map.Name ?? $"{entry.ID:X8}"}";
        ClientSize = new(1200, 760);
        StartPosition = FormStartPosition.CenterParent;
        Disposed += (_, _) => image.Dispose();
    }

    private static Color Colour(byte value) => value == WorldPainter2D.Outside
        ? Color.Transparent
        : FromHue(value * 0.618034 % 1, value % 2 == 0
            ? 0.65
            : 0.85);

    private static Color FromHue(double hue, double light)
    {
        double channel(double n)
        {
            double k = (n + hue * 6) % 6;
            return light * (1 - 0.6 * Math.Clamp(Math.Min(k, 4 - k), 0, 1));
        }

        return Color.FromArgb((int)(channel(5) * 255), (int)(channel(3) * 255), (int)(channel(1) * 255));
    }

    // World units to canvas pixels, keeping the world's proportions
    private (float Scale, PointF Offset) Transform()
    {
        float scale = Math.Max(0, Math.Min((canvas.ClientSize.Width - 16) / world.Width, (canvas.ClientSize.Height - 16) / world.Height));
        return (scale, new((canvas.ClientSize.Width - world.Width * scale) / 2 - world.X * scale, (canvas.ClientSize.Height - world.Height * scale) / 2 - world.Y * scale));
    }

    private static RectangleF ToScreen(RectangleF area, (float Scale, PointF Offset) t) =>
        new(area.X * t.Scale + t.Offset.X, area.Y * t.Scale + t.Offset.Y, area.Width * t.Scale, area.Height * t.Scale);

    private void Draw(Graphics g)
    {
        (float Scale, PointF Offset) t = Transform();

        if (t.Scale <= 0)
            return;

        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;

        // Earlier regions take prominence where they overlap
        using Region covered = new(RectangleF.Empty);

        foreach (WorldPainter2D.Region region in map.Regions)
        {
            RectangleF screen = ToScreen(region.World, t);
            g.SetClip(screen);
            g.ExcludeClip(covered);
            g.DrawImage(image, screen, region.Cells, GraphicsUnit.Pixel);
            covered.Union(screen);
            g.ResetClip();
            g.DrawRectangle(Pens.Gray, screen.X, screen.Y, screen.Width, screen.Height);
        }

        if (!labels.Checked)
            return;

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.Default;
        using Font font = new(Font.FontFamily, 9, FontStyle.Bold);
        using Pen outline = new(Color.FromArgb(200, 0, 0, 0), 3)
        {
            LineJoin = LineJoin.Round
        };

        using StringFormat centred = new()
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        foreach ((byte value, PointF centre) in centres)
        {
            using GraphicsPath text = new();
            text.AddString(map.Label(value), font.FontFamily, (int)font.Style, g.DpiY * font.SizeInPoints / 72, new PointF(centre.X * t.Scale + t.Offset.X, centre.Y * t.Scale + t.Offset.Y), centred);
            g.DrawPath(outline, text);
            g.FillPath(Brushes.White, text);
        }
    }

    private void Hover(Point point)
    {
        (float Scale, PointF Offset) t = Transform();

        if (t.Scale <= 0)
            return;

        float x = (point.X - t.Offset.X) / t.Scale, z = (point.Y - t.Offset.Y) / t.Scale;
        status.Text = map.Cell(x, z) is Point cell
            ? $"X {x:0}, Z {z:0}   cell {cell.X}, {cell.Y}   {map.Label(map[cell.X, cell.Y])}"
            : $"X {x:0}, Z {z:0}   outside the map";
    }
}
