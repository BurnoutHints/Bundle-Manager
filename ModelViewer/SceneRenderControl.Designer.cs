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
            components = new System.ComponentModel.Container();
            tmrUpdate = new System.Windows.Forms.Timer(this.components);
            SuspendLayout();
            // 
            // tmrUpdate
            // 
            tmrUpdate.Interval = 17;
            tmrUpdate.Tick += new System.EventHandler(this.tmrUpdate_Tick);
            // 
            // SceneRenderControl
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "SceneRenderControl";
            Size = new System.Drawing.Size(175, 173);
            ResumeLayout(false);
        }

        #endregion
        private System.Windows.Forms.Timer tmrUpdate;
    }
}
