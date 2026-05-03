using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;

namespace PVSFormat
{
    public class PVSEditControl : Control
    {
        private PVS _pvsFile;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public PVS PVS
        {
            get => _pvsFile;
            set
            {
                _pvsFile = value;
                OnPVSUpdated();
            }
        }

        public Image GameMap;

        protected const float Multiplier = 0.1f;

        protected float RenderScale;
        //protected float Zoom;
        protected PointF CameraPosition;
        protected PointF RenderOffset => new PointF(-CameraPosition.X + ClientRectangle.Width / 2.0f, -CameraPosition.Y + ClientRectangle.Height / 2.0f);
        protected float MapSize => (RenderScale * 32);
        protected bool isDragging;
        protected Point lastMouse;
        protected Point mousePos;

        protected enum MapSectionType
        {
            Unloaded,
            Current,
            Neighbour
        }

        internal PointF MousePosOnMap
        {
            get
            {
                return new PointF((mousePos.X - ClientRectangle.Width / 2.0f + CameraPosition.X) / RenderScale / Multiplier, (mousePos.Y - ClientRectangle.Height / 2.0f + CameraPosition.Y) / RenderScale / Multiplier);
            }
        }

        protected PointF ScaledMousePosOnMap
        {
            get
            {
                return new PointF(mousePos.X - ClientRectangle.Width / 2.0f + CameraPosition.X, mousePos.Y - ClientRectangle.Height / 2.0f + CameraPosition.Y);
            }
        }

        public PVSEditControl()
        {
            Width = 400;
            Height = 300;
            ForeColor = Color.White;
            BackColor = Color.Black;
            Font = new Font(FontFamily.GenericMonospace, 10.0f);
            DoubleBuffered = true;

            RenderScale = 1;

            CameraPosition = new Point(0, 0);
        }

        // Get zone ID for UI update
        public ulong GetZoneId()
        {
            ulong id = ulong.MaxValue;
            foreach (Zone zone in PVS.data.Zones)
            {
                List<PointF> points = new();
                foreach (Vector2 point in zone.Points)
                    points.Add(new PointF(point.X, point.Y));
                Polygon polygon = new Polygon(points);
                if (MousePosOnMap.X >= polygon.MinX && MousePosOnMap.X < polygon.MaxX
                    && MousePosOnMap.Y >= polygon.MinY && MousePosOnMap.Y < polygon.MaxY)
                {
                    id = zone.ZoneId;
                    break;
                }
            }
            return id;
        }

        // Get zone index for UI update
        public int GetZoneIndex(ulong zoneId)
        {
            return PVS.data.Zones.FindIndex(z => z.ZoneId == zoneId);
        }

        // Get zone info for UI update
        public List<Vector2> GetZonePoints(ulong zoneId)
        {
            List<Vector2> points = new();
            foreach (Vector2 point in PVS.data.Zones[GetZoneIndex(zoneId)].Points)
                points.Add(new Vector2(point.X, point.Y));
            return points;
        }

