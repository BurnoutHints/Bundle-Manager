using BundleUtilities;
using PluginAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace VehicleList
{
    public partial class VehicleListForm : Form, IEntryEditor
    {
        public delegate void OnEdit();
        public event OnEdit Edit;

        private VehicleListData _list;
        private System.Windows.Forms.Timer _searchTimer;

        public VehicleListData List
        {
            get => _list;
            set
            {
                _list = value;
                UpdateDisplay();
            }
        }

        public VehicleListForm()
        {
            InitializeComponent();
            _searchTimer = new System.Windows.Forms.Timer();
            _searchTimer.Interval = 300;
            _searchTimer.Tick += SearchTimer_Tick;

            tstbSearchItem.Text = "Search by ID, Parent ID, Vehicle or Manufacturer";
            tstbSearchItem.ForeColor = System.Drawing.SystemColors.GrayText;
        }

        private void UpdateDisplay(IEnumerable<Vehicle> vehiclesToDisplay = null)
        {
            lstVehicles.Items.Clear();

            if (List == null)
            {
                return;
            }

            // Use provided list for filtered results, otherwise use the full list.
            IEnumerable<Vehicle> sourceList = vehiclesToDisplay ?? List.Entries;

            foreach (Vehicle vehicle in sourceList)
            {
                // The order of these strings must match the order of ColumnHeaders in your ListView.
                string[] value = {
                    vehicle.Index.ToString(),
                    vehicle.ID.Value,
                    vehicle.ParentID.Value,
                    vehicle.WheelType,
                    vehicle.CarName,
                    vehicle.CarBrand,
                    vehicle.DamageLimit.ToString(),
                    vehicle.Flags.ToString(),
                    vehicle.BoostLength.ToString(),
                    vehicle.VehicleRank.ToString(),
                    vehicle.BoostCapacity.ToString(),
                    vehicle.DisplayStrength.ToString(),
                    vehicle.AttribSysCollectionKey.ToString(),
                    vehicle.ExhaustName.Value,
                    vehicle.ExhaustID.ToString(),
                    vehicle.EngineID.ToString(),
                    vehicle.EngineName.Value,
                    vehicle.ClassUnlockStreamHash.ToString(),
                    vehicle.CarShutdownStreamID.ToString(),
                    vehicle.CarReleasedStreamID.ToString(),
                    vehicle.AIMusicHash.ToString(),
                    vehicle.AIExhaustIndex.ToString(),
                    vehicle.AIExhaustIndex2.ToString(),
                    vehicle.AIExhaustIndex3.ToString(),
                    vehicle.Category.ToString(),
                    vehicle.VehicleType.ToString(),
                    vehicle.BoostType.ToString(),
                    vehicle.FinishType.ToString(),
                    vehicle.MaxSpeedNoBoost.ToString(),
                    vehicle.MaxSpeedBoost.ToString(),
                    vehicle.DisplaySpeed.ToString(),
                    vehicle.DisplayBoost.ToString(),
                    vehicle.Color.ToString(),
                    vehicle.ColorType.ToString()
                };
                lstVehicles.Items.Add(new ListViewItem(value));
            }

            // Reapply sorting if a sorter is active.
            if (lstVehicles.ListViewItemSorter is VehicleSorter sorter)
            {
                lstVehicles.Sort();
            }

            stlStatusLabel.Text = $"{lstVehicles.Items.Count} Item(s) Displayed";

            // Disable copy/delete buttons as no items are selected after a fresh display/filter.
            copyItemToolStripMenuItem.Enabled = false;
            deleteItemToolStripMenuItem.Enabled = false;
            tsbCopyItem.Enabled = false;
            tsbDeleteItem.Enabled = false;
        }

        private void PerformSearch(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText) || searchText == "Search by ID, Parent ID, Vehicle or Manufacturer")
            {
                UpdateDisplay();
                return;
            }

            string lowerSearchText = searchText.ToLowerInvariant();

            var filteredVehicles = List.Entries.Where(vehicle =>
            {
                string[] searchableData = {
                    vehicle.ID.Value,
                    vehicle.ParentID.Value,
                    vehicle.CarName,
                    vehicle.CarBrand
                };

                return searchableData.Any(data => data != null && data.ToLowerInvariant().Contains(lowerSearchText));
            }).ToList();

            UpdateDisplay(filteredVehicles);
        }

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            _searchTimer.Stop();
            PerformSearch(tstbSearchItem.Text);
        }

        private void EditSelectedEntry()
        {
            if (lstVehicles.SelectedItems.Count > 1 || List == null || lstVehicles.SelectedIndices.Count <= 0)
            {
                return;
            }

            if (!int.TryParse(lstVehicles.SelectedItems[0].Text, out int index))
            {
                return;
            }

            Vehicle vehicle = List.Entries[index];
            VehicleEditor editor = new VehicleEditor(List.Entries.Count - 1);
            editor.Vehicle = vehicle;
            editor.OnDone += Editor_OnDone;
            editor.ShowDialog(this);
        }

        private void AddItem()
        {
            if (List == null)
            {
                return;
            }

            Vehicle vehicle = new Vehicle();
            vehicle.Index = List.Entries.Count;
            vehicle.ID = new EncryptedString("");
            vehicle.ParentID = new EncryptedString("");
            vehicle.ExhaustName = new EncryptedString("");
            vehicle.EngineName = new EncryptedString("");

            VehicleEditor editor = new VehicleEditor(List.Entries.Count);
            editor.Vehicle = vehicle;
            editor.OnDone += Editor_OnDone1;
            editor.ShowDialog(this);
        }

        private void CopyItem()
        {
            if (List == null || lstVehicles.SelectedItems.Count != 1 || lstVehicles.SelectedIndices.Count <= 0)
            {
                return;
            }

            if (!int.TryParse(lstVehicles.SelectedItems[0].Text, out int index))
            {
                return;
            }

            Vehicle vehicle = new Vehicle(List.Entries[index]);
            vehicle.Index = List.Entries.Count;

            VehicleEditor editor = new VehicleEditor(List.Entries.Count);
            editor.Vehicle = vehicle;
            editor.OnDone += Editor_OnDone1;
            editor.ShowDialog(this);
        }

        private void DeleteItem()
        {
            if (List == null || lstVehicles.SelectedItems.Count != 1 || lstVehicles.SelectedIndices.Count <= 0)
            {
                return;
            }

            if (!int.TryParse(lstVehicles.SelectedItems[0].Text, out int index))
            {
                return;
            }

            List.Entries.RemoveAt(index);

            for (int i = index; i < List.Entries.Count; ++i)
            {
                List.Entries[i].Index--;
            }

            Edit?.Invoke();
            PerformSearch(tstbSearchItem.Text);
        }

        private void Editor_OnDone1(Vehicle vehicle)
        {
            if (vehicle.Index != List.Entries.Count)
            {
                List.Entries.Insert(vehicle.Index, vehicle);

                for (int i = 0; i < List.Entries.Count; ++i)
                {
                    List.Entries[i].Index = i;
                }
            }
            else
            {
                List.Entries.Add(vehicle);
            }

            Edit?.Invoke();
            PerformSearch(tstbSearchItem.Text);
        }

        private void Editor_OnDone(Vehicle vehicle)
        {
            int oldIndex = int.Parse(lstVehicles.SelectedItems[0].Text);

            if (oldIndex != vehicle.Index)
            {
                Vehicle old = List.Entries[oldIndex];
                List.Entries.RemoveAt(oldIndex);
                List.Entries.Insert(vehicle.Index, old);

                for (int i = 0; i < List.Entries.Count; ++i)
                {
                    List.Entries[i].Index = i;
                }
            }

            List.Entries[vehicle.Index] = vehicle;
            Edit?.Invoke();
            PerformSearch(tstbSearchItem.Text);
        }

        private void lstVehicles_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            EditSelectedEntry();
        }

        private void lstVehicles_SelectedIndexChanged(object sender, EventArgs e)
        {
            stlStatusLabel.Text = lstVehicles.SelectedItems.Count + " Item(s) Selected";
            copyItemToolStripMenuItem.Enabled = true;
            deleteItemToolStripMenuItem.Enabled = true;
            tsbCopyItem.Enabled = true;
            tsbDeleteItem.Enabled = true;
        }

        private void lstVehicles_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            int column = e.Column;
            bool direction = false;

            if (lstVehicles.ListViewItemSorter is VehicleSorter sorter)
            {
                if (sorter.Column == column)
                {
                    sorter.Swap();
                    lstVehicles.Sort();
                    return;
                }
                direction = sorter.Direction;
            }

            VehicleSorter newSorter = new VehicleSorter(column)
            {
                Direction = !direction
            };

            lstVehicles.ListViewItemSorter = newSorter;
            lstVehicles.Sort();
        }

        private class VehicleSorter : IComparer
        {
            public readonly int Column;
            public bool Direction;

            public VehicleSorter(int column)
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
                    {
                        return -1;
                    }
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
                        {
                            return val2 * -1;
                        }
                        return val2;
                    }
                }

                int val = string.CompareOrdinal(iX, iY);
                if (Direction)
                {
                    return val * -1;
                }
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

        private void tsbSearchItem_Click(object sender, EventArgs e)
        {
            PerformSearch(tstbSearchItem.Text);
        }

        private void tstbSearchItem_TextChanged(object sender, EventArgs e)
        {
            if (tstbSearchItem.Text == "Search by ID, Parent ID, Vehicle or Manufacturer")
            {
                return;
            }

            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void tstbSearchItem_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
            }
        }

        private void tstbSearchItem_Enter(object sender, EventArgs e)
        {
            if (tstbSearchItem.Text == "Search by ID, Parent ID, Vehicle or Manufacturer")
            {
                tstbSearchItem.Text = "";
                tstbSearchItem.ForeColor = System.Drawing.SystemColors.WindowText;

                _searchTimer.Stop();
            }
        }

        private void tstbSearchItem_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tstbSearchItem.Text))
            {
                tstbSearchItem.Text = "Search by ID, Parent ID, Vehicle or Manufacturer";
                tstbSearchItem.ForeColor = System.Drawing.SystemColors.GrayText;
            }
        }

        private void tsbClearSearch_Click(object sender, EventArgs e)
        {
            tstbSearchItem.Text = "Search by ID, Parent ID, Vehicle or Manufacturer";
            tstbSearchItem.ForeColor = System.Drawing.SystemColors.GrayText;

            UpdateDisplay();
        }
    }
}
