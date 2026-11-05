using PluginAPI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace BaseHandlers;

public partial class StreetDataEditor : Form, IEntryEditor
{
    public event Notify EditEvent;

    private StreetData model;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public StreetData Model
    {
        get => model;
        set
        {
            model = value;
            Rebuild();
        }
    }

    private TabContext streetsCtx;
    private TabContext junctionsCtx;
    private TabContext roadsCtx;
    private TabContext challengesCtx;

    public StreetDataEditor()
    {
        InitializeComponent();
        streetsCtx = new()
        {
            View = streetsList,
            ContextMenu = streetsContextMenu
        };
        junctionsCtx = new()
        {
            View = junctionsList,
            ContextMenu = junctionsContextMenu
        };
        roadsCtx = new()
        {
            View = roadsList,
            ContextMenu = roadsContextMenu
        };
        challengesCtx = new()
        {
            View = challengesList,
            ContextMenu = challengesContextMenu
        };
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

        if (model == null)
            return;

        ConfigureStreetsTab();
        ConfigureJunctionsTab();
        ConfigureRoadsTab();
        ConfigureChallengesTab();

        streetsCtx.Repopulate();
        junctionsCtx.Repopulate();
        roadsCtx.Repopulate();
        challengesCtx.Repopulate();

        UpdateTabTitles();
    }

