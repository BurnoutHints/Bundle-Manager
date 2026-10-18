using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace BaseHandlers;

// Picks float RGB colours for Vector3, Vector4 (alpha too) and Vector3[] (pick an element from a list) values with
// ColourPickerForm, and draws a swatch of each in the grid. Swatches of colours brighter than 1 show their hue.
public class VectorColorEditor : UITypeEditor
{
    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        => context?.PropertyDescriptor?.PropertyType == typeof(Vector3[]) ? UITypeEditorEditStyle.DropDown : UITypeEditorEditStyle.Modal;

    public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
    {
        if (provider?.GetService(typeof(IWindowsFormsEditorService)) is not IWindowsFormsEditorService service)
            return value;

        switch (value)
        {
            case Vector3 v:
                return Pick(service, v, null, out _) ?? v;
            case Vector4 v:
                return Pick(service, new Vector3(v.X, v.Y, v.Z), v.W, out float alpha) is Vector3 c ? new Vector4(c, alpha) : v;
            case Vector3[] colours:
                ListBox list = new() { BorderStyle = BorderStyle.None, IntegralHeight = false, Height = colours.Length * 18 + 4 };
                list.Items.AddRange([.. colours.Select((c, i) => (object)$"{i}: {c.X:0.###}, {c.Y:0.###}, {c.Z:0.###}")]);
                list.Click += (_, _) => service.CloseDropDown();
                service.DropDownControl(list);
                if (list.SelectedIndex < 0 || Pick(service, colours[list.SelectedIndex], null, out _) is not Vector3 picked)
                    return value;
                Vector3[] result = (Vector3[])colours.Clone();
                result[list.SelectedIndex] = picked;
                return result;
            default:
                return value;
        }
    }

    // Null if cancelled
    private static Vector3? Pick(IWindowsFormsEditorService service, Vector3 colour, float? alpha, out float newAlpha)
    {
        using ColourPickerForm form = new(colour, alpha);
        bool ok = service.ShowDialog(form) == DialogResult.OK;
        newAlpha = ok && alpha.HasValue ? form.Alpha : alpha ?? 0;
        return ok ? form.Colour : null;
    }

    public override bool GetPaintValueSupported(ITypeDescriptorContext context) => true;

    public override void PaintValue(PaintValueEventArgs e)
    {
        Vector3[] colours = e.Value switch
        {
            Vector3 v => [v],
            Vector4 v => [new Vector3(v.X, v.Y, v.Z)],
            Vector3[] a => a,
            _ => []
        };

        // Side by side for arrays
        for (int i = 0; i < colours.Length; i++)
        {
            int left = e.Bounds.Left + e.Bounds.Width * i / colours.Length;
            int right = e.Bounds.Left + e.Bounds.Width * (i + 1) / colours.Length;
            using SolidBrush brush = new(ColourPickerForm.ToColor(ColourPickerForm.Normalise(colours[i])));
            e.Graphics.FillRectangle(brush, left, e.Bounds.Top, right - left, e.Bounds.Height);
        }
    }
}
