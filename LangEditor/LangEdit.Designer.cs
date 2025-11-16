namespace LangEditor
{
    partial class LangEdit
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
            dgvMain = new System.Windows.Forms.DataGridView();
            colID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            colString = new System.Windows.Forms.DataGridViewTextBoxColumn();
            statusStrip1 = new System.Windows.Forms.StatusStrip();
            menuStrip1 = new System.Windows.Forms.MenuStrip();
            fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            importToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            exportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            applyChangesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            findToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            findNextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            hashToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)dgvMain).BeginInit();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dgvMain
            // 
            dgvMain.AllowUserToOrderColumns = true;
            dgvMain.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            dgvMain.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvMain.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { colID, colString });
            dgvMain.Dock = System.Windows.Forms.DockStyle.Fill;
            dgvMain.Location = new System.Drawing.Point(0, 24);
            dgvMain.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            dgvMain.MinimumSize = new System.Drawing.Size(70, 0);
            dgvMain.MultiSelect = false;
            dgvMain.Name = "dgvMain";
            dgvMain.RowHeadersWidth = 30;
            dgvMain.Size = new System.Drawing.Size(648, 384);
            dgvMain.TabIndex = 0;
            dgvMain.RowsAdded += dgvMain_RowsAdded;
            // 
            // colID
            // 
            colID.HeaderText = "ID";
            colID.Name = "colID";
            // 
            // colString
            // 
            colString.HeaderText = "String";
            colString.Name = "colString";
            // 
            // statusStrip1
            // 
            statusStrip1.Location = new System.Drawing.Point(0, 408);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
            statusStrip1.Size = new System.Drawing.Size(648, 22);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { fileToolStripMenuItem, editToolStripMenuItem, toolsToolStripMenuItem });
            menuStrip1.Location = new System.Drawing.Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
            menuStrip1.Size = new System.Drawing.Size(648, 24);
            menuStrip1.TabIndex = 2;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { importToolStripMenuItem, exportToolStripMenuItem, toolStripMenuItem1, applyChangesToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // importToolStripMenuItem
            // 
            importToolStripMenuItem.Name = "importToolStripMenuItem";
            importToolStripMenuItem.Size = new System.Drawing.Size(163, 22);
            importToolStripMenuItem.Text = "Import from CSV";
            importToolStripMenuItem.Click += importToolStripMenuItem_Click;
            // 
            // exportToolStripMenuItem
            // 
            exportToolStripMenuItem.Name = "exportToolStripMenuItem";
            exportToolStripMenuItem.Size = new System.Drawing.Size(163, 22);
            exportToolStripMenuItem.Text = "Export to CSV";
            exportToolStripMenuItem.Click += exportToolStripMenuItem_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new System.Drawing.Size(160, 6);
            // 
            // applyChangesToolStripMenuItem
            // 
            applyChangesToolStripMenuItem.Name = "applyChangesToolStripMenuItem";
            applyChangesToolStripMenuItem.Size = new System.Drawing.Size(163, 22);
            applyChangesToolStripMenuItem.Text = "Apply Changes";
            applyChangesToolStripMenuItem.Click += applyChangesToolStripMenuItem_Click;
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { findToolStripMenuItem, findNextToolStripMenuItem });
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            editToolStripMenuItem.Text = "Edit";
            // 
            // findToolStripMenuItem
            // 
            findToolStripMenuItem.Name = "findToolStripMenuItem";
            findToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F;
            findToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
            findToolStripMenuItem.Text = "Find";
            findToolStripMenuItem.Click += findToolStripMenuItem_Click;
            // 
            // findNextToolStripMenuItem
            // 
            findNextToolStripMenuItem.Enabled = false;
            findNextToolStripMenuItem.Name = "findNextToolStripMenuItem";
            findNextToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.F;
            findNextToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
            findNextToolStripMenuItem.Text = "Find next";
            findNextToolStripMenuItem.Click += findNextToolStripMenuItem_Click;
            // 
            // toolsToolStripMenuItem
            // 
            toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { hashToolStripMenuItem });
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
            toolsToolStripMenuItem.Text = "Tools";
            // 
            // hashToolStripMenuItem
            // 
            hashToolStripMenuItem.Name = "hashToolStripMenuItem";
            hashToolStripMenuItem.Size = new System.Drawing.Size(101, 22);
            hashToolStripMenuItem.Text = "Hash";
            hashToolStripMenuItem.Click += hashToolStripMenuItem_Click;
            // 
            // LangEdit
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(648, 430);
            Controls.Add(dgvMain);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "LangEdit";
            Text = "Language Editor";
            ((System.ComponentModel.ISupportInitialize)dgvMain).EndInit();
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.DataGridView dgvMain;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hashToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem findToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem applyChangesToolStripMenuItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn colID;
        private System.Windows.Forms.DataGridViewTextBoxColumn colString;
        private System.Windows.Forms.ToolStripMenuItem findNextToolStripMenuItem;
    }
}
