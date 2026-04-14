using OpenTK;
using OpenTK.GLControl;

namespace ModelViewer
{
    partial class SceneRenderControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.glcMain = new GLControl();
            this.tmrUpdate = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // glcMain
            // 
            this.glcMain.BackColor = System.Drawing.Color.Black;
            this.glcMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.glcMain.Location = new System.Drawing.Point(0, 0);
            this.glcMain.Name = "glcMain";
            this.glcMain.Size = new System.Drawing.Size(150, 150);
            this.glcMain.TabIndex = 1;
            this.glcMain.Paint += new System.Windows.Forms.PaintEventHandler(this.glcMain_Paint);
            // 
            // tmrUpdate
            // 
            this.tmrUpdate.Interval = 17;
            this.tmrUpdate.Tick += new System.EventHandler(this.tmrUpdate_Tick);
            // 
            // SceneRenderControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.glcMain);
            this.Name = "SceneRenderControl";
            this.ResumeLayout(false);
        }

        #endregion

        private GLControl glcMain;
        private System.Windows.Forms.Timer tmrUpdate;
    }
}
