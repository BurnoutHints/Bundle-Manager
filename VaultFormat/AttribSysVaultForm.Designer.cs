
namespace VaultFormat
{
    partial class AttribSysVaultForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            menu = new System.Windows.Forms.MenuStrip();
            toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            lstDataChunks = new System.Windows.Forms.ListView();
            colClassName = new System.Windows.Forms.ColumnHeader();
            colClassHash = new System.Windows.Forms.ColumnHeader();
            colCollectionHash = new System.Windows.Forms.ColumnHeader();
            contextMenu = new System.Windows.Forms.ContextMenuStrip(components);
            changeCollectionHashToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            propertyGrid2 = new System.Windows.Forms.PropertyGrid();
            exportVaultToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            menu.SuspendLayout();
            contextMenu.SuspendLayout();
            SuspendLayout();
            // 
            // menu
            // 
            menu.ImageScalingSize = new System.Drawing.Size(24, 24);
            menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { toolStripMenuItem1 });
            menu.Location = new System.Drawing.Point(0, 0);
            menu.Name = "menu";
            menu.Padding = new System.Windows.Forms.Padding(3, 2, 0, 2);
            menu.Size = new System.Drawing.Size(1020, 24);
            menu.TabIndex = 1;
            menu.Text = "menuStrip1";
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { exportVaultToolStripMenuItem });
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new System.Drawing.Size(38, 20);
            toolStripMenuItem1.Text = "File";
            // 
            // lstDataChunks
            // 
            lstDataChunks.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { colClassName, colClassHash, colCollectionHash });
            lstDataChunks.ContextMenuStrip = contextMenu;
            lstDataChunks.Dock = System.Windows.Forms.DockStyle.Top;
            lstDataChunks.FullRowSelect = true;
            lstDataChunks.GridLines = true;
            lstDataChunks.Location = new System.Drawing.Point(0, 24);
            lstDataChunks.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            lstDataChunks.Name = "lstDataChunks";
            lstDataChunks.Size = new System.Drawing.Size(1020, 242);
            lstDataChunks.TabIndex = 4;
            lstDataChunks.UseCompatibleStateImageBehavior = false;
            lstDataChunks.View = System.Windows.Forms.View.Details;
            lstDataChunks.DoubleClick += lstDataChunks_DoubleClick;
            // 
            // colClassName
            // 
            colClassName.Text = "ClassName";
            colClassName.Width = 250;
            // 
            // colClassHash
            // 
            colClassHash.Text = "ClassHash";
            colClassHash.Width = 250;
            // 
            // colCollectionHash
            // 
            colCollectionHash.Text = "CollectionHash";
            colCollectionHash.Width = 250;
            // 
            // contextMenu
            // 
            contextMenu.ImageScalingSize = new System.Drawing.Size(24, 24);
            contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { changeCollectionHashToolStripMenuItem });
            contextMenu.Name = "contextMenu";
            contextMenu.Size = new System.Drawing.Size(202, 26);
            // 
            // changeCollectionHashToolStripMenuItem
            // 
            changeCollectionHashToolStripMenuItem.Name = "changeCollectionHashToolStripMenuItem";
            changeCollectionHashToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            changeCollectionHashToolStripMenuItem.Text = "Change CollectionHash";
            changeCollectionHashToolStripMenuItem.Click += changeCollectionHashToolStripMenuItem_Click;
            // 
            // propertyGrid2
            // 
            propertyGrid2.Location = new System.Drawing.Point(0, 277);
            propertyGrid2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            propertyGrid2.Name = "propertyGrid2";
            propertyGrid2.Size = new System.Drawing.Size(1020, 602);
            propertyGrid2.TabIndex = 6;
            // 
            // exportVaultToolStripMenuItem
            // 
            exportVaultToolStripMenuItem.Name = "exportVaultToolStripMenuItem";
            exportVaultToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            exportVaultToolStripMenuItem.Text = "Export to JSON";
            exportVaultToolStripMenuItem.Click += exportVaultToolStripMenuItem_Click;
            // 
            // AttribSysVaultForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1020, 878);
            Controls.Add(propertyGrid2);
            Controls.Add(lstDataChunks);
            Controls.Add(menu);
            MainMenuStrip = menu;
            Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            Name = "AttribSysVaultForm";
            Text = "AttribSysVault Editor";
            FormClosed += AttribSysVaultForm_FormClosed;
            menu.ResumeLayout(false);
            menu.PerformLayout();
            contextMenu.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MenuStrip menu;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ListView lstDataChunks;
        private System.Windows.Forms.ColumnHeader colClassName;
        private System.Windows.Forms.ColumnHeader colClassHash;
        private System.Windows.Forms.ColumnHeader colCollectionHash;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem changeCollectionHashToolStripMenuItem;
        private System.Windows.Forms.PropertyGrid propertyGrid2;
        private System.Windows.Forms.ToolStripMenuItem exportVaultToolStripMenuItem;
    }
}

