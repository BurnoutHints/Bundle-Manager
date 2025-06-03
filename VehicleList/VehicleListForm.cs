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

        // Timer to delay search operations, preventing excessive calls during rapid typing.
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

            // Initialize the search timer.
            _searchTimer = new System.Windows.Forms.Timer();
            _searchTimer.Interval = 300; // Set interval to 300 milliseconds (adjust as needed for responsiveness vs. performance)
            _searchTimer.Tick += SearchTimer_Tick; // Assign the event handler for the timer tick
        }

        private void UpdateDisplay(IEnumerable<Vehicle> vehiclesToDisplay = null)
        {
            lstVehicles.Items.Clear(); // Clears all items from the ListView

            if (List == null)
                return; // If the main list is null, there's nothing to display

            // Determines which list to iterate over:
            // If 'vehiclesToDisplay' is provided, use it (for filtered results).
            // Otherwise, use the full 'List.Entries' (for showing all vehicles).
            IEnumerable<Vehicle> sourceList = vehiclesToDisplay ?? List.Entries;

            foreach (Vehicle vehicle in sourceList)
            {
                
                // Populate the ListViewItem with vehicle data.
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
            
            // Reapply sorting after updating the display to maintain order.
            if (lstVehicles.ListViewItemSorter is VehicleSorter sorter)
            {
                lstVehicles.Sort();
            }
            
            // Update the status label to reflect the number of displayed items.
            stlStatusLabel.Text = $"{lstVehicles.Items.Count} Item(s) Displayed";
            
            // Disable copy/delete buttons as no items are selected after a fresh display/filter.
            copyItemToolStripMenuItem.Enabled = false;
            deleteItemToolStripMenuItem.Enabled = false;
            tsbCopyItem.Enabled = false;
            tsbDeleteItem.Enabled = false;
        }

        private void PerformSearch(string searchText)
        {
            
            // If the search text is empty or null, display all vehicles.
            if (string.IsNullOrWhiteSpace(searchText))
            {
                UpdateDisplay(); // Calls the UpdateDisplay method without any specific vehicle list, showing all.
                return;
            }
            
            // Convert the search text to lowercase for case-insensitive comparison.
            string lowerSearchText = searchText.ToLowerInvariant();
            
            // Filter the main list of vehicles using LINQ.
            // LINQ (Language Integrated Query) provides a powerful way to query data.
            var filteredVehicles = List.Entries.Where(vehicle =>
            {
                
                // Create an array of string representations for all relevant vehicle properties.
                // This array's elements should ideally correspond to the columns in your ListView.
                string[] vehicleData = {
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
                
                // Check if any of the vehicle's data strings contains the search text.
                // The Any() method returns true if at least one element in the 'vehicleData' array
                // satisfies the condition (i.e., contains the search text).
                return vehicleData.Any(data => data != null && data.ToLowerInvariant().Contains(lowerSearchText));
            }).ToList(); // Convert the filtered results back to a List to pass to UpdateDisplay.
            
            // Display the filtered list in the ListView.
            UpdateDisplay(filteredVehicles);
        }

        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            _searchTimer.Stop(); // Stop the timer to prevent multiple calls
            PerformSearch(tstbSearchItem.Text); // Perform the search with the current text
        }

        private void EditSelectedEntry()
        {
            if (lstVehicles.SelectedItems.Count > 1)
                return;
            if (List == null || lstVehicles.SelectedIndices.Count <= 0)
                return;

            if (!int.TryParse(lstVehicles.SelectedItems[0].Text, out int index))
                return;
            Vehicle vehicle = List.Entries[index];

            VehicleEditor editor = new VehicleEditor(List.Entries.Count - 1);
            editor.Vehicle = vehicle;
            editor.OnDone += Editor_OnDone;
            editor.ShowDialog(this);
        }

        private void AddItem()
        {
            if (List == null)
                return;
            Vehicle vehicle = new Vehicle();
            vehicle.Index = List.Entries.Count;
            vehicle.ID = new EncryptedString("");
            vehicle.ParentID = new EncryptedString("");
            vehicle.ExhaustName = new EncryptedString("");
            vehicle.EngineName = new EncryptedString("");

            VehicleEditor editor = new VehicleEditor(List.Entries.Count);
            editor.Vehicle = vehicle;
            editor.OnDone += Editor_OnDone1; ;
            editor.ShowDialog(this);
        }

        private void CopyItem()
        {
            if (List == null || lstVehicles.SelectedItems.Count != 1
                || lstVehicles.SelectedIndices.Count <= 0)
                return;

            if (!int.TryParse(lstVehicles.SelectedItems[0].Text, out int index))
                return;
            Vehicle vehicle = new Vehicle(List.Entries[index]);
            vehicle.Index = List.Entries.Count;

            VehicleEditor editor = new VehicleEditor(List.Entries.Count);
            editor.Vehicle = vehicle;
            editor.OnDone += Editor_OnDone1;
            editor.ShowDialog(this);
        }

        private void DeleteItem()
        {
            if (List == null || lstVehicles.SelectedItems.Count != 1
                || lstVehicles.SelectedIndices.Count <= 0)
                return;

            if (!int.TryParse(lstVehicles.SelectedItems[0].Text, out int index))
                return;
            List.Entries.RemoveAt(index);
            
            for (int i = index; i < List.Entries.Count; ++i)
                List.Entries[i].Index--;

            Edit?.Invoke();
            
            // After deleting a vehicle, update the display.
            // Re-apply the current search filter (or show all if search is empty).
            PerformSearch(tstbSearchItem.Text); // Use PerformSearch to handle both filtered and full display
        }

        private void Editor_OnDone1(Vehicle vehicle)
        {
            
            // Insert if not at end, else add
            if (vehicle.Index != List.Entries.Count)
            {
                List.Entries.Insert(vehicle.Index, vehicle);

                for (int i = 0; i < List.Entries.Count; ++i)
                    List.Entries[i].Index = i;
            }
            else
            {
                List.Entries.Add(vehicle);
            }

            Edit?.Invoke();
            
            // After adding a new vehicle, update the display.
            // If there's search text, re-apply the filter to include the new item.
            // Otherwise, show all items (the new one will be included).
            PerformSearch(tstbSearchItem.Text); // Use PerformSearch to handle both filtered and full display
        }

        private void Editor_OnDone(Vehicle vehicle)
        {
            
            // If the index has changed, edit the list
            int oldIndex = int.Parse(lstVehicles.SelectedItems[0].Text); // Tried in EditSelectedEntry()
            
            if (oldIndex != vehicle.Index)
            {
                Vehicle old = List.Entries[oldIndex];
                List.Entries.RemoveAt(oldIndex);
                List.Entries.Insert(vehicle.Index, old);

                for (int i = 0; i < List.Entries.Count; ++i)
                    List.Entries[i].Index = i;
            }

            // Edit the vehicle
            List.Entries[vehicle.Index] = vehicle;
            Edit?.Invoke();
            
            // After editing a vehicle, update the display.
            // Re-apply the current search filter to ensure display consistency.
            PerformSearch(tstbSearchItem.Text); // Use PerformSearch to handle both filtered and full display
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

        private void tsbSearchItem_Click(object sender, EventArgs e)
        {
            
            // When the search button is clicked, call the PerformSearch method
            // with the current text from the search text box.
            PerformSearch(tstbSearchItem.Text);
        }

        private void tstbSearchItem_TextChanged(object sender, EventArgs e)
        {
            
            // When text changes, stop any pending search and restart the timer.
            // This creates a debounce effect, so search is only performed after a brief pause in typing.
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void tstbSearchItem_KeyPress(object sender, KeyPressEventArgs e)
        {
            
            // Check if the pressed key is the Enter key.
            if (e.KeyChar == (char)Keys.Enter)
            {
                
                // Prevent the "ding" sound by marking the event as handled.
                // This tells the system that we've processed the key press and no further action (like playing a sound) is needed.
                e.Handled = true;

                // Optionally, you might want to immediately perform the search here as well,
                // although the TextChanged event with the timer already handles it after a slight delay.
                // If you want instant search on Enter without waiting for the timer, you could call:
                // PerformSearch(tstbSearchItem.Text);
                // However, sticking with the timer for consistency with typing is generally better.
            }
        }
    }
}
