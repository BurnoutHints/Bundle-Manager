namespace BundleManager
{
    partial class ImportResourceForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImportResourceForm));
            importButton = new System.Windows.Forms.Button();
            resourceIDTextBox = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            resourceNameTextBox = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            label3 = new System.Windows.Forms.Label();
            secondaryDataPathSelectorButton = new System.Windows.Forms.Button();
            autoUpdateIDCheckBox = new System.Windows.Forms.CheckBox();
            selectTypeLabel = new System.Windows.Forms.Label();
            resourceTypeComboBox = new System.Windows.Forms.ComboBox();
            tertiaryDataPathSelectorButton = new System.Windows.Forms.Button();
            resourceNamePrefixLabel = new System.Windows.Forms.Label();
            importPathsLabel = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // importButton
            // 
            importButton.Location = new System.Drawing.Point(562, 258);
            importButton.Name = "importButton";
            importButton.Size = new System.Drawing.Size(88, 23);
            importButton.TabIndex = 0;
            importButton.Text = "Import";
            importButton.UseVisualStyleBackColor = true;
            importButton.Click += button1_Click;
            // 
            // resourceIDTextBox
            // 
            resourceIDTextBox.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            resourceIDTextBox.Location = new System.Drawing.Point(563, 27);
            resourceIDTextBox.MaxLength = 8;
            resourceIDTextBox.Name = "resourceIDTextBox";
            resourceIDTextBox.Size = new System.Drawing.Size(85, 23);
            resourceIDTextBox.TabIndex = 1;
            resourceIDTextBox.Text = "XXXXXXXX";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(563, 9);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(88, 15);
            label1.TabIndex = 2;
            label1.Text = "ID to import as:";
            // 
            // resourceNameTextBox
            // 
            resourceNameTextBox.Location = new System.Drawing.Point(112, 27);
            resourceNameTextBox.Name = "resourceNameTextBox";
            resourceNameTextBox.Size = new System.Drawing.Size(428, 23);
            resourceNameTextBox.TabIndex = 3;
            resourceNameTextBox.Text = "my_really_cool_and_extremely_long_resource_name";
            resourceNameTextBox.TextChanged += resourceNameTextBox_TextChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(12, 9);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(242, 15);
            label2.TabIndex = 4;
            label2.Text = "What would you like to name your resource?";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(546, 31);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(15, 15);
            label3.TabIndex = 5;
            label3.Text = ">";
            // 
            // secondaryDataPathSelectorButton
            // 
            secondaryDataPathSelectorButton.Location = new System.Drawing.Point(11, 258);
            secondaryDataPathSelectorButton.Name = "secondaryDataPathSelectorButton";
            secondaryDataPathSelectorButton.Size = new System.Drawing.Size(140, 23);
            secondaryDataPathSelectorButton.TabIndex = 6;
            secondaryDataPathSelectorButton.Text = "Select Secondary Data";
            secondaryDataPathSelectorButton.UseVisualStyleBackColor = true;
            secondaryDataPathSelectorButton.Click += secondaryDataPathSelectorButton_Click;
            // 
            // autoUpdateIDCheckBox
            // 
            autoUpdateIDCheckBox.AutoSize = true;
            autoUpdateIDCheckBox.Location = new System.Drawing.Point(546, 56);
            autoUpdateIDCheckBox.Name = "autoUpdateIDCheckBox";
            autoUpdateIDCheckBox.Size = new System.Drawing.Size(106, 19);
            autoUpdateIDCheckBox.TabIndex = 7;
            autoUpdateIDCheckBox.Text = "Auto update ID";
            autoUpdateIDCheckBox.UseVisualStyleBackColor = true;
            autoUpdateIDCheckBox.CheckedChanged += autoUpdateIDCheckBox_CheckedChanged;
            // 
            // selectTypeLabel
            // 
            selectTypeLabel.AutoSize = true;
            selectTypeLabel.Location = new System.Drawing.Point(11, 60);
            selectTypeLabel.Name = "selectTypeLabel";
            selectTypeLabel.Size = new System.Drawing.Size(202, 15);
            selectTypeLabel.TabIndex = 9;
            selectTypeLabel.Text = "Select the resource type to import as:";
            // 
            // resourceTypeComboBox
            // 
            resourceTypeComboBox.FormattingEnabled = true;
            resourceTypeComboBox.Location = new System.Drawing.Point(11, 78);
            resourceTypeComboBox.Name = "resourceTypeComboBox";
            resourceTypeComboBox.Size = new System.Drawing.Size(229, 23);
            resourceTypeComboBox.TabIndex = 10;
            resourceTypeComboBox.Text = "AptData";
            // 
            // tertiaryDataPathSelectorButton
            // 
            tertiaryDataPathSelectorButton.Location = new System.Drawing.Point(157, 258);
            tertiaryDataPathSelectorButton.Name = "tertiaryDataPathSelectorButton";
            tertiaryDataPathSelectorButton.Size = new System.Drawing.Size(140, 23);
            tertiaryDataPathSelectorButton.TabIndex = 11;
            tertiaryDataPathSelectorButton.Text = "Select Tertiary Data";
            tertiaryDataPathSelectorButton.UseVisualStyleBackColor = true;
            tertiaryDataPathSelectorButton.Click += tertiaryDataPathSelectorButton_Click;
            // 
            // resourceNamePrefixLabel
            // 
            resourceNamePrefixLabel.AutoSize = true;
            resourceNamePrefixLabel.Location = new System.Drawing.Point(11, 30);
            resourceNamePrefixLabel.Name = "resourceNamePrefixLabel";
            resourceNamePrefixLabel.Size = new System.Drawing.Size(104, 15);
            resourceNamePrefixLabel.TabIndex = 12;
            resourceNamePrefixLabel.Text = "bundlemanager://";
            // 
            // importPathsLabel
            // 
            importPathsLabel.AutoSize = true;
            importPathsLabel.Location = new System.Drawing.Point(11, 117);
            importPathsLabel.Name = "importPathsLabel";
            importPathsLabel.Size = new System.Drawing.Size(571, 120);
            importPathsLabel.TabIndex = 13;
            importPathsLabel.Text = resources.GetString("importPathsLabel.Text");
            importPathsLabel.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            // 
            // ImportResourceForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(660, 293);
            Controls.Add(importPathsLabel);
            Controls.Add(resourceNamePrefixLabel);
            Controls.Add(tertiaryDataPathSelectorButton);
            Controls.Add(resourceTypeComboBox);
            Controls.Add(selectTypeLabel);
            Controls.Add(autoUpdateIDCheckBox);
            Controls.Add(secondaryDataPathSelectorButton);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(resourceNameTextBox);
            Controls.Add(label1);
            Controls.Add(resourceIDTextBox);
            Controls.Add(importButton);
            Name = "ImportResourceForm";
            Text = "Import Resource into Bundle";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button importButton;
        private System.Windows.Forms.TextBox resourceIDTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox resourceNameTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button secondaryDataPathSelectorButton;
        private System.Windows.Forms.CheckBox autoUpdateIDCheckBox;
        private System.Windows.Forms.Label selectTypeLabel;
        private System.Windows.Forms.ComboBox resourceTypeComboBox;
        private System.Windows.Forms.Button tertiaryDataPathSelectorButton;
        private System.Windows.Forms.Label resourceNamePrefixLabel;
        private System.Windows.Forms.Label importPathsLabel;
    }
}