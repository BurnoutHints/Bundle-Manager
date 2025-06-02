using System.Windows.Forms;

namespace VehicleList
{
    partial class VehicleEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnOk = new Button();
            btnCancel = new Button();
            lblIndex = new Label();
            txtIndex = new TextBox();
            lblID = new Label();
            txtID = new TextBox();
            lblParentID = new Label();
            txtParentID = new TextBox();
            lblWheels = new Label();
            txtWheels = new TextBox();
            lblName = new Label();
            txtName = new TextBox();
            lblBrand = new Label();
            txtBrand = new TextBox();
            lblDamageLimit = new Label();
            txtDamageLimit = new TextBox();
            lblFlags = new Label();
            chlFlags = new CheckedListBox();
            lblBoostLength = new Label();
            txtBoostLength = new TextBox();
            lblRank = new Label();
            cboRank = new ComboBox();
            lblBoostCapacity = new Label();
            txtBoostCapacity = new TextBox();
            lblStrengthStat = new Label();
            txtStrengthStat = new TextBox();
            lblAttribSysCollectionKey = new Label();
            txtAttribSysCollectionKey = new TextBox();
            lblExhaustName = new Label();
            txtExhaustName = new TextBox();
            lblExhaustID = new Label();
            txtExhaustID = new TextBox();
            lblEngineID = new Label();
            txtEngineID = new TextBox();
            lblEngineName = new Label();
            txtEngineName = new TextBox();
            lblClassUnlock = new Label();
            cboClassUnlock = new ComboBox();
            lblCarWon = new Label();
            txtCarWon = new TextBox();
            lblCarReleased = new Label();
            txtCarReleased = new TextBox();
            lblAIMusic = new Label();
            cboAIMusic = new ComboBox();
            lblAIExhaust1 = new Label();
            cboAIExhaust1 = new ComboBox();
            lblAIExhaust2 = new Label();
            cboAIExhaust2 = new ComboBox();
            lblAIExhaust3 = new Label();
            cboAIExhaust3 = new ComboBox();
            lblCategory = new Label();
            chlCategory = new CheckedListBox();
            lblVehicleType = new Label();
            cboVehicleType = new ComboBox();
            lblBoostType = new Label();
            cboBoostType = new ComboBox();
            lblFinishType = new Label();
            cboFinishType = new ComboBox();
            lblMaxSpeed = new Label();
            txtMaxSpeed = new TextBox();
            lblMaxBoostSpeed = new Label();
            txtMaxBoostSpeed = new TextBox();
            lblSpeedStat = new Label();
            txtSpeedStat = new TextBox();
            lblBoostStat = new Label();
            txtBoostStat = new TextBox();
            lblColor = new Label();
            txtColor = new TextBox();
            lblColorType = new Label();
            cboColorType = new ComboBox();
            SuspendLayout();
            // 
            // btnOk
            // 
            btnOk.Location = new System.Drawing.Point(1003, 680);
            btnOk.Margin = new Padding(4, 5, 4, 5);
            btnOk.Name = "btnOk";
            btnOk.Size = new System.Drawing.Size(120, 35);
            btnOk.TabIndex = 0;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new System.Drawing.Point(1131, 680);
            btnCancel.Margin = new Padding(4, 5, 4, 5);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(120, 35);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // lblIndex
            // 
            lblIndex.AutoSize = true;
            lblIndex.Location = new System.Drawing.Point(14, 10);
            lblIndex.Margin = new Padding(4, 0, 4, 0);
            lblIndex.Name = "lblIndex";
            lblIndex.Size = new System.Drawing.Size(48, 20);
            lblIndex.TabIndex = 2;
            lblIndex.Text = "Index:";
            // 
            // txtIndex
            // 
            txtIndex.Location = new System.Drawing.Point(150, 7);
            txtIndex.Margin = new Padding(4, 5, 4, 5);
            txtIndex.Name = "txtIndex";
            txtIndex.Size = new System.Drawing.Size(478, 27);
            txtIndex.TabIndex = 3;
            // 
            // lblID
            // 
            lblID.AutoSize = true;
            lblID.Location = new System.Drawing.Point(14, 70);
            lblID.Margin = new Padding(4, 0, 4, 0);
            lblID.Name = "lblID";
            lblID.Size = new System.Drawing.Size(27, 20);
            lblID.TabIndex = 4;
            lblID.Text = "ID:";
            // 
            // txtID
            // 
            txtID.Location = new System.Drawing.Point(150, 67);
            txtID.Margin = new Padding(4, 5, 4, 5);
            txtID.Name = "txtID";
            txtID.Size = new System.Drawing.Size(478, 27);
            txtID.TabIndex = 5;
            // 
            // lblParentID
            // 
            lblParentID.AutoSize = true;
            lblParentID.Location = new System.Drawing.Point(14, 40);
            lblParentID.Margin = new Padding(4, 0, 4, 0);
            lblParentID.Name = "lblParentID";
            lblParentID.Size = new System.Drawing.Size(72, 20);
            lblParentID.TabIndex = 6;
            lblParentID.Text = "Parent ID:";
            // 
            // txtParentID
            // 
            txtParentID.Location = new System.Drawing.Point(150, 37);
            txtParentID.Margin = new Padding(4, 5, 4, 5);
            txtParentID.Name = "txtParentID";
            txtParentID.Size = new System.Drawing.Size(478, 27);
            txtParentID.TabIndex = 7;
            // 
            // lblWheels
            // 
            lblWheels.AutoSize = true;
            lblWheels.Location = new System.Drawing.Point(14, 550);
            lblWheels.Margin = new Padding(4, 0, 4, 0);
            lblWheels.Name = "lblWheels";
            lblWheels.Size = new System.Drawing.Size(60, 20);
            lblWheels.TabIndex = 8;
            lblWheels.Text = "Wheels:";
            // 
            // txtWheels
            // 
            txtWheels.Location = new System.Drawing.Point(150, 547);
            txtWheels.Margin = new Padding(4, 5, 4, 5);
            txtWheels.Name = "txtWheels";
            txtWheels.Size = new System.Drawing.Size(478, 27);
            txtWheels.TabIndex = 9;
            // 
            // lblName
            // 
            lblName.AutoSize = true;
            lblName.Location = new System.Drawing.Point(14, 130);
            lblName.Margin = new Padding(4, 0, 4, 0);
            lblName.Name = "lblName";
            lblName.Size = new System.Drawing.Size(52, 20);
            lblName.TabIndex = 10;
            lblName.Text = "Name:";
            // 
            // txtName
            // 
            txtName.Location = new System.Drawing.Point(150, 127);
            txtName.Margin = new Padding(4, 5, 4, 5);
            txtName.Name = "txtName";
            txtName.Size = new System.Drawing.Size(478, 27);
            txtName.TabIndex = 11;
            // 
            // lblBrand
            // 
            lblBrand.AutoSize = true;
            lblBrand.Location = new System.Drawing.Point(14, 100);
            lblBrand.Margin = new Padding(4, 0, 4, 0);
            lblBrand.Name = "lblBrand";
            lblBrand.Size = new System.Drawing.Size(51, 20);
            lblBrand.TabIndex = 12;
            lblBrand.Text = "Brand:";
            // 
            // txtBrand
            // 
            txtBrand.Location = new System.Drawing.Point(150, 97);
            txtBrand.Margin = new Padding(4, 5, 4, 5);
            txtBrand.Name = "txtBrand";
            txtBrand.Size = new System.Drawing.Size(478, 27);
            txtBrand.TabIndex = 13;
            // 
            // lblDamageLimit
            // 
            lblDamageLimit.AutoSize = true;
            lblDamageLimit.Location = new System.Drawing.Point(14, 370);
            lblDamageLimit.Margin = new Padding(4, 0, 4, 0);
            lblDamageLimit.Name = "lblDamageLimit";
            lblDamageLimit.Size = new System.Drawing.Size(106, 20);
            lblDamageLimit.TabIndex = 14;
            lblDamageLimit.Text = "Damage Limit:";
            // 
            // txtDamageLimit
            // 
            txtDamageLimit.Location = new System.Drawing.Point(150, 367);
            txtDamageLimit.Margin = new Padding(4, 5, 4, 5);
            txtDamageLimit.Name = "txtDamageLimit";
            txtDamageLimit.Size = new System.Drawing.Size(478, 27);
            txtDamageLimit.TabIndex = 15;
            // 
            // lblFlags
            // 
            lblFlags.AutoSize = true;
            lblFlags.Location = new System.Drawing.Point(674, 570);
            lblFlags.Margin = new Padding(4, 0, 4, 0);
            lblFlags.Name = "lblFlags";
            lblFlags.Size = new System.Drawing.Size(46, 20);
            lblFlags.TabIndex = 16;
            lblFlags.Text = "Flags:";
            // 
            // chlFlags
            // 
            chlFlags.CheckOnClick = true;
            chlFlags.Items.AddRange(new object[] { "Is Race Vehicle", "Can Check Traffic", "Can Be Checked", "Is Trailer", "Can Tow Trailer", "Can Be Painted", "Unknown0", "Is First Car in Speed Range", "Has Switchable Boost", "Unknown1", "Unknown2", "Is WIP/Dev", "Is from 1.0", "Is from 1.3", "Is from 1.4", "Is from 1.5", "Is from 1.6", "Is from 1.7", "Is from 1.8", "Is from 1.9" });
            chlFlags.Location = new System.Drawing.Point(776, 507);
            chlFlags.Margin = new Padding(4, 5, 4, 5);
            chlFlags.Name = "chlFlags";
            chlFlags.Size = new System.Drawing.Size(476, 158);
            chlFlags.TabIndex = 17;
            // 
            // lblBoostLength
            // 
            lblBoostLength.AutoSize = true;
            lblBoostLength.Location = new System.Drawing.Point(14, 310);
            lblBoostLength.Margin = new Padding(4, 0, 4, 0);
            lblBoostLength.Name = "lblBoostLength";
            lblBoostLength.Size = new System.Drawing.Size(99, 20);
            lblBoostLength.TabIndex = 18;
            lblBoostLength.Text = "Boost Length:";
            // 
            // txtBoostLength
            // 
            txtBoostLength.Location = new System.Drawing.Point(150, 307);
            txtBoostLength.Margin = new Padding(4, 5, 4, 5);
            txtBoostLength.Name = "txtBoostLength";
            txtBoostLength.Size = new System.Drawing.Size(478, 27);
            txtBoostLength.TabIndex = 19;
            // 
            // lblRank
            // 
            lblRank.AutoSize = true;
            lblRank.Location = new System.Drawing.Point(674, 160);
            lblRank.Margin = new Padding(4, 0, 4, 0);
            lblRank.Name = "lblRank";
            lblRank.Size = new System.Drawing.Size(44, 20);
            lblRank.TabIndex = 20;
            lblRank.Text = "Rank:";
            // 
            // cboRank
            // 
            cboRank.DropDownStyle = ComboBoxStyle.DropDownList;
            cboRank.FormattingEnabled = true;
            cboRank.Items.AddRange(new object[] { "Learner", "D_Class", "C_Class", "B_Class", "A_Class", "Burnout" });
            cboRank.Location = new System.Drawing.Point(776, 157);
            cboRank.Margin = new Padding(4, 5, 4, 5);
            cboRank.Name = "cboRank";
            cboRank.Size = new System.Drawing.Size(476, 28);
            cboRank.TabIndex = 21;
            // 
            // lblBoostCapacity
            // 
            lblBoostCapacity.AutoSize = true;
            lblBoostCapacity.Location = new System.Drawing.Point(14, 280);
            lblBoostCapacity.Margin = new Padding(4, 0, 4, 0);
            lblBoostCapacity.Name = "lblBoostCapacity";
            lblBoostCapacity.Size = new System.Drawing.Size(111, 20);
            lblBoostCapacity.TabIndex = 22;
            lblBoostCapacity.Text = "Boost Capacity:";
            // 
            // txtBoostCapacity
            // 
            txtBoostCapacity.Location = new System.Drawing.Point(150, 277);
            txtBoostCapacity.Margin = new Padding(4, 5, 4, 5);
            txtBoostCapacity.Name = "txtBoostCapacity";
            txtBoostCapacity.Size = new System.Drawing.Size(478, 27);
            txtBoostCapacity.TabIndex = 23;
            // 
            // lblStrengthStat
            // 
            lblStrengthStat.AutoSize = true;
            lblStrengthStat.Location = new System.Drawing.Point(14, 340);
            lblStrengthStat.Margin = new Padding(4, 0, 4, 0);
            lblStrengthStat.Name = "lblStrengthStat";
            lblStrengthStat.Size = new System.Drawing.Size(98, 20);
            lblStrengthStat.TabIndex = 24;
            lblStrengthStat.Text = "Strength Stat:";
            // 
            // txtStrengthStat
            // 
            txtStrengthStat.Location = new System.Drawing.Point(150, 337);
            txtStrengthStat.Margin = new Padding(4, 5, 4, 5);
            txtStrengthStat.Name = "txtStrengthStat";
            txtStrengthStat.Size = new System.Drawing.Size(478, 27);
            txtStrengthStat.TabIndex = 25;
            // 
            // lblAttribSysCollectionKey
            // 
            lblAttribSysCollectionKey.AutoSize = true;
            lblAttribSysCollectionKey.Location = new System.Drawing.Point(14, 400);
            lblAttribSysCollectionKey.Margin = new Padding(4, 0, 4, 0);
            lblAttribSysCollectionKey.Name = "lblAttribSysCollectionKey";
            lblAttribSysCollectionKey.Size = new System.Drawing.Size(90, 20);
            lblAttribSysCollectionKey.TabIndex = 26;
            lblAttribSysCollectionKey.Text = "AttribSys ID:";
            // 
            // txtAttribSysCollectionKey
            // 
            txtAttribSysCollectionKey.Location = new System.Drawing.Point(150, 397);
            txtAttribSysCollectionKey.Margin = new Padding(4, 5, 4, 5);
            txtAttribSysCollectionKey.Name = "txtAttribSysCollectionKey";
            txtAttribSysCollectionKey.Size = new System.Drawing.Size(478, 27);
            txtAttribSysCollectionKey.TabIndex = 27;
            // 
            // lblExhaustName
            // 
            lblExhaustName.AutoSize = true;
            lblExhaustName.Location = new System.Drawing.Point(14, 520);
            lblExhaustName.Margin = new Padding(4, 0, 4, 0);
            lblExhaustName.Name = "lblExhaustName";
            lblExhaustName.Size = new System.Drawing.Size(106, 20);
            lblExhaustName.TabIndex = 28;
            lblExhaustName.Text = "Exhaust Name:";
            // 
            // txtExhaustName
            // 
            txtExhaustName.Location = new System.Drawing.Point(150, 517);
            txtExhaustName.Margin = new Padding(4, 5, 4, 5);
            txtExhaustName.Name = "txtExhaustName";
            txtExhaustName.Size = new System.Drawing.Size(478, 27);
            txtExhaustName.TabIndex = 29;
            // 
            // lblExhaustID
            // 
            lblExhaustID.AutoSize = true;
            lblExhaustID.Location = new System.Drawing.Point(14, 490);
            lblExhaustID.Margin = new Padding(4, 0, 4, 0);
            lblExhaustID.Name = "lblExhaustID";
            lblExhaustID.Size = new System.Drawing.Size(81, 20);
            lblExhaustID.TabIndex = 30;
            lblExhaustID.Text = "Exhaust ID:";
            // 
            // txtExhaustID
            // 
            txtExhaustID.Location = new System.Drawing.Point(150, 487);
            txtExhaustID.Margin = new Padding(4, 5, 4, 5);
            txtExhaustID.Name = "txtExhaustID";
            txtExhaustID.Size = new System.Drawing.Size(478, 27);
            txtExhaustID.TabIndex = 31;
            // 
            // lblEngineID
            // 
            lblEngineID.AutoSize = true;
            lblEngineID.Location = new System.Drawing.Point(14, 430);
            lblEngineID.Margin = new Padding(4, 0, 4, 0);
            lblEngineID.Name = "lblEngineID";
            lblEngineID.Size = new System.Drawing.Size(76, 20);
            lblEngineID.TabIndex = 32;
            lblEngineID.Text = "Engine ID:";
            // 
            // txtEngineID
            // 
            txtEngineID.Location = new System.Drawing.Point(150, 427);
            txtEngineID.Margin = new Padding(4, 5, 4, 5);
            txtEngineID.Name = "txtEngineID";
            txtEngineID.Size = new System.Drawing.Size(478, 27);
            txtEngineID.TabIndex = 33;
            // 
            // lblEngineName
            // 
            lblEngineName.AutoSize = true;
            lblEngineName.Location = new System.Drawing.Point(14, 460);
            lblEngineName.Margin = new Padding(4, 0, 4, 0);
            lblEngineName.Name = "lblEngineName";
            lblEngineName.Size = new System.Drawing.Size(101, 20);
            lblEngineName.TabIndex = 34;
            lblEngineName.Text = "Engine Name:";
            // 
            // txtEngineName
            // 
            txtEngineName.Location = new System.Drawing.Point(150, 457);
            txtEngineName.Margin = new Padding(4, 5, 4, 5);
            txtEngineName.Name = "txtEngineName";
            txtEngineName.Size = new System.Drawing.Size(478, 27);
            txtEngineName.TabIndex = 35;
            // 
            // lblClassUnlock
            // 
            lblClassUnlock.AutoSize = true;
            lblClassUnlock.Location = new System.Drawing.Point(674, 130);
            lblClassUnlock.Margin = new Padding(4, 0, 4, 0);
            lblClassUnlock.Name = "lblClassUnlock";
            lblClassUnlock.Size = new System.Drawing.Size(94, 20);
            lblClassUnlock.TabIndex = 36;
            lblClassUnlock.Text = "Class Unlock:";
            // 
            // cboClassUnlock
            // 
            cboClassUnlock.DropDownStyle = ComboBoxStyle.DropDownList;
            cboClassUnlock.FormattingEnabled = true;
            cboClassUnlock.Items.AddRange(new object[] { "SuperClassUnlock", "MuscleClassUnlock", "F1ClassUnlock", "TunerClassUnlock", "HotRodClassUnlock", "RivalGen" });
            cboClassUnlock.Location = new System.Drawing.Point(776, 127);
            cboClassUnlock.Margin = new Padding(4, 5, 4, 5);
            cboClassUnlock.Name = "cboClassUnlock";
            cboClassUnlock.Size = new System.Drawing.Size(476, 28);
            cboClassUnlock.TabIndex = 37;
            // 
            // lblCarWon
            // 
            lblCarWon.AutoSize = true;
            lblCarWon.Location = new System.Drawing.Point(14, 610);
            lblCarWon.Margin = new Padding(4, 0, 4, 0);
            lblCarWon.Name = "lblCarWon";
            lblCarWon.Size = new System.Drawing.Size(68, 20);
            lblCarWon.TabIndex = 38;
            lblCarWon.Text = "Car Won:";
            // 
            // txtCarWon
            // 
            txtCarWon.Location = new System.Drawing.Point(150, 607);
            txtCarWon.Margin = new Padding(4, 5, 4, 5);
            txtCarWon.Name = "txtCarWon";
            txtCarWon.Size = new System.Drawing.Size(478, 27);
            txtCarWon.TabIndex = 39;
            // 
            // lblCarReleased
            // 
            lblCarReleased.AutoSize = true;
            lblCarReleased.Location = new System.Drawing.Point(14, 580);
            lblCarReleased.Margin = new Padding(4, 0, 4, 0);
            lblCarReleased.Name = "lblCarReleased";
            lblCarReleased.Size = new System.Drawing.Size(98, 20);
            lblCarReleased.TabIndex = 40;
            lblCarReleased.Text = "Car Released:";
            // 
            // txtCarReleased
            // 
            txtCarReleased.Location = new System.Drawing.Point(150, 577);
            txtCarReleased.Margin = new Padding(4, 5, 4, 5);
            txtCarReleased.Name = "txtCarReleased";
            txtCarReleased.Size = new System.Drawing.Size(478, 27);
            txtCarReleased.TabIndex = 41;
            // 
            // lblAIMusic
            // 
            lblAIMusic.AutoSize = true;
            lblAIMusic.Location = new System.Drawing.Point(674, 190);
            lblAIMusic.Margin = new Padding(4, 0, 4, 0);
            lblAIMusic.Name = "lblAIMusic";
            lblAIMusic.Size = new System.Drawing.Size(68, 20);
            lblAIMusic.TabIndex = 42;
            lblAIMusic.Text = "AI Music:";
            // 
            // cboAIMusic
            // 
            cboAIMusic.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAIMusic.FormattingEnabled = true;
            cboAIMusic.Items.AddRange(new object[] { "None", "Muscle", "Truck", "Tuner", "Sedan", "Exotic", "Super" });
            cboAIMusic.Location = new System.Drawing.Point(776, 187);
            cboAIMusic.Margin = new Padding(4, 5, 4, 5);
            cboAIMusic.Name = "cboAIMusic";
            cboAIMusic.Size = new System.Drawing.Size(476, 28);
            cboAIMusic.TabIndex = 43;
            // 
            // lblAIExhaust1
            // 
            lblAIExhaust1.AutoSize = true;
            lblAIExhaust1.Location = new System.Drawing.Point(674, 220);
            lblAIExhaust1.Margin = new Padding(4, 0, 4, 0);
            lblAIExhaust1.Name = "lblAIExhaust1";
            lblAIExhaust1.Size = new System.Drawing.Size(80, 20);
            lblAIExhaust1.TabIndex = 44;
            lblAIExhaust1.Text = "AI Exhaust:";
            // 
            // cboAIExhaust1
            // 
            cboAIExhaust1.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAIExhaust1.FormattingEnabled = true;
            cboAIExhaust1.Items.AddRange(new object[] { "None", "AIROD_EX", "AI_CIVIC_EX", "AI_GT_ENG", "AI_MUST_EX", "AI_F1_EX" });
            cboAIExhaust1.Location = new System.Drawing.Point(776, 217);
            cboAIExhaust1.Margin = new Padding(4, 5, 4, 5);
            cboAIExhaust1.Name = "cboAIExhaust1";
            cboAIExhaust1.Size = new System.Drawing.Size(476, 28);
            cboAIExhaust1.TabIndex = 45;
            // 
            // lblAIExhaust2
            // 
            lblAIExhaust2.AutoSize = true;
            lblAIExhaust2.Location = new System.Drawing.Point(674, 250);
            lblAIExhaust2.Margin = new Padding(4, 0, 4, 0);
            lblAIExhaust2.Name = "lblAIExhaust2";
            lblAIExhaust2.Size = new System.Drawing.Size(92, 20);
            lblAIExhaust2.TabIndex = 46;
            lblAIExhaust2.Text = "AI Exhaust 2:";
            // 
            // cboAIExhaust2
            // 
            cboAIExhaust2.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAIExhaust2.FormattingEnabled = true;
            cboAIExhaust2.Items.AddRange(new object[] { "None", "AIROD_EX", "AI_CIVIC_EX", "AI_GT_ENG", "AI_MUST_EX", "AI_F1_EX" });
            cboAIExhaust2.Location = new System.Drawing.Point(776, 247);
            cboAIExhaust2.Margin = new Padding(4, 5, 4, 5);
            cboAIExhaust2.Name = "cboAIExhaust2";
            cboAIExhaust2.Size = new System.Drawing.Size(476, 28);
            cboAIExhaust2.TabIndex = 47;
            // 
            // lblAIExhaust3
            // 
            lblAIExhaust3.AutoSize = true;
            lblAIExhaust3.Location = new System.Drawing.Point(674, 280);
            lblAIExhaust3.Margin = new Padding(4, 0, 4, 0);
            lblAIExhaust3.Name = "lblAIExhaust3";
            lblAIExhaust3.Size = new System.Drawing.Size(92, 20);
            lblAIExhaust3.TabIndex = 48;
            lblAIExhaust3.Text = "AI Exhaust 3:";
            // 
            // cboAIExhaust3
            // 
            cboAIExhaust3.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAIExhaust3.FormattingEnabled = true;
            cboAIExhaust3.Items.AddRange(new object[] { "None", "AIROD_EX", "AI_CIVIC_EX", "AI_GT_ENG", "AI_MUST_EX", "AI_F1_EX" });
            cboAIExhaust3.Location = new System.Drawing.Point(776, 277);
            cboAIExhaust3.Margin = new Padding(4, 5, 4, 5);
            cboAIExhaust3.Name = "cboAIExhaust3";
            cboAIExhaust3.Size = new System.Drawing.Size(476, 28);
            cboAIExhaust3.TabIndex = 49;
            // 
            // lblCategory
            // 
            lblCategory.AutoSize = true;
            lblCategory.Location = new System.Drawing.Point(674, 370);
            lblCategory.Margin = new Padding(4, 0, 4, 0);
            lblCategory.Name = "lblCategory";
            lblCategory.Size = new System.Drawing.Size(72, 20);
            lblCategory.TabIndex = 50;
            lblCategory.Text = "Category:";
            // 
            // chlCategory
            // 
            chlCategory.CheckOnClick = true;
            chlCategory.Items.AddRange(new object[] { "Paradise Cars", "Paradise Bikes", "Online Cars", "Toy Cars", "Legendary Cars", "Boost Specials", "Cop Cars", "Island Cars" });
            chlCategory.Location = new System.Drawing.Point(776, 307);
            chlCategory.Margin = new Padding(4, 5, 4, 5);
            chlCategory.Name = "chlCategory";
            chlCategory.Size = new System.Drawing.Size(476, 158);
            chlCategory.TabIndex = 51;
            // 
            // lblVehicleType
            // 
            lblVehicleType.AutoSize = true;
            lblVehicleType.Location = new System.Drawing.Point(674, 10);
            lblVehicleType.Margin = new Padding(4, 0, 4, 0);
            lblVehicleType.Name = "lblVehicleType";
            lblVehicleType.Size = new System.Drawing.Size(94, 20);
            lblVehicleType.TabIndex = 52;
            lblVehicleType.Text = "Vehicle Type:";
            // 
            // cboVehicleType
            // 
            cboVehicleType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboVehicleType.FormattingEnabled = true;
            cboVehicleType.Items.AddRange(new object[] { "Car", "Bike", "Plane" });
            cboVehicleType.Location = new System.Drawing.Point(776, 7);
            cboVehicleType.Margin = new Padding(4, 5, 4, 5);
            cboVehicleType.Name = "cboVehicleType";
            cboVehicleType.Size = new System.Drawing.Size(476, 28);
            cboVehicleType.TabIndex = 53;
            // 
            // lblBoostType
            // 
            lblBoostType.AutoSize = true;
            lblBoostType.Location = new System.Drawing.Point(674, 40);
            lblBoostType.Margin = new Padding(4, 0, 4, 0);
            lblBoostType.Name = "lblBoostType";
            lblBoostType.Size = new System.Drawing.Size(85, 20);
            lblBoostType.TabIndex = 54;
            lblBoostType.Text = "Boost Type:";
            // 
            // cboBoostType
            // 
            cboBoostType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboBoostType.FormattingEnabled = true;
            cboBoostType.Items.AddRange(new object[] { "Speed", "Aggression", "Stunt", "None", "Locked" });
            cboBoostType.Location = new System.Drawing.Point(776, 37);
            cboBoostType.Margin = new Padding(4, 5, 4, 5);
            cboBoostType.Name = "cboBoostType";
            cboBoostType.Size = new System.Drawing.Size(476, 28);
            cboBoostType.TabIndex = 55;
            // 
            // lblFinishType
            // 
            lblFinishType.AutoSize = true;
            lblFinishType.Location = new System.Drawing.Point(674, 70);
            lblFinishType.Margin = new Padding(4, 0, 4, 0);
            lblFinishType.Name = "lblFinishType";
            lblFinishType.Size = new System.Drawing.Size(84, 20);
            lblFinishType.TabIndex = 56;
            lblFinishType.Text = "Finish Type:";
            // 
            // cboFinishType
            // 
            cboFinishType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboFinishType.FormattingEnabled = true;
            cboFinishType.Items.AddRange(new object[] { "Default", "Colour", "Pattern", "Platinum", "Gold", "Community" });
            cboFinishType.Location = new System.Drawing.Point(776, 67);
            cboFinishType.Margin = new Padding(4, 5, 4, 5);
            cboFinishType.Name = "cboFinishType";
            cboFinishType.Size = new System.Drawing.Size(476, 28);
            cboFinishType.TabIndex = 57;
            // 
            // lblMaxSpeed
            // 
            lblMaxSpeed.AutoSize = true;
            lblMaxSpeed.Location = new System.Drawing.Point(14, 190);
            lblMaxSpeed.Margin = new Padding(4, 0, 4, 0);
            lblMaxSpeed.Name = "lblMaxSpeed";
            lblMaxSpeed.Size = new System.Drawing.Size(86, 20);
            lblMaxSpeed.TabIndex = 58;
            lblMaxSpeed.Text = "Max Speed:";
            // 
            // txtMaxSpeed
            // 
            txtMaxSpeed.Location = new System.Drawing.Point(150, 187);
            txtMaxSpeed.Margin = new Padding(4, 5, 4, 5);
            txtMaxSpeed.Name = "txtMaxSpeed";
            txtMaxSpeed.Size = new System.Drawing.Size(478, 27);
            txtMaxSpeed.TabIndex = 59;
            // 
            // lblMaxBoostSpeed
            // 
            lblMaxBoostSpeed.AutoSize = true;
            lblMaxBoostSpeed.Location = new System.Drawing.Point(14, 250);
            lblMaxBoostSpeed.Margin = new Padding(4, 0, 4, 0);
            lblMaxBoostSpeed.Name = "lblMaxBoostSpeed";
            lblMaxBoostSpeed.Size = new System.Drawing.Size(128, 20);
            lblMaxBoostSpeed.TabIndex = 60;
            lblMaxBoostSpeed.Text = "Max Boost Speed:";
            // 
            // txtMaxBoostSpeed
            // 
            txtMaxBoostSpeed.Location = new System.Drawing.Point(150, 247);
            txtMaxBoostSpeed.Margin = new Padding(4, 5, 4, 5);
            txtMaxBoostSpeed.Name = "txtMaxBoostSpeed";
            txtMaxBoostSpeed.Size = new System.Drawing.Size(478, 27);
            txtMaxBoostSpeed.TabIndex = 61;
            // 
            // lblSpeedStat
            // 
            lblSpeedStat.AutoSize = true;
            lblSpeedStat.Location = new System.Drawing.Point(14, 160);
            lblSpeedStat.Margin = new Padding(4, 0, 4, 0);
            lblSpeedStat.Name = "lblSpeedStat";
            lblSpeedStat.Size = new System.Drawing.Size(84, 20);
            lblSpeedStat.TabIndex = 62;
            lblSpeedStat.Text = "Speed Stat:";
            // 
            // txtSpeedStat
            // 
            txtSpeedStat.Location = new System.Drawing.Point(150, 157);
            txtSpeedStat.Margin = new Padding(4, 5, 4, 5);
            txtSpeedStat.Name = "txtSpeedStat";
            txtSpeedStat.Size = new System.Drawing.Size(478, 27);
            txtSpeedStat.TabIndex = 63;
            // 
            // lblBoostStat
            // 
            lblBoostStat.AutoSize = true;
            lblBoostStat.Location = new System.Drawing.Point(14, 220);
            lblBoostStat.Margin = new Padding(4, 0, 4, 0);
            lblBoostStat.Name = "lblBoostStat";
            lblBoostStat.Size = new System.Drawing.Size(80, 20);
            lblBoostStat.TabIndex = 64;
            lblBoostStat.Text = "Boost Stat:";
            // 
            // txtBoostStat
            // 
            txtBoostStat.Location = new System.Drawing.Point(150, 217);
            txtBoostStat.Margin = new Padding(4, 5, 4, 5);
            txtBoostStat.Name = "txtBoostStat";
            txtBoostStat.Size = new System.Drawing.Size(478, 27);
            txtBoostStat.TabIndex = 65;
            // 
            // lblColor
            // 
            lblColor.AutoSize = true;
            lblColor.Location = new System.Drawing.Point(14, 640);
            lblColor.Margin = new Padding(4, 0, 4, 0);
            lblColor.Name = "lblColor";
            lblColor.Size = new System.Drawing.Size(48, 20);
            lblColor.TabIndex = 66;
            lblColor.Text = "Color:";
            // 
            // txtColor
            // 
            txtColor.Location = new System.Drawing.Point(150, 637);
            txtColor.Margin = new Padding(4, 5, 4, 5);
            txtColor.Name = "txtColor";
            txtColor.Size = new System.Drawing.Size(478, 27);
            txtColor.TabIndex = 67;
            // 
            // lblColorType
            // 
            lblColorType.AutoSize = true;
            lblColorType.Location = new System.Drawing.Point(674, 100);
            lblColorType.Margin = new Padding(4, 0, 4, 0);
            lblColorType.Name = "lblColorType";
            lblColorType.Size = new System.Drawing.Size(83, 20);
            lblColorType.TabIndex = 68;
            lblColorType.Text = "Color Type:";
            // 
            // cboColorType
            // 
            cboColorType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboColorType.FormattingEnabled = true;
            cboColorType.Items.AddRange(new object[] { "Gloss", "Metallic", "Pearlescent", "Special", "Party" });
            cboColorType.Location = new System.Drawing.Point(776, 97);
            cboColorType.Margin = new Padding(4, 5, 4, 5);
            cboColorType.Name = "cboColorType";
            cboColorType.Size = new System.Drawing.Size(476, 28);
            cboColorType.TabIndex = 69;
            // 
            // VehicleEditor
            // 
            AcceptButton = btnOk;
            AccessibleName = "";
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            CancelButton = btnCancel;
            ClientSize = new System.Drawing.Size(1264, 729);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            Controls.Add(lblIndex);
            Controls.Add(txtIndex);
            Controls.Add(lblID);
            Controls.Add(txtID);
            Controls.Add(lblParentID);
            Controls.Add(txtParentID);
            Controls.Add(lblWheels);
            Controls.Add(txtWheels);
            Controls.Add(lblName);
            Controls.Add(txtName);
            Controls.Add(lblBrand);
            Controls.Add(txtBrand);
            Controls.Add(lblDamageLimit);
            Controls.Add(txtDamageLimit);
            Controls.Add(lblFlags);
            Controls.Add(chlFlags);
            Controls.Add(lblBoostLength);
            Controls.Add(txtBoostLength);
            Controls.Add(lblRank);
            Controls.Add(cboRank);
            Controls.Add(lblBoostCapacity);
            Controls.Add(txtBoostCapacity);
            Controls.Add(lblStrengthStat);
            Controls.Add(txtStrengthStat);
            Controls.Add(lblAttribSysCollectionKey);
            Controls.Add(txtAttribSysCollectionKey);
            Controls.Add(lblExhaustName);
            Controls.Add(txtExhaustName);
            Controls.Add(lblExhaustID);
            Controls.Add(txtExhaustID);
            Controls.Add(lblEngineID);
            Controls.Add(txtEngineID);
            Controls.Add(lblEngineName);
            Controls.Add(txtEngineName);
            Controls.Add(lblClassUnlock);
            Controls.Add(cboClassUnlock);
            Controls.Add(lblCarWon);
            Controls.Add(txtCarWon);
            Controls.Add(lblCarReleased);
            Controls.Add(txtCarReleased);
            Controls.Add(lblAIMusic);
            Controls.Add(cboAIMusic);
            Controls.Add(lblAIExhaust1);
            Controls.Add(cboAIExhaust1);
            Controls.Add(lblAIExhaust2);
            Controls.Add(cboAIExhaust2);
            Controls.Add(lblAIExhaust3);
            Controls.Add(cboAIExhaust3);
            Controls.Add(lblCategory);
            Controls.Add(chlCategory);
            Controls.Add(lblVehicleType);
            Controls.Add(cboVehicleType);
            Controls.Add(lblBoostType);
            Controls.Add(cboBoostType);
            Controls.Add(lblFinishType);
            Controls.Add(cboFinishType);
            Controls.Add(lblMaxSpeed);
            Controls.Add(txtMaxSpeed);
            Controls.Add(lblMaxBoostSpeed);
            Controls.Add(txtMaxBoostSpeed);
            Controls.Add(lblSpeedStat);
            Controls.Add(txtSpeedStat);
            Controls.Add(lblBoostStat);
            Controls.Add(txtBoostStat);
            Controls.Add(lblColor);
            Controls.Add(txtColor);
            Controls.Add(lblColorType);
            Controls.Add(cboColorType);
            DoubleBuffered = true;
            Margin = new Padding(4, 5, 4, 5);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "VehicleEditor";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Vehicle Editor";
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblIndex;
        private System.Windows.Forms.TextBox txtIndex;
        private System.Windows.Forms.Label lblID;
        private System.Windows.Forms.TextBox txtID;
        private System.Windows.Forms.Label lblParentID;
        private System.Windows.Forms.TextBox txtParentID;
        private System.Windows.Forms.Label lblWheels;
        private System.Windows.Forms.TextBox txtWheels;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblBrand;
        private System.Windows.Forms.TextBox txtBrand;
        private System.Windows.Forms.Label lblDamageLimit;
        private System.Windows.Forms.TextBox txtDamageLimit;
        private System.Windows.Forms.Label lblFlags;
        public  System.Windows.Forms.CheckedListBox chlFlags;
        private System.Windows.Forms.Label lblBoostLength;
        private System.Windows.Forms.TextBox txtBoostLength;
        private System.Windows.Forms.Label lblRank;
        private System.Windows.Forms.ComboBox cboRank;
        private System.Windows.Forms.Label lblBoostCapacity;
        private System.Windows.Forms.TextBox txtBoostCapacity;
        private System.Windows.Forms.Label lblStrengthStat;
        private System.Windows.Forms.TextBox txtStrengthStat;
        private System.Windows.Forms.Label lblAttribSysCollectionKey;
        private System.Windows.Forms.TextBox txtAttribSysCollectionKey;
        private System.Windows.Forms.Label lblExhaustName;
        private System.Windows.Forms.TextBox txtExhaustName;
        private System.Windows.Forms.Label lblExhaustID;
        private System.Windows.Forms.TextBox txtExhaustID;
        private System.Windows.Forms.Label lblEngineID;
        private System.Windows.Forms.TextBox txtEngineID;
        private System.Windows.Forms.Label lblEngineName;
        private System.Windows.Forms.TextBox txtEngineName;
        private System.Windows.Forms.Label lblClassUnlock;
        private System.Windows.Forms.ComboBox cboClassUnlock;
        private System.Windows.Forms.Label lblCarWon;
        private System.Windows.Forms.TextBox txtCarWon;
        private System.Windows.Forms.Label lblCarReleased;
        private System.Windows.Forms.TextBox txtCarReleased;
        private System.Windows.Forms.Label lblAIMusic;
        private System.Windows.Forms.ComboBox cboAIMusic;
        private System.Windows.Forms.Label lblAIExhaust1;
        private System.Windows.Forms.ComboBox cboAIExhaust1;
        private System.Windows.Forms.Label lblAIExhaust2;
        private System.Windows.Forms.ComboBox cboAIExhaust2;
        private System.Windows.Forms.Label lblAIExhaust3;
        private System.Windows.Forms.ComboBox cboAIExhaust3;
        private System.Windows.Forms.Label lblCategory;
        private System.Windows.Forms.CheckedListBox chlCategory;
        private System.Windows.Forms.Label lblVehicleType;
        private System.Windows.Forms.ComboBox cboVehicleType;
        private System.Windows.Forms.Label lblBoostType;
        private System.Windows.Forms.ComboBox cboBoostType;
        private System.Windows.Forms.Label lblFinishType;
        private System.Windows.Forms.ComboBox cboFinishType;
        private System.Windows.Forms.Label lblMaxSpeed;
        private System.Windows.Forms.TextBox txtMaxSpeed;
        private System.Windows.Forms.Label lblMaxBoostSpeed;
        private System.Windows.Forms.TextBox txtMaxBoostSpeed;
        private System.Windows.Forms.Label lblSpeedStat;
        private System.Windows.Forms.TextBox txtSpeedStat;
        private System.Windows.Forms.Label lblBoostStat;
        private System.Windows.Forms.TextBox txtBoostStat;
        private System.Windows.Forms.Label lblColor;
        private System.Windows.Forms.TextBox txtColor;
        private System.Windows.Forms.Label lblColorType;
        private System.Windows.Forms.ComboBox cboColorType;
    }
}
