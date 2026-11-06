
namespace LuaList
{
    partial class LuaListEditor
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
            this.SuspendLayout();

            //
            // propertyGrid1
            //
            this.propertyGrid1.Location = new System.Drawing.Point(-2, -4);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(802, 456);
            this.propertyGrid1.TabIndex = 0;

            //
            // LuaListEditor
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.propertyGrid1);
            this.Name = "LuaListEditor";
            this.Text = "Lua List Editor";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.LuaListEditor_FormClosed);
            this.ResumeLayout(false);

            this.Controls.Add(this.propertyGrid1);

        }

        #endregion

        private System.Windows.Forms.PropertyGrid propertyGrid1;
    }
}
