using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace BaseHandlers;

public class ObjectTreeEditor : UserControl
{
    private readonly TreeView tree = new()
    {
        Dock = DockStyle.Fill,
        HideSelection = false
    };

    private readonly PropertyGrid grid = new()
    {
        Dock = DockStyle.Fill,
        PropertySort = PropertySort.Categorized
    };

    private readonly ToolStripMenuItem addItem, duplicateItem, deleteItem, upItem, downItem;
    private readonly object root;
    private readonly ObjectHistory history;

    private readonly TextBox search = new()
    {
        Dock = DockStyle.Top,
        PlaceholderText = "Search names, IDs or road sections",
        AccessibleName = "Search traffic"
    };

    private readonly ListBox results = new()
    {
        Dock = DockStyle.Bottom,
        Height = 130,
        Visible = false,
        IntegralHeight = false
    };

    private readonly Label breadcrumb = new()
    {
        Dock = DockStyle.Bottom,
        Height = 38,
        Padding = new(3),
        AutoEllipsis = true
    };

    private readonly ToolTip pathTip = new();

    private readonly Label collectionHelp = new()
    {
        Dock = DockStyle.Fill,
        Padding = new(8),
        Visible = false
    };

    private readonly ToolStrip commands = new()
    {
        Dock = DockStyle.Top,
        GripStyle = ToolStripGripStyle.Hidden
    };

    private readonly ToolStrip relations = new()
    {
        Dock = DockStyle.Top,
        GripStyle = ToolStripGripStyle.Hidden
    };

    private readonly ToolStripButton undoButton = new("Undo"), redoButton = new("Redo"), goButton = new("Go to reference"), usedButton = new("Used by…");
    private readonly List<(ToolStripButton Button, ToolStripMenuItem Command)> commandButtons = [];

    public bool Modified => history.Modified;

    public bool CanUndo => history.CanUndo;

    public bool CanRedo => history.CanRedo;

    public bool CanDuplicate => duplicateItem.Enabled;

    public object SelectedObject => tree.SelectedNode?.Tag;

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<object, bool> DuplicateRequested { get; set; }

    // The selected node's object or list
    public event Action<object> SelectionChanged;

    public event Action Changed;

    public ObjectTreeEditor(object initialRoot, string name)
    {
        root = initialRoot;
        history = new(initialRoot);
        Dock = DockStyle.Fill;
        SplitContainer split = new()
        {
            Size = new(460, 650),
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 260
        };

        split.Panel1.Controls.Add(tree);
        split.Panel1.Controls.Add(results);
        Panel searchPanel = new()
        {
            Dock = DockStyle.Top,
            Height = search.PreferredHeight + 4
        };

        search.Dock = DockStyle.Fill;
        searchPanel.Controls.Add(search);
        searchPanel.Controls.Add(new Label()
        {
            Text = "&Find:",
            Dock = DockStyle.Left,
            Width = 40,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        });
        split.Panel1.Controls.Add(searchPanel);
        split.Panel1.Controls.Add(breadcrumb);
        split.Panel2.Controls.Add(grid);
        split.Panel2.Controls.Add(collectionHelp);
        Controls.Add(split);
        Controls.Add(relations);
        Controls.Add(commands);

        addItem = new("Add", null, (_, _) => Add(), Keys.Insert);
        duplicateItem = new("Duplicate…", null, (_, _) => Duplicate(), Keys.Control | Keys.D);
        deleteItem = new("Delete", null, (_, _) => Delete(), Keys.Delete);
        upItem = new("Move up", null, (_, _) => MoveItem(-1), Keys.Alt | Keys.Up);
        downItem = new("Move down", null, (_, _) => MoveItem(1), Keys.Alt | Keys.Down);
        tree.ContextMenuStrip = new();
        tree.ContextMenuStrip.Items.AddRange([addItem, duplicateItem, deleteItem, new ToolStripSeparator(), upItem, downItem]);

        foreach (ToolStripMenuItem command in (ToolStripMenuItem[])[addItem, duplicateItem, deleteItem])
        {
            ToolStripButton button = new(command.Text)
            {
                ToolTipText = command.Text + " (" + new KeysConverter().ConvertToString(command.ShortcutKeys) + ")"
            };

            button.Click += (_, _) => command.PerformClick();
            commandButtons.Add((button: button, command: command));
            commands.Items.Add(button);
        }

        commands.Items.AddRange([new ToolStripSeparator(), undoButton, redoButton]);
        relations.Items.AddRange([goButton, usedButton]);
        undoButton.Click += (_, _) => Undo();
        redoButton.Click += (_, _) => Redo();
        goButton.Click += (_, _) => GoToReference();
        usedButton.Click += (_, _) => ShowUses(SelectedObject, false);
        grid.SelectedGridItemChanged += (_, _) => UpdateCommands();
        search.TextChanged += (_, _) => Search();
        search.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Down && results.Items.Count > 0)
            {
                results.SelectedIndex = 0;
                results.Focus();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
                search.Clear();
        };
        results.SelectedIndexChanged += (_, _) =>
        {
            if (results.SelectedItem is ObjectGraph.Node node)
                Select(node.Path);
        };

