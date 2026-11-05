using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Windows.Forms;

namespace BaseHandlers;

public sealed class TrafficReferenceEditor : UITypeEditor
{
    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) => UITypeEditorEditStyle.Modal;

    public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
    {
        if (context?.PropertyDescriptor == null)
            return value;

        bool multiple = typeof(IList).IsAssignableFrom(context.PropertyDescriptor.PropertyType);
        Type currentType = multiple
            ? context.PropertyDescriptor.PropertyType.GetGenericArguments()[0]
            : context.PropertyDescriptor.PropertyType;
        using TrafficReferenceDialog dialog = new(context.Instance, context.PropertyDescriptor.Name, currentType, multiple, value);

        if (dialog.ShowDialog() != DialogResult.OK)
            return value;

        if (!multiple)
            return dialog.SelectedValues.FirstOrDefault();

        object[] selected = dialog.SelectedValues;

        if (value is IList current && current.Cast<object>().SequenceEqual(selected))
            return value;

        IList result = (IList)Activator.CreateInstance(context.PropertyDescriptor.PropertyType);

        foreach (object item in selected)
            result.Add(item);

        return result;
    }
}

public sealed class TrafficReferenceDialog : Form
{
    private sealed record Choice(object Value, string Label)
    {
        public override string ToString() => Label;
    }

    private readonly ListBox candidates = new()
    {
        Dock = DockStyle.Fill,
        IntegralHeight = false
    };

    private readonly ListBox chosen = new()
    {
        Dock = DockStyle.Fill,
        IntegralHeight = false
    };

    private readonly Label status = new()
    {
        Dock = DockStyle.Fill,
        AutoSize = true
    };

    private readonly Choice[] choices;
    private readonly bool multiple;
    private readonly int maximum;

    public object[] SelectedValues => multiple
        ? chosen.Items.Cast<Choice>().Select(c => c.Value).ToArray()
        : candidates.SelectedItem is Choice choice && choice.Value != null
            ? [choice.Value]
            : [];

