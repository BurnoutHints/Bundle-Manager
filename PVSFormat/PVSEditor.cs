using DebugHelper;
using PluginAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;

namespace PVSFormat
{
    public partial class PVSEditor : Form, IEntryEditor
    {
        public delegate void OnEdit();
        public event OnEdit Edit;

        private PVS _currentPVS;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Image GameMap
        {
            get => pvsMain.GameMap;
            set => pvsMain.GameMap = value;
        }

        public PVSEditor()
        {
            InitializeComponent();
        }

        public void UpdateDisplay()
        {
            //dbgMain.SelectedObject = _currentPVS;
            pvsMain.PVS = _currentPVS;

            foreach (Zone zone in pvsMain.PVS.data.Zones)
                zonesListBox.Items.Add(zone.ZoneId.ToString("000"));
        }

        public void Open(PVS pvs)
        {
            _currentPVS = pvs;

            UpdateDisplay();
        }

        private List<Zone> GetZones()
        {
            return pvsMain.PVS.data.Zones;
        }

        private List<Vector2> GetPoints()
        {
            return pvsMain.PVS.data.Zones[pvsMain.GetZoneIndex(ulong.Parse(zonesListBox.SelectedItem.ToString()))].Points;
        }

        private List<Neighbour> GetUnsafeNeighbours()
        {
            return pvsMain.PVS.data.Zones[pvsMain.GetZoneIndex(ulong.Parse(zonesListBox.SelectedItem.ToString()))].UnsafeNeighbours;
        }

        private void SelectZone(ulong zoneId)
        {
            neighboursListBox.Items.Clear();
            renderCheckBox.Checked = false;
            immediateCheckBox.Checked = false;

            pvsMain.PVS.selectedZoneId = zoneId;

            List<Vector2> points = pvsMain.GetZonePoints(zoneId);
            List<Neighbour> neighbours = pvsMain.GetZoneNeighbours(zoneId);

            point1XNumericUpDown.Value = (decimal)points[0].X;
            point1YNumericUpDown.Value = (decimal)points[0].Y;
            point2XNumericUpDown.Value = (decimal)points[1].X;
            point2YNumericUpDown.Value = (decimal)points[1].Y;
            point3XNumericUpDown.Value = (decimal)points[2].X;
            point3YNumericUpDown.Value = (decimal)points[2].Y;
            point4XNumericUpDown.Value = (decimal)points[3].X;
            point4YNumericUpDown.Value = (decimal)points[3].Y;

            foreach (Neighbour neighbour in neighbours)
                neighboursListBox.Items.Add(neighbour.ZoneId.ToString("000"));

            point1XNumericUpDown.Enabled = true;
            point1YNumericUpDown.Enabled = true;
            point2XNumericUpDown.Enabled = true;
            point2YNumericUpDown.Enabled = true;
            point3XNumericUpDown.Enabled = true;
            point3YNumericUpDown.Enabled = true;
            point4XNumericUpDown.Enabled = true;
            point4YNumericUpDown.Enabled = true;
            neighboursListBox.Enabled = true;
            addNeighbourButton.Enabled = true;
            deleteNeighbourButton.Enabled = false;
            renderCheckBox.Enabled = false;
            immediateCheckBox.Enabled = false;
        }

        private void PVSEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            pvsMain.PVS.selectedZoneId = ulong.MaxValue;
            Edit?.Invoke();
        }

        private void pvsMain_DoubleClick(object sender, EventArgs e)
        {
            ulong zoneId = pvsMain.GetZoneId();
            if (zoneId == ulong.MaxValue)
                return;

            zonesListBox.SelectedIndex = zonesListBox.FindStringExact(zoneId.ToString("000"));
        }

        private void pvsMain_Click(object sender, EventArgs e)
        {
            if (((MouseEventArgs)e).Button == MouseButtons.Right
                && pvsMain.PVS.selectedZoneId != ulong.MaxValue)
                OpenContextMenu();
        }

