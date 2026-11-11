using System.Collections.Generic;
using System.Windows.Forms;
using BundleFormat;

namespace BaseHandlers;

public sealed partial class TrafficEditor : ResourceEditor
{
    private readonly Traffic data;
    private readonly TrafficView view;

    public TrafficEditor(Traffic source, BundleEntry initialEntry, bool promptForWorld = true) : base(initialEntry, "Traffic", "traffic")
    {
        data = (Traffic)ObjectTreeEditor.Clone(source);
        TrafficLabels.Install();
        Traffic previous = TrafficReferences.Data;
        TrafficReferences.Data = data;
        view = new(data)
        {
            Dock = DockStyle.Fill
        };

        Initialize(data, "Traffic", view);
        TrafficReferences.Preview = view.Highlight;
        TrafficReferences.RestorePreview = () => view.Highlight(Tree.SelectedObject);
        Disposed += (_, _) =>
        {
            TrafficReferences.Data = previous;
            TrafficReferences.Preview = null;
            TrafficReferences.RestorePreview = null;
        };

        InitializePlacement(EditMenu);
        InitializeWorldVisualization(promptForWorld);
    }

    protected override List<TrafficRules.Issue> FindIssues() => TrafficRules.Validate(data, ResourceEntry);

    protected override void WriteResource() => data.Write(ResourceEntry);
}
