using BundleUtilities;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace VehicleList
{
    public partial class VehicleEditor : Form
    {
        public delegate void Done(Vehicle vehicle);
        public event Done OnDone;
        private Vehicle _vehicle;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Vehicle Vehicle
        {
            get
            {
                return _vehicle;
            }
            set
            {
                _vehicle = value;
                UpdateDisplay();
            }
        }

        public VehicleEditor()
        {
            InitializeComponent();
        }

        private int maxIndex;
        public VehicleEditor(int max)
        {
            InitializeComponent();
            maxIndex = max;
        }

        private void UpdateDisplay()
        {
            Text = "Edit Vehicle: " + Vehicle.Index;

            txtIndex.Text = Vehicle.Index.ToString();
            txtID.Text = Vehicle.ID.Value;
            txtParentID.Text = Vehicle.ParentID.Value;
            txtWheels.Text = Vehicle.WheelType;
            txtName.Text = Vehicle.CarName;
            txtBrand.Text = Vehicle.CarBrand;
            txtDamageLimit.Text = Vehicle.DamageLimit.ToString();

            // Flags
            // Loop through all items
            for (int i = 0; i < chlFlags.Items.Count; ++i)
            {
                // If the flag is set, check the item at this index
                if ((((uint)Vehicle.Flags) & (1 << i)) == (1 << i))
                {
                    chlFlags.SetItemChecked(i, true);
                }
                else
                {
                    // Ensure nothing is checked incorrectly
                    chlFlags.SetItemCheckState(i, CheckState.Unchecked);
                }
            }

            txtBoostLength.Text = Vehicle.BoostLength.ToString();
            cboRank.SelectedIndex = (int)Vehicle.VehicleRank;
            txtBoostCapacity.Text = Vehicle.BoostCapacity.ToString();
            txtStrengthStat.Text = Vehicle.DisplayStrength.ToString();
            txtAttribSysCollectionKey.Text = Vehicle.AttribSysCollectionKey.ToString();
            txtExhaustName.Text = Vehicle.ExhaustName.Value;
            txtExhaustID.Text = Vehicle.ExhaustID.ToString();
            txtEngineID.Text = Vehicle.EngineID.ToString();
            txtEngineName.Text = Vehicle.EngineName.Value;

            // Class Unlock Stream Hash
            uint classUnlockHash = (uint)Vehicle.ClassUnlockStreamHash;
            // Set the selected index based on hash
            switch (classUnlockHash)
            {
                case 0x0470A5BF:
                    cboClassUnlock.SelectedIndex = 0;
                    break;
                case 0x48346FEF:
                    cboClassUnlock.SelectedIndex = 1;
                    break;
                case 0x817B91D9:
                    cboClassUnlock.SelectedIndex = 2;
                    break;
                case 0xA3E2D8C9:
                    cboClassUnlock.SelectedIndex = 3;
                    break;
                case 0xB3845465:
                    cboClassUnlock.SelectedIndex = 4;
                    break;
                case 0xEBE39AE9:
                    cboClassUnlock.SelectedIndex = 5;
                    break;
            }

            txtCarWon.Text = Vehicle.CarShutdownStreamID.ToString();
            txtCarReleased.Text = Vehicle.CarReleasedStreamID.ToString();

            //AI Music Hash
            uint aiMusicHash = (uint)Vehicle.AIMusicHash;
            switch (aiMusicHash)
            {
                case 0:
                    cboAIMusic.SelectedIndex = 0;
                    break;
                case 0xA9813C9D:
                    cboAIMusic.SelectedIndex = 1;
                    break;
                case 0xCB72AEA7:
                    cboAIMusic.SelectedIndex = 2;
                    break;
                case 0x284D944B:
                    cboAIMusic.SelectedIndex = 3;
                    break;
                case 0xD95C2309:
                    cboAIMusic.SelectedIndex = 4;
                    break;
                case 0x8A1A90E9:
                    cboAIMusic.SelectedIndex = 5;
                    break;
                case 0xB12A34DD:
                    cboAIMusic.SelectedIndex = 6;
                    break;
            }

            cboAIExhaust1.SelectedIndex = (int)Vehicle.AIExhaustIndex;
            cboAIExhaust2.SelectedIndex = (int)Vehicle.AIExhaustIndex2;
            cboAIExhaust3.SelectedIndex = (int)Vehicle.AIExhaustIndex3;

            // Category
            for (int i = 0; i < chlCategory.Items.Count; ++i)
            {
                if ((((uint)Vehicle.Category) & (1 << i)) == (1 << i))
                {
                    chlCategory.SetItemChecked(i, true);
                }
                else
                {
                    chlCategory.SetItemCheckState(i, CheckState.Unchecked);
                }
            }

            cboVehicleType.SelectedIndex = (int)Vehicle.VehicleType;
            cboBoostType.SelectedIndex = (int)Vehicle.BoostType;
            cboFinishType.SelectedIndex = (int)Vehicle.FinishType;
            txtMaxSpeed.Text = Vehicle.MaxSpeedNoBoost.ToString();
            txtMaxBoostSpeed.Text = Vehicle.MaxSpeedBoost.ToString();
            txtSpeedStat.Text = Vehicle.DisplaySpeed.ToString();
            txtBoostStat.Text = Vehicle.DisplayBoost.ToString();
            txtColor.Text = Vehicle.Color.ToString();
            cboColorType.SelectedIndex = (int)Vehicle.ColorType;
        }

        private Vehicle GetModifiedVehicle()
        {
            Vehicle result = new Vehicle(Vehicle);

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
            // Vehicle ID
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

            // Parent ID
            try
            {
                EncryptedString ParentID = new EncryptedString(txtParentID.Text);
                result.ParentID = ParentID;
            }
            catch (ArgumentException e)
            {
                MessageBox.Show(this, e.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Wheel name
            result.WheelType = txtWheels.Text;
            if (result.WheelType.Trim().Length == 0)
            {
                MessageBox.Show(this, "Wheel name cannot be left blank.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Vehicle name
            result.CarName = txtName.Text;
            if (result.CarName.Trim().Length == 0)
            {
                MessageBox.Show(this, "Car name cannot be left blank.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Manufacturer name
            result.CarBrand = txtBrand.Text;
            if (result.CarBrand.Trim().Length == 0)
            {
                MessageBox.Show(this, "Manufacturer cannot be left blank.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Damage limit
            if (float.TryParse(txtDamageLimit.Text, out float damageLimit))
            {
                result.DamageLimit = damageLimit;
            }
            else
            {
                MessageBox.Show(this, "Damage limit is invalid.\nEnsure the value is a decimal number.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Flags
            // Ensure only valid flags are used
            if ((uint)Vehicle.Flags < 0x4000000)
            {
                // Write flags based on whether CheckedListBox fields are checked
                uint flags = 0;
                for (int i = 0; i < chlFlags.Items.Count; ++i)
                {
                    if ((int)chlFlags.GetItemCheckState(i) == 1)
                    {
                        flags += (uint)(1 << i);
                    }
                }
                result.Flags = (Flags)flags;
            }
            else
            {
                MessageBox.Show(this, "Invalid flags used.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Boost length
            if (byte.TryParse(txtBoostLength.Text, out byte boostLength))
            {
                result.BoostLength = boostLength;
            }
            else
            {
                MessageBox.Show(this, "Boost length is invalid.\nEnsure the value is a decimal integer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Rank
            if (byte.TryParse(cboRank.SelectedIndex.ToString(), out byte rank))
            {
                result.VehicleRank = (VehicleRank)rank;
            }
            else
            {
                MessageBox.Show(this, "Rank is invalid.\nEnsure the value is a decimal integer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Boost capacity
            if (byte.TryParse(txtBoostCapacity.Text, out byte boostCapacity))
            {
                result.BoostCapacity = boostCapacity;
            }
            else
            {
                MessageBox.Show(this, "Boost capacity is invalid.\nEnsure the value is a decimal integer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Strength stat
            if (byte.TryParse(txtStrengthStat.Text, out byte displayStrength))
            {
                result.DisplayStrength = displayStrength;
            }
            else
            {
                MessageBox.Show(this, "Strength stat is invalid.\nEnsure the value is a decimal integer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // AttribSys ID
            if (long.TryParse(txtAttribSysCollectionKey.Text, out long attribsysId))
            {
                result.AttribSysCollectionKey = attribsysId;
            }
            else
            {
                MessageBox.Show(this, "AttribSys ID is invalid.\nEnsure the value is a decimal integer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Exhaust name
            if (txtExhaustName.Text.Trim().Length == 0)
            {
                MessageBox.Show(this, "Exhaust name cannot be left blank.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            try
            {
                EncryptedString OtherID1 = new EncryptedString(txtExhaustName.Text);
                result.ExhaustName = OtherID1;
            }
            catch (ArgumentException e)
            {
                MessageBox.Show(this, e.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Exhaust ID
            if (long.TryParse(txtExhaustID.Text, out long group1))
            {
                result.ExhaustID = group1;
            }
            else
            {
                MessageBox.Show(this, "Exhaust ID is invalid.\nEnsure the value is a decimal integer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Engine ID
            if (long.TryParse(txtEngineID.Text, out long group2))
            {
                result.EngineID = group2;
            }
            else
            {
                MessageBox.Show(this, "Engine ID is invalid.\nEnsure the value is a decimal integer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Engine name
            if (txtEngineName.Text.Trim().Length == 0)
            {
                MessageBox.Show(this, "Engine name cannot be left blank.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }
            try
            {
                EncryptedString OtherID2 = new EncryptedString(txtEngineName.Text);
                result.EngineName = OtherID2;
            }
            catch (ArgumentException e)
            {
                MessageBox.Show(this, e.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Class unlock stream hash
            if (uint.TryParse(cboClassUnlock.SelectedIndex.ToString(), out uint classUnlock))
            {
                switch (classUnlock)
                {
                    case 0:
                        classUnlock = 0x0470A5BF;
                        break;
                    case 1:
                        classUnlock = 0x48346FEF;
                        break;
                    case 2:
                        classUnlock = 0x817B91D9;
                        break;
                    case 3:
                        classUnlock = 0xA3E2D8C9;
                        break;
                    case 4:
                        classUnlock = 0xB3845465;
                        break;
                    case 5:
                        classUnlock = 0xEBE39AE9;
                        break;
                }
                result.ClassUnlockStreamHash = (ClassUnlock)classUnlock;
            }
            else
            {
                MessageBox.Show(this, "Please select a class unlock stream option.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Car won stream ID
            if (long.TryParse(txtCarWon.Text, out long carWon))
            {
                result.CarShutdownStreamID = carWon;
            }
            else
            {
                MessageBox.Show(this, "Car won voiceover stream ID is invalid.\nEnsure the value is a decimal integer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Car released stream ID
            if (long.TryParse(txtCarReleased.Text, out long carReleased))
            {
                result.CarReleasedStreamID = carReleased;
            }
            else
            {
                MessageBox.Show(this, "Car released voiceover stream ID is invalid.\nEnsure the value is a decimal integer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // AI music loop stream hash
            if (uint.TryParse(cboAIMusic.SelectedIndex.ToString(), out uint aiMusic))
            {
                switch (aiMusic)
                {
                    case 0:
                        aiMusic = 0;
                        break;
                    case 1:
                        aiMusic = 0xA9813C9D;
                        break;
                    case 2:
                        aiMusic = 0xCB72AEA7;
                        break;
                    case 3:
                        aiMusic = 0x284D944B;
                        break;
                    case 4:
                        aiMusic = 0xD95C2309;
                        break;
                    case 5:
                        aiMusic = 0x8A1A90E9;
                        break;
                    case 6:
                        aiMusic = 0xB12A34DD;
                        break;
                }
                result.AIMusicHash = (AIMusic)aiMusic;
            }
            else
            {
                MessageBox.Show(this, "Please select an AI music loop option.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // AI exhuast index
            if (byte.TryParse(cboAIExhaust1.SelectedIndex.ToString(), out byte aiEx1))
            {
                result.AIExhaustIndex = (AIExhaustIndex)aiEx1;
            }
            else
            {
                MessageBox.Show(this, "Please select an AI exhaust option.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // AI exhuast index 2
            if (byte.TryParse(cboAIExhaust2.SelectedIndex.ToString(), out byte aiEx2))
            {
                result.AIExhaustIndex2 = (AIExhaustIndex)aiEx2;
            }
            else
            {
                MessageBox.Show(this, "Please select an AI exhuast 2 option.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // AI exhuast index 3
            if (byte.TryParse(cboAIExhaust3.SelectedIndex.ToString(), out byte aiEx3))
            {
                result.AIExhaustIndex3 = (AIExhaustIndex)aiEx3;
            }
            else
            {
                MessageBox.Show(this, "Please select an AI exhuast 3 option.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Vehicle category
            if ((uint)Vehicle.Category < 0x100)
            {
                uint cat = 0;
                for (int i = 0; i < chlCategory.Items.Count; ++i)
                {
                    if ((int)chlCategory.GetItemCheckState(i) == 1)
                    {
                        cat += (uint)(1 << i);
                    }
                }
                result.Category = (VehicleCategory)cat;
            }
            else
            {
                MessageBox.Show(this, "Invalid category selected.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Vehicle type
            if (int.TryParse(cboVehicleType.SelectedIndex.ToString(), out int vehType))
            {
                result.VehicleType = (VehicleType)vehType;
            }
            else
            {
                MessageBox.Show(this, "Please select a vehicle type.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Boost type
            if (byte.TryParse(cboBoostType.SelectedIndex.ToString(), out byte boostType))
            {
                result.BoostType = (BoostType)boostType;
            }
            else
            {
                MessageBox.Show(this, "Please select a boost type.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Finish type
            if (byte.TryParse(cboFinishType.SelectedIndex.ToString(), out byte finishType))
            {
                result.FinishType = (FinishType)cboFinishType.SelectedIndex;
            }
            else
            {
                MessageBox.Show(this, "Please select a finish type.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Max speed
            if (byte.TryParse(txtMaxSpeed.Text, out byte maxSpeed))
            {
                result.MaxSpeedNoBoost = maxSpeed;
            }
            else
            {
                MessageBox.Show(this, "Max speed is invalid.\nEnsure the value is a decimal integer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Max boost speed
            if (byte.TryParse(txtMaxBoostSpeed.Text, out byte maxBoostSpeed))
            {
                result.MaxSpeedBoost = maxBoostSpeed;
            }
            else
            {
                MessageBox.Show(this, "Max boost speed is invalid.\nEnsure the value is a decimal integer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Speed stat
            if (byte.TryParse(txtSpeedStat.Text, out byte displaySpeed))
            {
                result.DisplaySpeed = displaySpeed;
            }
            else
            {
                MessageBox.Show(this, "Speed stat is invalid.\nEnsure the value is a decimal integer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Boost stat
            if (byte.TryParse(txtBoostStat.Text, out byte displayBoost))
            {
                result.DisplayBoost = displayBoost;
            }
            else
            {
                MessageBox.Show(this, "Boost stat is invalid.\nEnsure the value is a decimal integer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Color index
            if (byte.TryParse(txtColor.Text, out byte color))
            {
                result.Color = color;
            }
            else
            {
                MessageBox.Show(this, "Color index is invalid.\nEnsure the value is a decimal integer.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Color type
            if (byte.TryParse(cboColorType.SelectedIndex.ToString(), out byte colorType))
            {
                result.ColorType = (ColorType)colorType;
            }
            else
            {
                MessageBox.Show(this, "Please select a color type.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            Vehicle retVehicle = GetModifiedVehicle();
            if (retVehicle == null)
            {
                return;
            }

            OnDone?.Invoke(retVehicle);

            Close();
        }
    }
}
