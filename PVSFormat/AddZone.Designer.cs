namespace PVSFormat
{
    partial class AddZone
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
            okButton = new System.Windows.Forms.Button();
            cancelButton = new System.Windows.Forms.Button();
            zoneIdNumericUpDown = new System.Windows.Forms.NumericUpDown();
            label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)zoneIdNumericUpDown).BeginInit();
            SuspendLayout();

            //
            // okButton
            //
            okButton.Location = new System.Drawing.Point(12, 65);
            okButton.Name = "okButton";
            okButton.Size = new System.Drawing.Size(75, 23);
            okButton.TabIndex = 0;
            okButton.Text = "OK";
            okButton.UseVisualStyleBackColor = true;
            okButton.Click += okButton_Click;

            //
            // cancelButton
            //
            cancelButton.Location = new System.Drawing.Point(93, 65);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(75, 23);
            cancelButton.TabIndex = 1;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            cancelButton.Click += cancelButton_Click;

            //
            // zoneIdNumericUpDown
            //
            zoneIdNumericUpDown.Location = new System.Drawing.Point(42, 32);
            zoneIdNumericUpDown.Maximum = new decimal(new int[]
            {
                511,
                0,
                0,
                0
            });
            zoneIdNumericUpDown.Name = "zoneIdNumericUpDown";
            zoneIdNumericUpDown.Size = new System.Drawing.Size(100, 23);
            zoneIdNumericUpDown.TabIndex = 2;

            //
            // label1
            //
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(41, 9);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(104, 15);
            label1.TabIndex = 3;
            label1.Text = "Enter new zone ID:";

            //
            // AddZone
            //
            AcceptButton = okButton;
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new System.Drawing.Size(184, 101);
            Controls.Add(label1);
            Controls.Add(zoneIdNumericUpDown);
            Controls.Add(cancelButton);
            Controls.Add(okButton);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Name = "AddZone";
            Text = "Add Zone";
            ((System.ComponentModel.ISupportInitialize)zoneIdNumericUpDown).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.NumericUpDown zoneIdNumericUpDown;
        private System.Windows.Forms.Label label1;
    }
}
