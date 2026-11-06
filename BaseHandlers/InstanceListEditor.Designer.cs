namespace BaseHandlers
{
    partial class InstanceListEditor
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
            this.mnuMain = new System.Windows.Forms.MenuStrip();
            this.debugToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.stsMain = new System.Windows.Forms.StatusStrip();
            this.renderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lstMain = new System.Windows.Forms.ListView();
            this.colModel = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colTranslation = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colRotation = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colScale = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.mnuMain.SuspendLayout();
            this.SuspendLayout();

            //
            // mnuMain
            //
            this.mnuMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
            {
                this.renderToolStripMenuItem,
                this.debugToolStripMenuItem
            });
            this.mnuMain.Location = new System.Drawing.Point(0, 0);
            this.mnuMain.Name = "mnuMain";
            this.mnuMain.Size = new System.Drawing.Size(800, 24);
            this.mnuMain.TabIndex = 0;
            this.mnuMain.Text = "menuStrip1";

            //
            // debugToolStripMenuItem
            //
            this.debugToolStripMenuItem.Name = "debugToolStripMenuItem";
            this.debugToolStripMenuItem.Size = new System.Drawing.Size(54, 20);
            this.debugToolStripMenuItem.Text = "Debug";
            this.debugToolStripMenuItem.Click += new System.EventHandler(this.debugToolStripMenuItem_Click);

            //
            // stsMain
            //
            this.stsMain.Location = new System.Drawing.Point(0, 428);
            this.stsMain.Name = "stsMain";
            this.stsMain.Size = new System.Drawing.Size(800, 22);
            this.stsMain.TabIndex = 1;
            this.stsMain.Text = "statusStrip1";

            //
            // renderToolStripMenuItem
            //
            this.renderToolStripMenuItem.Name = "renderToolStripMenuItem";
            this.renderToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
            this.renderToolStripMenuItem.Text = "Render";
            this.renderToolStripMenuItem.Click += new System.EventHandler(this.renderToolStripMenuItem_Click);

            //
            // lstMain
            //
            this.lstMain.Columns.AddRange(new System.Windows.Forms.ColumnHeader[]
            {
                this.colModel,
                this.colTranslation,
                this.colRotation,
                this.colScale
            });
            this.lstMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstMain.FullRowSelect = true;
            this.lstMain.GridLines = true;
            this.lstMain.HideSelection = false;
            this.lstMain.Location = new System.Drawing.Point(0, 24);
            this.lstMain.Name = "lstMain";
            this.lstMain.Size = new System.Drawing.Size(800, 404);
            this.lstMain.TabIndex = 2;
            this.lstMain.UseCompatibleStateImageBehavior = false;
            this.lstMain.View = System.Windows.Forms.View.Details;
            this.lstMain.SizeChanged += new System.EventHandler(this.lstMain_SizeChanged);
            this.lstMain.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lstMain_MouseDoubleClick);

            //
            // colModel
            //
            this.colModel.Text = "Model";

            //
            // colTranslation
            //
            this.colTranslation.Text = "Position";
            this.colTranslation.Width = 120;

            //
            // colRotation
            //
            this.colRotation.Text = "Rotation";
            this.colRotation.Width = 120;

            //
            // colScale
            //
            this.colScale.Text = "Scale";
            this.colScale.Width = 120;

            //
            // InstanceListEditor
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.lstMain);
            this.Controls.Add(this.stsMain);
            this.Controls.Add(this.mnuMain);
            this.MainMenuStrip = this.mnuMain;
            this.Name = "InstanceListEditor";
            this.Text = "InstanceListEditor";
            this.Load += new System.EventHandler(this.InstanceListEditor_Load);
            this.mnuMain.ResumeLayout(false);
            this.mnuMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip mnuMain;
        private System.Windows.Forms.ToolStripMenuItem debugToolStripMenuItem;
        private System.Windows.Forms.StatusStrip stsMain;
        private System.Windows.Forms.ToolStripMenuItem renderToolStripMenuItem;
        private System.Windows.Forms.ListView lstMain;
        private System.Windows.Forms.ColumnHeader colModel;
        private System.Windows.Forms.ColumnHeader colTranslation;
        private System.Windows.Forms.ColumnHeader colRotation;
        private System.Windows.Forms.ColumnHeader colScale;
    }
}
