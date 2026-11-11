using BundleUtilities;
using LangEditor;
using PluginAPI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace VaultFormat;

public delegate void Notify();  // delegate
public partial class AttribSysVaultForm : Form, IEntryEditor
{
    private readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    public AttribSysVaultForm()
    {
        InitializeComponent();
    }

    public event Notify EditEvent;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public AttribSys AttribSys
    {
        get;
        set
        {
            field = value;
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

            string[] value = [
                chunk.GetHeader().ClassName,
                chunk.GetHeader().ClassHash.ToString("X16"),
                chunk.GetHeader().CollectionHash.ToString("X16"),
            ];
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

                    if (Direction)
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

    private void changeCollectionHashToolStripMenuItem_Click(object sender, EventArgs e)
    {
        string value = InputDialog.ShowInput(this, "Please enter the value to get the lookup 8.");

        if (value == null)
            return;

        ulong result = Utilities.CalcLookup8(value);
        int index = AttribSys.Attributes.FindIndex(i => i.GetHeader().ClassName == lstDataChunks.SelectedItems[0].Text);
        AttribSys.Attributes[index].GetHeader().CollectionHash = result;
        MessageBox.Show(this, "The lookup 8 hashed value is: " + result.ToString("X16"), "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        EditEvent?.Invoke();
        UpdateDisplay();
    }

    private void lstDataChunks_DoubleClick(object sender, EventArgs e)
    {
        Console.WriteLine(lstDataChunks.SelectedItems[0].Text);
        int index = AttribSys.Attributes.FindIndex(i => i.GetHeader().ClassName == lstDataChunks.SelectedItems[0].Text);
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
        SaveFileDialog saveDialog = new()
        {
            Filter = "JSON File (*.json)|*.json",
            Title = "Save AttribSys as JSON",
            FileName = "AttribSys.json"
        };

        // Dialog Logic
        if (saveDialog.ShowDialog() == DialogResult.OK)
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(AttribSys, jsonSerializerOptions);

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
