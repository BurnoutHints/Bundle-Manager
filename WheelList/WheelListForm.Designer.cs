using System.Drawing;
using System.Windows.Forms;

namespace WheelList
{
    partial class WheelListForm
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
            menuStrip1 = new MenuStrip();
            itemsToolStripMenuItem = new ToolStripMenuItem();
            addItemToolStripMenuItem = new ToolStripMenuItem();
            copyItemToolStripMenuItem = new ToolStripMenuItem();
            deleteItemToolStripMenuItem = new ToolStripMenuItem();
            toolStrip1 = new ToolStrip();
            tsbAddItem = new ToolStripButton();
            tsbCopyItem = new ToolStripButton();
            tsbDeleteItem = new ToolStripButton();
            lstWheels = new ListView();
            Index = new ColumnHeader();
            ID = new ColumnHeader();
            WheelName = new ColumnHeader();
            stsMain = new StatusStrip();
            stlStatusLabel = new ToolStripStatusLabel();
            menuStrip1.SuspendLayout();
            toolStrip1.SuspendLayout();
            stsMain.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { itemsToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(261, 24);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // itemsToolStripMenuItem
            // 
            itemsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { addItemToolStripMenuItem, copyItemToolStripMenuItem, deleteItemToolStripMenuItem });
            itemsToolStripMenuItem.Name = "itemsToolStripMenuItem";
            itemsToolStripMenuItem.Size = new Size(48, 20);
            itemsToolStripMenuItem.Text = "Items";
            // 
            // addItemToolStripMenuItem
            // 
            addItemToolStripMenuItem.Image = Properties.Resources.AddTableHS;
            addItemToolStripMenuItem.Name = "addItemToolStripMenuItem";
            addItemToolStripMenuItem.Size = new Size(134, 22);
            addItemToolStripMenuItem.Text = "Add Item";
            addItemToolStripMenuItem.Click += addItemToolStripMenuItem_Click;
            // 
            // copyItemToolStripMenuItem
            // 
            copyItemToolStripMenuItem.Enabled = false;
            copyItemToolStripMenuItem.Image = Properties.Resources.CopyHS;
            copyItemToolStripMenuItem.Name = "copyItemToolStripMenuItem";
            copyItemToolStripMenuItem.Size = new Size(134, 22);
            copyItemToolStripMenuItem.Text = "Copy Item";
            copyItemToolStripMenuItem.Click += copyItemToolStripMenuItem_Click;
            // 
            // deleteItemToolStripMenuItem
            // 
            deleteItemToolStripMenuItem.Enabled = false;
            deleteItemToolStripMenuItem.Image = Properties.Resources.remove_xform;
            deleteItemToolStripMenuItem.Name = "deleteItemToolStripMenuItem";
            deleteItemToolStripMenuItem.Size = new Size(134, 22);
            deleteItemToolStripMenuItem.Text = "Delete Item";
            deleteItemToolStripMenuItem.Click += deleteItemToolStripMenuItem_Click;
            // 
            // toolStrip1
            // 
            toolStrip1.Items.AddRange(new ToolStripItem[] { tsbAddItem, tsbCopyItem, tsbDeleteItem });
            toolStrip1.Location = new Point(0, 24);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(261, 25);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsbAddItem
            // 
            tsbAddItem.DisplayStyle = ToolStripItemDisplayStyle.Image;
            tsbAddItem.Image = Properties.Resources.AddTableHS;
            tsbAddItem.ImageTransparentColor = Color.Magenta;
            tsbAddItem.Name = "tsbAddItem";
            tsbAddItem.Size = new Size(23, 22);
            tsbAddItem.Text = "toolStripButton1";
            tsbAddItem.ToolTipText = "Add Item";
            tsbAddItem.Click += tsbAddItem_Click;
            // 
            // tsbCopyItem
            // 
            tsbCopyItem.DisplayStyle = ToolStripItemDisplayStyle.Image;
            tsbCopyItem.Enabled = false;
            tsbCopyItem.Image = Properties.Resources.CopyHS;
            tsbCopyItem.ImageTransparentColor = Color.Magenta;
            tsbCopyItem.Name = "tsbCopyItem";
            tsbCopyItem.Size = new Size(23, 22);
            tsbCopyItem.Text = "toolStripButton2";
            tsbCopyItem.ToolTipText = "Copy Item";
            tsbCopyItem.Click += tsbCopyItem_Click;
            // 
            // tsbDeleteItem
            // 
            tsbDeleteItem.DisplayStyle = ToolStripItemDisplayStyle.Image;
            tsbDeleteItem.Enabled = false;
            tsbDeleteItem.Image = Properties.Resources.remove_xform;
            tsbDeleteItem.ImageTransparentColor = Color.Magenta;
            tsbDeleteItem.Name = "tsbDeleteItem";
            tsbDeleteItem.Size = new Size(23, 22);
            tsbDeleteItem.Text = "toolStripButton3";
            tsbDeleteItem.ToolTipText = "Delete Item";
            tsbDeleteItem.Click += tsbDeleteItem_Click;
            // 
            // lstWheels
            // 
            lstWheels.Columns.AddRange(new ColumnHeader[] { Index, ID, WheelName });
            lstWheels.Dock = DockStyle.Fill;
            lstWheels.FullRowSelect = true;
            lstWheels.GridLines = true;
            lstWheels.Location = new Point(0, 49);
            lstWheels.Name = "lstWheels";
            lstWheels.Size = new Size(261, 370);
            lstWheels.TabIndex = 2;
            lstWheels.UseCompatibleStateImageBehavior = false;
            lstWheels.View = View.Details;
            lstWheels.ColumnClick += lstWheels_ColumnClick;
            lstWheels.SelectedIndexChanged += lstWheels_SelectedIndexChanged;
            lstWheels.MouseDoubleClick += lstWheels_MouseDoubleClick;
            // 
            // Index
            // 
            Index.Text = "Index";
            Index.Width = 48;
            // 
            // ID
            // 
            ID.Text = "ID";
            ID.Width = 64;
            // 
            // WheelName
            // 
            WheelName.Text = "Name";
            WheelName.Width = 128;
            // 
            // stsMain
            // 
            stsMain.Items.AddRange(new ToolStripItem[] { stlStatusLabel });
            stsMain.Location = new Point(0, 419);
            stsMain.Name = "stsMain";
            stsMain.Size = new Size(261, 22);
            stsMain.TabIndex = 3;
            stsMain.Text = "statusStrip1";
            // 
            // stlStatusLabel
            // 
            stlStatusLabel.Name = "stlStatusLabel";
            stlStatusLabel.Size = new Size(0, 17);
            // 
            // WheelListForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(261, 441);
            Controls.Add(lstWheels);
            Controls.Add(stsMain);
            Controls.Add(toolStrip1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "WheelListForm";
            Text = "Wheel List Viewer";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            stsMain.ResumeLayout(false);
            stsMain.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem itemsToolStripMenuItem;
        private ToolStripMenuItem addItemToolStripMenuItem;
        private ToolStripMenuItem copyItemToolStripMenuItem;
        private ToolStripMenuItem deleteItemToolStripMenuItem;
        private ToolStrip toolStrip1;
        private ToolStripButton tsbAddItem;
        private ToolStripButton tsbCopyItem;
        private ToolStripButton tsbDeleteItem;
        private ListView lstWheels;
        private StatusStrip stsMain;
        private ColumnHeader Index;
        private ColumnHeader ID;
        private ColumnHeader WheelName;
        private ToolStripStatusLabel stlStatusLabel;
    }
}
