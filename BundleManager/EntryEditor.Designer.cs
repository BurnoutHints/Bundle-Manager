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
            tabPrimary = new System.Windows.Forms.TabPage();
            hexData = new HexEditor.HexView();
            tabSecondary = new System.Windows.Forms.TabPage();
            hexExtraData = new HexEditor.HexView();
            valuesData = new HexEditor.ValueView();
            valuesExtraData = new HexEditor.ValueView();
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
            editSeparator = new System.Windows.Forms.ToolStripSeparator();
            editModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            goToOffsetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tabImports = new System.Windows.Forms.TabPage();
            importsView = new ImportsView();
            imageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            exportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            importToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            stsMain = new System.Windows.Forms.StatusStrip();
            statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            pbMain = new System.Windows.Forms.ProgressBar();
            pboImage = new System.Windows.Forms.PictureBox();
            tabList.SuspendLayout();
            tabInfo.SuspendLayout();
            tabPrimary.SuspendLayout();
            tabSecondary.SuspendLayout();
            tabImports.SuspendLayout();
            mnuBar.SuspendLayout();
            stsMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pboImage).BeginInit();
            SuspendLayout();
            // 
            // tabList
            // 
            tabList.Controls.Add(tabInfo);
            tabList.Controls.Add(tabImports);
            tabList.Controls.Add(tabPrimary);
            tabList.Controls.Add(tabSecondary);
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
            // tabPrimary
            // 
            tabPrimary.Controls.Add(hexData);
            tabPrimary.Controls.Add(valuesData);
            tabPrimary.Location = new System.Drawing.Point(4, 24);
            tabPrimary.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabPrimary.Name = "tabPrimary";
            tabPrimary.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabPrimary.Size = new System.Drawing.Size(720, 459);
            tabPrimary.TabIndex = 0;
            tabPrimary.Text = "Primary";
            tabPrimary.UseVisualStyleBackColor = true;
            // 
            // hexData
            // 
            hexData.AutoScroll = true;
            hexData.BackColor = System.Drawing.SystemColors.Window;
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
            // valuesData
            // 
            valuesData.Dock = System.Windows.Forms.DockStyle.Right;
            valuesData.Location = new System.Drawing.Point(376, 3);
            valuesData.Name = "valuesData";
            valuesData.Size = new System.Drawing.Size(340, 453);
            valuesData.TabIndex = 1;
            // 
            // tabSecondary
            // 
            tabSecondary.Controls.Add(hexExtraData);
            tabSecondary.Controls.Add(valuesExtraData);
            tabSecondary.Location = new System.Drawing.Point(4, 24);
            tabSecondary.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabSecondary.Name = "tabSecondary";
            tabSecondary.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabSecondary.Size = new System.Drawing.Size(720, 459);
            tabSecondary.TabIndex = 2;
            tabSecondary.Text = "Secondary";
            tabSecondary.UseVisualStyleBackColor = true;
            // 
            // hexExtraData
            // 
            hexExtraData.AutoScroll = true;
            hexExtraData.BackColor = System.Drawing.SystemColors.Window;
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
            // valuesExtraData
            // 
            valuesExtraData.Dock = System.Windows.Forms.DockStyle.Right;
            valuesExtraData.Location = new System.Drawing.Point(376, 3);
            valuesExtraData.Name = "valuesExtraData";
            valuesExtraData.Size = new System.Drawing.Size(340, 453);
            valuesExtraData.TabIndex = 1;
            //
            // tabImports
            //
            tabImports.Controls.Add(importsView);
            tabImports.Location = new System.Drawing.Point(4, 24);
            tabImports.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabImports.Name = "tabImports";
            tabImports.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            tabImports.Size = new System.Drawing.Size(720, 459);
            tabImports.TabIndex = 4;
            tabImports.Text = "Imports";
            tabImports.UseVisualStyleBackColor = true;
            //
            // importsView
            //
            importsView.Dock = System.Windows.Forms.DockStyle.Fill;
            importsView.Location = new System.Drawing.Point(4, 3);
            importsView.Name = "importsView";
            importsView.Size = new System.Drawing.Size(712, 453);
            importsView.TabIndex = 0;
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
            importHeaderToolStripMenuItem.Text = "Import Primary Data";
            importHeaderToolStripMenuItem.Click += importDataToolStripMenuItem_Click;
            // 
            // exportHeaderToolStripMenuItem
            // 
            exportHeaderToolStripMenuItem.Name = "exportHeaderToolStripMenuItem";
            exportHeaderToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            exportHeaderToolStripMenuItem.Text = "Export Primary Data";
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
            importBodyToolStripMenuItem.Text = "Import Secondary Data";
            importBodyToolStripMenuItem.Click += importExtraToolStripMenuItem_Click;
            // 
            // exportBodyToolStripMenuItem
            // 
            exportBodyToolStripMenuItem.Name = "exportBodyToolStripMenuItem";
            exportBodyToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
            exportBodyToolStripMenuItem.Text = "Export Secondary Data";
            exportBodyToolStripMenuItem.Click += exportExtraToolStripMenuItem_Click;
            // 
            // editStripMenuItem
            // 
            editStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { editHashToolStripMenuItem, calcLookupHashToolStripMenuItem, editSeparator, editModeToolStripMenuItem, goToOffsetToolStripMenuItem });
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
            // editSeparator
            //
            editSeparator.Name = "editSeparator";
            editSeparator.Size = new System.Drawing.Size(173, 6);
            //
            // editModeToolStripMenuItem
            //
            editModeToolStripMenuItem.CheckOnClick = true;
            editModeToolStripMenuItem.Name = "editModeToolStripMenuItem";
            editModeToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E;
            editModeToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            editModeToolStripMenuItem.Text = "Edit Mode";
            editModeToolStripMenuItem.ToolTipText = "Edit the primary and secondary data as hex.";
            editModeToolStripMenuItem.CheckedChanged += editModeToolStripMenuItem_CheckedChanged;
            //
            // goToOffsetToolStripMenuItem
            //
            goToOffsetToolStripMenuItem.Name = "goToOffsetToolStripMenuItem";
            goToOffsetToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.G;
            goToOffsetToolStripMenuItem.Size = new System.Drawing.Size(176, 22);
            goToOffsetToolStripMenuItem.Text = "Go to Offset...";
            goToOffsetToolStripMenuItem.Click += goToOffsetToolStripMenuItem_Click;
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
            stsMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { statusLabel });
            stsMain.Location = new System.Drawing.Point(0, 487);
            stsMain.Name = "stsMain";
            stsMain.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
            stsMain.Size = new System.Drawing.Size(728, 22);
            stsMain.TabIndex = 7;
            // 
            // statusLabel
            // 
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new System.Drawing.Size(83, 17);
            statusLabel.Text = "Compression: ";
            statusLabel.Visible = false;
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
            ClientSize = new System.Drawing.Size(1000, 560);
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
            tabPrimary.ResumeLayout(false);
            tabSecondary.ResumeLayout(false);
            tabImports.ResumeLayout(false);
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
        private System.Windows.Forms.TabPage tabSecondary;
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
        private System.Windows.Forms.TabPage tabPrimary;
        private HexEditor.HexView hexData;
        private HexEditor.HexView hexExtraData;
        private HexEditor.ValueView valuesData;
        private HexEditor.ValueView valuesExtraData;
        private System.Windows.Forms.ToolStripMenuItem imageToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exportToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importToolStripMenuItem;
        private System.Windows.Forms.PictureBox pboImage;
        private System.Windows.Forms.TabPage tabInfo;
        private System.Windows.Forms.TextBox txtInfo;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.ToolStripSeparator editSeparator;
        private System.Windows.Forms.ToolStripMenuItem editModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem goToOffsetToolStripMenuItem;
        private System.Windows.Forms.TabPage tabImports;
        private ImportsView importsView;
    }
}