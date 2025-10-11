using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Numerics;

namespace BundleManager.TypeConverters;

sealed class Vector4Converter : ExpandableObjectConverter
{
    sealed class FieldPD : PropertyDescriptor
    {
        readonly string n;
        public FieldPD(string name) : base(name, null) { n = name; }
        public override Type ComponentType => typeof(Vector4);
        public override bool IsReadOnly => false;
        public override Type PropertyType => typeof(float);
        public override object GetValue(object component)
        {
            var v = (Vector4)component;
            return n == "X" ? v.X : n == "Y" ? v.Y : n == "Z" ? v.Z : v.W;
        }
        public override void SetValue(object component, object value) { }
        public override bool CanResetValue(object component) => false;
        public override void ResetValue(object component) { }
        public override bool ShouldSerializeValue(object component) => true;
    }

    public override bool GetPropertiesSupported(ITypeDescriptorContext c) => true;
    public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext c, object value, Attribute[] a)
        => new PropertyDescriptorCollection([
        new FieldPD("X"), new FieldPD("Y"), new FieldPD("Z"), new FieldPD("W")
        ]).Sort(["X", "Y", "Z", "W"]);


    public override bool GetCreateInstanceSupported(ITypeDescriptorContext c) => true;
    public override object CreateInstance(ITypeDescriptorContext c, IDictionary p)
        => new Vector4(Convert.ToSingle(p["X"]), Convert.ToSingle(p["Y"]),
                       Convert.ToSingle(p["Z"]), Convert.ToSingle(p["W"]));

    public override object ConvertTo(ITypeDescriptorContext c, CultureInfo u, object v, Type t)
        => t == typeof(string) && v is Vector4 s ? $"{s.X}, {s.Y}, {s.Z}, {s.W}" : base.ConvertTo(c, u, v, t);
}
