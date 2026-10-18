
namespace BaseHandlers
{
    partial class TriggerDataEditor
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importYamlToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exportYamlToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid1.Location = new System.Drawing.Point(0, 24);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(800, 426);
            this.propertyGrid1.TabIndex = 0;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(800, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.importYamlToolStripMenuItem,
            this.exportYamlToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // importYamlToolStripMenuItem
            // 
            this.importYamlToolStripMenuItem.Name = "importYamlToolStripMenuItem";
            this.importYamlToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.importYamlToolStripMenuItem.Text = "Import from YAML...";
            this.importYamlToolStripMenuItem.Click += new System.EventHandler(this.importYamlToolStripMenuItem_Click);
            // 
            // exportYamlToolStripMenuItem
            // 
            this.exportYamlToolStripMenuItem.Name = "exportYamlToolStripMenuItem";
            this.exportYamlToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.exportYamlToolStripMenuItem.Text = "Export as YAML...";
            this.exportYamlToolStripMenuItem.Click += new System.EventHandler(this.exportYamlToolStripMenuItem_Click);
            // 
            // TriggerDataEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.propertyGrid1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "TriggerDataEditor";
            this.Text = "Trigger Data Editor";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.TriggerDataEditor_FormClosed);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PropertyGrid propertyGrid1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importYamlToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportYamlToolStripMenuItem;
    }
}
