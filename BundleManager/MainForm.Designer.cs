namespace BundleManager
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            mnuMain = new System.Windows.Forms.MenuStrip();
            fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            searchForEntryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            pluginToolsSeparatorItem = new System.Windows.Forms.ToolStripSeparator();
            addResourcesToBundleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStrip1 = new System.Windows.Forms.ToolStrip();
            tsbNew = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            tsbOpen = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            tsbSave = new System.Windows.Forms.ToolStripButton();
            toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            tsbSwitchMode = new System.Windows.Forms.ToolStripButton();
            lstEntries = new BetterListView();
            colIndex = new System.Windows.Forms.ColumnHeader();
            colName = new System.Windows.Forms.ColumnHeader();
            colID = new System.Windows.Forms.ColumnHeader();
            colType = new System.Windows.Forms.ColumnHeader();
            colSize = new System.Windows.Forms.ColumnHeader();
            colPreview = new System.Windows.Forms.ColumnHeader();
            mnuLst = new System.Windows.Forms.ContextMenuStrip(components);
            previewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
            viewDataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            viewDebugMenuStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            mnuMain.SuspendLayout();
            toolStrip1.SuspendLayout();
            mnuLst.SuspendLayout();
            SuspendLayout();
            // 
            // mnuMain
            // 
            mnuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { fileToolStripMenuItem, toolsToolStripMenuItem });
            mnuMain.Location = new System.Drawing.Point(0, 0);
            mnuMain.Name = "mnuMain";
            mnuMain.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
            mnuMain.Size = new System.Drawing.Size(915, 24);
            mnuMain.TabIndex = 0;
            mnuMain.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { newToolStripMenuItem, toolStripMenuItem3, openToolStripMenuItem, toolStripMenuItem1, saveToolStripMenuItem, saveAsToolStripMenuItem, toolStripMenuItem2, exitToolStripMenuItem, closeToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // newToolStripMenuItem
            // 
            newToolStripMenuItem.Image = Properties.Resources.NewDocumentHS;
            newToolStripMenuItem.Name = "newToolStripMenuItem";
            newToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N;
            newToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            newToolStripMenuItem.Text = "New";
            newToolStripMenuItem.Click += newToolStripMenuItem_Click;
            // 
            // toolStripMenuItem3
            // 
            toolStripMenuItem3.Name = "toolStripMenuItem3";
            toolStripMenuItem3.Size = new System.Drawing.Size(183, 6);
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Image = Properties.Resources.openHS;
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O;
            openToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            openToolStripMenuItem.Text = "Open";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new System.Drawing.Size(183, 6);
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Image = Properties.Resources.saveHS;
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S;
            saveToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            saveToolStripMenuItem.Text = "Save";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Image = Properties.Resources.SaveAllHS;
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.S;
            saveAsToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            saveAsToolStripMenuItem.Text = "Save As";
            saveAsToolStripMenuItem.Click += saveAsToolStripMenuItem_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new System.Drawing.Size(183, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // closeToolStripMenuItem
            // 
            closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            closeToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            closeToolStripMenuItem.Text = "Close";
            closeToolStripMenuItem.Visible = false;
            closeToolStripMenuItem.Click += closeToolStripMenuItem_Click;
            // 
            // toolsToolStripMenuItem
            // 
            toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { searchForEntryToolStripMenuItem, addResourcesToBundleToolStripMenuItem, pluginToolsSeparatorItem });
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
            toolsToolStripMenuItem.Text = "Tools";
            // 
            // searchForEntryToolStripMenuItem
            // 
            searchForEntryToolStripMenuItem.Name = "searchForEntryToolStripMenuItem";
            searchForEntryToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F;
            searchForEntryToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
            searchForEntryToolStripMenuItem.Text = "Search for entry";
            searchForEntryToolStripMenuItem.Click += searchForEntryToolStripMenuItem_Click;
            // 
            // pluginToolsSeparatorItem
            // 
            pluginToolsSeparatorItem.Name = "pluginToolsSeparatorItem";
            pluginToolsSeparatorItem.Size = new System.Drawing.Size(217, 6);
            // 
            // addResourcesToBundleToolStripMenuItem
            // 
            addResourcesToBundleToolStripMenuItem.Name = "addResourcesToBundleToolStripMenuItem";
            addResourcesToBundleToolStripMenuItem.Size = new System.Drawing.Size(220, 22);
            addResourcesToBundleToolStripMenuItem.Text = "Add resource(s) to bundle...";
            addResourcesToBundleToolStripMenuItem.Click += addResourcesToBundleToolStripMenuItem_Click;
            // 
            // toolStrip1
            // 
            toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { tsbNew, toolStripSeparator1, tsbOpen, toolStripSeparator2, tsbSave, toolStripSeparator3, tsbSwitchMode });
            toolStrip1.Location = new System.Drawing.Point(0, 24);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new System.Drawing.Size(915, 25);
            toolStrip1.TabIndex = 1;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsbNew
            // 
            tsbNew.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsbNew.Image = Properties.Resources.NewDocumentHS;
            tsbNew.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsbNew.Name = "tsbNew";
            tsbNew.Size = new System.Drawing.Size(23, 22);
            tsbNew.Text = "New";
            tsbNew.Click += tsbNew_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbOpen
            // 
            tsbOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsbOpen.Image = Properties.Resources.openHS;
            tsbOpen.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsbOpen.Name = "tsbOpen";
            tsbOpen.Size = new System.Drawing.Size(23, 22);
            tsbOpen.Text = "Open";
            tsbOpen.Click += tsbOpen_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbSave
            // 
            tsbSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            tsbSave.Image = Properties.Resources.saveHS;
            tsbSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsbSave.Name = "tsbSave";
            tsbSave.Size = new System.Drawing.Size(23, 22);
            tsbSave.Text = "Save";
            tsbSave.Click += tsbSave_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbSwitchMode
            // 
            tsbSwitchMode.Image = Properties.Resources.icon;
            tsbSwitchMode.ImageTransparentColor = System.Drawing.Color.Magenta;
            tsbSwitchMode.Name = "tsbSwitchMode";
            tsbSwitchMode.Size = new System.Drawing.Size(148, 22);
            tsbSwitchMode.Text = "Switch To Studio Mode";
            tsbSwitchMode.Click += tsbSwitchMode_Click;
            // 
            // lstEntries
            // 
            lstEntries.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { colIndex, colName, colID, colType, colSize, colPreview });
            lstEntries.ContextMenuStrip = mnuLst;
            lstEntries.Dock = System.Windows.Forms.DockStyle.Fill;
            lstEntries.FullRowSelect = true;
            lstEntries.GridLines = true;
            lstEntries.Location = new System.Drawing.Point(0, 49);
            lstEntries.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            lstEntries.MultiSelect = false;
            lstEntries.Name = "lstEntries";
            lstEntries.Size = new System.Drawing.Size(915, 598);
            lstEntries.TabIndex = 2;
            lstEntries.UseCompatibleStateImageBehavior = false;
            lstEntries.View = System.Windows.Forms.View.Details;
            lstEntries.ColumnClick += lstEntries_ColumnClick;
            lstEntries.MouseDoubleClick += lstEntries_MouseDoubleClick;
            // 
            // colIndex
            // 
            colIndex.Text = "Index";
            colIndex.Width = 70;
            // 
            // colName
            // 
            colName.Text = "Name";
            colName.Width = 140;
            // 
            // colID
            // 
            colID.Text = "ID";
            colID.Width = 117;
            // 
            // colType
            // 
            colType.Text = "Type";
            colType.Width = 233;
            // 
            // colSize
            // 
            colSize.Text = "Size";
            colSize.Width = 70;
            // 
            // colPreview
            // 
            colPreview.Text = "Preview";
            colPreview.Width = 350;
            // 
            // mnuLst
            // 
            mnuLst.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { previewToolStripMenuItem, toolStripMenuItem4, viewDataToolStripMenuItem, viewDebugMenuStripMenuItem });
            mnuLst.Name = "mnuLst";
            mnuLst.Size = new System.Drawing.Size(172, 76);
            // 
            // previewToolStripMenuItem
            // 
            previewToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            previewToolStripMenuItem.Name = "previewToolStripMenuItem";
            previewToolStripMenuItem.Size = new System.Drawing.Size(171, 22);
            previewToolStripMenuItem.Text = "Preview";
            previewToolStripMenuItem.Click += previewToolStripMenuItem_Click;
            // 
            // toolStripMenuItem4
            // 
            toolStripMenuItem4.Name = "toolStripMenuItem4";
            toolStripMenuItem4.Size = new System.Drawing.Size(168, 6);
            // 
            // viewDataToolStripMenuItem
            // 
            viewDataToolStripMenuItem.Name = "viewDataToolStripMenuItem";
            viewDataToolStripMenuItem.Size = new System.Drawing.Size(171, 22);
            viewDataToolStripMenuItem.Text = "View Header";
            viewDataToolStripMenuItem.Click += viewDataToolStripMenuItem_Click;
            // 
            // viewDebugMenuStripMenuItem
            // 
            viewDebugMenuStripMenuItem.Name = "viewDebugMenuStripMenuItem";
            viewDebugMenuStripMenuItem.Size = new System.Drawing.Size(171, 22);
            viewDebugMenuStripMenuItem.Text = "View Debug Menu";
            viewDebugMenuStripMenuItem.Click += viewDebugMenuStripMenuItem_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(915, 647);
            Controls.Add(lstEntries);
            Controls.Add(toolStrip1);
            Controls.Add(mnuMain);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = mnuMain;
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "MainForm";
            Text = "Burnout Bundle Manager";
            FormClosing += MainForm_FormClosing;
            mnuMain.ResumeLayout(false);
            mnuMain.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            mnuLst.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip mnuMain;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsbNew;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton tsbOpen;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton tsbSave;
        private BundleManager.BetterListView lstEntries;
        private System.Windows.Forms.ColumnHeader colIndex;
        private System.Windows.Forms.ColumnHeader colPreview;
        private System.Windows.Forms.ContextMenuStrip mnuLst;
        private System.Windows.Forms.ToolStripMenuItem previewToolStripMenuItem;
        private System.Windows.Forms.ColumnHeader colType;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem viewDataToolStripMenuItem;        
        private System.Windows.Forms.ToolStripMenuItem viewDebugMenuStripMenuItem;
        private System.Windows.Forms.ColumnHeader colID;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton tsbSwitchMode;
        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
        private System.Windows.Forms.ColumnHeader colSize;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem searchForEntryToolStripMenuItem;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ToolStripSeparator pluginToolsSeparatorItem;
        private System.Windows.Forms.ToolStripMenuItem addResourcesToBundleToolStripMenuItem;
    }
}

