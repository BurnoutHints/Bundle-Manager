namespace LoopModel
{
    partial class LoopModelEditor
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
            rpmPitchPlot = new ScottPlot.WinForms.FormsPlot();
            editorStatus = new System.Windows.Forms.StatusStrip();
            mainContainer = new System.Windows.Forms.SplitContainer();
            plotsTable = new System.Windows.Forms.TableLayoutPanel();
            rpmGainPlot = new ScottPlot.WinForms.FormsPlot();
            accelGainPlot = new ScottPlot.WinForms.FormsPlot();
            sidebarTable = new System.Windows.Forms.TableLayoutPanel();
            pointsGrid = new System.Windows.Forms.DataGridView();
            pointsGridXPos = new System.Windows.Forms.DataGridViewTextBoxColumn();
            pointsGridYPos = new System.Windows.Forms.DataGridViewTextBoxColumn();
            graphsGrid = new System.Windows.Forms.DataGridView();
            graphName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            partialsGrid = new System.Windows.Forms.DataGridView();
            showPartial = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            waveNameString = new System.Windows.Forms.DataGridViewTextBoxColumn();
            waveNameHash = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)mainContainer).BeginInit();
            mainContainer.Panel1.SuspendLayout();
            mainContainer.Panel2.SuspendLayout();
            mainContainer.SuspendLayout();
            plotsTable.SuspendLayout();
            sidebarTable.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pointsGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)graphsGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)partialsGrid).BeginInit();
            SuspendLayout();
            // 
            // rpmPitchPlot
            // 
            rpmPitchPlot.DisplayScale = 1F;
            rpmPitchPlot.Dock = System.Windows.Forms.DockStyle.Fill;
            rpmPitchPlot.Location = new System.Drawing.Point(3, 3);
            rpmPitchPlot.Name = "rpmPitchPlot";
            rpmPitchPlot.Size = new System.Drawing.Size(1296, 273);
            rpmPitchPlot.TabIndex = 0;
            // 
            // editorStatus
            // 
            editorStatus.Location = new System.Drawing.Point(0, 839);
            editorStatus.Name = "editorStatus";
            editorStatus.Size = new System.Drawing.Size(1584, 22);
            editorStatus.TabIndex = 1;
            editorStatus.Text = "loopModelStatus";
            // 
            // mainContainer
            // 
            mainContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            mainContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            mainContainer.Location = new System.Drawing.Point(0, 0);
            mainContainer.Name = "mainContainer";
            // 
            // mainContainer.Panel1
            // 
            mainContainer.Panel1.Controls.Add(plotsTable);
            // 
            // mainContainer.Panel2
            // 
            mainContainer.Panel2.Controls.Add(sidebarTable);
            mainContainer.Size = new System.Drawing.Size(1584, 839);
            mainContainer.SplitterDistance = 1302;
            mainContainer.TabIndex = 2;
            // 
            // plotsTable
            // 
            plotsTable.ColumnCount = 1;
            plotsTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            plotsTable.Controls.Add(rpmPitchPlot, 0, 0);
            plotsTable.Controls.Add(rpmGainPlot, 0, 1);
            plotsTable.Controls.Add(accelGainPlot, 0, 2);
            plotsTable.Dock = System.Windows.Forms.DockStyle.Fill;
            plotsTable.Location = new System.Drawing.Point(0, 0);
            plotsTable.Name = "plotsTable";
            plotsTable.RowCount = 3;
            plotsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.3333321F));
            plotsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.3333359F));
            plotsTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.3333359F));
            plotsTable.Size = new System.Drawing.Size(1302, 839);
            plotsTable.TabIndex = 1;
            // 
            // rpmGainPlot
            // 
            rpmGainPlot.DisplayScale = 1F;
            rpmGainPlot.Dock = System.Windows.Forms.DockStyle.Fill;
            rpmGainPlot.Location = new System.Drawing.Point(3, 282);
            rpmGainPlot.Name = "rpmGainPlot";
            rpmGainPlot.Size = new System.Drawing.Size(1296, 273);
            rpmGainPlot.TabIndex = 1;
            // 
            // accelGainPlot
            // 
            accelGainPlot.DisplayScale = 1F;
            accelGainPlot.Dock = System.Windows.Forms.DockStyle.Fill;
            accelGainPlot.Location = new System.Drawing.Point(3, 561);
            accelGainPlot.Name = "accelGainPlot";
            accelGainPlot.Size = new System.Drawing.Size(1296, 275);
            accelGainPlot.TabIndex = 2;
            // 
            // sidebarTable
            // 
            sidebarTable.ColumnCount = 1;
            sidebarTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            sidebarTable.Controls.Add(pointsGrid, 0, 2);
            sidebarTable.Controls.Add(graphsGrid, 0, 1);
            sidebarTable.Controls.Add(partialsGrid, 0, 0);
            sidebarTable.Dock = System.Windows.Forms.DockStyle.Fill;
            sidebarTable.Location = new System.Drawing.Point(0, 0);
            sidebarTable.Name = "sidebarTable";
            sidebarTable.RowCount = 3;
            sidebarTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.3333321F));
            sidebarTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.3333359F));
            sidebarTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.3333359F));
            sidebarTable.Size = new System.Drawing.Size(278, 839);
            sidebarTable.TabIndex = 3;
            // 
            // pointsGrid
            // 
            pointsGrid.AllowUserToAddRows = false;
            pointsGrid.AllowUserToDeleteRows = false;
            pointsGrid.AllowUserToResizeRows = false;
            pointsGrid.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            pointsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            pointsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { pointsGridXPos, pointsGridYPos });
            pointsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            pointsGrid.Location = new System.Drawing.Point(3, 561);
            pointsGrid.MultiSelect = false;
            pointsGrid.Name = "pointsGrid";
            pointsGrid.RowHeadersVisible = false;
            pointsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            pointsGrid.Size = new System.Drawing.Size(272, 275);
            pointsGrid.TabIndex = 1;
            pointsGrid.CellEndEdit += pointsGrid_CellEndEdit;
            // 
            // pointsGridXPos
            // 
            pointsGridXPos.HeaderText = "X";
            pointsGridXPos.Name = "pointsGridXPos";
            pointsGridXPos.Width = 90;
            // 
            // pointsGridYPos
            // 
            pointsGridYPos.HeaderText = "Y";
            pointsGridYPos.Name = "pointsGridYPos";
            pointsGridYPos.Width = 90;
            // 
            // graphsGrid
            // 
            graphsGrid.AllowUserToAddRows = false;
            graphsGrid.AllowUserToDeleteRows = false;
            graphsGrid.AllowUserToResizeRows = false;
            graphsGrid.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            graphsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            graphsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { graphName });
            graphsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            graphsGrid.Location = new System.Drawing.Point(3, 282);
            graphsGrid.MultiSelect = false;
            graphsGrid.Name = "graphsGrid";
            graphsGrid.ReadOnly = true;
            graphsGrid.RowHeadersVisible = false;
            graphsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            graphsGrid.Size = new System.Drawing.Size(272, 273);
            graphsGrid.TabIndex = 1;
            graphsGrid.RowEnter += graphsGrid_RowEnter;
            // 
            // graphName
            // 
            graphName.HeaderText = "Graph";
            graphName.Name = "graphName";
            graphName.ReadOnly = true;
            graphName.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            graphName.Width = 180;
            // 
            // partialsGrid
            // 
            partialsGrid.AllowUserToAddRows = false;
            partialsGrid.AllowUserToDeleteRows = false;
            partialsGrid.AllowUserToResizeRows = false;
            partialsGrid.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            partialsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            partialsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { showPartial, waveNameString, waveNameHash });
            partialsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            partialsGrid.Location = new System.Drawing.Point(3, 3);
            partialsGrid.MultiSelect = false;
            partialsGrid.Name = "partialsGrid";
            partialsGrid.RowHeadersVisible = false;
            partialsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            partialsGrid.Size = new System.Drawing.Size(272, 273);
            partialsGrid.TabIndex = 1;
            partialsGrid.CellValueChanged += partialsGrid_CellValueChanged;
            partialsGrid.CurrentCellDirtyStateChanged += partialsGrid_CurrentCellDirtyStateChanged;
            partialsGrid.RowEnter += partialsGrid_RowEnter;
            // 
            // showPartial
            // 
            showPartial.HeaderText = "Show";
            showPartial.Name = "showPartial";
            showPartial.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            showPartial.Width = 50;
            // 
            // waveNameString
            // 
            waveNameString.HeaderText = "Wave Name";
            waveNameString.Name = "waveNameString";
            waveNameString.ReadOnly = true;
            waveNameString.Width = 200;
            // 
            // waveNameHash
            // 
            waveNameHash.HeaderText = "Hash";
            waveNameHash.Name = "waveNameHash";
            waveNameHash.ReadOnly = true;
            // 
            // LoopModelEditor
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1584, 861);
            Controls.Add(mainContainer);
            Controls.Add(editorStatus);
            Name = "LoopModelEditor";
            Text = "Loop Model";
            FormClosed += LoopModelEditor_FormClosed;
            mainContainer.Panel1.ResumeLayout(false);
            mainContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)mainContainer).EndInit();
            mainContainer.ResumeLayout(false);
            plotsTable.ResumeLayout(false);
            sidebarTable.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pointsGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)graphsGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)partialsGrid).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ScottPlot.WinForms.FormsPlot rpmPitchPlot;
        private System.Windows.Forms.StatusStrip editorStatus;
        private System.Windows.Forms.SplitContainer mainContainer;
        private System.Windows.Forms.ListView graphsList;
        private System.Windows.Forms.ColumnHeader index;
        private System.Windows.Forms.TableLayoutPanel sidebarTable;
        private System.Windows.Forms.DataGridView partialsGrid;
        private System.Windows.Forms.DataGridView pointsGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn pointsGridXPos;
        private System.Windows.Forms.DataGridViewTextBoxColumn pointsGridYPos;
        private System.Windows.Forms.TableLayoutPanel plotsTable;
        private ScottPlot.WinForms.FormsPlot rpmGainPlot;
        private ScottPlot.WinForms.FormsPlot accelGainPlot;
        private System.Windows.Forms.DataGridView graphsGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn graphName;
        private System.Windows.Forms.DataGridViewCheckBoxColumn showPartial;
        private System.Windows.Forms.DataGridViewTextBoxColumn waveNameString;
        private System.Windows.Forms.DataGridViewTextBoxColumn waveNameHash;
    }
}