    private void ConfigureStreetsTab()
    {
        TabContext ctx = streetsCtx;
        ctx.Get = i => model.streets[i];
        ctx.Format = i =>
        {
            Street s = model.streets[i];
            return [i.ToString(), s.super_SpanBase.miRoadIndex.ToString(), s.super_SpanBase.miSpanIndex.ToString(), s.super_SpanBase.meSpanType.ToString(), s.mAiInfo.muMaxSpeedMPS.ToString(), s.mAiInfo.muMinSpeedMPS.ToString(),];
        };
        ctx.Repopulate = () => RepopulateView(ctx, model.streets.Count);
        ctx.AddNew = () => model.streets.Add(new());
        ctx.Remove = i => model.streets.RemoveAt(i);
        ctx.Duplicate = i =>
        {
            Street src = model.streets[i];
            model.streets.Insert(i + 1, new()
            {
                super_SpanBase = new()
                {
                    miRoadIndex = src.super_SpanBase.miRoadIndex,
                    miSpanIndex = src.super_SpanBase.miSpanIndex,
                    meSpanType = src.super_SpanBase.meSpanType,
                },
                mAiInfo = new()
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
        TabContext ctx = junctionsCtx;
        ctx.Get = i => model.junctions[i];
        ctx.Format = i =>
        {
            Junction j = model.junctions[i];

            // SpanBase.miRoadIndex is always -1 for junctions; display
            // "-" so the column reads as n/a.
            int roadIdx = j.super_SpanBase.miRoadIndex;
            return [i.ToString(), roadIdx == -1
                ? "-"
                : roadIdx.ToString(), j.super_SpanBase.miSpanIndex.ToString(), j.super_SpanBase.meSpanType.ToString(), (j.macName ?? string.Empty).TrimEnd('\0'), ];
        };
        ctx.Repopulate = () => RepopulateView(ctx, model.junctions.Count);
        ctx.AddNew = () => model.junctions.Add(new()
        {
            macName = new('\0', 16),
        });
        ctx.Remove = i => model.junctions.RemoveAt(i);
        ctx.Duplicate = i =>
        {
            Junction src = model.junctions[i];
            Junction copy = new()
            {
                super_SpanBase = new()
                {
                    miRoadIndex = src.super_SpanBase.miRoadIndex,
                    miSpanIndex = src.super_SpanBase.miSpanIndex,
                    meSpanType = src.super_SpanBase.meSpanType,
                },
                macName = src.macName,
            };

            model.junctions.Insert(i + 1, copy);
        };
        BuildContextMenu(ctx);
    }

    private void ConfigureRoadsTab()
    {
        TabContext ctx = roadsCtx;
        ctx.Get = i => model.roads[i];
        ctx.Format = i =>
        {
            Road r = model.roads[i];
            return [i.ToString(), (r.macDebugName ?? string.Empty).TrimEnd('\0'), r.mReferencePosition.X.ToString("0.###", CultureInfo.InvariantCulture), r.mReferencePosition.Y.ToString("0.###", CultureInfo.InvariantCulture), r.mReferencePosition.Z.ToString("0.###", CultureInfo.InvariantCulture), r.mChallenge.ToString(), r.mId.ToString(),];
        };
        ctx.Repopulate = () => RepopulateView(ctx, model.roads.Count);
        ctx.AddNew = () =>
        {
            // Roads and Challenges are 1:1; keep the counts aligned.
            model.roads.Add(new()
            {
                macDebugName = new('\0', 16)
            });
            model.challenges.Add(new());
        };
        ctx.Remove = i =>
        {
            model.roads.RemoveAt(i);

            if (i < model.challenges.Count)
                model.challenges.RemoveAt(i);
        };
        ctx.Duplicate = i =>
        {
            Road src = model.roads[i];
            Road copy = new()
            {
                mReferencePosition = src.mReferencePosition,
                mId = src.mId,
                miRoadLimitId0 = src.miRoadLimitId0,
                miRoadLimitId1 = src.miRoadLimitId1,
                macDebugName = src.macDebugName,
                mChallenge = src.mChallenge,
                unknown = src.unknown,
            };

            model.roads.Insert(i + 1, copy);
            model.challenges.Insert(i + 1, new());
        };
        BuildContextMenu(ctx);
    }

    private void ConfigureChallengesTab()
    {
        TabContext ctx = challengesCtx;
        ctx.Get = i => model.challenges[i];
        ctx.Format = i =>
        {
            ChallengeParScores c = model.challenges[i];
            return [i.ToString(), c.challengeData.mScoreList.maScores[0].ToString(), c.challengeData.mScoreList.maScores[1].ToString(), c.mRivals[0].ToString("X16"), c.mRivals[1].ToString("X16"),];
        };
        ctx.Repopulate = () => RepopulateView(ctx, model.challenges.Count);

        // Add/remove happens via the Roads tab to keep the counts aligned.
        ctx.AddNew = null;
        ctx.Remove = null;
        ctx.Duplicate = null;
        BuildContextMenu(ctx);
    }

    private void BuildContextMenu(TabContext ctx)
    {
        ctx.ContextMenu.Items.Clear();

        ToolStripMenuItem addItem = new("Add new")
        {
            Enabled = ctx.AddNew != null
        };

        addItem.Click += (s, e) =>
        {
            if (ctx.AddNew == null)
                return;

            ctx.AddNew();
            AfterMutate(ctx);
        };

        ToolStripMenuItem dupItem = new("Duplicate selected")
        {
            Enabled = ctx.Duplicate != null
        };

        dupItem.Click += (s, e) =>
        {
            if (ctx.Duplicate == null)
                return;

            int idx = SelectedModelIndex(ctx);

            if (idx < 0)
                return;

            ctx.Duplicate(idx);
            AfterMutate(ctx);
        };

        ToolStripMenuItem rmItem = new("Remove selected")
        {
            Enabled = ctx.Remove != null
        };

        rmItem.Click += (s, e) =>
        {
            if (ctx.Remove == null)
                return;

            int idx = SelectedModelIndex(ctx);

            if (idx < 0)
                return;

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
        if (ctx == roadsCtx)
            challengesCtx.Repopulate();

        if (ctx == challengesCtx)
            roadsCtx.Repopulate();

        UpdateTabTitles();

        // Re-fire EditEvent so the bundle on disk reflects the mutation.
        editFired = false;
        RaiseEdit();
    }

    // The ListView may be sorted; use the row's Tag (the live model
    // object) to find the model index instead of the row index.
    private int SelectedModelIndex(TabContext ctx)
    {
        if (ctx.View.SelectedItems.Count == 0)
            return -1;

        object tag = ctx.View.SelectedItems[0].Tag;

        switch (tag)
        {
            case Street s:
                return model.streets.IndexOf(s);
            case Junction j:
                return model.junctions.IndexOf(j);
            case Road r:
                return model.roads.IndexOf(r);
            case ChallengeParScores c:
                return model.challenges.IndexOf(c);
            default:
                return -1;
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
                ListViewItem item = new(row)
                {
                    Tag = ctx.Get(i)
                };

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
        if (model == null)
            return;

        streetsPage.Text = "Streets (" + model.streets.Count + ")";
        junctionsPage.Text = "Junctions (" + model.junctions.Count + ")";
        roadsPage.Text = "Roads (" + model.roads.Count + ")";
        challengesPage.Text = "Challenges (" + model.challenges.Count + ")";
    }

    private void list_DoubleClick(object sender, EventArgs e)
    {
        ListView list = (ListView)sender;

        if (list.SelectedItems.Count == 0)
            return;

        propertyGrid.SelectedObject = list.SelectedItems[0].Tag;
    }

    private void list_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
            return;

        ListView list = (ListView)sender;

        if (list.SelectedItems.Count == 0)
            return;

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

            return Descending
                ? -cmp
                : cmp;
        }
    }

    private void exportJsonItem_Click(object sender, EventArgs e)
    {
        if (model == null)
            return;

        SaveFileDialog saveDialog = new()
        {
            Filter = "JSON File (*.json)|*.json",
            Title = "Export StreetData as JSON",
            FileName = "StreetData.json"
        };

        if (saveDialog.ShowDialog() != DialogResult.OK)
            return;

        try
        {
            JsonSerializerOptions options = new()
            {
                WriteIndented = true,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            };

            string content = JsonSerializer.Serialize(model, options);
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

    private bool editFired;

    private void RaiseEdit()
    {
        if (editFired)
            return;

        editFired = true;
        EditEvent?.Invoke();
    }

    private void StreetDataEditor_FormClosed(object sender, FormClosedEventArgs e)
    {
        RaiseEdit();
    }
}
