using PluginAPI;
using System.ComponentModel;
using System.Windows.Forms;

namespace LuaList
{
    public delegate void Notify();  // delegate
    public partial class LuaListEditor : Form, IEntryEditor
    {
        public event Notify EditEvent;

        private LuaList _luaList;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public LuaList LuaList
        {
            get => _luaList;
            set
            {
                _luaList = value;
                UpdateComponent();
            }
        }

        public void UpdateComponent()
        {
            propertyGrid1.SelectedObject = LuaList;
        }

        public LuaListEditor()
        {
            InitializeComponent();
            UpdateComponent();
        }

        private void LuaListEditor_FormClosed(object s, FormClosedEventArgs e)
        {
            EditEvent?.Invoke();
        }
    }
}
