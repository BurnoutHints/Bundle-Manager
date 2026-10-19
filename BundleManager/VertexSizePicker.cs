using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace BundleManager;

public partial class VertexSizePicker : Form
{
    public int VertexSize
    {
        get
        {
            string val = cboSelect.Text;

            if (!int.TryParse(val, out int result))
                return -1;
            return result;
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public List<int> VertexSizeList
    {
        get;
        set
        {
            field = value;

            cboSelect.Items.Clear();
            foreach (int size in field)
            {
                cboSelect.Items.Add(size);
            }
            cboSelect.SelectedIndex = 0;
        }
    }

    public VertexSizePicker()
    {
        InitializeComponent();
    }

    private void btnOk_Click(object sender, EventArgs e)
    {
        Close();
    }
}
