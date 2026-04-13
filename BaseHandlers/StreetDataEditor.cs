using PluginAPI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace BaseHandlers
{
    public partial class StreetDataEditor : Form, IEntryEditor
    {
        public event Notify EditEvent;

        private StreetData _model;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public StreetData Model
        {
            get => _model;
            set
            {
                _model = value;
                Rebuild();
            }
        }

        private TabContext _streetsCtx;
        private TabContext _junctionsCtx;
        private TabContext _roadsCtx;
        private TabContext _challengesCtx;

        public StreetDataEditor()
        {
            InitializeComponent();
            _streetsCtx    = new TabContext { View = streetsList,    ContextMenu = streetsContextMenu };
            _junctionsCtx  = new TabContext { View = junctionsList,  ContextMenu = junctionsContextMenu };
            _roadsCtx      = new TabContext { View = roadsList,      ContextMenu = roadsContextMenu };
            _challengesCtx = new TabContext { View = challengesList, ContextMenu = challengesContextMenu };
        }

        private sealed class TabContext
        {
            public ListView View;
            public ContextMenuStrip ContextMenu;
            public Func<int, object> Get;
            public Action Repopulate;
            public Func<int, string[]> Format;
            public Action AddNew;
            public Action<int> Remove;
            public Action<int> Duplicate;
        }

        private void Rebuild()
        {
            streetsList.Items.Clear();
            junctionsList.Items.Clear();
            roadsList.Items.Clear();
            challengesList.Items.Clear();
            propertyGrid.SelectedObject = null;

            if (_model == null) return;

            ConfigureStreetsTab();
            ConfigureJunctionsTab();
            ConfigureRoadsTab();
            ConfigureChallengesTab();

            _streetsCtx.Repopulate();
            _junctionsCtx.Repopulate();
            _roadsCtx.Repopulate();
            _challengesCtx.Repopulate();

            UpdateTabTitles();
        }

        private void ConfigureStreetsTab()
        {
            TabContext ctx = _streetsCtx;
            ctx.Get = i => _model.streets[i];
            ctx.Format = i =>
            {
                Street s = _model.streets[i];
                return new[]
                {
                    i.ToString(),
                    s.super_SpanBase.miRoadIndex.ToString(),
                    s.super_SpanBase.miSpanIndex.ToString(),
                    s.super_SpanBase.meSpanType.ToString(),
                    s.mAiInfo.muMaxSpeedMPS.ToString(),
                    s.mAiInfo.muMinSpeedMPS.ToString(),
                };
            };
            ctx.Repopulate = () => RepopulateView(ctx, _model.streets.Count);
            ctx.AddNew = () => _model.streets.Add(new Street());
            ctx.Remove = i => _model.streets.RemoveAt(i);
            ctx.Duplicate = i =>
            {
                Street src = _model.streets[i];
                _model.streets.Insert(i + 1, new Street
                {
                    super_SpanBase = new SpanBase
                    {
                        miRoadIndex = src.super_SpanBase.miRoadIndex,
                        miSpanIndex = src.super_SpanBase.miSpanIndex,
                        meSpanType = src.super_SpanBase.meSpanType,
                    },
                    mAiInfo = new AIInfo
                    {
                        muMaxSpeedMPS = src.mAiInfo.muMaxSpeedMPS,
                        muMinSpeedMPS = src.mAiInfo.muMinSpeedMPS,
                    },
                });
            };
            BuildContextMenu(ctx);
        }

        private void ConfigureJunctionsTab()
        {
            TabContext ctx = _junctionsCtx;
            ctx.Get = i => _model.junctions[i];
            ctx.Format = i =>
            {
                Junction j = _model.junctions[i];
                // SpanBase.miRoadIndex is always -1 for junctions; display
                // "-" so the column reads as n/a.
                int roadIdx = j.super_SpanBase.miRoadIndex;
                return new[]
                {
                    i.ToString(),
                    roadIdx == -1 ? "-" : roadIdx.ToString(),
                    j.super_SpanBase.miSpanIndex.ToString(),
                    j.super_SpanBase.meSpanType.ToString(),
                    (j.macName ?? string.Empty).TrimEnd('\0'),
                };
            };
            ctx.Repopulate = () => RepopulateView(ctx, _model.junctions.Count);
            ctx.AddNew = () => _model.junctions.Add(new Junction
            {
                macName = new string('\0', 16),
            });
            ctx.Remove = i => _model.junctions.RemoveAt(i);
            ctx.Duplicate = i =>
            {
                Junction src = _model.junctions[i];
                Junction copy = new Junction
                {
                    super_SpanBase = new SpanBase
                    {
                        miRoadIndex = src.super_SpanBase.miRoadIndex,
                        miSpanIndex = src.super_SpanBase.miSpanIndex,
                        meSpanType = src.super_SpanBase.meSpanType,
                    },
                    macName = src.macName,
                };
                _model.junctions.Insert(i + 1, copy);
            };
            BuildContextMenu(ctx);
        }

        private void ConfigureRoadsTab()
        {
            TabContext ctx = _roadsCtx;
            ctx.Get = i => _model.roads[i];
            ctx.Format = i =>
            {
                Road r = _model.roads[i];
                return new[]
                {
                    i.ToString(),
                    (r.macDebugName ?? string.Empty).TrimEnd('\0'),
                    r.mReferencePosition.X.ToString("0.###", CultureInfo.InvariantCulture),
                    r.mReferencePosition.Y.ToString("0.###", CultureInfo.InvariantCulture),
                    r.mReferencePosition.Z.ToString("0.###", CultureInfo.InvariantCulture),
                    r.mChallenge.ToString(),
                    r.mId.ToString(),
                };
            };
            ctx.Repopulate = () => RepopulateView(ctx, _model.roads.Count);
            ctx.AddNew = () =>
            {
                // Roads and Challenges are 1:1; keep the counts aligned.
                _model.roads.Add(new Road { macDebugName = new string('\0', 16) });
                _model.challenges.Add(new ChallengeParScores());
            };
            ctx.Remove = i =>
            {
                _model.roads.RemoveAt(i);
                if (i < _model.challenges.Count)
                    _model.challenges.RemoveAt(i);
            };
            ctx.Duplicate = i =>
            {
                Road src = _model.roads[i];
                Road copy = new Road
                {
                    mReferencePosition = src.mReferencePosition,
                    mId = src.mId,
                    miRoadLimitId0 = src.miRoadLimitId0,
                    miRoadLimitId1 = src.miRoadLimitId1,
                    macDebugName = src.macDebugName,
                    mChallenge = src.mChallenge,
                    unknown = src.unknown,
                };
                _model.roads.Insert(i + 1, copy);
                _model.challenges.Insert(i + 1, new ChallengeParScores());
            };
            BuildContextMenu(ctx);
        }

        private void ConfigureChallengesTab()
        {
            TabContext ctx = _challengesCtx;
            ctx.Get = i => _model.challenges[i];
            ctx.Format = i =>
            {
                ChallengeParScores c = _model.challenges[i];
                return new[]
                {
                    i.ToString(),
                    c.challengeData.mScoreList.maScores[0].ToString(),
                    c.challengeData.mScoreList.maScores[1].ToString(),
                    c.mRivals[0].ToString("X16"),
                    c.mRivals[1].ToString("X16"),
                };
            };
            ctx.Repopulate = () => RepopulateView(ctx, _model.challenges.Count);
            // Add/remove happens via the Roads tab to keep the counts aligned.
            ctx.AddNew = null;
            ctx.Remove = null;
            ctx.Duplicate = null;
            BuildContextMenu(ctx);
        }

        private void BuildContextMenu(TabContext ctx)
        {
            ctx.ContextMenu.Items.Clear();

            ToolStripMenuItem addItem = new ToolStripMenuItem("Add new") { Enabled = ctx.AddNew != null };
            addItem.Click += (s, e) =>
            {
                if (ctx.AddNew == null) return;
                ctx.AddNew();
                AfterMutate(ctx);
            };

            ToolStripMenuItem dupItem = new ToolStripMenuItem("Duplicate selected") { Enabled = ctx.Duplicate != null };
            dupItem.Click += (s, e) =>
            {
                if (ctx.Duplicate == null) return;
                int idx = SelectedModelIndex(ctx);
                if (idx < 0) return;
                ctx.Duplicate(idx);
                AfterMutate(ctx);
            };

            ToolStripMenuItem rmItem = new ToolStripMenuItem("Remove selected") { Enabled = ctx.Remove != null };
            rmItem.Click += (s, e) =>
            {
                if (ctx.Remove == null) return;
                int idx = SelectedModelIndex(ctx);
                if (idx < 0) return;
                ctx.Remove(idx);
                AfterMutate(ctx);
            };

            ctx.ContextMenu.Items.Add(addItem);
            ctx.ContextMenu.Items.Add(dupItem);
            ctx.ContextMenu.Items.Add(rmItem);
        }

        private void AfterMutate(TabContext ctx)
        {
            ctx.Repopulate();
            // Roads and Challenges are 1:1, so a Roads mutation also needs
            // the Challenges tab refreshed (and vice versa).
            if (ctx == _roadsCtx) _challengesCtx.Repopulate();
            if (ctx == _challengesCtx) _roadsCtx.Repopulate();
            UpdateTabTitles();

            // Re-fire EditEvent so the bundle on disk reflects the mutation.
            _editFired = false;
            RaiseEdit();
        }

        // The ListView may be sorted; use the row's Tag (the live model
        // object) to find the model index instead of the row index.
        private int SelectedModelIndex(TabContext ctx)
        {
            if (ctx.View.SelectedItems.Count == 0) return -1;
            object tag = ctx.View.SelectedItems[0].Tag;
            switch (tag)
            {
                case Street s:             return _model.streets.IndexOf(s);
                case Junction j:           return _model.junctions.IndexOf(j);
                case Road r:               return _model.roads.IndexOf(r);
                case ChallengeParScores c: return _model.challenges.IndexOf(c);
                default:                   return -1;
            }
        }

        private void RepopulateView(TabContext ctx, int count)
        {
            ctx.View.BeginUpdate();
            try
            {
                ctx.View.Items.Clear();
                for (int i = 0; i < count; i++)
                {
                    string[] row = ctx.Format(i);
                    ListViewItem item = new ListViewItem(row) { Tag = ctx.Get(i) };
                    ctx.View.Items.Add(item);
                }
                foreach (ColumnHeader col in ctx.View.Columns)
                    col.Width = -2; // auto-size to content + header
            }
            finally
            {
                ctx.View.EndUpdate();
            }
        }

        private void UpdateTabTitles()
        {
            if (_model == null) return;
            streetsPage.Text    = "Streets ("    + _model.streets.Count    + ")";
            junctionsPage.Text  = "Junctions ("  + _model.junctions.Count  + ")";
            roadsPage.Text      = "Roads ("      + _model.roads.Count      + ")";
            challengesPage.Text = "Challenges (" + _model.challenges.Count + ")";
        }

        private void list_DoubleClick(object sender, EventArgs e)
        {
            ListView list = (ListView)sender;
            if (list.SelectedItems.Count == 0) return;
            propertyGrid.SelectedObject = list.SelectedItems[0].Tag;
        }

        private void list_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            ListView list = (ListView)sender;
            if (list.SelectedItems.Count == 0) return;
            propertyGrid.SelectedObject = list.SelectedItems[0].Tag;
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void list_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListView list = (ListView)sender;
            if (list.ListViewItemSorter is ListItemSorter existing && existing.Column == e.Column)
            {
                existing.Descending = !existing.Descending;
            }
            else
            {
                list.ListViewItemSorter = new ListItemSorter(e.Column);
            }
            list.Sort();
        }

        // Numeric-aware sorter, falls back to ordinal string compare.
        private sealed class ListItemSorter : IComparer
        {
            public int Column { get; }
            public bool Descending { get; set; }

            public ListItemSorter(int column)
            {
                Column = column;
                Descending = false;
            }

            public int Compare(object x, object y)
            {
                ListViewItem ix = (ListViewItem)x;
                ListViewItem iy = (ListViewItem)y;

                if (Column >= ix.SubItems.Count || Column >= iy.SubItems.Count)
                    return 0;

                string sx = ix.SubItems[Column].Text;
                string sy = iy.SubItems[Column].Text;

                int cmp;
                if (long.TryParse(sx, NumberStyles.Integer, CultureInfo.InvariantCulture, out long lx) &&
                    long.TryParse(sy, NumberStyles.Integer, CultureInfo.InvariantCulture, out long ly))
                {
                    cmp = lx.CompareTo(ly);
                }
                else if (double.TryParse(sx, NumberStyles.Float, CultureInfo.InvariantCulture, out double fx) &&
                         double.TryParse(sy, NumberStyles.Float, CultureInfo.InvariantCulture, out double fy))
                {
                    cmp = fx.CompareTo(fy);
                }
                else
                {
                    cmp = string.CompareOrdinal(sx, sy);
                }
                return Descending ? -cmp : cmp;
            }
        }

        private void exportJsonItem_Click(object sender, EventArgs e)
        {
            if (_model == null) return;

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "JSON File (*.json)|*.json";
            saveDialog.Title = "Export StreetData as JSON";
            saveDialog.FileName = "StreetData.json";

            if (saveDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                };
                string content = JsonSerializer.Serialize(_model, options);
                File.WriteAllText(saveDialog.FileName, content);
                MessageBox.Show(this,
                    "Successfully exported to " + saveDialog.FileName,
                    "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this,
                    "Failed to export file. Error: " + ex.Message,
                    "Export Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool _editFired;
        private void RaiseEdit()
        {
            if (_editFired) return;
            _editFired = true;
            EditEvent?.Invoke();
        }

        private void StreetDataEditor_FormClosed(object sender, FormClosedEventArgs e)
        {
            RaiseEdit();
        }
    }
}
