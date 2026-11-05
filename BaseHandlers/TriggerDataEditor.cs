using PluginAPI;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace BaseHandlers;

public delegate void Notify();  // delegate
public partial class TriggerDataEditor : Form, IEntryEditor
{
    public event Notify EditEvent;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public TriggerData Trigger
    {
        get;
        set
        {
            field = value;
            Console.WriteLine(value.muSize);
            Console.WriteLine(field.muSize);
            Console.WriteLine(Trigger.muSize);

            UpdateComponent();
        }
    }

    public void UpdateComponent()
    {
        propertyGrid1.SelectedObject = Trigger;
    }

    public TriggerDataEditor()
    {
        InitializeComponent();
        UpdateComponent();
    }

    private void TriggerDataEditor_FormClosed(object s, FormClosedEventArgs e)
    {
        EditEvent?.Invoke();
    }

    private void importYamlToolStripMenuItem_Click(object sender, EventArgs e)
    {
        OpenFileDialog openDialog = new()
        {
            Filter = "YAML files (*.yaml;*.yml)|*.yaml;*.yml|All files (*.*)|*.*"
        };

        if (openDialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            ObjectYAML.Populate(Trigger, File.ReadAllText(openDialog.FileName));
        }
        catch (Exception ex) when (ex is FormatException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"There was an issue importing: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        propertyGrid1.Refresh();
    }

    private void exportYamlToolStripMenuItem_Click(object sender, EventArgs e)
    {
        SaveFileDialog saveDialog = new()
        {
            Filter = "YAML files (*.yaml)|*.yaml|All files (*.*)|*.*",
            FileName = "triggers.yaml"
        };

        if (saveDialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            File.WriteAllText(saveDialog.FileName, ObjectYAML.Serialize(Trigger));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, $"There was an issue exporting: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