    public TrafficReferenceDialog(object owner, string property, Type type, bool initialMultiple, object value)
    {
        multiple = initialMultiple;
        maximum = TrafficRules.CollectionLimit(owner, property);
        choices = [.. TrafficReferences.Candidates(owner, property, type).Select(c => new Choice(c.Value,
            $"{c.Key}: {TrafficReferences.Describe(c.Value)}"))];
        Text = "Choose " + TrafficLabels.Name(property).ToLowerInvariant();
        ClientSize = new(initialMultiple
            ? 760
            : 520, 450);
        MinimumSize = new(initialMultiple
            ? 620
            : 400, 340);
        AutoScaleMode = AutoScaleMode.Font;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = MaximizeBox = false;
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            Padding = new(10),
            ColumnCount = 1,
            RowCount = 4
        };

        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.Percent, 100));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.AutoSize));
        TextBox filter = new()
        {
            Dock = DockStyle.Fill,
            PlaceholderText = "Search names, IDs or indexes",
            AccessibleName = "Search reference candidates"
        };

        layout.Controls.Add(filter, 0, 0);
        TableLayoutPanel lists = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = initialMultiple
                ? 3
                : 1
        };

        lists.ColumnStyles.Add(new(SizeType.Percent, 50));
        Panel available = new()
        {
            Dock = DockStyle.Fill
        };

        available.Controls.Add(candidates);
        available.Controls.Add(new Label()
        {
            Text = "Available items",
            AutoSize = true,
            Dock = DockStyle.Top
        });
        lists.Controls.Add(available, 0, 0);

        if (initialMultiple)
        {
            lists.ColumnStyles.Add(new(SizeType.AutoSize));
            lists.ColumnStyles.Add(new(SizeType.Percent, 50));
            FlowLayoutPanel actions = new()
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown
            };

            Button Button(string text, Action click)
            {
                Button b = new()
                {
                    Text = text,
                    AutoSize = true
                };

                b.Click += (_, _) => click();
                actions.Controls.Add(b);
                return b;
            }

            Button("Add →", Add);
            Button("← Remove", () =>
            {
                if (chosen.SelectedIndex >= 0)
                    chosen.Items.RemoveAt(chosen.SelectedIndex);

                UpdateStatus();
            });
            Button("Move up", () => MoveChoice(-1));
            Button("Move down", () => MoveChoice(1));
            lists.Controls.Add(actions, 1, 0);
            Panel selected = new()
            {
                Dock = DockStyle.Fill
            };

            selected.Controls.Add(chosen);
            selected.Controls.Add(new Label()
            {
                Text = "Selected items (saved order)",
                AutoSize = true,
                Dock = DockStyle.Top
            });
            lists.Controls.Add(selected, 2, 0);

            if (value is IList items)
                foreach (object item in items)
                    chosen.Items.Add(choices.FirstOrDefault(c => c.Value == item) ?? new(item, "Missing reference — remove or replace"));

            candidates.DoubleClick += (_, _) => Add();
        }

        layout.Controls.Add(lists, 0, 1);
        layout.Controls.Add(status, 0, 2);
        FlowLayoutPanel buttons = new()
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };

        Button cancel = new()
        {
            Text = "Cancel",
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };

        Button ok = new()
        {
            Text = "OK",
            AutoSize = true
        };

        ok.Click += (_, _) =>
        {
            if (initialMultiple && chosen.Items.Count > maximum)
            {
                status.Text = $"Choose at most {maximum} items.";
                return;
            }

            if (initialMultiple && chosen.Items.Cast<Choice>().Any(c => !choices.Any(v => v.Value == c.Value)))
            {
                status.Text = "Remove or replace missing references.";
                return;
            }

            DialogResult = DialogResult.OK;
        };
        buttons.Controls.AddRange([cancel, ok]);
        layout.Controls.Add(buttons, 0, 3);
        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = cancel;

        void Filter()
        {
            object selected = (candidates.SelectedItem as Choice)?.Value ?? value;
            candidates.Items.Clear();

            if (!initialMultiple)
                candidates.Items.Add(new Choice(null, "(none)"));

            foreach (Choice choice in choices.Where(c => c.Label.Contains(filter.Text, StringComparison.CurrentCultureIgnoreCase)))
                candidates.Items.Add(choice);

            candidates.SelectedItem = candidates.Items.Cast<Choice>().FirstOrDefault(c => c.Value == selected);
        }

        filter.TextChanged += (_, _) => Filter();
        candidates.SelectedIndexChanged += (_, _) => TrafficReferences.Preview?.Invoke((candidates.SelectedItem as Choice)?.Value);
        chosen.SelectedIndexChanged += (_, _) => TrafficReferences.Preview?.Invoke((chosen.SelectedItem as Choice)?.Value);
        FormClosed += (_, _) => TrafficReferences.RestorePreview?.Invoke();
        Filter();
        UpdateStatus();
    }

    private void Add()
    {
        if (candidates.SelectedItem is not Choice choice)
            return;

        if (chosen.Items.Count >= maximum)
        {
            status.Text = $"Choose at most {maximum} items.";
            return;
        }

        if (!chosen.Items.Cast<Choice>().Any(c => c.Value == choice.Value))
            chosen.SelectedIndex = chosen.Items.Add(choice);

        UpdateStatus();
    }

    private void MoveChoice(int by)
    {
        int currentFrom = chosen.SelectedIndex, to = currentFrom + by;

        if (currentFrom < 0 || to < 0 || to >= chosen.Items.Count)
            return;

        object item = chosen.Items[currentFrom];
        chosen.Items.RemoveAt(currentFrom);
        chosen.Items.Insert(to, item);
        chosen.SelectedIndex = to;
    }

    private void UpdateStatus() => status.Text = multiple
        ? $"{chosen.Items.Count} selected · Maximum {maximum}. The right list preserves the saved order."
        : "Choose an item. Its geometry is highlighted in the traffic view when available.";
}
