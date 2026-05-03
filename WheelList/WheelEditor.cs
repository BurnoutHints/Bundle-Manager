using BundleUtilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace WheelList
{
    public partial class WheelEditor : Form
    {
        public delegate void Done(Wheel wheel);
        public event Done OnDone;
        private Wheel _wheel;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Wheel Wheel
        {
            get
            {
                return _wheel;
            }
            set
            {
                _wheel = value;
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
            Wheel result = new Wheel(Wheel);

            // Index
            int index;
            if (int.TryParse(txtIndex.Text, out index))
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
                EncryptedString ID = new EncryptedString(txtID.Text);
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
}
