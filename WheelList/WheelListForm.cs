using BundleUtilities;
using PluginAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WheelList
{
    public partial class WheelListForm : Form, IEntryEditor
    {
        public delegate void OnEdit();
        public event OnEdit Edit;

        private WheelListData _list;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public WheelListData List
        {
            get => _list;
            set
            {
                _list = value;
                UpdateDisplay();
            }
        }

        public WheelListForm()
        {
            InitializeComponent();
        }

        private void UpdateDisplay()
        {
            lstWheels.Items.Clear();

            if (List == null)
                return;

            for (int i = 0; i < List.Entries.Count; i++)
            {
                Wheel wheel = List.Entries[i];

                string[] value = {
                    wheel.Index.ToString(),
                    wheel.ID.Value,
                    wheel.Name
                };
                lstWheels.Items.Add(new ListViewItem(value));
            }

            lstWheels.ListViewItemSorter = new WheelSorter(0);
            lstWheels.Sort();

            stlStatusLabel.Text = "";
            copyItemToolStripMenuItem.Enabled = false;
            deleteItemToolStripMenuItem.Enabled = false;
            tsbCopyItem.Enabled = false;
            tsbDeleteItem.Enabled = false;
        }

        private void EditSelectedEntry()
        {
            if (lstWheels.SelectedItems.Count > 1)
                return;
            if (List == null || lstWheels.SelectedIndices.Count <= 0)
                return;

            if (!int.TryParse(lstWheels.SelectedItems[0].Text, out int index))
                return;
            Wheel wheel = List.Entries[index];

            WheelEditor editor = new WheelEditor(List.Entries.Count - 1);
            editor.Wheel = wheel;
            editor.OnDone += Editor_OnDone;
            editor.ShowDialog(this);
        }

        private void AddItem()
        {
            if (List == null)
                return;
            Wheel wheel = new();
            wheel.Index = List.Entries.Count;
            wheel.ID = new EncryptedString("");
            wheel.Name = "";

            WheelEditor editor = new WheelEditor(List.Entries.Count);
            editor.Wheel = wheel;
            editor.OnDone += Editor_OnDone1; ;
            editor.ShowDialog(this);
        }

        private void CopyItem()
        {
            if (List == null || lstWheels.SelectedItems.Count != 1
                || lstWheels.SelectedIndices.Count <= 0)
                return;

            if (!int.TryParse(lstWheels.SelectedItems[0].Text, out int index))
                return;
            Wheel wheel = new Wheel(List.Entries[index]);
            wheel.Index = List.Entries.Count;

            WheelEditor editor = new WheelEditor(List.Entries.Count);
            editor.Wheel = wheel;
            editor.OnDone += Editor_OnDone1;
            editor.ShowDialog(this);
        }

        private void DeleteItem()
        {
            if (List == null || lstWheels.SelectedItems.Count != 1
                || lstWheels.SelectedIndices.Count <= 0)
                return;

            if (!int.TryParse(lstWheels.SelectedItems[0].Text, out int index))
                return;
            List.Entries.RemoveAt(index);
            for (int i = index; i < List.Entries.Count; ++i)
                List.Entries[i].Index--;

            Edit?.Invoke();
            UpdateDisplay();
        }

        private void Editor_OnDone1(Wheel wheel)
        {
            // Insert if not at end, else add
            if (wheel.Index != List.Entries.Count)
            {
                List.Entries.Insert(wheel.Index, wheel);

                for (int i = 0; i < List.Entries.Count; ++i)
                    List.Entries[i].Index = i;
            }
            else
            {
                List.Entries.Add(wheel);
            }

            Edit?.Invoke();
            UpdateDisplay();
        }

        private void Editor_OnDone(Wheel wheel)
        {
            // If the index has changed, edit the list
            int oldIndex = int.Parse(lstWheels.SelectedItems[0].Text); // Tried in EditSelectedEntry()
            if (oldIndex != wheel.Index)
            {
                Wheel old = List.Entries[oldIndex];
                List.Entries.RemoveAt(oldIndex);
                List.Entries.Insert(wheel.Index, old);

                for (int i = 0; i < List.Entries.Count; ++i)
                    List.Entries[i].Index = i;
            }

            // Edit the wheel
            List.Entries[wheel.Index] = wheel;
            Edit?.Invoke();
            UpdateDisplay();
        }

        private void lstWheels_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            EditSelectedEntry();
        }

        private void lstWheels_SelectedIndexChanged(object sender, EventArgs e)
        {
            stlStatusLabel.Text = lstWheels.SelectedItems.Count + " Item(s) Selected";
            copyItemToolStripMenuItem.Enabled = true;
            deleteItemToolStripMenuItem.Enabled = true;
            tsbCopyItem.Enabled = true;
            tsbDeleteItem.Enabled = true;
        }

        private void lstWheels_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            int column = e.Column;

            bool direction = false;

            if (lstWheels.ListViewItemSorter is WheelSorter sorter)
            {
                if (sorter.Column == column)
                {
                    sorter.Swap();
                    lstWheels.Sort();
                    return;
                }
                direction = sorter.Direction;
            }

            WheelSorter newSorter = new WheelSorter(column)
            {
                Direction = !direction
            };
            lstWheels.ListViewItemSorter = newSorter;
            lstWheels.Sort();
        }

        private class WheelSorter : IComparer
        {
            public readonly int Column;
            public bool Direction;

            public WheelSorter(int column)
            {
                Column = column;
                Direction = false;
            }

            public int Compare(object x, object y)
            {
                ListViewItem itemX = (ListViewItem)x;
                ListViewItem itemY = (ListViewItem)y;

                if (Column > itemX.SubItems.Count || Column > itemY.SubItems.Count)
                {
                    if (Direction)
                        return -1;
                    return 1;
                }

                string iX = itemX.SubItems[Column].Text;
                string iY = itemY.SubItems[Column].Text;


                if (int.TryParse(iX, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out int iXint))
                {
                    if (int.TryParse(iY, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out int iYint))
                    {
                        int val2 = iXint.CompareTo(iYint);
                        if (this.Direction)
                            return val2 * -1;
                        return val2;
                    }
                }

                int val = string.CompareOrdinal(iX, iY);
                if (Direction)
                    return val * -1;
                return val;
            }

            public void Swap()
            {
                Direction = !Direction;
            }
        }

        private void addItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddItem();
        }

        private void tsbAddItem_Click(object sender, EventArgs e)
        {
            AddItem();
        }

        private void copyItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyItem();
        }

        private void tsbCopyItem_Click(object sender, EventArgs e)
        {
            CopyItem();
        }

        private void deleteItemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteItem();
        }

        private void tsbDeleteItem_Click(object sender, EventArgs e)
        {
            DeleteItem();
        }
    }
}
