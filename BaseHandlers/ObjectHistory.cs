using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace BaseHandlers;

public sealed class ObjectHistory
{
    private record Member(object Owner, PropertyInfo Property);

    private sealed record Value(object Instance, object[] Items)
    {
        public static Value Read(Member member)
        {
            object value = member.Property.GetValue(member.Owner);
            return new(value, value is IList list
                ? list.Cast<object>().ToArray()
                : null);
        }

        public bool Same(Value other) => Equals(Instance, other.Instance) &&
                    (Items == null
                        ? other.Items == null
                        : other.Items != null && Items.SequenceEqual(other.Items));

        public void Restore(Member member)
        {
            member.Property.SetValue(member.Owner, Instance);

            if (Instance is Array array)
                for (int i = 0; i < Items.Length; i++)
                    array.SetValue(Items[i], i);
            else if (Instance is IList list)
            {
                list.Clear();

                foreach (object item in Items)
                    list.Add(item);
            }
        }
    }

    private record Change(Member Member, Value Before, Value After);

    private record Edit(List<Change> Changes, long Before, long After);

    private readonly object root;
    private readonly Stack<Edit> undo = new(), redo = new();
    private Dictionary<Member, Value> state;
    private long revision, nextRevision, appliedRevision;

    public bool CanUndo => undo.Count > 0;

    public bool CanRedo => redo.Count > 0;

    public bool Modified => revision != appliedRevision;

    public ObjectHistory(object initialRoot)
    {
        root = initialRoot;
        state = Capture();
    }

    public void MarkApplied() => appliedRevision = revision;

    private Dictionary<Member, Value> Capture() => ObjectGraph.Nodes(root).Where(n => n.Value is not IList)
        .SelectMany(n => ObjectGraph.Properties(n.Value).Select(p => new Member(n.Value, p)))
        .ToDictionary(m => m, Value.Read);

    public bool Record()
    {
        Dictionary<Member, Value> next = Capture();
        List<Change> changes = [];

        foreach ((Member member, Value after) in next)
            if (state.TryGetValue(member, out Value before) && !before.Same(after))
                changes.Add(new(member, before, after));

        state = next;

        if (changes.Count == 0)
            return false;

        long previous = revision;
        revision = ++nextRevision;
        undo.Push(new(changes, previous, revision));
        redo.Clear();
        return true;
    }

    public bool Undo() => Restore(undo, redo, false);

    public bool Redo() => Restore(redo, undo, true);

    private bool Restore(Stack<Edit> from, Stack<Edit> to, bool forward)
    {
        if (!from.TryPop(out Edit edit))
            return false;

        foreach (Change change in forward
            ? edit.Changes
            : edit.Changes.AsEnumerable().Reverse())
            (forward
                ? change.After
                : change.Before).Restore(change.Member);

        revision = forward
            ? edit.After
            : edit.Before;
        to.Push(edit);
        state = Capture();
        return true;
    }
}

public static class ObjectGraph
{
    public sealed record Node(object Value, object[] Path, string Label)
    {
        public override string ToString() => Label;
    }

    public sealed record Usage(Node Node, string Property)
    {
        public override string ToString() => Node.Label + " → " + TrafficLabels.Name(Property);
    }

    public static IEnumerable<PropertyInfo> Properties(object value) => value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0);

    public static bool Owned(PropertyInfo property) => property.GetCustomAttribute<BrowsableAttribute>() is { Browsable: false };

    public static List<Node> Nodes(object root)
    {
        List<Node> nodes = [];
        HashSet<object> visited = new(ReferenceEqualityComparer.Instance);

        void visit(object value, object[] path, string label)
        {
            if (value == null || value is string || value.GetType().IsValueType || value is Array || !visited.Add(value))
                return;

            nodes.Add(new(value, path, label));

            if (value is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                    visit(list[i], [.. path, list[i]], $"{label} / {i}: {TrafficReferences.Describe(list[i])}");
            }
            else
                foreach (PropertyInfo currentProperty in Properties(value).Where(Owned))
                    visit(currentProperty.GetValue(value), [.. path, currentProperty.GetValue(value)], label + " / " + TrafficLabels.Name(currentProperty.Name));
        }

        visit(root, [], TrafficReferences.Describe(root));
        return nodes;
    }

    public static List<Usage> Incoming(object root, object target)
    {
        HashSet<object> removed = new(Nodes(target).Select(n => n.Value), ReferenceEqualityComparer.Instance);
        List<Usage> found = [];

        foreach (Node node in Nodes(root).Where(n => n.Value is not IList && !removed.Contains(n.Value)))
            foreach (PropertyInfo currentProperty in Properties(node.Value).Where(p => !Owned(p)))
            {
                object value = currentProperty.GetValue(node.Value);

                if (value != null && (removed.Contains(value) || value is IList list && list.Cast<object>().Any(removed.Contains)))
                    found.Add(new(node, currentProperty.Name));
            }

        return found;
    }
}