        private void OpenContextMenu()
        {
            ContextMenuStrip menu = new();
            for (int i = 0; i < 4; ++i)
                menu.Items.Add("Use as point " + (i + 1).ToString());
            menu.Items[0].Click += (s, e) =>
            {
                Vector2 pos = pvsMain.MousePosOnMap.ToVector2();
                point1XNumericUpDown.Value = (decimal)pos.X;
                point1YNumericUpDown.Value = (decimal)pos.Y;
            };
            menu.Items[1].Click += (s, e) =>
            {
                Vector2 pos = pvsMain.MousePosOnMap.ToVector2();
                point2XNumericUpDown.Value = (decimal)pos.X;
                point2YNumericUpDown.Value = (decimal)pos.Y;
            };
            menu.Items[2].Click += (s, e) =>
            {
                Vector2 pos = pvsMain.MousePosOnMap.ToVector2();
                point3XNumericUpDown.Value = (decimal)pos.X;
                point3YNumericUpDown.Value = (decimal)pos.Y;
            };
            menu.Items[3].Click += (s, e) =>
            {
                Vector2 pos = pvsMain.MousePosOnMap.ToVector2();
                point4XNumericUpDown.Value = (decimal)pos.X;
                point4YNumericUpDown.Value = (decimal)pos.Y;
            };
            menu.Show(Cursor.Position);
        }

        private void zonesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (zonesListBox.SelectedIndex == -1)
                return;
            ulong zoneId = ulong.Parse(zonesListBox.SelectedItem.ToString());
            SelectZone(zoneId);