        tree.NodeMouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
                tree.SelectedNode = e.Node;
        };
        tree.BeforeExpand += (_, e) =>
        {
            if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Tag == null)
                Fill(e.Node);
        };
        tree.AfterSelect += (_, _) =>
        {
            grid.SelectedObject = tree.SelectedNode?.Tag is IList
                ? null
                : tree.SelectedNode?.Tag;
            PrepareProperties();
            collectionHelp.Visible = SelectedObject is IList;
            collectionHelp.Text = SelectedObject is IList list
                ? $"{list.Count} items.\n\nUse Add to create an item, or select an item in the tree to edit it. Search finds items by name, ID or index."
                : "";
            breadcrumb.Text = tree.SelectedNode?.FullPath.Replace("\\", " / ");
            pathTip.SetToolTip(breadcrumb, breadcrumb.Text);
            UpdateCommands();
            SelectionChanged?.Invoke(tree.SelectedNode?.Tag);
        };
        grid.PropertyValueChanged += (_, _) =>
        {
            tree.SelectedNode?.Text = Label(tree.SelectedNode);
            RecordChange();
            RefreshTree();
            SelectionChanged?.Invoke(tree.SelectedNode?.Tag);
        };

        TreeNode rootNode = MakeNode(name, initialRoot, false);
        tree.Nodes.Add(rootNode);
        rootNode.Expand();
        tree.SelectedNode = rootNode;
    }

    private static Type ElementType(Type listType) => listType.IsGenericType
        ? listType.GetGenericArguments()[0]
        : listType.GetElementType();

    private static bool Owned(PropertyInfo property) => property.GetCustomAttribute<BrowsableAttribute>() is { Browsable: false };

    private static bool IsObject(Type type) => type != typeof(string) && !type.IsValueType && !type.IsArray
        && (!typeof(IList).IsAssignableFrom(type) || ElementType(type) is { IsValueType: false } element && element != typeof(string));

    private static IEnumerable<PropertyInfo> NodeProperties(object value) => value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && Owned(p) && IsObject(p.PropertyType));

    private static string Describe(object value) => TrafficReferences.Describe(value);

    private static string Label(string name, object value, bool item) =>
        value is IList list
            ? $"{TrafficLabels.Name(name)} ({list.Count})"
            : item
                ? $"{name}: {Describe(value)}"
                : TrafficLabels.Name(name);

    private static string Label(TreeNode node) => Label(node.Name, node.Tag, node.Parent?.Tag is IList);

    private static TreeNode MakeNode(string name, object value, bool item)
    {
        TreeNode node = new(Label(name, value, item))
        {
            Name = name,
            Tag = value
        };

        if (value is IList list
            ? list.Count > 0
            : NodeProperties(value).Any(p => p.GetValue(value) != null))
            node.Nodes.Add(new TreeNode()); // Filled in when it's expanded

        return node;
    }

    private void Fill(TreeNode node)
    {
        tree.BeginUpdate();
        node.Nodes.Clear();

        if (node.Tag is IList list)
        {
            for (int i = 0; i < list.Count; i++)
                node.Nodes.Add(MakeNode(i.ToString(), list[i], true));
        }
        else
        {
            foreach (PropertyInfo currentProperty in NodeProperties(node.Tag))
                if (currentProperty.GetValue(node.Tag) is object value)
                    node.Nodes.Add(MakeNode(currentProperty.Name, value, false));
        }

        tree.EndUpdate();
    }

    // Selects a node by the objects and lists on the way to it from the root, the root itself not included
    public void Select(object[] path)
    {
        TreeNode node = tree.Nodes[0];

        foreach (object step in path)
        {
            if (node.Nodes.Count == 1 && node.Nodes[0].Tag == null)
                Fill(node);

            node = node.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Tag == step);

            if (node == null)
                return;
        }

        node.EnsureVisible();
        tree.SelectedNode = node;
    }

    private TreeNode ListNode => tree.SelectedNode?.Tag is IList
        ? tree.SelectedNode
        : ItemNode?.Parent;

    private TreeNode ItemNode => tree.SelectedNode?.Parent?.Tag is IList
        ? tree.SelectedNode
        : null;

    private void UpdateCommands()
    {
        addItem.Enabled = ListNode != null;
        duplicateItem.Enabled = ItemNode != null || DuplicateRequested != null && TrafficCopy.CanCopy(SelectedObject);
        deleteItem.Enabled = ItemNode != null;
        upItem.Enabled = ItemNode?.PrevNode != null;
        downItem.Enabled = ItemNode?.NextNode != null;

        foreach ((ToolStripButton button, ToolStripMenuItem command) in commandButtons)
            button.Enabled = command.Enabled;

        undoButton.Enabled = CanUndo;
        redoButton.Enabled = CanRedo;
        goButton.Enabled = grid.SelectedGridItem?.Value is object value && !value.GetType().IsValueType && value is not string;
        usedButton.Enabled = SelectedObject != null && SelectedObject is not IList;
    }

    // Lists the node again, as indexes change, and selects the item that was changed
    private void Refill(TreeNode listNode, int select)
    {
        Fill(listNode);
        listNode.Text = Label(listNode);
        listNode.Expand();
        RecordChange();
        tree.SelectedNode = select >= 0 && select < listNode.Nodes.Count
            ? listNode.Nodes[select]
            : listNode;
        UpdateCommands();
    }

    private void Add()
    {
        if (ListNode is not TreeNode listNode)
            return;

        IList list = (IList)listNode.Tag;

        if (!CanGrow(listNode))
            return;

        list.Add(Activator.CreateInstance(ElementType(list.GetType())));
        Refill(listNode, list.Count - 1);
    }

    private void Duplicate()
    {
        if (DuplicateRequested?.Invoke(SelectedObject) == true)
            return;

        if (ItemNode is not TreeNode item)
            return;

        IList list = (IList)item.Parent.Tag;

        if (!CanGrow(item.Parent))
            return;

        list.Insert(item.Index + 1, Clone(list[item.Index]));
        Refill(item.Parent, item.Index + 1);
    }

    public void DuplicateSelection() => Duplicate();

    public void CompleteEdit(object selection = null)
    {
        RecordChange();
        RefreshTree();

        if (selection != null)
            SelectObject(selection);

        grid.Refresh();
        SelectionChanged?.Invoke(SelectedObject);
    }

    public void RefreshProperties() => grid.Refresh();

    public void AddPlacementCommands(params ToolStripItem[] items)
    {
        ToolStrip placement = new()
        {
            Dock = DockStyle.Top,
            GripStyle = ToolStripGripStyle.Hidden
        };

        placement.Items.AddRange(items);
        Controls.Add(placement);
        Controls.SetChildIndex(placement, Controls.GetChildIndex(relations));
    }

    private void Delete()
    {
        object selected = SelectedObject;

        if (!TryDeleteSelected(out List<ObjectGraph.Usage> references) && references.Count > 0)
            ShowUses(selected, true);
    }

    public bool TryDeleteSelected(out List<ObjectGraph.Usage> references)
    {
        references = [];

        if (ItemNode is not TreeNode item)
            return false;

        references = ObjectGraph.Incoming(root, item.Tag);

        if (references.Count > 0)
            return false;

        IList list = (IList)item.Parent.Tag;
        int index = item.Index;
        list.RemoveAt(index);
        Refill(item.Parent, Math.Min(index, list.Count - 1));
        return true;
    }

    private void MoveItem(int by)
    {
        if (ItemNode is not TreeNode item)
            return;

        IList list = (IList)item.Parent.Tag;
        int currentFrom = item.Index, to = currentFrom + by;

        if (to < 0 || to >= list.Count)
            return;

        (list[currentFrom], list[to]) = (list[to], list[currentFrom]);
        Refill(item.Parent, to);
    }

    public void MarkApplied()
    {
        history.MarkApplied();
        UpdateCommands();
    }

    public void RecordChange()
    {
        history.Record();
        UpdateCommands();
        Search();
        Changed?.Invoke();
    }

    public void Undo()
    {
        if (!ValidateChildren() || !history.Undo())
            return;

        RefreshTree();
        Changed?.Invoke();
        SelectionChanged?.Invoke(SelectedObject);
    }

    public void Redo()
    {
        if (!ValidateChildren() || !history.Redo())
            return;

        RefreshTree();
        Changed?.Invoke();
        SelectionChanged?.Invoke(SelectedObject);
    }

    public void FocusSearch()
    {
        search.Focus();
        search.SelectAll();
    }

    private void PrepareProperties()
    {
        GridItem rootItem = grid.SelectedGridItem;

        while (rootItem?.Parent != null)
            rootItem = rootItem.Parent;

        if (rootItem == null)
            return;

        GridItem firstTask = null;

        foreach (GridItem category in rootItem.GridItems)
        {
            if (category.GridItemType != GridItemType.Category)
                continue;

            category.Expanded = category.Label != "Advanced";

            if (category.Label == "Traffic flow")
                firstTask = category.GridItems.Cast<GridItem>().FirstOrDefault(i => i.PropertyDescriptor?.Name is "mfSpeed" or "VehicleMix") ?? category.GridItems.Cast<GridItem>().FirstOrDefault();
        }

        if (firstTask != null)
            grid.SelectedGridItem = firstTask;

        if (firstTask != null)
            foreach (GridItem category in rootItem.GridItems)
                if (category.GridItemType == GridItemType.Category)
                    category.Expanded = category.Label == "Traffic flow";
    }

    public void SelectObject(object value, string property = null)
    {
        ObjectGraph.Node node = ObjectGraph.Nodes(root).FirstOrDefault(n => n.Value == value);

        if (node == null)
            return;

        Select(node.Path);

        if (property != null)
        {
            GridItem item = grid.SelectedGridItem;

            while (item?.Parent != null)
                item = item.Parent;

            GridItem find(GridItem parent)
            {
                if (parent == null)
                    return null;

                if (parent.PropertyDescriptor?.Name == property)
                    return parent;

                foreach (GridItem child in parent.GridItems)
                    if (find(child) is GridItem found)
                        return found;

                return null;
            }

            if (find(item) is GridItem match)
                grid.SelectedGridItem = match;

            grid.Focus();
        }
    }

    private void RefreshTree()
    {
        object selected = SelectedObject;
        HashSet<object> expanded = new(ReferenceEqualityComparer.Instance);

        void remember(TreeNode node)
        {
            if (node.IsExpanded && node.Tag != null)
                expanded.Add(node.Tag);

            foreach (TreeNode child in node.Nodes)
                remember(child);
        }

        remember(tree.Nodes[0]);
        Fill(tree.Nodes[0]);

        void expand(TreeNode node)
        {
            if (node.Tag == null || !expanded.Contains(node.Tag))
                return;

            node.Expand();

            foreach (TreeNode child in node.Nodes)
                expand(child);
        }

        expand(tree.Nodes[0]);
        SelectObject(selected);
        tree.SelectedNode ??= tree.Nodes[0];
        grid.Refresh();
        Search();
        UpdateCommands();
    }

    private void Search()
    {
        results.BeginUpdate();
        results.Items.Clear();
        results.Visible = !string.IsNullOrWhiteSpace(search.Text);

        if (results.Visible)
            foreach (ObjectGraph.Node node in ObjectGraph.Nodes(root).Where(n => n.Value is not IList &&
                n.Label.Contains(search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)).Take(500))
                results.Items.Add(node);

        results.EndUpdate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            pathTip.Dispose();

        base.Dispose(disposing);
    }

    private bool CanGrow(TreeNode listNode)
    {
        int maximum = TrafficRules.CollectionLimit(listNode.Parent?.Tag, listNode.Name);

        if (((IList)listNode.Tag).Count < maximum)
            return true;

        MessageBox.Show(this, $"{TrafficLabels.Name(listNode.Name)} can contain at most {maximum} items.", "Collection full", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return false;
    }

    private void GoToReference()
    {
        object value = grid.SelectedGridItem?.Value;

        if (value is IList list)
        {
            using Form dialog = new()
            {
                Text = "Go to reference",
                Width = 520,
                Height = 320,
                StartPosition = FormStartPosition.CenterParent
            };

            ListBox choices = new()
            {
                Dock = DockStyle.Fill,
                DisplayMember = "Label"
            };

            choices.Items.AddRange([.. ObjectGraph.Nodes(root).Where(n => list.Contains(n.Value))]);
            Button go = new()
            {
                Text = "Go to item",
                Dock = DockStyle.Bottom
            };

            go.Click += (_, _) =>
            {
                if (choices.SelectedItem is ObjectGraph.Node node)
                {
                    Select(node.Path);
                    dialog.Close();
                }
            };
            dialog.Controls.Add(choices);
            dialog.Controls.Add(go);
            dialog.ShowDialog(this);
        }
        else if (value != null)
            SelectObject(value);
    }

    private void ShowUses(object value, bool deleting)
    {
        List<ObjectGraph.Usage> usages = ObjectGraph.Incoming(root, value);
        using Form dialog = new()
        {
            Text = deleting
                ? "This item is still in use"
                : "Used by",
            Width = 720,
            Height = 360,
            MinimumSize = new(450, 260),
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false
        };

        Label explanation = new()
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new(8),
            Text = deleting
                ? "Change these references before deleting this item. No items have been removed."
                : $"{usages.Count} reference(s). Choose one to edit its field."
        };

        ListBox list = new()
        {
            Dock = DockStyle.Fill,
            HorizontalScrollbar = true
        };

        list.Items.AddRange([.. usages]);
        FlowLayoutPanel buttons = new()
        {
            AutoSize = true,
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft
        };

        Button close = new()
        {
            Text = "Close",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };

        Button go = new()
        {
            Text = "Go to reference",
            AutoSize = true,
            Enabled = usages.Count > 0
        };

        go.Click += (_, _) =>
        {
            if (list.SelectedItem is ObjectGraph.Usage usage)
            {
                SelectObject(usage.Node.Value, usage.Property);
                dialog.Close();
            }
        };
        buttons.Controls.AddRange([close, go]);
        dialog.Controls.Add(list);
        dialog.Controls.Add(explanation);
        dialog.Controls.Add(buttons);
        dialog.CancelButton = close;

        if (usages.Count > 0)
            list.SelectedIndex = 0;

        dialog.ShowDialog(this);
    }

    // Copies what the object holds, and points references to anything copied at the copy
    public static object Clone(object value)
    {
        Dictionary<object, object> copies = new(ReferenceEqualityComparer.Instance);
        object copy = Copy(value, copies);

        foreach (object copied in (object[])[.. copies.Values])
            Relink(copied, copies);

        return copy;
    }

    private static object Copy(object value, Dictionary<object, object> copies)
    {
        switch (value)
        {
            case null or string:
                return value;
            case Array array:
                Array arrayCopy = (Array)array.Clone();
                for (int i = 0; i < arrayCopy.Length; i++)
                    arrayCopy.SetValue(Copy(arrayCopy.GetValue(i), copies), i);
                return arrayCopy;
            case IList list:
                IList listCopy = (IList)Activator.CreateInstance(list.GetType());
                foreach (object item in list)
                    listCopy.Add(Copy(item, copies));
                return listCopy;
        }

        Type currentType = value.GetType();

        if (currentType.IsValueType)
            return value;

        object copy = Activator.CreateInstance(currentType);
        copies[value] = copy;

        foreach (PropertyInfo currentProperty in currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!currentProperty.CanRead || !currentProperty.CanWrite || currentProperty.GetIndexParameters().Length > 0)
                continue;

            object propertyValue = currentProperty.GetValue(value);

            // References are kept, a list of them is a new list of the same ones
            currentProperty.SetValue(copy, Owned(currentProperty) || !IsObject(currentProperty.PropertyType)
                ? Copy(propertyValue, copies)
                : propertyValue is IList references
                    ? SameItems(references)
                    : propertyValue);
        }

        return copy;
    }

    private static IList SameItems(IList list)
    {
        IList copy = (IList)Activator.CreateInstance(list.GetType());

        foreach (object item in list)
            copy.Add(item);

        return copy;
    }

    private static void Relink(object copy, Dictionary<object, object> copies)
    {
        foreach (PropertyInfo currentProperty in copy.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!currentProperty.CanRead || !currentProperty.CanWrite || currentProperty.GetIndexParameters().Length > 0 || Owned(currentProperty) || !IsObject(currentProperty.PropertyType))
                continue;

            object value = currentProperty.GetValue(copy);

            if (value is IList references)
            {
                for (int i = 0; i < references.Count; i++)
                    if (references[i] != null && copies.TryGetValue(references[i], out object relinked))
                        references[i] = relinked;
            }
            else if (value != null && copies.TryGetValue(value, out object relinked))
            {
                currentProperty.SetValue(copy, relinked);
            }
        }
    }
}
