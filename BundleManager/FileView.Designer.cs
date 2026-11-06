namespace BundleManager
{
    partial class FileView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FileView));
            this.stsMain = new System.Windows.Forms.StatusStrip();
            this.mnuMain = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.recentSeparatorToolStripMenu = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ignoreIDConflictsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadMaterialsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tspMain = new System.Windows.Forms.ToolStrip();
            this.tsbOpen = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbSwitchMode = new System.Windows.Forms.ToolStripButton();
            this.lstMain = new System.Windows.Forms.ListView();
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.dumpInfoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuMain.SuspendLayout();
            this.tspMain.SuspendLayout();
            this.SuspendLayout();

            //
            // stsMain
            //
            this.stsMain.Location = new System.Drawing.Point(0, 436);
            this.stsMain.Name = "stsMain";
            this.stsMain.Size = new System.Drawing.Size(582, 22);
            this.stsMain.TabIndex = 0;
            this.stsMain.Text = "statusStrip1";

            //
            // mnuMain
            //
            this.mnuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
            {
                this.fileToolStripMenuItem,
                this.viewToolStripMenuItem
            });
            this.mnuMain.Location = new System.Drawing.Point(0, 0);
            this.mnuMain.Name = "mnuMain";
            this.mnuMain.Size = new System.Drawing.Size(582, 24);
            this.mnuMain.TabIndex = 1;
            this.mnuMain.Text = "menuStrip1";

            //
            // fileToolStripMenuItem
            //
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[]
            {
                this.openFolderToolStripMenuItem,
                this.dumpInfoToolStripMenuItem,
                this.recentSeparatorToolStripMenu,
                this.toolStripMenuItem1,
                this.exitToolStripMenuItem
            });
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";

            //
            // openFolderToolStripMenuItem
            //
            this.openFolderToolStripMenuItem.Image = global::BundleManager.Properties.Resources.openfolderHS;
            this.openFolderToolStripMenuItem.Name = "openFolderToolStripMenuItem";
            this.openFolderToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openFolderToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.openFolderToolStripMenuItem.Text = "Open Folder";
            this.openFolderToolStripMenuItem.Click += new System.EventHandler(this.openFolderToolStripMenuItem_Click);

            //
            // recentSeparatorToolStripMenu
            //
            this.recentSeparatorToolStripMenu.Name = "recentSeparatorToolStripMenu";
            this.recentSeparatorToolStripMenu.Size = new System.Drawing.Size(179, 6);

            //
            // toolStripMenuItem1
            //
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(179, 6);

            //
            // exitToolStripMenuItem
            //
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);

            //
            // viewToolStripMenuItem
            //
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[]
            {
                this.ignoreIDConflictsToolStripMenuItem,
                this.loadMaterialsToolStripMenuItem
            });
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "View";

            //
            // ignoreIDConflictsToolStripMenuItem
            //
            this.ignoreIDConflictsToolStripMenuItem.Checked = true;
            this.ignoreIDConflictsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ignoreIDConflictsToolStripMenuItem.Name = "ignoreIDConflictsToolStripMenuItem";
            this.ignoreIDConflictsToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.ignoreIDConflictsToolStripMenuItem.Text = "Ignore ID Conflicts";
            this.ignoreIDConflictsToolStripMenuItem.Click += new System.EventHandler(this.ignoreIDConflictsToolStripMenuItem_Click);

            //
            // loadMaterialsToolStripMenuItem
            //
            this.loadMaterialsToolStripMenuItem.Checked = true;
            this.loadMaterialsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.loadMaterialsToolStripMenuItem.Name = "loadMaterialsToolStripMenuItem";
            this.loadMaterialsToolStripMenuItem.Size = new System.Drawing.Size(172, 22);
            this.loadMaterialsToolStripMenuItem.Text = "Load Materials";
            this.loadMaterialsToolStripMenuItem.Click += new System.EventHandler(this.loadMaterialsToolStripMenuItem_Click);

            //
            // tspMain
            //
            this.tspMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
            {
                this.tsbOpen,
                this.toolStripSeparator1,
                this.tsbSwitchMode
            });
            this.tspMain.Location = new System.Drawing.Point(0, 24);
            this.tspMain.Name = "tspMain";
            this.tspMain.Size = new System.Drawing.Size(582, 25);
            this.tspMain.TabIndex = 2;
            this.tspMain.Text = "toolStrip1";

            //
            // tsbOpen
            //
            this.tsbOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbOpen.Image = global::BundleManager.Properties.Resources.openfolderHS;
            this.tsbOpen.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbOpen.Name = "tsbOpen";
            this.tsbOpen.Size = new System.Drawing.Size(23, 22);
            this.tsbOpen.Text = "Open Folder";
            this.tsbOpen.Click += new System.EventHandler(this.tsbOpen_Click);

            //
            // toolStripSeparator1
            //
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);

            //
            // tsbSwitchMode
            //
            this.tsbSwitchMode.Image = global::BundleManager.Properties.Resources.icon;
            this.tsbSwitchMode.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSwitchMode.Name = "tsbSwitchMode";
            this.tsbSwitchMode.Size = new System.Drawing.Size(152, 22);
            this.tsbSwitchMode.Text = "Switch To Bundle Mode";
            this.tsbSwitchMode.Click += new System.EventHandler(this.tsbSwitchMode_Click);

            //
            // lstMain
            //
            this.lstMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstMain.Columns.AddRange(new System.Windows.Forms.ColumnHeader[]
            {
                this.colName
            });
            this.lstMain.Location = new System.Drawing.Point(12, 52);
            this.lstMain.Name = "lstMain";
            this.lstMain.Size = new System.Drawing.Size(558, 381);
            this.lstMain.TabIndex = 3;
            this.lstMain.UseCompatibleStateImageBehavior = false;
            this.lstMain.View = System.Windows.Forms.View.Details;
            this.lstMain.DoubleClick += new System.EventHandler(this.lstMain_DoubleClick);

            //
            // colName
            //
            this.colName.Text = "Name";
            this.colName.Width = 480;

            //
            // dumpInfoToolStripMenuItem
            //
            this.dumpInfoToolStripMenuItem.Name = "dumpInfoToolStripMenuItem";
            this.dumpInfoToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
            this.dumpInfoToolStripMenuItem.Text = "Dump Info";
            this.dumpInfoToolStripMenuItem.Click += new System.EventHandler(this.dumpInfoToolStripMenuItem_Click);

            //
            // FileView
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(582, 458);
            this.Controls.Add(this.lstMain);
            this.Controls.Add(this.tspMain);
            this.Controls.Add(this.stsMain);
            this.Controls.Add(this.mnuMain);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.mnuMain;
            this.Name = "FileView";
            this.Text = "Burnout Studio";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FileView_FormClosing);
            this.mnuMain.ResumeLayout(false);
            this.mnuMain.PerformLayout();
            this.tspMain.ResumeLayout(false);
            this.tspMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip stsMain;
        private System.Windows.Forms.MenuStrip mnuMain;
        private System.Windows.Forms.ToolStrip tspMain;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openFolderToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripButton tsbOpen;
        private System.Windows.Forms.ToolStripSeparator recentSeparatorToolStripMenu;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton tsbSwitchMode;
        private System.Windows.Forms.ListView lstMain;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ignoreIDConflictsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadMaterialsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem dumpInfoToolStripMenuItem;
    }
}
