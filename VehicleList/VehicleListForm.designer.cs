namespace VehicleList
{
    
    partial class VehicleListForm
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VehicleListForm));
            mnuMain = new System.Windows.Forms.MenuStrip();
            itemsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            addItemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            copyItemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            deleteItemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            lstVehicles = new System.Windows.Forms.ListView();
            colIndex = new System.Windows.Forms.ColumnHeader();
            colVehicleID = new System.Windows.Forms.ColumnHeader();
            colParentID = new System.Windows.Forms.ColumnHeader();
            colWheel = new System.Windows.Forms.ColumnHeader();
            colVehicleName = new System.Windows.Forms.ColumnHeader();
            colMfrName = new System.Windows.Forms.ColumnHeader();
            colDamageLimit = new System.Windows.Forms.ColumnHeader();
            colFlags = new System.Windows.Forms.ColumnHeader();
            colBoostLength = new System.Windows.Forms.ColumnHeader();
            colVehicleRank = new System.Windows.Forms.ColumnHeader();
            colBoostCapacity = new System.Windows.Forms.ColumnHeader();
            colStrengthStat = new System.Windows.Forms.ColumnHeader();
            colAttribKey = new System.Windows.Forms.ColumnHeader();
            colExhaustName = new System.Windows.Forms.ColumnHeader();
            colExhaustID = new System.Windows.Forms.ColumnHeader();
            colEngineID = new System.Windows.Forms.ColumnHeader();
            colEngineName = new System.Windows.Forms.ColumnHeader();
            colClassUnlockStr = new System.Windows.Forms.ColumnHeader();
            colCarWonStrID = new System.Windows.Forms.ColumnHeader();
            colCarReleasedStrID = new System.Windows.Forms.ColumnHeader();
            colAIMusicHash = new System.Windows.Forms.ColumnHeader();
            colAIExStr1 = new System.Windows.Forms.ColumnHeader();
            colAIExStr2 = new System.Windows.Forms.ColumnHeader();
            colAIExStr3 = new System.Windows.Forms.ColumnHeader();
            colCategory = new System.Windows.Forms.ColumnHeader();
            colVehicleType = new System.Windows.Forms.ColumnHeader();
            colBoostType = new System.Windows.Forms.ColumnHeader();
            colFinishType = new System.Windows.Forms.ColumnHeader();
            colTopSpeed = new System.Windows.Forms.ColumnHeader();
            colTopSpeedBoost = new System.Windows.Forms.ColumnHeader();
            colTopSpeedStat = new System.Windows.Forms.ColumnHeader();
            colTopSpeedBoostStat = new System.Windows.Forms.ColumnHeader();
            colColor = new System.Windows.Forms.ColumnHeader();
            colColorPalette = new System.Windows.Forms.ColumnHeader();
            stsMain = new System.Windows.Forms.StatusStrip();
            stlStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsbAddItem = new System.Windows.Forms.ToolStripButton();
            tsbCopyItem = new System.Windows.Forms.ToolStripButton();
            tsbDeleteItem = new System.Windows.Forms.ToolStripButton();
            tstbSearchItem = new System.Windows.Forms.ToolStripTextBox();
            tsbSearchItem = new System.Windows.Forms.ToolStripButton();
            timer1 = new System.Windows.Forms.Timer(components);
            tsbClearSearch = new System.Windows.Forms.ToolStripButton();
            mnuMain.SuspendLayout();
            stsMain.SuspendLayout();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // mnuMain
            // 
            mnuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { itemsToolStripMenuItem });
            mnuMain.Location = new System.Drawing.Point(0, 0);
            mnuMain.Name = "mnuMain";
            mnuMain.Padding = new System.Windows.Forms.Padding(8, 3, 0, 3);
            mnuMain.Size = new System.Drawing.Size(1439, 30);
            mnuMain.TabIndex = 0;
            mnuMain.Text = "menuStrip1";
            // 
            // itemsToolStripMenuItem
            // 
            itemsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { addItemToolStripMenuItem, copyItemToolStripMenuItem, deleteItemToolStripMenuItem });
            itemsToolStripMenuItem.Name = "itemsToolStripMenuItem";
            itemsToolStripMenuItem.Size = new System.Drawing.Size(57, 24);
            itemsToolStripMenuItem.Text = "Items";
            // 
            // addItemToolStripMenuItem
            // 
            addItemToolStripMenuItem.Image = Properties.Resources.AddTableHS;
            addItemToolStripMenuItem.Name = "addItemToolStripMenuItem";
            addItemToolStripMenuItem.Size = new System.Drawing.Size(156, 24);
            addItemToolStripMenuItem.Text = "Add Item";
            addItemToolStripMenuItem.Click += addItemToolStripMenuItem_Click;
            // 
            // copyItemToolStripMenuItem
            // 
            copyItemToolStripMenuItem.Enabled = false;
            copyItemToolStripMenuItem.Image = Properties.Resources.CopyHS;
            copyItemToolStripMenuItem.Name = "copyItemToolStripMenuItem";
            copyItemToolStripMenuItem.Size = new System.Drawing.Size(156, 24);
            copyItemToolStripMenuItem.Text = "Copy Item";
            copyItemToolStripMenuItem.Click += copyItemToolStripMenuItem_Click;
            // 
            // deleteItemToolStripMenuItem
            // 
            deleteItemToolStripMenuItem.Enabled = false;
            deleteItemToolStripMenuItem.Image = Properties.Resources.remove_xform;
            deleteItemToolStripMenuItem.Name = "deleteItemToolStripMenuItem";
            deleteItemToolStripMenuItem.Size = new System.Drawing.Size(156, 24);
            deleteItemToolStripMenuItem.Text = "Delete Item";
            deleteItemToolStripMenuItem.Click += deleteItemToolStripMenuItem_Click;
            // 
            // lstVehicles
            // 
            lstVehicles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { colIndex, colVehicleID, colParentID, colWheel, colVehicleName, colMfrName, colDamageLimit, colFlags, colBoostLength, colVehicleRank, colBoostCapacity, colStrengthStat, colAttribKey, colExhaustName, colExhaustID, colEngineID, colEngineName, colClassUnlockStr, colCarWonStrID, colCarReleasedStrID, colAIMusicHash, colAIExStr1, colAIExStr2, colAIExStr3, colCategory, colVehicleType, colBoostType, colFinishType, colTopSpeed, colTopSpeedBoost, colTopSpeedStat, colTopSpeedBoostStat, colColor, colColorPalette });
            lstVehicles.Dock = System.Windows.Forms.DockStyle.Fill;
            lstVehicles.FullRowSelect = true;
            lstVehicles.GridLines = true;
            lstVehicles.Location = new System.Drawing.Point(0, 57);
            lstVehicles.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            lstVehicles.Name = "lstVehicles";
            lstVehicles.Size = new System.Drawing.Size(1439, 670);
            lstVehicles.TabIndex = 1;
            lstVehicles.UseCompatibleStateImageBehavior = false;
            lstVehicles.View = System.Windows.Forms.View.Details;
            lstVehicles.ColumnClick += lstVehicles_ColumnClick;
            lstVehicles.SelectedIndexChanged += lstVehicles_SelectedIndexChanged;
            lstVehicles.MouseDoubleClick += lstVehicles_MouseDoubleClick;
            // 
            // colIndex
            // 
            colIndex.Text = "Index";
            // 
            // colVehicleID
            // 
            colVehicleID.Text = "ID";
            colVehicleID.Width = 80;
            // 
            // colParentID
            // 
            colParentID.Text = "Parent ID";
            // 
            // colWheel
            // 
            colWheel.Text = "Wheel";
            // 
            // colVehicleName
            // 
            colVehicleName.Text = "Vehicle";
            colVehicleName.Width = 180;
            // 
            // colMfrName
            // 
            colMfrName.Text = "Manufacturer";
            colMfrName.Width = 100;
            // 
            // colDamageLimit
            // 
            colDamageLimit.Text = "Damage Limit";
            // 
            // colFlags
            // 
            colFlags.Text = "Flags";
            colFlags.Width = 80;
            // 
            // colBoostLength
            // 
            colBoostLength.Text = "Boost Length";
            // 
            // colVehicleRank
            // 
            colVehicleRank.Text = "Rank";
            // 
            // colBoostCapacity
            // 
            colBoostCapacity.Text = "Boost Capacity";
            // 
            // colStrengthStat
            // 
            colStrengthStat.Text = "Strength Stat";
            // 
            // colAttribKey
            // 
            colAttribKey.Text = "AttribSys ID";
            // 
            // colExhaustName
            // 
            colExhaustName.Text = "Exhaust Name";
            colExhaustName.Width = 120;
            // 
            // colExhaustID
            // 
            colExhaustID.Text = "Exhaust ID";
            colExhaustID.Width = 80;
            // 
            // colEngineID
            // 
            colEngineID.Text = "Engine ID";
            colEngineID.Width = 80;
            // 
            // colEngineName
            // 
            colEngineName.Text = "Engine Name";
            colEngineName.Width = 120;
            // 
            // colClassUnlockStr
            // 
            colClassUnlockStr.Text = "Class Unlock Stream ID";
            // 
            // colCarWonStrID
            // 
            colCarWonStrID.Text = "Car Won Stream ID";
            // 
            // colCarReleasedStrID
            // 
            colCarReleasedStrID.Text = "Car Released Stream ID";
            // 
            // colAIMusicHash
            // 
            colAIMusicHash.Text = "AI Music";
            // 
            // colAIExStr1
            // 
            colAIExStr1.Text = "AI Exhaust 1";
            // 
            // colAIExStr2
            // 
            colAIExStr2.Text = "AI Exhaust 2";
            // 
            // colAIExStr3
            // 
            colAIExStr3.Text = "AI Exhaust 3";
            // 
            // colCategory
            // 
            colCategory.Text = "Category";
            colCategory.Width = 80;
            // 
            // colVehicleType
            // 
            colVehicleType.Text = "Vehicle Type";
            // 
            // colBoostType
            // 
            colBoostType.Text = "Boost Type";
            colBoostType.Width = 90;
            // 
            // colFinishType
            // 
            colFinishType.Text = "Finish Type";
            colFinishType.Width = 80;
            // 
            // colTopSpeed
            // 
            colTopSpeed.Text = "Top Speed";
            // 
            // colTopSpeedBoost
            // 
            colTopSpeedBoost.Text = "Top Boost Speed";
            // 
            // colTopSpeedStat
            // 
            colTopSpeedStat.Text = "Speed Stat";
            // 
            // colTopSpeedBoostStat
            // 
            colTopSpeedBoostStat.Text = "Boost Stat";
            // 
            // colColor
            // 
            colColor.Text = "Default Color";
            // 
            // colColorPalette
            // 
            colColorPalette.Text = "Default Color Type";
            // 
            // stsMain
            // 
            stsMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { stlStatusLabel });
            stsMain.Location = new System.Drawing.Point(0, 727);
            stsMain.Name = "stsMain";
            stsMain.Padding = new System.Windows.Forms.Padding(1, 0, 18, 0);
            stsMain.Size = new System.Drawing.Size(1439, 22);
            stsMain.TabIndex = 2;
            stsMain.Text = "statusStrip1";
            // 
            // stlStatusLabel
            // 
            stlStatusLabel.Name = "stlStatusLabel";
            stlStatusLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // toolStrip1
            // 
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsbAddItem, tsbCopyItem, tsbDeleteItem, tstbSearchItem, tsbSearchItem, tsbClearSearch });
            toolStrip1.Location = new System.Drawing.Point(0, 30);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(1439, 27);
            toolStrip1.TabIndex = 3;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsbAddItem
            // 
            tsbAddItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsbAddItem.Image = Properties.Resources.AddTableHS;
            tsbAddItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsbAddItem.Name = "tsbAddItem";
            tsbAddItem.Size = new System.Drawing.Size(23, 24);
            tsbAddItem.Text = "Add Item";
            tsbAddItem.Click += tsbAddItem_Click;
            // 
            // tsbCopyItem
            // 
            tsbCopyItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsbCopyItem.Enabled = false;
            tsbCopyItem.Image = Properties.Resources.CopyHS;
            tsbCopyItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsbCopyItem.Name = "tsbCopyItem";
            tsbCopyItem.Size = new System.Drawing.Size(23, 24);
            tsbCopyItem.Text = "Copy Item";
            tsbCopyItem.Click += tsbCopyItem_Click;
            // 
            // tsbDeleteItem
            // 
            tsbDeleteItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsbDeleteItem.Enabled = false;
            tsbDeleteItem.Image = Properties.Resources.remove_xform;
            tsbDeleteItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsbDeleteItem.Name = "tsbDeleteItem";
            tsbDeleteItem.Size = new System.Drawing.Size(23, 24);
            tsbDeleteItem.Text = "Delete Item";
            tsbDeleteItem.Click += tsbDeleteItem_Click;
            // 
            // tstbSearchItem
            // 
            tstbSearchItem.Name = "tstbSearchItem";
            tstbSearchItem.Size = new System.Drawing.Size(350, 27);
            tstbSearchItem.Enter += tstbSearchItem_Enter;
            tstbSearchItem.Leave += tstbSearchItem_Leave;
            tstbSearchItem.KeyPress += tstbSearchItem_KeyPress;
            tstbSearchItem.TextChanged += tstbSearchItem_TextChanged;
            // 
            // tsbSearchItem
            // 
            tsbSearchItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsbSearchItem.Image = (System.Drawing.Image)resources.GetObject("tsbSearchItem.Image");
            tsbSearchItem.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsbSearchItem.Name = "tsbSearchItem";
            tsbSearchItem.Size = new System.Drawing.Size(23, 24);
            tsbSearchItem.Text = "toolStripButton1";
            tsbSearchItem.Click += tsbSearchItem_Click;
            // 
            // tsbClearSearch
            // 
            tsbClearSearch.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsbClearSearch.Image = (System.Drawing.Image)resources.GetObject("tsbClearSearch.Image");
            tsbClearSearch.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsbClearSearch.Name = "tsbClearSearch";
            tsbClearSearch.Size = new System.Drawing.Size(23, 24);
            tsbClearSearch.Text = "toolStripButton1";
            tsbClearSearch.Click += tsbClearSearch_Click;
            // 
            // VehicleListForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1439, 749);
            Controls.Add(lstVehicles);
            Controls.Add(toolStrip1);
            Controls.Add(stsMain);
            Controls.Add(mnuMain);
            MainMenuStrip = mnuMain;
            Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            Name = "VehicleListForm";
            Text = "Vehicle List Viewer";
            mnuMain.ResumeLayout(false);
            mnuMain.PerformLayout();
            stsMain.ResumeLayout(false);
            stsMain.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.MenuStrip mnuMain;
        private System.Windows.Forms.ListView lstVehicles;
        private System.Windows.Forms.ColumnHeader colIndex;
        private System.Windows.Forms.ColumnHeader colVehicleName;
        private System.Windows.Forms.ColumnHeader colMfrName;
        private System.Windows.Forms.ColumnHeader colCategory;
        private System.Windows.Forms.ColumnHeader colFlags;
        private System.Windows.Forms.ColumnHeader colExhaustID;
        private System.Windows.Forms.ColumnHeader colEngineID;
        private System.Windows.Forms.StatusStrip stsMain;
        private System.Windows.Forms.ToolStripStatusLabel stlStatusLabel;
        private System.Windows.Forms.ColumnHeader colVehicleID;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsbAddItem;
        private System.Windows.Forms.ToolStripMenuItem itemsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addItemToolStripMenuItem;
        private System.Windows.Forms.ColumnHeader colExhaustName;
        private System.Windows.Forms.ColumnHeader colEngineName;
        private System.Windows.Forms.ColumnHeader colBoostType;
        private System.Windows.Forms.ColumnHeader colFinishType;
        private System.Windows.Forms.ColumnHeader colColor;
        private System.Windows.Forms.ColumnHeader colTopSpeedStat;
        private System.Windows.Forms.ColumnHeader colTopSpeedBoostStat;
        private System.Windows.Forms.ColumnHeader colStrengthStat;
        private System.Windows.Forms.ColumnHeader colWheel;
        private System.Windows.Forms.ColumnHeader colParentID;
        private System.Windows.Forms.ColumnHeader colBoostLength;
        private System.Windows.Forms.ColumnHeader colVehicleRank;
        private System.Windows.Forms.ColumnHeader colBoostCapacity;
        private System.Windows.Forms.ColumnHeader colAttribKey;
        private System.Windows.Forms.ColumnHeader colClassUnlockStr;
        private System.Windows.Forms.ColumnHeader colCarWonStrID;
        private System.Windows.Forms.ColumnHeader colCarReleasedStrID;
        private System.Windows.Forms.ColumnHeader colAIMusicHash;
        private System.Windows.Forms.ColumnHeader colAIExStr1;
        private System.Windows.Forms.ColumnHeader colAIExStr2;
        private System.Windows.Forms.ColumnHeader colAIExStr3;
        private System.Windows.Forms.ColumnHeader colVehicleType;
        private System.Windows.Forms.ColumnHeader colTopSpeed;
        private System.Windows.Forms.ColumnHeader colTopSpeedBoost;
        private System.Windows.Forms.ColumnHeader colDamageLimit;
        private System.Windows.Forms.ColumnHeader colColorPalette;
        private System.Windows.Forms.ToolStripButton tsbCopyItem;
        private System.Windows.Forms.ToolStripMenuItem copyItemToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteItemToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsbDeleteItem;
        private System.Windows.Forms.ToolStripTextBox tstbSearchItem;
        private System.Windows.Forms.ToolStripButton tsbSearchItem;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.ToolStripButton tsbClearSearch;
    }
}

