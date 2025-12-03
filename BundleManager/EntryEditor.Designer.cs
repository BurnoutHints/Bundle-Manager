using System;

namespace BundleManager
{
    partial class EntryEditor
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EntryEditor));
            tabList = new System.Windows.Forms.TabControl();
            tabInfo = new System.Windows.Forms.TabPage();
            txtInfo = new System.Windows.Forms.TextBox();
            tabHeader = new System.Windows.Forms.TabPage();
            hexData = new HexEditor.HexView();
            tabBody = new System.Windows.Forms.TabPage();
            hexExtraData = new HexEditor.HexView();
            lblLoading = new System.Windows.Forms.Label();
            mnuBar = new System.Windows.Forms.MenuStrip();
            binaryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            importHeaderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            exportHeaderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            importBodyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            exportBodyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            editStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            editHashToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            calcLookupHashToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            imageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            exportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            importToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            stsMain = new System.Windows.Forms.StatusStrip();
            imageStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            pbMain = new System.Windows.Forms.ProgressBar();
            pboImage = new System.Windows.Forms.PictureBox();
            tabList.SuspendLayout();
            tabInfo.SuspendLayout();
            tabHeader.SuspendLayout();
            tabBody.SuspendLayout();
            mnuBar.SuspendLayout();
            stsMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pboImage).BeginInit();
            SuspendLayout();
            // 
            // tabList
            // 
            tabList.Controls.Add(tabInfo);
            tabList.Controls.Add(tabHeader);
            tabList.Controls.Add(tabBody);
            tabList.Dock = System.Windows.Forms.DockStyle.Fill;
            tabList.Location = new System.Drawing.Point(0, 0);
            tabList.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabList.Name = "tabList";
            tabList.SelectedIndex = 0;
            tabList.Size = new System.Drawing.Size(728, 487);
            tabList.TabIndex = 0;
            tabList.SelectedIndexChanged += tabList_SelectedIndexChanged;
            // 
            // tabInfo
            // 
            tabInfo.Controls.Add(txtInfo);
            tabInfo.Location = new System.Drawing.Point(4, 24);
            tabInfo.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabInfo.Name = "tabInfo";
            tabInfo.Size = new System.Drawing.Size(720, 459);
            tabInfo.TabIndex = 3;
            tabInfo.Text = "Info";
            tabInfo.UseVisualStyleBackColor = true;
            // 
            // txtInfo
            // 
            txtInfo.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtInfo.BackColor = System.Drawing.SystemColors.ControlLightLight;
            txtInfo.BorderStyle = System.Windows.Forms.BorderStyle.None;
            txtInfo.Font = new System.Drawing.Font("Consolas", 9.75F);
            txtInfo.Location = new System.Drawing.Point(9, 3);
            txtInfo.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            txtInfo.Multiline = true;
            txtInfo.Name = "txtInfo";
            txtInfo.ReadOnly = true;
            txtInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtInfo.Size = new System.Drawing.Size(700, 451);
            txtInfo.TabIndex = 1;
            // 
            // tabHeader
            // 
            tabHeader.Controls.Add(hexData);
            tabHeader.Location = new System.Drawing.Point(4, 24);
            tabHeader.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabHeader.Name = "tabHeader";
            tabHeader.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabHeader.Size = new System.Drawing.Size(720, 459);
            tabHeader.TabIndex = 0;
            tabHeader.Text = "Header";
            tabHeader.UseVisualStyleBackColor = true;
            // 
            // hexData
            // 
            hexData.AutoScroll = true;
            hexData.BackColor = System.Drawing.Color.White;
            hexData.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            hexData.Dock = System.Windows.Forms.DockStyle.Fill;
            hexData.Font = new System.Drawing.Font("Courier New", 10F);
            hexData.HexData = null;
            hexData.Location = new System.Drawing.Point(4, 3);
            hexData.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            hexData.Name = "hexData";
            hexData.Size = new System.Drawing.Size(712, 453);
            hexData.TabIndex = 0;
            // 
            // tabBody
            // 
            tabBody.Controls.Add(hexExtraData);
            tabBody.Location = new System.Drawing.Point(4, 24);
            tabBody.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabBody.Name = "tabBody";
            tabBody.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabBody.Size = new System.Drawing.Size(720, 459);
            tabBody.TabIndex = 2;
            tabBody.Text = "Body";
            tabBody.UseVisualStyleBackColor = true;
            // 
            // hexExtraData
            // 
            hexExtraData.AutoScroll = true;
            hexExtraData.BackColor = System.Drawing.Color.White;
            hexExtraData.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            hexExtraData.Dock = System.Windows.Forms.DockStyle.Fill;
            hexExtraData.Font = new System.Drawing.Font("Courier New", 10F);
            hexExtraData.HexData = null;
            hexExtraData.Location = new System.Drawing.Point(4, 3);
            hexExtraData.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            hexExtraData.Name = "hexExtraData";
            hexExtraData.Size = new System.Drawing.Size(712, 453);
            hexExtraData.TabIndex = 0;
            // 
            // lblLoading
            // 
            lblLoading.Dock = System.Windows.Forms.DockStyle.Fill;
            lblLoading.Font = new System.Drawing.Font("Courier New", 36F, System.Drawing.FontStyle.Bold);
            lblLoading.Location = new System.Drawing.Point(0, 0);
            lblLoading.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblLoading.Name = "lblLoading";
            lblLoading.Size = new System.Drawing.Size(728, 509);
            lblLoading.TabIndex = 4;
            lblLoading.Text = "Loading";
            lblLoading.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // mnuBar
            // 
            mnuBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { binaryToolStripMenuItem, editStripMenuItem, imageToolStripMenuItem });
            mnuBar.Location = new System.Drawing.Point(0, 0);
            mnuBar.Name = "mnuBar";
            mnuBar.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
            mnuBar.Size = new System.Drawing.Size(728, 24);
            mnuBar.TabIndex = 6;
            mnuBar.Text = "menuStrip1";
            // 
            // binaryToolStripMenuItem
            // 
            binaryToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { importHeaderToolStripMenuItem, exportHeaderToolStripMenuItem, toolStripMenuItem1, importBodyToolStripMenuItem, exportBodyToolStripMenuItem });
            binaryToolStripMenuItem.Name = "binaryToolStripMenuItem";
            binaryToolStripMenuItem.Size = new System.Drawing.Size(52, 20);
            binaryToolStripMenuItem.Text = "Binary";
            // 
            // importHeaderToolStripMenuItem
            // 
            importHeaderToolStripMenuItem.Name = "importHeaderToolStripMenuItem";
            importHeaderToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            importHeaderToolStripMenuItem.Text = "Import Header";
            importHeaderToolStripMenuItem.Click += importDataToolStripMenuItem_Click;
            // 
            // exportHeaderToolStripMenuItem
            // 
            exportHeaderToolStripMenuItem.Name = "exportHeaderToolStripMenuItem";
            exportHeaderToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            exportHeaderToolStripMenuItem.Text = "Export Header";
            exportHeaderToolStripMenuItem.Click += exportDataToolStripMenuItem_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new System.Drawing.Size(148, 6);
            // 
            // importBodyToolStripMenuItem
            // 
            importBodyToolStripMenuItem.Name = "importBodyToolStripMenuItem";
            importBodyToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            importBodyToolStripMenuItem.Text = "Import Body";
            importBodyToolStripMenuItem.Click += importExtraToolStripMenuItem_Click;
            // 
            // exportBodyToolStripMenuItem
            // 
            exportBodyToolStripMenuItem.Name = "exportBodyToolStripMenuItem";
            exportBodyToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            exportBodyToolStripMenuItem.Text = "Export Body";
            exportBodyToolStripMenuItem.Click += exportExtraToolStripMenuItem_Click;
            // 
            // editStripMenuItem
            // 
            editStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { editHashToolStripMenuItem, calcLookupHashToolStripMenuItem });
            editStripMenuItem.Name = "editStripMenuItem";
            editStripMenuItem.Size = new System.Drawing.Size(39, 20);
            editStripMenuItem.Text = "Edit";
            // 
            // editHashToolStripMenuItem
            // 
            editHashToolStripMenuItem.Name = "editHashToolStripMenuItem";
            editHashToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            editHashToolStripMenuItem.Text = "Edit ID";
            editHashToolStripMenuItem.Click += editId_Click;
            // 
            // calcLookupHashToolStripMenuItem
            // 
            calcLookupHashToolStripMenuItem.Name = "calcLookupHashToolStripMenuItem";
            calcLookupHashToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            calcLookupHashToolStripMenuItem.Text = "Calc Lookup8 Hash";
            calcLookupHashToolStripMenuItem.Click += calcLookup8_Click;
            // 
            // imageToolStripMenuItem
            // 
            imageToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { exportToolStripMenuItem, importToolStripMenuItem });
            imageToolStripMenuItem.Name = "imageToolStripMenuItem";
            imageToolStripMenuItem.Size = new System.Drawing.Size(52, 20);
            imageToolStripMenuItem.Text = "Image";
            // 
            // exportToolStripMenuItem
            // 
            exportToolStripMenuItem.Name = "exportToolStripMenuItem";
            exportToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            exportToolStripMenuItem.Text = "Export";
            exportToolStripMenuItem.Click += exportToolStripMenuItem_Click;
            // 
            // importToolStripMenuItem
            // 
            importToolStripMenuItem.Name = "importToolStripMenuItem";
            importToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            importToolStripMenuItem.Text = "Import";
            importToolStripMenuItem.Click += importToolStripMenuItem_Click;
            // 
            // stsMain
            // 
            stsMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { imageStatusLabel });
            stsMain.Location = new System.Drawing.Point(0, 487);
            stsMain.Name = "stsMain";
            stsMain.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
            stsMain.Size = new System.Drawing.Size(728, 22);
            stsMain.TabIndex = 7;
            // 
            // imageStatusLabel
            // 
            imageStatusLabel.Name = "imageStatusLabel";
            imageStatusLabel.Size = new System.Drawing.Size(83, 17);
            imageStatusLabel.Text = "Compression: ";
            imageStatusLabel.Visible = false;
            // 
            // pbMain
            // 
            pbMain.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            pbMain.Location = new System.Drawing.Point(14, 443);
            pbMain.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            pbMain.MarqueeAnimationSpeed = 10;
            pbMain.Name = "pbMain";
            pbMain.Size = new System.Drawing.Size(700, 27);
            pbMain.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            pbMain.TabIndex = 8;
            // 
            // pboImage
            // 
            pboImage.BackColor = System.Drawing.Color.DarkGray;
            pboImage.Dock = System.Windows.Forms.DockStyle.Fill;
            pboImage.Location = new System.Drawing.Point(0, 24);
            pboImage.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            pboImage.Name = "pboImage";
            pboImage.Size = new System.Drawing.Size(728, 463);
            pboImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            pboImage.TabIndex = 9;
            pboImage.TabStop = false;
            // 
            // EntryEditor
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(728, 509);
            Controls.Add(pboImage);
            Controls.Add(mnuBar);
            Controls.Add(tabList);
            Controls.Add(stsMain);
            Controls.Add(pbMain);
            Controls.Add(lblLoading);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = mnuBar;
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MinimizeBox = false;
            MinimumSize = new System.Drawing.Size(347, 225);
            Name = "EntryEditor";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "EntryEditor";
            Shown += EntryEditor_Shown;
            tabList.ResumeLayout(false);
            tabInfo.ResumeLayout(false);
            tabInfo.PerformLayout();
            tabHeader.ResumeLayout(false);
            tabBody.ResumeLayout(false);
            mnuBar.ResumeLayout(false);
            mnuBar.PerformLayout();
            stsMain.ResumeLayout(false);
            stsMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pboImage).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }


        #endregion

        private System.Windows.Forms.TabControl tabList;
        private System.Windows.Forms.TabPage tabBody;
        private System.Windows.Forms.Label lblLoading;
        private System.Windows.Forms.MenuStrip mnuBar;
        private System.Windows.Forms.ToolStripMenuItem editStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editHashToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem calcLookupHashToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem binaryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importHeaderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportHeaderToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem importBodyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportBodyToolStripMenuItem;
        private System.Windows.Forms.StatusStrip stsMain;
        private System.Windows.Forms.ProgressBar pbMain;
        private System.Windows.Forms.TabPage tabHeader;
        private HexEditor.HexView hexData;
        private HexEditor.HexView hexExtraData;
        private System.Windows.Forms.ToolStripMenuItem imageToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importToolStripMenuItem;
        private System.Windows.Forms.PictureBox pboImage;
        private System.Windows.Forms.TabPage tabInfo;
        private System.Windows.Forms.TextBox txtInfo;
        private System.Windows.Forms.ToolStripStatusLabel imageStatusLabel;
    }
}