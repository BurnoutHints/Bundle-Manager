using PluginAPI;
using ScottPlot;
using System;
using System.Windows.Forms;

namespace LoopModel
{
    public partial class LoopModelEditor : Form, IEntryEditor
    {
        public delegate void OnEdit();
        public event OnEdit Edit;

        // Used in handling changes to the plots
        private bool[] partialsShown = new bool[10];
        private ScottPlot.Color[] partialsColors =
        {
            Colors.Red,
            Colors.Green,
            Colors.Blue,
            Colors.Cyan,
            Colors.Magenta,
            Colors.Yellow,
            Colors.Orange,
            Colors.Purple,
            Colors.Brown,
            Colors.Grey
        };
        private int selectedGraphIndex = 0;

        private LoopModelData _data;
        public LoopModelData Data
        {
            get => _data;
            set
            {
                _data = value;
                UpdatePartialsGrid();
                UpdatePointsGrid(0, 0);
                UpdateAllPlots();
            }
        }

        public LoopModelEditor()
        {
            InitializeComponent();

            // Create axis labels
            rpmPitchPlot.Plot.XLabel("RPM");
            rpmPitchPlot.Plot.YLabel("Pitch");
            rpmGainPlot.Plot.XLabel("RPM");
            rpmGainPlot.Plot.YLabel("Gain");
            accelGainPlot.Plot.XLabel("Accelerator");
            accelGainPlot.Plot.YLabel("Gain");

            // Create graph grid rows (they're always the same)
            string[] graphNames = { "Pitch/RPM", "Gain/RPM", "Gain/Accelerator" };
            for (int i = 0; i < graphNames.Length; ++i)
                graphsGrid.Rows.Add(graphNames[i]);

            // Set up shown partials (all are shown at first)
            Array.Fill(partialsShown, true);
        }

        public void UpdatePartialsGrid()
        {
            partialsGrid.Rows.Clear();
            for (int i = 0; i < _data.Partials.Count; ++i)
            {
                partialsGrid.Rows.Add(new DataGridViewRow());
                partialsGrid[0, i].Value = true;
                partialsGrid[1, i].Value = _data.Partials[i].WaveName;
                partialsGrid[2, i].Value = "0x" + _data.Partials[i].WaveNameHash.ToString("X8");
            }
        }

        public void UpdatePointsGrid(int partialIndex, int graphIndex)
        {
            pointsGrid.Rows.Clear();
            for (int i = 0; i < _data.Partials[partialIndex].Graphs[graphIndex].Points.Count; ++i)
            {
                pointsGrid.Rows.Add(new DataGridViewRow());
                pointsGrid[0, i].Value = _data.Partials[partialIndex].Graphs[graphIndex].Points[i].X;
                pointsGrid[1, i].Value = _data.Partials[partialIndex].Graphs[graphIndex].Points[i].Y;
            }
        }

        public void UpdateAllPlots()
        {
            MakeNewScatterPlot(rpmPitchPlot, 0);
            MakeNewScatterPlot(rpmGainPlot, 1);
            MakeNewScatterPlot(accelGainPlot, 2);
        }

        public void MakeNewScatterPlot(ScottPlot.WinForms.FormsPlot plot, int graphIndex)
        {
            plot.Plot.Clear();
            for (int i = 0; i < _data.Partials.Count; ++i)
            {
                if ((bool)partialsGrid.Rows[i].Cells[0].Value == false)
                    continue;
                ScottPlot.Coordinates[] coords = new ScottPlot.Coordinates[_data.Partials[i].Graphs[graphIndex].Points.Count];
                for (int j = 0; j < _data.Partials[i].Graphs[graphIndex].Points.Count; ++j)
                {
                    coords[j].X = _data.Partials[i].Graphs[graphIndex].Points[j].X;
                    coords[j].Y = _data.Partials[i].Graphs[graphIndex].Points[j].Y;
                }
                var scatter = plot.Plot.Add.Scatter(coords, partialsColors[i]);
                scatter.LegendText = _data.Partials[i].WaveName;
            }
            plot.Plot.Legend.FontSize = 10;
            plot.Plot.ShowLegend(Edge.Right);
            plot.Refresh();
        }

        private void UpdateSinglePlot(int changedRowIndex, int changedColumnIndex)
        {
            if (changedColumnIndex == 0)
            {
                _data.Partials[partialsGrid.SelectedRows[0].Index]
                    .Graphs[graphsGrid.SelectedRows[0].Index]
                    .Points[changedRowIndex]
                    .X = Convert.ToSingle(pointsGrid.Rows[changedRowIndex].Cells[changedColumnIndex].Value);
            }
            else if (changedColumnIndex == 1)
            {
                _data.Partials[partialsGrid.SelectedRows[0].Index]
                    .Graphs[graphsGrid.SelectedRows[0].Index]
                    .Points[changedRowIndex]
                    .Y = Convert.ToSingle(pointsGrid.Rows[changedRowIndex].Cells[changedColumnIndex].Value);
            }
            if (graphsGrid.SelectedRows[0].Index == 0)
                MakeNewScatterPlot(rpmPitchPlot, 0);
            else if (graphsGrid.SelectedRows[0].Index == 1)
                MakeNewScatterPlot(rpmGainPlot, 1);
            else if (graphsGrid.SelectedRows[0].Index == 2)
                MakeNewScatterPlot(accelGainPlot, 2);
        }

        private void partialsGrid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            partialsGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            bool isPartialShown = (bool)partialsGrid.Rows[e.RowIndex].Cells[0].Value;
            if (isPartialShown != partialsShown[e.RowIndex])
            {
                partialsShown[e.RowIndex] = isPartialShown;
                UpdateAllPlots();
            }
        }

        private void partialsGrid_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (partialsGrid.SelectedRows.Count > 0)
                UpdatePointsGrid(partialsGrid.SelectedRows[0].Index, selectedGraphIndex);
        }

        private void graphsGrid_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (graphsGrid.SelectedRows.Count > 0 && graphsGrid.SelectedRows[0].Index != selectedGraphIndex)
            {
                selectedGraphIndex = graphsGrid.SelectedRows[0].Index;
                UpdatePointsGrid(partialsGrid.SelectedRows[0].Index, selectedGraphIndex);
            }
        }

        private void pointsGrid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            Single changedValue = Convert.ToSingle(pointsGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
            Single originalValue = 0;
            if (e.ColumnIndex == 0)
            {
                originalValue = _data.Partials[partialsGrid.SelectedRows[0].Index]
                    .Graphs[graphsGrid.SelectedRows[0].Index]
                    .Points[e.RowIndex].X;
            }
            else
            {
                originalValue = _data.Partials[partialsGrid.SelectedRows[0].Index]
                    .Graphs[graphsGrid.SelectedRows[0].Index]
                    .Points[e.RowIndex].Y;
            }
            if (originalValue != changedValue)
                UpdateSinglePlot(e.RowIndex, e.ColumnIndex);
        }

        private void LoopModelEditor_FormClosed(object sender, EventArgs e)
        {
            Edit();
        }
    }
}
