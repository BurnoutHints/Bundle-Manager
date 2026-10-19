using BundleUtilities;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace WheelList;

public partial class WheelEditor : Form
{
    public delegate void Done(Wheel wheel);
    public event Done OnDone;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Wheel Wheel
    {
        get;
        set
        {
            field = value;
            UpdateDisplay();
        }
    }

    public WheelEditor()
    {
        InitializeComponent();
    }

    private int maxIndex;
    public WheelEditor(int max)
    {
        InitializeComponent();
        maxIndex = max;
    }

    private void UpdateDisplay()
    {
        Text = "Edit Wheel: " + Wheel.Index;

        txtIndex.Text = Wheel.Index.ToString();
        txtID.Text = Wheel.ID.Value;
        txtName.Text = Wheel.Name;
    }

    private Wheel GetModifiedWheel()
    {
        Wheel result = new(Wheel);

        // Index
        if (int.TryParse(txtIndex.Text, out int index))
        {
            if (index <= maxIndex)
                result.Index = index;
            else
            {
                MessageBox.Show(this, "Index out of bounds.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
        }
        else
        {
            MessageBox.Show(this, "Index is invalid.\nEnsure the value is a decimal integer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }
        // Wheel ID
        if (txtID.Text.Trim().Length == 0)
        {
            MessageBox.Show(this, "ID cannot be left blank.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }
        try
        {
            EncryptedString ID = new(txtID.Text);
            result.ID = ID;
        }
        catch (ArgumentException e)
        {
            MessageBox.Show(this, e.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        // Vehicle name
        result.Name = txtName.Text;
        if (result.Name.Trim().Length == 0)
        {
            MessageBox.Show(this, "Car name cannot be left blank.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        return result;
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void btnOk_Click(object sender, EventArgs e)
    {
        Wheel retWheel = GetModifiedWheel();
        if (retWheel == null)
        {
            return;
        }

        OnDone?.Invoke(retWheel);

        Close();
    }
}
