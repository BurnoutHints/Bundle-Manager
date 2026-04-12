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

            menu = new MenuStrip();
            fileMenu = new ToolStripMenuItem();
            exportJsonItem = new ToolStripMenuItem();

            tabs = new TabControl();
            streetsPage = new TabPage();
            junctionsPage = new TabPage();
            roadsPage = new TabPage();
            challengesPage = new TabPage();

            streetsList = new ListView();
            junctionsList = new ListView();
            roadsList = new ListView();
            challengesList = new ListView();

            streetsContextMenu = new ContextMenuStrip(components);
            junctionsContextMenu = new ContextMenuStrip(components);
            roadsContextMenu = new ContextMenuStrip(components);
            challengesContextMenu = new ContextMenuStrip(components);

            propertyGrid = new PropertyGrid();

            menu.SuspendLayout();
            tabs.SuspendLayout();
            streetsPage.SuspendLayout();
            junctionsPage.SuspendLayout();
            roadsPage.SuspendLayout();
            challengesPage.SuspendLayout();
            SuspendLayout();
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
            fileMenu.Size = new Size(38, 20);
            fileMenu.Text = "File";
            //
            // exportJsonItem
            //
            exportJsonItem.Name = "exportJsonItem";
            exportJsonItem.Size = new Size(180, 22);
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
            tabs.Size = new Size(1020, 376);
            tabs.TabIndex = 1;
            //
            // streetsPage
            //
            streetsPage.Controls.Add(streetsList);
            streetsPage.Name = "streetsPage";
            streetsPage.Text = "Streets";
            //
            // junctionsPage
            //
            junctionsPage.Controls.Add(junctionsList);
            junctionsPage.Name = "junctionsPage";
            junctionsPage.Text = "Junctions";
            //
            // roadsPage
            //
            roadsPage.Controls.Add(roadsList);
            roadsPage.Name = "roadsPage";
            roadsPage.Text = "Roads";
            //
            // challengesPage
            //
            challengesPage.Controls.Add(challengesList);
            challengesPage.Name = "challengesPage";
            challengesPage.Text = "Challenges";
            //
            // streetsList
            //
            ConfigureListView(streetsList, streetsContextMenu, "streetsList",
                new[] { "#", "RoadIndex", "SpanIndex", "SpanType", "MaxSpeed", "MinSpeed" });
            //
            // junctionsList
            //
            ConfigureListView(junctionsList, junctionsContextMenu, "junctionsList",
                new[] { "#", "RoadIndex", "SpanIndex", "SpanType", "Name" });
            //
            // roadsList
            //
            ConfigureListView(roadsList, roadsContextMenu, "roadsList",
                new[] { "#", "DebugName", "RefX", "RefY", "RefZ", "Challenge", "GameDB ID" });
            //
            // challengesList
            //
            ConfigureListView(challengesList, challengesContextMenu, "challengesList",
                new[] { "#", "Score 0", "Score 1", "Rival 0", "Rival 1" });
            //
            // propertyGrid
            //
            propertyGrid.Dock = DockStyle.Bottom;
            propertyGrid.Name = "propertyGrid";
            propertyGrid.Size = new Size(1020, 320);
            propertyGrid.TabIndex = 2;
            //
            // StreetDataEditor
            //
            AutoScaleDimensions = new SizeF(7F, 16F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1020, 720);
            Controls.Add(tabs);
            Controls.Add(propertyGrid);
            Controls.Add(menu);
            MainMenuStrip = menu;
            MinimumSize = new Size(720, 500);
            Name = "StreetDataEditor";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Street Data Editor";
            FormClosed += StreetDataEditor_FormClosed;

            menu.ResumeLayout(false);
            menu.PerformLayout();
            streetsPage.ResumeLayout(false);
            junctionsPage.ResumeLayout(false);
            roadsPage.ResumeLayout(false);
            challengesPage.ResumeLayout(false);
            tabs.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        // Shared ListView setup. Hand-written rather than declarative-per-list
        // because four list views with the same configuration would otherwise
        // be ~30 boilerplate lines each.
        private void ConfigureListView(ListView list, ContextMenuStrip ctxMenu, string name, string[] columnHeaders)
        {
            list.ContextMenuStrip = ctxMenu;
            list.Dock = DockStyle.Fill;
            list.FullRowSelect = true;
            list.GridLines = true;
            list.HideSelection = false;
            list.MultiSelect = false;
            list.Name = name;
            list.UseCompatibleStateImageBehavior = false;
            list.View = View.Details;
            list.AllowColumnReorder = true;
            for (int i = 0; i < columnHeaders.Length; i++)
                list.Columns.Add(columnHeaders[i], -2, HorizontalAlignment.Left);
            list.ColumnClick += list_ColumnClick;
            list.DoubleClick += list_DoubleClick;
            list.KeyDown += list_KeyDown;
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
    }
}
