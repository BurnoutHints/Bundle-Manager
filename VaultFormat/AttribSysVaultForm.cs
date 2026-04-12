using System;
using System.Collections;
using System.Globalization;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using PluginAPI;
using BundleUtilities;
using LangEditor;

namespace VaultFormat
{
    public delegate void Notify();  // delegate
    public partial class AttribSysVaultForm : Form, IEntryEditor
    {
        public AttribSysVaultForm()
        {
            InitializeComponent();
        }
       
        public event Notify EditEvent;

        private AttribSys _attribSys;
        public AttribSys AttribSys
        {
            get => _attribSys;
            set
            {
                _attribSys = value;
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            lstDataChunks.Items.Clear();

            if (AttribSys == null)
                return;

            for (int i = 0; i < AttribSys.Attributes.Count; i++)
            {
                IAttribute chunk = AttribSys.Attributes[i];

                string[] value = {
                    chunk.getHeader().ClassName,
                    chunk.getHeader().ClassHash.ToString("X16"),
                    chunk.getHeader().CollectionHash.ToString("X16"),
                };
                lstDataChunks.Items.Add(new ListViewItem(value));
            }

            lstDataChunks.ListViewItemSorter = new AttribSysVaultSorter(0);
            lstDataChunks.Sort();
        }

        private class AttribSysVaultSorter : IComparer
        {
            public readonly int Column;
            public bool Direction;

            public AttribSysVaultSorter(int column)
            {
                this.Column = column;
                this.Direction = false;
            }

            public int Compare(object x, object y)
            {
                ListViewItem itemX = (ListViewItem)x;
                ListViewItem itemY = (ListViewItem)y;

                if (Column > itemX.SubItems.Count || Column > itemY.SubItems.Count)
                {
                    if (this.Direction)
                        return -1;
                    return 1;
                }

                string iX = itemX.SubItems[Column].Text;
                string iY = itemY.SubItems[Column].Text;

                int iXint;
                int iYint;

                if (int.TryParse(iX, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out iXint))
                {
                    if (int.TryParse(iY, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out iYint))
                    {
                        int val2 = iXint.CompareTo(iYint);
                        if (this.Direction)
                            return val2 * -1;
                        return val2;
                    }
                }

                int val = String.CompareOrdinal(iX, iY);
                if (this.Direction)
                    return val * -1;
                return val;
            }

            public void Swap()
            {
                this.Direction = !this.Direction;
            }
        }

        private void changeCollectionHashToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string value = InputDialog.ShowInput(this, "Please enter the value to get the lookup 8.");
            if (value == null)
                return;
            ulong result = Utilities.CalcLookup8(value);
            int index = AttribSys.Attributes.FindIndex(i => i.getHeader().ClassName == lstDataChunks.SelectedItems[0].Text);
            AttribSys.Attributes[index].getHeader().CollectionHash = result;
            MessageBox.Show(this, "The lookup 8 hashed value is: " + result.ToString("X16"), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            EditEvent?.Invoke();
            UpdateDisplay();
        }

        private void lstDataChunks_DoubleClick(object sender, EventArgs e)
        {
            Console.WriteLine(lstDataChunks.SelectedItems[0].Text);
            int index = AttribSys.Attributes.FindIndex(i => i.getHeader().ClassName == lstDataChunks.SelectedItems[0].Text);
            propertyGrid2.SelectedObject = AttribSys.Attributes[index];
        }

        private void AttribSysVaultForm_FormClosed(object s, FormClosedEventArgs e)
        {
            EditEvent?.Invoke();
            UpdateDisplay();
        }

        private void exportVaultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Dialog Creation
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "JSON File (*.json)|*.json";
            saveDialog.Title = "Save AttribSys as JSON";
            saveDialog.FileName = "AttribSys.json";

            // Dialog Logic
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Convertion of the AttribSys. Might need smarter structure to only keep the essential data concerning the configuration of the vehicle
                    string jsonContent = JsonConvert.SerializeObject(AttribSys, Formatting.Indented);

                    File.WriteAllText(saveDialog.FileName, jsonContent);

                    MessageBox.Show("Successfully exported to " + saveDialog.FileName, "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to export file. Error: " + ex.Message, "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
