using System.ComponentModel;
using System.Globalization;
using BaseHandlers;
using BundleFormat;

internal static partial class Program
{
    private static T Internal<T>(object obj, string property) => (T)obj.GetType().GetProperty(property, Private).GetValue(obj);

    // Every field's value as text, with views expanded, so two reads can be compared
    private static List<string> DumpVault(AttribVault vault)
    {
        List<string> lines = [];

        void dump(string path, object owner, PropertyDescriptorCollection properties)
        {
            foreach (PropertyDescriptor property in properties)
            {
                object value = property.GetValue(owner);
                lines.Add($"{path}/{property.Name} = {property.Converter.ConvertToString(null, CultureInfo.InvariantCulture, value)}");

                if (value is AttribView view)
                    dump($"{path}/{property.Name}", view, property.Converter.GetProperties(view));
            }
        }

        foreach (AttribClass group in vault.Classes)
            foreach (AttribCollection collection in group.Collections)
                dump($"{group.Name}/{TypeDescriptor.GetProperties(collection)["ID"].GetValue(collection)}", collection, TypeDescriptor.GetProperties(collection));

        return lines;
    }

    private static IEnumerable<PropertyDescriptor> Scalars(object owner, PropertyDescriptorCollection properties)
    {
        foreach (PropertyDescriptor property in properties)
        {
            if (property.GetValue(owner) is AttribView view)
                foreach (PropertyDescriptor inner in Scalars(view, property.Converter.GetProperties(view)))
                    yield return inner;
            else if (!property.IsReadOnly)
                yield return property;
        }
    }

