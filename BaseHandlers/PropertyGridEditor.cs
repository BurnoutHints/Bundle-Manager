using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using PluginAPI;

namespace BaseHandlers;

// A description that ends with the field's name and, when given, the range the game's own tweak menu allows.
// PropertyGridEditor warns about values outside the range but keeps them, they're slider limits rather than engine ones.
[AttributeUsage(AttributeTargets.Property)]
public class FieldDescriptionAttribute(string description, float min = float.NaN, float max = float.NaN, [CallerMemberName] string field = null)
    : DescriptionAttribute(string.Join(" ", new[] { description, float.IsNaN(min) ? "" : $"Game range: {min} to {max}.", $"({field})" }.Where(s => s.Length > 0)))
{
    public float Min => min;
    public float Max => max;
}

// Edits a resource in property grids (or given controls), one tab per page, and saves it on close if it changed
public class PropertyGridEditor : Form, IEntryEditor
{
    // serialize gives the bytes write would save. Collection editors change lists in place without telling the grid,
    // so changes are found by comparing those bytes from opening and closing.
    public PropertyGridEditor(string title, Func<byte[]> serialize, Action write, params (string Name, object Data)[] pages)
    {
        Label warning = new() { Dock = DockStyle.Bottom, Height = 24, ForeColor = Color.DarkOrange, TextAlign = ContentAlignment.MiddleLeft, Visible = false };

        PropertyGrid MakeGrid(object data)
        {
            // Categorized without Alphabetical keeps fields in the order they're declared
            PropertyGrid grid = new() { Dock = DockStyle.Fill, SelectedObject = data, PropertySort = PropertySort.Categorized };
            grid.PropertyValueChanged += (_, e) =>
            {
                warning.Text = RangeWarning(e.ChangedItem);
                warning.Visible = warning.Text.Length > 0;
            };
            return grid;
        }

        // A page that's already a control is shown as it is
        Control MakePage(object data) => data is Control control ? control : MakeGrid(data);

        if (pages.Length == 1)
        {
            Controls.Add(MakePage(pages[0].Data));
        }
        else
        {
            TabControl tabs = new() { Dock = DockStyle.Fill };
            foreach ((string name, object data) in pages)
                tabs.TabPages.Add(new TabPage(name) { Controls = { MakePage(data) } });
            Controls.Add(tabs);
        }
        Controls.Add(warning);

        Text = title;
        ClientSize = new Size(800, 600);

        byte[] opened = serialize();

        FormClosing += (_, e) =>
        {
            try
            {
                if (!serialize().AsSpan().SequenceEqual(opened))
                    write();
            }
            catch (InvalidDataException ex)
            {
                e.Cancel = MessageBox.Show(this, $"{ex.Message}\n\nClose without saving?", "Can't save", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No;
            }
        };
    }

    // The changed item may be a vector component or array element, the range is on the property holding it
    private static string RangeWarning(GridItem item)
    {
        for (; item != null; item = item.Parent)
        {
            if (item.PropertyDescriptor?.Attributes[typeof(FieldDescriptionAttribute)] is not FieldDescriptionAttribute range || float.IsNaN(range.Min))
                continue;

            float[] outside = [.. Components(item.Value).Where(v => v < range.Min || v > range.Max)];
            return outside.Length == 0 ? "" : $"{item.Label}: {string.Join(", ", outside)} is outside the game's range of {range.Min} to {range.Max}. It's kept anyway.";
        }
        return "";
    }

    private static float[] Components(object value) => value switch
    {
        float f => [f],
        Vector2 v => [v.X, v.Y],
        Vector3 v => [v.X, v.Y, v.Z],
        Vector4 v => [v.X, v.Y, v.Z, v.W],
        IEnumerable items => [.. items.Cast<object>().SelectMany(Components)],
        _ => []
    };
}