            deleteZoneButton.Enabled = true;
        }

        private void addZoneButton_Click(object sender, EventArgs e)
        {
            var zones = GetZones();
            ulong newZoneId;
            while (true)
            {
                newZoneId = new AddZone().GetNewZoneId();
                if (zones.FindIndex(z => z.ZoneId == newZoneId) != -1)
                {
                    MessageBox.Show("A zone with ID " + newZoneId + " already exists.");
                    continue;
                }
                break;
            }
            if (newZoneId == ulong.MaxValue)
                return;

            Zone newZone = new();
            newZone.ZoneId = newZoneId;
            zones.Add(newZone);
            pvsMain.PVS.data.TotalZones++;

            zonesListBox.Items.Add(newZoneId.ToString("000"));
            zonesListBox.SelectedIndex = zonesListBox.FindStringExact(newZoneId.ToString("000"));
            if (zonesListBox.Items.Count == 1)
                deleteZoneButton.Enabled = true;
        }

        private void deleteZoneButton_Click(object sender, EventArgs e)
        {
            int index = zonesListBox.SelectedIndex;
            ulong zoneId = ulong.Parse(zonesListBox.Items[index].ToString());
            GetZones().Remove(GetZones().Find(z => z.ZoneId == zoneId));
            pvsMain.PVS.data.TotalZones--;

            zonesListBox.Items.RemoveAt(index);
            if (zonesListBox.Items.Count == 0)
            {
                deleteZoneButton.Enabled = false;
                return;
            }
            if (index == zonesListBox.Items.Count)
                zonesListBox.SelectedIndex = zonesListBox.Items.Count - 1;
            else
                zonesListBox.SelectedIndex = index;
        }

        private void point1XNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            Vector2 point = GetPoints()[0];
            point.X = (float)point1XNumericUpDown.Value;
            GetPoints()[0] = point;
            pvsMain.Invalidate();
        }

        private void point1YNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            Vector2 point = GetPoints()[0];
            point.Y = (float)point1YNumericUpDown.Value;
            GetPoints()[0] = point;
            pvsMain.Invalidate();
        }

        private void point2XNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            Vector2 point = GetPoints()[1];
            point.X = (float)point2XNumericUpDown.Value;
            GetPoints()[1] = point;
            pvsMain.Invalidate();
        }

        private void point2YNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            Vector2 point = GetPoints()[1];
            point.Y = (float)point2YNumericUpDown.Value;
            GetPoints()[1] = point;
            pvsMain.Invalidate();
        }

        private void point3XNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            Vector2 point = GetPoints()[2];
            point.X = (float)point3XNumericUpDown.Value;
            GetPoints()[2] = point;
            pvsMain.Invalidate();
        }

        private void point3YNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            Vector2 point = GetPoints()[2];
            point.Y = (float)point3YNumericUpDown.Value;
            GetPoints()[2] = point;
            pvsMain.Invalidate();
        }

        private void point4XNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            Vector2 point = GetPoints()[3];
            point.X = (float)point4XNumericUpDown.Value;
            GetPoints()[3] = point;
            pvsMain.Invalidate();
        }

        private void point4YNumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            Vector2 point = GetPoints()[3];
            point.Y = (float)point4YNumericUpDown.Value;
            GetPoints()[3] = point;
            pvsMain.Invalidate();
        }

        private void neighboursListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (neighboursListBox.SelectedIndex == -1)
                return;
            int neighbourIndex = GetUnsafeNeighbours().FindIndex(n => n.ZoneId == ulong.Parse(neighboursListBox.SelectedItem.ToString()));
            Neighbour neighbour = GetUnsafeNeighbours()[neighbourIndex];
            renderCheckBox.Checked = neighbour.Flags.HasFlag(NeighbourFlags.E_NEIGHBOURFLAG_RENDER);
            immediateCheckBox.Checked = neighbour.Flags.HasFlag(NeighbourFlags.E_NEIGHBOURFLAG_IMMEDIATE);

            if (neighboursListBox.Items.Count != 0)
                deleteNeighbourButton.Enabled = true;
            renderCheckBox.Enabled = true;
            immediateCheckBox.Enabled = true;
        }

        private void addNeighbourButton_Click(object sender, EventArgs e)
        {
            ulong newZoneId = new AddZone().GetNewZoneId();
            if (newZoneId == ulong.MaxValue)
                return;
            var neighbours = GetUnsafeNeighbours();
            Neighbour newNeighbour = new();
            newNeighbour.ZoneId = newZoneId;
            neighbours.Add(newNeighbour);
            int parentIndex = GetZones().FindIndex(z => z.ZoneId == ulong.Parse(zonesListBox.SelectedItem.ToString()));
            Zone parent = GetZones()[parentIndex];
            parent.NumUnsafeNeighbours++;
            GetZones()[parentIndex] = parent;

            neighboursListBox.Items.Add(newZoneId.ToString("000"));
            neighboursListBox.SelectedIndex = neighboursListBox.FindStringExact(newZoneId.ToString("000"));
            pvsMain.Invalidate();
            if (neighboursListBox.Items.Count == 1)
                deleteNeighbourButton.Enabled = true;
        }

        private void deleteNeighbourButton_Click(object sender, EventArgs e)
        {
            int index = neighboursListBox.SelectedIndex;
            ulong zoneId = ulong.Parse(neighboursListBox.Items[index].ToString());
            GetUnsafeNeighbours().Remove(GetUnsafeNeighbours().Find(n => n.ZoneId == zoneId));
            int parentIndex = GetZones().FindIndex(z => z.ZoneId == ulong.Parse(zonesListBox.SelectedItem.ToString()));
            Zone parent = GetZones()[parentIndex];
            parent.NumUnsafeNeighbours--;
            GetZones()[parentIndex] = parent;

            neighboursListBox.Items.RemoveAt(index);
            pvsMain.Invalidate();
            if (neighboursListBox.Items.Count == 0)
            {
                deleteNeighbourButton.Enabled = false;
                renderCheckBox.Enabled = false;
                immediateCheckBox.Enabled = false;
                renderCheckBox.Checked = false;
                immediateCheckBox.Checked = false;
                return;
            }
            if (index == neighboursListBox.Items.Count)
                neighboursListBox.SelectedIndex = neighboursListBox.Items.Count - 1;
            else
                neighboursListBox.SelectedIndex = index;
        }

        private void renderCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (neighboursListBox.SelectedIndex == -1)
                return;
            int neighbourIndex = GetUnsafeNeighbours().FindIndex(n => n.ZoneId == ulong.Parse(neighboursListBox.SelectedItem.ToString()));
            Neighbour neighbour = GetUnsafeNeighbours()[neighbourIndex];
            if (renderCheckBox.Checked)
                neighbour.Flags |= NeighbourFlags.E_NEIGHBOURFLAG_RENDER;
            else
                neighbour.Flags &= ~NeighbourFlags.E_NEIGHBOURFLAG_RENDER;
            GetUnsafeNeighbours()[neighbourIndex] = neighbour;
        }

        private void immediateCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (neighboursListBox.SelectedIndex == -1)
                return;
            int neighbourIndex = GetUnsafeNeighbours().FindIndex(n => n.ZoneId == ulong.Parse(neighboursListBox.SelectedItem.ToString()));
            Neighbour neighbour = GetUnsafeNeighbours()[neighbourIndex];
            if (immediateCheckBox.Checked)
                neighbour.Flags |= NeighbourFlags.E_NEIGHBOURFLAG_IMMEDIATE;
            else
                neighbour.Flags &= ~NeighbourFlags.E_NEIGHBOURFLAG_IMMEDIATE;
            GetUnsafeNeighbours()[neighbourIndex] = neighbour;
        }
    }
}