    private static void CheckVaultFiles(string[] args)
    {
        List<string> paths = [.. args.Where(a => a.StartsWith("--vault-file=")).Select(a => a["--vault-file=".Length..])];

        foreach (string folder in args.Where(a => a.StartsWith("--vault-dir=")).Select(a => a["--vault-dir=".Length..]))
            paths.AddRange(Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                .Where(p => Path.GetExtension(p).ToUpperInvariant() is ".BIN" or ".BUNDLE" or ".BNDL"));

        int vaults = 0, collections = 0, unknown = 0, fields = 0;
        HashSet<string> unknownClasses = [];
        Dictionary<string, int> versions = [];
        bool edited = false, retexted = false, editor = false;

        foreach (string path in paths)
        {
            BundleArchive bundle;

            try
            {
                bundle = BundleArchive.Read(path);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (BundleEntry entry in bundle?.Entries.Where(e => e.Type == EntryType.AttribSysVault) ?? [])
            {
                string label = $"{path} {entry.ID:X8}";
                byte[] original = entry.GetPrimaryWithoutImports();
                AttribVault vault = new();

                try
                {
                    vault.Read(entry);
                }
                catch (Exception exception)
                {
                    throw new($"{label}: {exception.Message}", exception);
                }

                Check(vault.Serialize().SequenceEqual(original), $"{label}: saves unchanged");
                List<string> before = DumpVault(vault);
                Check(!before.Any(l => l.EndsWith("(doesn't fit)")), $"{label}: every field fits its collection");
                vaults++;
                collections += vault.Classes.Sum(c => c.Collections.Count);
                unknown += vault.Unknown;
                unknownClasses.UnionWith(vault.Classes.SelectMany(c => c.Collections).Where(c => Internal<AttribSchema.Class>(c, "Schema") == null).Select(c => $"{Internal<string>(c, "ClassName")} ({c.Bytes.Length} bytes)"));
                fields += before.Count;
                versions[vault.Schema] = versions.GetValueOrDefault(vault.Schema) + 1;

                // A number edit only changes its own bytes
                if (!edited && vault.Classes.SelectMany(c => c.Collections).Select(c => (c, p: Scalars(c, TypeDescriptor.GetProperties(c)).FirstOrDefault(p => p.PropertyType == typeof(float))))
                    .FirstOrDefault(x => x.p != null) is (AttribCollection collection, PropertyDescriptor number))
                {
                    float value = (float)number.GetValue(collection) + 1.5f;
                    number.SetValue(collection, value);
                    byte[] saved = vault.Serialize();
                    AttribVault reread = Reread(saved, entry);
                    Check(saved.Length == original.Length && saved.Zip(original).Count(b => b.First != b.Second) <= 4, $"{label}: a number edit changes only its own bytes");
                    Check(DumpVault(reread).Except(before).Count() is 1 or 2, $"{label}: a number edit reads back, and nothing else changed");
                    edited = true;
                    vault.Read(entry);
                }

                // A new string grows StrE and moves every payload after it, which must all read back the same
                if (!retexted && vault.Classes.SelectMany(c => c.Collections).Select(c => (c, p: Scalars(c, TypeDescriptor.GetProperties(c)).FirstOrDefault(p => p.PropertyType == typeof(string) && p.Name != "ID" && p.GetValue(c) != null && !p.Description.Contains("bytes"))))
                    .FirstOrDefault(x => x.p != null) is (AttribCollection textOwner, PropertyDescriptor text))
                {
                    text.SetValue(textOwner, "BUNDLE MANAGER VAULT CHECK");
                    byte[] saved = vault.Serialize();
                    List<string> after = DumpVault(Reread(saved, entry));
                    Check(saved.Length > original.Length && (saved.Length - original.Length) % 16 == 0, $"{label}: a new string grows the vault by whole 16-byte blocks");
                    Check(after.Count == before.Count && after.Except(before).Count() is 1 or 2 && after.Any(l => l.Contains("BUNDLE MANAGER VAULT CHECK")),
                        $"{label}: after a new string moves the payloads, only that string reads differently");
                    retexted = true;
                    vault.Read(entry);
                }

                if (!editor && entry.ID == 0x7657D9BF)
                {
                    CheckVaultEditor(vault, entry, label);
                    editor = true;
                }
            }
        }

        Check(vaults > 0 || paths.Count == 0, $"{vaults} vaults ({string.Join(", ", versions.Select(v => $"{v.Value} {v.Key}"))}) with {collections} collections and {fields} values read and save unchanged; {unknown} collections have no known layout: {string.Join(", ", unknownClasses)}");
    }

    private static AttribVault Reread(byte[] bytes, BundleEntry entry)
    {
        byte[] previous = entry.GetPrimaryWithoutImports();
        entry.SetPrimary(bytes, entry.GetImports());
        AttribVault vault = new();
        vault.Read(entry);
        entry.SetPrimary(previous, entry.GetImports());
        return vault;
    }

    private static void CheckVaultEditor(AttribVault source, BundleEntry entry, string label)
    {
        byte[] before = entry.GetPrimaryWithoutImports();
        using AttribVaultEditor editor = new(source, entry);
        Hidden(editor);
        AttribVault draft = Field<AttribVault>(editor, "data");
        ObjectTreeEditor tree = Field<ObjectTreeEditor>(editor, "tree");
        AttribCollection bin = draft.Classes.First(c => c.Name == "propscrashbin").Collections[0];
        tree.SelectObject(bin);
        Check(tree.SelectedObject == bin && !tree.CanDuplicate, $"{label}: a collection can be selected, but not duplicated");

        editor.ClientSize = new(1100, 760);
        Application.DoEvents();
        Capture(editor, $"vault-{entry.ID:X8}.png");

        PropertyDescriptor priority = TypeDescriptor.GetProperties(bin)["Priority"];
        priority.SetValue(bin, 3.25f);
        tree.RecordChange();
        Check(entry.GetPrimaryWithoutImports().SequenceEqual(before) && (bool)Call(editor, "ApplyChanges"), $"{label}: edits stay in the draft until Apply writes them");
        AttribVault applied = Reread(entry.GetPrimaryWithoutImports(), entry);
        Check((float)TypeDescriptor.GetProperties(applied.Classes.First(c => c.Name == "propscrashbin").Collections[0])["Priority"].GetValue(applied.Classes.First(c => c.Name == "propscrashbin").Collections[0]) == 3.25f,
            $"{label}: Apply writes the new value");

        tree.Undo();
        Check((float)priority.GetValue(bin) != 3.25f, $"{label}: Undo restores the bytes");

        // Splice lists can be typed
        PropertyDescriptor small = TypeDescriptor.GetProperties(bin)["mCollisionsSmall"];
        AttribView view = (AttribView)small.GetValue(bin);
        TypeConverter converter = small.Converter;
        string typed = string.Join(", ", Enumerable.Range(100, converter.GetProperties(view).Count - 1));
        Check(converter.CanConvertFrom(new Context(bin, small), typeof(string)), $"{label}: a number array can be typed");
        converter.ConvertFrom(new Context(bin, small), CultureInfo.InvariantCulture, typed);
        Check(small.GetValue(bin).ToString().StartsWith("100, 101"), $"{label}: typing a number array writes its items");

        // References offer the collections of their class, and point at real collections
        AttribCollection global = draft.Classes.First(c => c.Name == "burnoutglobaldata").Collections[0];
        PropertyDescriptor list = TypeDescriptor.GetProperties(global).Cast<PropertyDescriptor>()
            .FirstOrDefault(p => p.GetValue(global) is AttribCollection c && Internal<string>(c, "ClassName") == "propscrashbinlist");
        Check(list?.Converter.GetStandardValues(null).Count == 2, $"{label}: the global data's props crash bin list reference resolves and offers that class's collections");
        Check(tree.FindUses(list.GetValue(global)).Any(u => u.Node.Value == global && u.Property == list.Name), $"{label}: Used by finds the global data's reference to the list");

        // The prop material table is one list of props by name, each picking its material by name
        AttribCollection table = draft.Classes.First(c => c.Name == "proptomaterialmappings").Collections[0];
        PropertyDescriptor mappings = TypeDescriptor.GetProperties(table)["Mappings"];

        PropertyDescriptor[] parts(object view) => [.. mappings.Converter.GetProperties(view).Cast<PropertyDescriptor>()];

        object rows = mappings?.GetValue(table);
        PropertyDescriptor row = rows == null
            ? null
            : parts(rows).Skip(1).FirstOrDefault(r => !r.DisplayName.StartsWith('[') && !r.DisplayName.StartsWith("0x"));
        Check(row != null && (int)parts(rows)[0].GetValue(rows) == parts(rows).Length - 1, $"{label}: the prop material table lists its props by name");
        Context context = new(rows, row);
        TypeConverter pick = row.Converter;
        string current = pick.ConvertToString(context, row.GetValue(rows));
        string other = pick.GetStandardValues(context).Cast<object>().Select(v => pick.ConvertToString(context, v)).First(t => t != current);
        row.SetValue(rows, pick.ConvertFrom(context, CultureInfo.InvariantCulture, other));
        Check(parts(mappings.GetValue(table)).First(r => r.Name == row.Name).GetValue(null).ToString() == other, $"{label}: picking a prop's material by name writes it");

        tree.SelectObject(table);
        PropertyGrid grid = Field<PropertyGrid>(tree, "grid");
        GridItem top = grid.SelectedGridItem;

        while (top?.Parent != null)
            top = top.Parent;

        GridItem expand = top.GridItems.Cast<GridItem>().SelectMany(category => category.GridItems.Cast<GridItem>()).First(g => g.Label == "Mappings");
        expand.Expanded = true;
        expand.GridItems[2].Select();
        Application.DoEvents();
        Capture(editor, $"vault-mappings-{entry.ID:X8}.png");
        entry.SetPrimary(before, entry.GetImports());
    }

    private sealed class Context(object instance, PropertyDescriptor property) : ITypeDescriptorContext
    {
        public IContainer Container => null;

        public object Instance => instance;

        public PropertyDescriptor PropertyDescriptor => property;

        public object GetService(Type serviceType) => null;

        public void OnComponentChanged() { }

        public bool OnComponentChanging() => true;
    }
}
