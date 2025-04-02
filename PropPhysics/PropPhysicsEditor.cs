using PluginAPI;

namespace PropPhysics
{
    public partial class PropPhysicsEditor : Form, IEntryEditor
    {
        public delegate void OnEdit();
        public event OnEdit Edit;

        private PropPhysicsData _data;
        public PropPhysicsData Data
        {
            get => _data;
            set
            {
                _data = value;
                UpdateDisplay();
            }
        }

        private bool gridPopulating = true;

        public PropPhysicsEditor()
        {
            InitializeComponent();
            propPhysicsGrid.Columns["resourceId"].DefaultCellStyle.Format = "X";
        }

        private void UpdateDisplay()
        {
            propPhysicsGrid.Rows.Clear();
            propPhysicsGrid.Rows.Add((int)Data.NumPropTypes);
            for (int i = 0; i < Data.NumPropTypes; ++i)
            {
                propPhysicsGrid["id", i].Value = i;
                propPhysicsGrid["jointLocator", i].Value = Data.PropTypes[i].jointLocator;
                propPhysicsGrid["comOffset", i].Value = Data.PropTypes[i].comOffset;
                propPhysicsGrid["inertia", i].Value = Data.PropTypes[i].inertia;
                propPhysicsGrid["resourceId", i].Value = Data.PropTypes[i].resourceId;
                propPhysicsGrid["mass", i].Value = Data.PropTypes[i].mass;
                propPhysicsGrid["collisionVolumes", i].Value = (int)Data.PropTypes[i].collisionVolumesStartIndex;
                propPhysicsGrid["parts", i].Value = (int)Data.PropTypes[i].partsStartIndex;
                propPhysicsGrid["sphereRadius", i].Value = Data.PropTypes[i].sphereRadius;
                propPhysicsGrid["maxJointAngleCos", i].Value = Data.PropTypes[i].maxJointAngleCos;
                propPhysicsGrid["leanThreshold", i].Value = Data.PropTypes[i].leanThreshold;
                propPhysicsGrid["moveThreshold", i].Value = Data.PropTypes[i].moveThreshold;
                propPhysicsGrid["smashThreshold", i].Value = Data.PropTypes[i].smashThreshold;
                propPhysicsGrid["sceneUriId", i].Value = Data.PropTypes[i].sceneUriId;
                propPhysicsGrid["maxState", i].Value = Data.PropTypes[i].maxState;
                propPhysicsGrid["numberOfParts", i].Value = Data.PropTypes[i].numParts;
                propPhysicsGrid["numberOfVolumes", i].Value = Data.PropTypes[i].numVolumes;
                propPhysicsGrid["jointType", i].Value = Data.PropTypes[i].jointType;
                propPhysicsGrid["extraTypeInfo", i].Value = Data.PropTypes[i].extraTypeInfo;
            }

            gridPopulating = false;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            Edit?.Invoke();
            Close();
        }

        private void OnCellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (gridPopulating)
                return;
            if (e.ColumnIndex == propPhysicsGrid.Columns["mass"].Index)
            {
                float val;
                if (float.TryParse((string)propPhysicsGrid[e.ColumnIndex, e.RowIndex].Value, out val) == false)
                {
                    MessageBox.Show("Failed to parse value", "Error", MessageBoxButtons.OK);
                    return;
                }
                Data.PropTypes[e.RowIndex].mass = val;
            }
            if (e.ColumnIndex == propPhysicsGrid.Columns["leanThreshold"].Index)
            {
                float val;
                if (float.TryParse((string)propPhysicsGrid[e.ColumnIndex, e.RowIndex].Value, out val) == false)
                {
                    MessageBox.Show("Failed to parse value", "Error", MessageBoxButtons.OK);
                    return;
                }
                Data.PropTypes[e.RowIndex].leanThreshold = val;
            }
            if (e.ColumnIndex == propPhysicsGrid.Columns["moveThreshold"].Index)
            {
                float val;
                if (float.TryParse((string)propPhysicsGrid[e.ColumnIndex, e.RowIndex].Value, out val) == false)
                {
                    MessageBox.Show("Failed to parse value", "Error", MessageBoxButtons.OK);
                    return;
                }
                Data.PropTypes[e.RowIndex].moveThreshold = val;
            }
            if (e.ColumnIndex == propPhysicsGrid.Columns["smashThreshold"].Index)
            {
                float val;
                if (float.TryParse((string)propPhysicsGrid[e.ColumnIndex, e.RowIndex].Value, out val) == false)
                {
                    MessageBox.Show("Failed to parse value", "Error", MessageBoxButtons.OK);
                    return;
                }
                Data.PropTypes[e.RowIndex].smashThreshold = val;
            }
            if (e.ColumnIndex == propPhysicsGrid.Columns["jointType"].Index)
            {
                byte val;
                if (byte.TryParse((string)propPhysicsGrid[e.ColumnIndex, e.RowIndex].Value, out val) == false)
                {
                    MessageBox.Show("Failed to parse value", "Error", MessageBoxButtons.OK);
                    return;
                }
                Data.PropTypes[e.RowIndex].jointType = val;
            }
            if (e.ColumnIndex == propPhysicsGrid.Columns["extraTypeInfo"].Index)
            {
                byte val;
                if (byte.TryParse((string)propPhysicsGrid[e.ColumnIndex, e.RowIndex].Value, out val) == false)
                {
                    MessageBox.Show("Failed to parse value", "Error", MessageBoxButtons.OK);
                    return;
                }
                Data.PropTypes[e.RowIndex].extraTypeInfo = val;
            }
        }
    }
}
