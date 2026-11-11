using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using BundleFormat;
using OpenTK.Mathematics;
using static BaseHandlers.AISections;
using NVector3 = System.Numerics.Vector3;
using NVector4 = System.Numerics.Vector4;
using Vertex = BaseHandlers.TrafficWorldGeometry.Vertex;

namespace BaseHandlers;

public sealed class AISectionsEditor : ResourceEditor
{
    private readonly AISections data;

    public AISectionsEditor(AISections source, BundleEntry initialEntry, bool promptForWorld = true) : base(initialEntry, "AI sections", "AI sections")
    {
        data = (AISections)ObjectTreeEditor.Clone(source);
        TrafficLabels.Install();
        TrafficLabels.Development = data.Development;
        AISections previous = TrafficReferences.AIData;
        TrafficReferences.AIData = data;
        AISectionsView view = new(data)
        {
            Dock = DockStyle.Fill
        };

        Initialize(data, "AISections", view);
        Tree.SearchPrompt = data.Development
            ? "Search sections"
            : "Search section IDs";
        Tree.CollectionLimit = (_, list) => list switch
        {
            nameof(AISections.maSections) => 20000,
            nameof(AISection.maPortals) => 32,
            nameof(AISections.maSectionResetPairs) when data.Development => 0,
            _ => int.MaxValue
        };

        TrafficReferences.Preview = view.Highlight;
        TrafficReferences.RestorePreview = () => view.Highlight(Tree.SelectedObject);
        Disposed += (_, _) =>
        {
            TrafficLabels.Development = false;
            TrafficReferences.AIData = previous;
            TrafficReferences.Preview = null;
            TrafficReferences.RestorePreview = null;
        };

        InitializeWorldVisualization(promptForWorld);
    }

    protected override List<TrafficRules.Issue> FindIssues() => [.. data.Problems()];

    protected override void WriteResource() => data.Write(ResourceEntry);
}

// 3D visualization of AI sections, portals, and connectivity
public sealed class AISectionsView : SceneView
{
    private static readonly string[] layerNames = ["Sections", "Portals", "Boundary lines", "No-go lines", "Links", "Reset pairs"];
    private const int Sections = 0, Portals = 1, Boundaries = 2, NoGo = 3, Links = 4, Resets = 5;

    private static readonly uint[] speedColours = [Colour(90, 130, 255), Colour(60, 200, 220), Colour(120, 220, 90), Colour(240, 200, 60), Colour(250, 90, 60)];

    private static readonly uint highlightColour = Colour(255, 230, 0), linkColour = Colour(80, 255, 80), incomingColour = Colour(255, 90, 60),
        noGoColour = Colour(170, 170, 180), resetColour = Colour(255, 80, 255);

    private readonly AISections data;

    public AISectionsView(AISections initialData) : base(layerNames)
    {
        data = initialData;
        Tips.SetToolTip(this, data.Development
            ? "Sections are coloured by danger rating: red for freeway, green for normal, blue for dangerous."
            : "Sections are coloured by speed band, from blue (very slow) to red (very fast).");
        Rebuild();
        FrameAll();
    }

    private uint SectionColour(AISection section) => data.Development
        ? speedColours[Math.Max(4 - 2 * (int)section.muDangerRating, 0)]
        : speedColours[Math.Min((int)section.muSpeed, speedColours.Length - 1)];

    private static float Height(AISection section) => section.maPortals.Count > 0
        ? section.maPortals[0].mPosition.Y
        : 0;

    private static Vector3 At(NVector3 v) => new(v.X, v.Y, v.Z);

    // Y coordinate is derived from the first portal
    private static Vector3 Middle(AISection section)
    {
        float x = 0, z = 0;

        foreach (System.Numerics.Vector2 corner in section.maCorners)
        {
            x += corner.X;
            z += corner.Y;
        }

        return new(x / 4, Height(section), z / 4);
    }

    private static Vector3 Corner(AISection section, int index) => new(section.maCorners[index % 4].X, Height(section), section.maCorners[index % 4].Y);

    private static void Outline(List<Vertex> to, AISection section, uint colour)
    {
        for (int i = 0; i < 4; i++)
            Line(to, Corner(section, i), Corner(section, i + 1), colour);
    }

    private static void BoundaryLine(List<Vertex> to, NVector4 line, float height, uint colour) =>
        Line(to, new(line.X, height, line.Y), new(line.Z, height, line.W), colour);

