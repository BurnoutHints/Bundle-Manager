using System.Drawing;
using System.Windows.Forms;

namespace WheelList
{
    partial class WheelEditor
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
            btnOk = new Button();
            btnCancel = new Button();
            txtIndex = new TextBox();
            txtID = new TextBox();
            txtName = new TextBox();
            lblIndex = new Label();
            lblID = new Label();
            lblName = new Label();
            SuspendLayout();
            // 
            // btnOk
            // 
            btnOk.Location = new Point(52, 117);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(75, 23);
            btnOk.TabIndex = 0;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += btnOk_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(133, 117);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 23);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // txtIndex
            // 
            txtIndex.Location = new Point(58, 23);
            txtIndex.Name = "txtIndex";
            txtIndex.Size = new Size(150, 23);
            txtIndex.TabIndex = 2;
            // 
            // txtID
            // 
            txtID.Location = new Point(58, 53);
            txtID.Name = "txtID";
            txtID.Size = new Size(150, 23);
            txtID.TabIndex = 3;
            // 
            // txtName
            // 
            txtName.Location = new Point(58, 83);
            txtName.Name = "txtName";
            txtName.Size = new Size(150, 23);
            txtName.TabIndex = 4;
            // 
            // lblIndex
            // 
            lblIndex.AutoSize = true;
            lblIndex.Location = new Point(12, 27);
            lblIndex.Name = "lblIndex";
            lblIndex.Size = new Size(39, 15);
            lblIndex.TabIndex = 5;
            lblIndex.Text = "Index:";
            // 
            // lblID
            // 
            lblID.AutoSize = true;
            lblID.Location = new Point(12, 57);
            lblID.Name = "lblID";
            lblID.Size = new Size(21, 15);
            lblID.TabIndex = 6;
            lblID.Text = "ID:";
            // 
            // lblName
            // 
            lblName.AutoSize = true;
            lblName.Location = new Point(11, 86);
            lblName.Name = "lblName";
            lblName.Size = new Size(42, 15);
            lblName.TabIndex = 7;
            lblName.Text = "Name:";
            // 
            // WheelEditor
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(234, 156);
            Controls.Add(lblName);
            Controls.Add(lblID);
            Controls.Add(lblIndex);
            Controls.Add(txtName);
            Controls.Add(txtID);
            Controls.Add(txtIndex);
            Controls.Add(btnCancel);
            Controls.Add(btnOk);
            Name = "WheelEditor";
            Text = "WheelEditor";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnOk;
        private Button btnCancel;
        private TextBox txtIndex;
        private TextBox txtID;
        private TextBox txtName;
        private Label lblIndex;
        private Label lblID;
        private Label lblName;
    }
}
