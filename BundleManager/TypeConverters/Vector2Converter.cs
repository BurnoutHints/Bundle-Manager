using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Numerics;

namespace BundleManager.TypeConverters;

sealed class Vector2Converter : ExpandableObjectConverter
{
    sealed class FieldPD : PropertyDescriptor
    {
        readonly string n;
        public FieldPD(string name) : base(name, null) { n = name; }
        public override Type ComponentType => typeof(Vector2);
        public override bool IsReadOnly => false;
        public override Type PropertyType => typeof(float);
        public override object GetValue(object component)
        {
            Vector2 v = (Vector2)component;
            return n == "X" ? v.X : v.Y;
        }
        public override void SetValue(object component, object value) { }
        public override bool CanResetValue(object component) => false;
        public override void ResetValue(object component) { }
        public override bool ShouldSerializeValue(object component) => true;
    }

    public override bool GetPropertiesSupported(ITypeDescriptorContext c) => true;
    public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext c, object value, Attribute[] a)
        => new PropertyDescriptorCollection([new FieldPD("X"), new FieldPD("Y")]).Sort(["X", "Y"]);

    public override bool GetCreateInstanceSupported(ITypeDescriptorContext c) => true;
    public override object CreateInstance(ITypeDescriptorContext c, IDictionary p)
        => new Vector2(Convert.ToSingle(p["X"]), Convert.ToSingle(p["Y"]));

    public override object ConvertTo(ITypeDescriptorContext c, CultureInfo u, object v, Type t)
        => t == typeof(string) && v is Vector2 s ? $"{s.X}, {s.Y}" : base.ConvertTo(c, u, v, t);
}
