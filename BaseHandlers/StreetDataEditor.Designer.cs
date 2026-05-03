using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace BaseHandlers
{
    partial class StreetDataEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            components = new Container();
            streetNumber = new ColumnHeader();
            streetRoadIndex = new ColumnHeader();
            streetSpanIndex = new ColumnHeader();
            streetSpanType = new ColumnHeader();
            streetMaxSpeed = new ColumnHeader();
            streetMinSpeed = new ColumnHeader();
            menu = new MenuStrip();
            fileMenu = new ToolStripMenuItem();
            exportJsonItem = new ToolStripMenuItem();
            tabs = new TabControl();
            streetsPage = new TabPage();
            streetsList = new ListView();
            streetsContextMenu = new ContextMenuStrip(components);
            junctionsPage = new TabPage();
            junctionsList = new ListView();
            junctionNumber = new ColumnHeader();
            junctionRoadIndex = new ColumnHeader();
            junctionSpanIndex = new ColumnHeader();
            junctionSpanType = new ColumnHeader();
            junctionName = new ColumnHeader();
            junctionsContextMenu = new ContextMenuStrip(components);
            roadsPage = new TabPage();
            roadsList = new ListView();
            roadNumber = new ColumnHeader();
            roadDebugName = new ColumnHeader();
            roadRefX = new ColumnHeader();
            roadRefY = new ColumnHeader();
            roadRefZ = new ColumnHeader();
            roadChallenge = new ColumnHeader();
            roadGamedbId = new ColumnHeader();
            roadsContextMenu = new ContextMenuStrip(components);
            challengesPage = new TabPage();
            challengesList = new ListView();
            challengesContextMenu = new ContextMenuStrip(components);
            propertyGrid = new PropertyGrid();
            challengeNumber = new ColumnHeader();
            challengeScore0 = new ColumnHeader();
            challengeScore1 = new ColumnHeader();
            challengeRival0 = new ColumnHeader();
            challengeRival1 = new ColumnHeader();
            menu.SuspendLayout();
            tabs.SuspendLayout();
            streetsPage.SuspendLayout();
            junctionsPage.SuspendLayout();
            roadsPage.SuspendLayout();
            challengesPage.SuspendLayout();
            SuspendLayout();
            // 
            // streetNumber
            // 
            streetNumber.Text = "#";
            // 
            // streetRoadIndex
            // 
            streetRoadIndex.Text = "ReadIndex";
            // 
            // streetSpanIndex
            // 
            streetSpanIndex.Text = "SpanIndex";
            // 
            // streetSpanType
            // 
            streetSpanType.Text = "SpanType";
            // 
            // streetMaxSpeed
            // 
            streetMaxSpeed.Text = "MaxSpeed";
            // 
            // streetMinSpeed
            // 
            streetMinSpeed.Text = "MinSpeed";
            // 
            // menu
            // 
            menu.Items.AddRange(new ToolStripItem[] { fileMenu });
            menu.Location = new Point(0, 0);
            menu.Name = "menu";
            menu.Size = new Size(1020, 24);
            menu.TabIndex = 0;
            // 
            // fileMenu
            // 
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { exportJsonItem });
            fileMenu.Name = "fileMenu";
            fileMenu.Size = new Size(37, 20);
            fileMenu.Text = "File";
            // 
            // exportJsonItem
            // 
            exportJsonItem.Name = "exportJsonItem";
            exportJsonItem.Size = new Size(153, 22);
            exportJsonItem.Text = "Export to JSON";
            exportJsonItem.Click += exportJsonItem_Click;
            // 
            // tabs
            // 
            tabs.Controls.Add(streetsPage);
            tabs.Controls.Add(junctionsPage);
            tabs.Controls.Add(roadsPage);
            tabs.Controls.Add(challengesPage);
            tabs.Dock = DockStyle.Fill;
            tabs.Location = new Point(0, 24);
            tabs.Name = "tabs";
            tabs.SelectedIndex = 0;
            tabs.Size = new Size(1020, 351);
            tabs.TabIndex = 1;
            // 
            // streetsPage
            // 
            streetsPage.Controls.Add(streetsList);
            streetsPage.Location = new Point(4, 24);
            streetsPage.Name = "streetsPage";
            streetsPage.Size = new Size(1012, 323);
            streetsPage.TabIndex = 0;
            streetsPage.Text = "Streets";
            // 
            // streetsList
            // 
            streetsList.AllowColumnReorder = true;
            streetsList.Columns.AddRange(new ColumnHeader[] { streetNumber, streetRoadIndex, streetSpanIndex, streetSpanType, streetMaxSpeed, streetMinSpeed });
            streetsList.ContextMenuStrip = streetsContextMenu;
            streetsList.Dock = DockStyle.Fill;
            streetsList.FullRowSelect = true;
            streetsList.GridLines = true;
            streetsList.Location = new Point(0, 0);
            streetsList.MultiSelect = false;
            streetsList.Name = "streetsList";
            streetsList.Size = new Size(1012, 323);
            streetsList.TabIndex = 0;
            streetsList.UseCompatibleStateImageBehavior = false;
            streetsList.View = View.Details;
            streetsList.ColumnClick += list_ColumnClick;
            streetsList.DoubleClick += list_DoubleClick;
            streetsList.KeyDown += list_KeyDown;
            // 
            // streetsContextMenu
            // 
            streetsContextMenu.Name = "streetsContextMenu";
            streetsContextMenu.Size = new Size(61, 4);
            // 
            // junctionsPage
            // 
            junctionsPage.Controls.Add(junctionsList);
            junctionsPage.Location = new Point(4, 24);
            junctionsPage.Name = "junctionsPage";
            junctionsPage.Size = new Size(1012, 323);
            junctionsPage.TabIndex = 1;
            junctionsPage.Text = "Junctions";
            // 
            // junctionsList
            // 
            junctionsList.AllowColumnReorder = true;
            junctionsList.Columns.AddRange(new ColumnHeader[] { junctionNumber, junctionRoadIndex, junctionSpanIndex, junctionSpanType, junctionName });
            junctionsList.ContextMenuStrip = junctionsContextMenu;
            junctionsList.Dock = DockStyle.Fill;
            junctionsList.FullRowSelect = true;
            junctionsList.GridLines = true;
            junctionsList.Location = new Point(0, 0);
            junctionsList.MultiSelect = false;
            junctionsList.Name = "junctionsList";
            junctionsList.Size = new Size(1012, 323);
            junctionsList.TabIndex = 0;
            junctionsList.UseCompatibleStateImageBehavior = false;
            junctionsList.View = View.Details;
            junctionsList.ColumnClick += list_ColumnClick;
            junctionsList.DoubleClick += list_DoubleClick;
            junctionsList.KeyDown += list_KeyDown;
            // 
            // junctionNumber
            // 
            junctionNumber.Text = "#";
            // 
            // junctionRoadIndex
            // 
            junctionRoadIndex.Text = "RoadIndex";
            // 
            // junctionSpanIndex
            // 
            junctionSpanIndex.Text = "SpanIndex";
            // 
            // junctionSpanType
            // 
            junctionSpanType.Text = "SpanType";
            // 
            // junctionName
            // 
            junctionName.Text = "Name";
            // 
            // junctionsContextMenu
            // 
            junctionsContextMenu.Name = "junctionsContextMenu";
            junctionsContextMenu.Size = new Size(61, 4);
            // 
            // roadsPage
            // 
            roadsPage.Controls.Add(roadsList);
            roadsPage.Location = new Point(4, 24);
            roadsPage.Name = "roadsPage";
            roadsPage.Size = new Size(1012, 323);
            roadsPage.TabIndex = 2;
            roadsPage.Text = "Roads";
            // 
            // roadsList
            // 
            roadsList.AllowColumnReorder = true;
            roadsList.Columns.AddRange(new ColumnHeader[] { roadNumber, roadDebugName, roadRefX, roadRefY, roadRefZ, roadChallenge, roadGamedbId });
            roadsList.ContextMenuStrip = roadsContextMenu;
            roadsList.Dock = DockStyle.Fill;
            roadsList.FullRowSelect = true;
            roadsList.GridLines = true;
            roadsList.Location = new Point(0, 0);
            roadsList.MultiSelect = false;
            roadsList.Name = "roadsList";
            roadsList.Size = new Size(1012, 323);
            roadsList.TabIndex = 0;
            roadsList.UseCompatibleStateImageBehavior = false;
            roadsList.View = View.Details;
            roadsList.ColumnClick += list_ColumnClick;
            roadsList.DoubleClick += list_DoubleClick;
            roadsList.KeyDown += list_KeyDown;
            // 
            // roadNumber
            // 
            roadNumber.Text = "#";
            // 
            // roadDebugName
            // 
            roadDebugName.Text = "DebugName";
            // 
            // roadRefX
            // 
            roadRefX.Text = "RefX";
            // 
            // roadRefY
            // 
            roadRefY.Text = "RefY";
            // 
            // roadRefZ
            // 
            roadRefZ.Text = "RefZ";
            // 
            // roadChallenge
            // 
            roadChallenge.Text = "Challenge";
            // 
            // roadGamedbId
            // 
            roadGamedbId.Text = "GameDB ID";
            // 
            // roadsContextMenu
            // 
            roadsContextMenu.Name = "roadsContextMenu";
            roadsContextMenu.Size = new Size(61, 4);
            // 
            // challengesPage
            // 
            challengesPage.Controls.Add(challengesList);
            challengesPage.Location = new Point(4, 24);
            challengesPage.Name = "challengesPage";
            challengesPage.Size = new Size(1012, 323);
            challengesPage.TabIndex = 3;
            challengesPage.Text = "Challenges";
            // 
            // challengesList
            // 
            challengesList.AllowColumnReorder = true;
            challengesList.Columns.AddRange(new ColumnHeader[] { challengeNumber, challengeScore0, challengeScore1, challengeRival0, challengeRival1 });
            challengesList.ContextMenuStrip = challengesContextMenu;
            challengesList.Dock = DockStyle.Fill;
            challengesList.FullRowSelect = true;
            challengesList.GridLines = true;
            challengesList.Location = new Point(0, 0);
            challengesList.MultiSelect = false;
            challengesList.Name = "challengesList";
            challengesList.Size = new Size(1012, 323);
            challengesList.TabIndex = 0;
            challengesList.UseCompatibleStateImageBehavior = false;
            challengesList.View = View.Details;
            challengesList.ColumnClick += list_ColumnClick;
            challengesList.DoubleClick += list_DoubleClick;
            challengesList.KeyDown += list_KeyDown;
            // 
            // challengesContextMenu
            // 
            challengesContextMenu.Name = "challengesContextMenu";
            challengesContextMenu.Size = new Size(61, 4);
            // 
            // propertyGrid
            // 
            propertyGrid.Dock = DockStyle.Bottom;
            propertyGrid.Location = new Point(0, 375);
            propertyGrid.Name = "propertyGrid";
            propertyGrid.Size = new Size(1020, 300);
            propertyGrid.TabIndex = 2;
            // 
            // challengeNumber
            // 
            challengeNumber.Text = "#";
            // 
            // challengeScore0
            // 
            challengeScore0.Text = "Score 0";
            // 
            // challengeScore1
            // 
            challengeScore1.Text = "Score 1";
            // 
            // challengeRival0
            // 
            challengeRival0.Text = "Rival 0";
            // 
            // challengeRival1
            // 
            challengeRival1.Text = "Rival 1";
            // 
            // StreetDataEditor
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1020, 675);
            Controls.Add(tabs);
            Controls.Add(propertyGrid);
            Controls.Add(menu);
            MainMenuStrip = menu;
            MinimumSize = new Size(720, 471);
            Name = "StreetDataEditor";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Street Data Editor";
            FormClosed += StreetDataEditor_FormClosed;
            menu.ResumeLayout(false);
            menu.PerformLayout();
            tabs.ResumeLayout(false);
            streetsPage.ResumeLayout(false);
            junctionsPage.ResumeLayout(false);
            roadsPage.ResumeLayout(false);
            challengesPage.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menu;
        private ToolStripMenuItem fileMenu;
        private ToolStripMenuItem exportJsonItem;

        private TabControl tabs;
        private TabPage streetsPage;
        private TabPage junctionsPage;
        private TabPage roadsPage;
        private TabPage challengesPage;

        private ListView streetsList;
        private ListView junctionsList;
        private ListView roadsList;
        private ListView challengesList;

        private ContextMenuStrip streetsContextMenu;
        private ContextMenuStrip junctionsContextMenu;
        private ContextMenuStrip roadsContextMenu;
        private ContextMenuStrip challengesContextMenu;

        private PropertyGrid propertyGrid;
        private ColumnHeader streetNumber;
        private ColumnHeader streetRoadIndex;
        private ColumnHeader streetSpanIndex;
        private ColumnHeader streetSpanType;
        private ColumnHeader streetMaxSpeed;
        private ColumnHeader streetMinSpeed;
        private ColumnHeader junctionNumber;
        private ColumnHeader junctionRoadIndex;
        private ColumnHeader junctionSpanIndex;
        private ColumnHeader junctionSpanType;
        private ColumnHeader junctionName;
        private ColumnHeader roadNumber;
        private ColumnHeader roadDebugName;
        private ColumnHeader roadRefX;
        private ColumnHeader roadRefY;
        private ColumnHeader roadRefZ;
        private ColumnHeader roadChallenge;
        private ColumnHeader roadGamedbId;
        private ColumnHeader challengeNumber;
        private ColumnHeader challengeScore0;
        private ColumnHeader challengeScore1;
        private ColumnHeader challengeRival0;
        private ColumnHeader challengeRival1;
    }
}