    private static void Walls(List<Vertex> to, AISection section, uint colour)
    {
        foreach (NVector4 line in section.maNoGoLines)
            BoundaryLine(to, line, Height(section) + 0.5f, colour);
    }

    private static void PortalMarker(List<Vertex> to, Portal portal, uint colour)
    {
        Cross(to, At(portal.mPosition), 1.5f, colour);

        foreach (NVector4 line in portal.maBoundaryLines)
            BoundaryLine(to, line, portal.mPosition.Y, colour);
    }

    private static void Arrow(List<Vertex> to, Vector3 from, Vector3 at, uint colour)
    {
        Line(to, from, at, colour);
        Vector3 direction = at - from;

        if (direction.LengthSquared < 1e-6f)
            return;

        direction = direction.Normalized() * Math.Min(3, direction.Length / 3);
        Vector3 side = new(-direction.Z / 2, 0, direction.X / 2);
        Line(to, at, at - direction + side, colour);
        Line(to, at, at - direction - side, colour);
    }

    protected override void BuildScene()
    {
        foreach (AISection section in data.maSections)
        {
            object[] path = [data.maSections, section];
            Outline(Layer(Sections), section, SectionColour(section));
            AddPickable(Middle(section), Sections, path);

            for (int i = 0; i < 4; i++)
                AddSegment(Corner(section, i), Corner(section, i + 1), Sections, path);

            Walls(Layer(NoGo), section, noGoColour);

            foreach (Portal portal in section.maPortals)
            {
                Cross(Layer(Portals), At(portal.mPosition), 1.5f, Colour(255, 255, 255));
                AddPickable(At(portal.mPosition), Portals, [data.maSections, section, section.maPortals, portal]);

                foreach (NVector4 line in portal.maBoundaryLines)
                    BoundaryLine(Layer(Boundaries), line, portal.mPosition.Y, Colour(0, 220, 255));

                if (portal.mLinkSection != null)
                    Line(Layer(Links), At(portal.mPosition), Middle(portal.mLinkSection), Colour(120, 160, 120));
            }
        }

        foreach (SectionResetPair pair in data.maSectionResetPairs.Where(p => p.mStartSection != null && p.mResetSection != null))
        {
            Vector3 start = Middle(pair.mStartSection), end = Middle(pair.mResetSection);
            Arrow(Layer(Resets), start, end, resetColour);
            AddPickable((start + end) / 2, Resets, [data.maSectionResetPairs, pair]);
        }
    }

    protected override void AddHighlight(object item, int depth)
    {
        switch (item)
        {
            case AISections or null:
                return;
            case IList list:
                foreach (object entry in list)
                    AddHighlight(entry, depth);
                return;
            case AISection section:
                Outline(HighlightLines, section, highlightColour);
                Walls(HighlightLines, section, highlightColour);
                if (depth > 0)
                    return;
                foreach (Portal portal in section.maPortals)
                {
                    PortalMarker(HighlightLines, portal, highlightColour);

                    if (portal.mLinkSection != null)
                    {
                        Outline(HighlightLines, portal.mLinkSection, linkColour);
                        Line(HighlightLines, Middle(section), Middle(portal.mLinkSection), linkColour);
                    }
                }
                foreach (AISection other in data.maSections.Where(s => s != section && s.maPortals.Any(p => p.mLinkSection == section) && !section.maPortals.Any(p => p.mLinkSection == s)))
                {
                    Outline(HighlightLines, other, incomingColour);
                    Line(HighlightLines, Middle(other), Middle(section), incomingColour);
                }
                foreach (SectionResetPair pair in data.maSectionResetPairs.Where(p => p.mStartSection == section || p.mResetSection == section))
                    AddHighlight(pair, depth + 1);
                return;
            case Portal portal:
                PortalMarker(HighlightLines, portal, highlightColour);
                if (portal.mLinkSection != null)
                    Outline(HighlightLines, portal.mLinkSection, linkColour);
                return;
            case SectionResetPair pair:
                if (pair.mStartSection == null || pair.mResetSection == null)
                    return;
                Outline(HighlightLines, pair.mStartSection, highlightColour);
                Outline(HighlightLines, pair.mResetSection, linkColour);
                Arrow(HighlightLines, Middle(pair.mStartSection), Middle(pair.mResetSection), resetColour);
                return;
        }
    }
}
