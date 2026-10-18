using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace BaseHandlers;

// Picks float RGB colours for Vector3, Vector4 (alpha kept) and Vector3[] (pick an element from a list) values.
// Colours brighter than 1 keep their brightness, the picker sets the hue.
public class VectorColorEditor : UITypeEditor
{
    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        => context?.PropertyDescriptor?.PropertyType == typeof(Vector3[]) ? UITypeEditorEditStyle.DropDown : UITypeEditorEditStyle.Modal;

    public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
    {
        switch (value)
        {
            case Vector3 v:
                return Pick(v);
            case Vector4 v:
                return new Vector4(Pick(new Vector3(v.X, v.Y, v.Z)), v.W);
            case Vector3[] colours when provider?.GetService(typeof(IWindowsFormsEditorService)) is IWindowsFormsEditorService service:
                ListBox list = new() { BorderStyle = BorderStyle.None, IntegralHeight = false, Height = colours.Length * 18 + 4 };
                list.Items.AddRange([.. colours.Select((c, i) => (object)$"{i}: {c.X:0.###}, {c.Y:0.###}, {c.Z:0.###}")]);
                list.Click += (_, _) => service.CloseDropDown();
                service.DropDownControl(list);
                if (list.SelectedIndex < 0)
                    return value;
                Vector3[] result = (Vector3[])colours.Clone();
                result[list.SelectedIndex] = Pick(colours[list.SelectedIndex]);
                return result;
            default:
                return value;
        }
    }

    public static Vector3 Pick(Vector3 colour)
    {
        float scale = Math.Max(1, Math.Max(colour.X, Math.Max(colour.Y, colour.Z)));
        Vector3 rgb = Vector3.Clamp(colour / scale, Vector3.Zero, Vector3.One) * 255;
        Color initial = Color.FromArgb((int)MathF.Round(rgb.X), (int)MathF.Round(rgb.Y), (int)MathF.Round(rgb.Z));

        using ColorDialog dialog = new() { Color = initial, FullOpen = true };
        if (dialog.ShowDialog() != DialogResult.OK || dialog.Color.ToArgb() == initial.ToArgb())
            return colour; // Unchanged, so rounding to bytes doesn't change it
        return new Vector3(dialog.Color.R, dialog.Color.G, dialog.Color.B) / 255 * scale;
    }
}