        // Get zone neighbours for UI update
        public List<Neighbour> GetZoneNeighbours(ulong zoneId)
        {
            List<Neighbour> neighbours = new();
            foreach (Neighbour neighbour in PVS.data.Zones[GetZoneIndex(zoneId)].UnsafeNeighbours)
                neighbours.Add(neighbour);
            return neighbours;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            // Move the camera
            if (e.Button == MouseButtons.Left)
            {
                lastMouse = e.Location;

                isDragging = true;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
                lastMouse = new Point(0, 0);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Reset the camera
            if (e.KeyData == Keys.R)
            {
                RenderScale = 1;
                CameraPosition.X = 0;
                CameraPosition.Y = 0;
                Invalidate();
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            lastMouse = new Point(0, 0);
        }

        // Move the camera
        protected override void OnMouseMove(MouseEventArgs e)
        {
            mousePos = e.Location;
            Invalidate();
            if (isDragging)
            {
                Size difference = new Size(e.X - lastMouse.X, e.Y - lastMouse.Y);
                lastMouse = new Point(e.X, e.Y);

                CameraPosition -= difference;
                Invalidate();
            }
        }

        // Zoom
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int wheelDelta = 120;

            float oldScale = RenderScale;

            RenderScale += (e.Delta * (RenderScale / 4)) / (wheelDelta * 1.0f);

            if (RenderScale < 1)
            {
                RenderScale = 1;
            }
            else if (RenderScale > 500)
            {
                RenderScale = oldScale;
            }

            if (oldScale != RenderScale)
            {
                CameraPosition.X += (ScaledMousePosOnMap.X / 4) * Math.Sign(e.Delta);
                CameraPosition.Y += (ScaledMousePosOnMap.Y / 4) * Math.Sign(e.Delta);
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            DrawMap(g);
            DrawPVS(g);
            DrawOverlay(g);
        }

        protected void DrawMap(Graphics g)
        {
            if (GameMap == null)
                return;

            float x = -4148.0f; //-GameMap.Width / 2 + 4148.0f;// + 200 - 50;
            float y = -5858.0f; //-GameMap.Height / 2 + 5858.0f;// - 400 - 250;
            float width = 10200.0f;// - 4148.0f; //GameMap.Width;
            float height = 9718.0f;// - 5858.0f; // GameMap.Height;

            float mult = 0.1f;

            //x = x + RenderOffset.X;
            //y = y + RenderOffset.Y;

            x = x * mult * RenderScale + RenderOffset.X;
            y = y * mult * RenderScale + RenderOffset.Y;
            width = width * mult * RenderScale;
            height = height * mult * RenderScale;

            RectangleF rect = new RectangleF(x, y, width, height);

            g.DrawImage(GameMap, rect);
        }

        protected void DrawPVS(Graphics g)
        {
            if (PVS == null)
                return;

            // Draw all plates
            int selectedZoneIndex = PVS.data.Zones.FindIndex(z => z.ZoneId == PVS.selectedZoneId);
            for (int i = 0; i < PVS.data.Zones.Count; i++)
            {
                Zone plate = PVS.data.Zones[i];

                if (selectedZoneIndex != -1)
                {
                    if (i == selectedZoneIndex)
                    {
                        DrawMapSection(g, plate, MapSectionType.Current);
                        continue;
                    }
                    if (PVS.data.Zones[selectedZoneIndex].UnsafeNeighbours.FindIndex(n => n.ZoneId == plate.ZoneId) != -1)
                    {
                        DrawMapSection(g, plate, MapSectionType.Neighbour);
                        continue;
                    }
                }
                
                DrawMapSection(g, plate);
            }
        }

        protected void DrawMapSection(Graphics g, Zone entry, MapSectionType type = MapSectionType.Unloaded)
        {
            PointF[] points = new PointF[entry.Points.Count];
            for (int i = 0; i < entry.Points.Count; i++)
            {
                Vector2 data = entry.Points[i];
                points[i] = new PointF(data.X * Multiplier * RenderScale + RenderOffset.X, data.Y * Multiplier * RenderScale + RenderOffset.Y);
            }

            Polygon poly = new Polygon(points);
            RectangleF bounds = poly.Bounds;

            if (bounds.IntersectsWith(ClientRectangle))
            {
                if (type == MapSectionType.Current)
                    g.FillPolygon(new SolidBrush(Color.FromArgb(127, 255, 0, 0)), points); // Red
                else if (type == MapSectionType.Neighbour)
                    g.FillPolygon(new SolidBrush(Color.FromArgb(127, 255, 255, 0)), points); // Yellow
                g.DrawPolygon(new Pen(Color.Cyan), points);

                Font font;

                string infoText;
                SizeF textSize;

                int times = 0;

                // Scale font to fit within rectangle
                bool noRender = false;
                do
                {
                    if (RenderScale / Multiplier - times < 0.01f)
                    {
                        font = null;
                        infoText = null;
                        textSize = new SizeF();
                        noRender = true;
                        break;
                    }
                    float textScale = RenderScale / (Multiplier * 2) - times;
                    font = new Font(Font.FontFamily, textScale);
                    infoText = entry.ZoneId.ToString();
                    textSize = g.MeasureString(infoText, font);

                    times++;
                } while (textSize.Width > bounds.Width || textSize.Height > bounds.Height);

                if (!noRender)
                {
                    // Render text
                    StringFormat format = new StringFormat(StringFormat.GenericDefault);
                    format.Alignment = StringAlignment.Center;
                    g.DrawString(infoText, font, new SolidBrush(ForeColor), bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2 - textSize.Height / 2, format);
                }
            }
        }

        protected void DrawOverlay(Graphics g)
        {
            // Render Overlay Text
            string status = PVS == null ? "Nothing Loaded" : "Loaded";
            g.DrawString(status, Font, new SolidBrush(ForeColor), 10, 10);

            g.DrawString("Zoom x" + (int)RenderScale, Font, new SolidBrush(ForeColor), 10, 25);
            g.DrawString("Camera (" + CameraPosition.X + ", " + CameraPosition.Y + ")", Font, new SolidBrush(ForeColor), 10, 40);
            g.DrawString("Mouse (" + mousePos.X + ", " + mousePos.Y + ")", Font, new SolidBrush(ForeColor), 10, 55);
            //g.DrawString("Map Size " + MapSize + "px", Font, new SolidBrush(ForeColor), 10, 70);
            g.DrawString("Mouse Pos On Map (" + MousePosOnMap.X + ", " + MousePosOnMap.Y + ")", Font, new SolidBrush(ForeColor), 10, 85);
            //g.DrawString("Scaled Mouse Pos On Map (" + ScaledMousePosOnMap.X + ", " + ScaledMousePosOnMap.Y + ")", Font, new SolidBrush(ForeColor), 10, 100);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.FillRectangle(new SolidBrush(BackColor), ClientRectangle);
        }

        protected override void OnResize(EventArgs e)
        {
            Invalidate();
        }

        private void OnPVSUpdated()
        {
            Refresh();
            Invalidate();
        }
    }
}
