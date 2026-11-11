using System.ComponentModel;
using System.Numerics;
using System.Reflection;
using BaseHandlers;
using BundleFormat;
using BundleUtilities;
using OpenTK.GLControl;
using Vertex = BaseHandlers.TrafficWorldGeometry.Vertex;

internal static partial class Program
{
    private static BundleEntry Entry(byte[] bytes, BundlePlatform platform)
    {
        BundleEntry entry = Entry(bytes);
        entry.Platform = platform;
        entry.Archive.Platform = platform;
        return entry;
    }

    private static AISections.AISection AISection(uint id, float x)
    {
        AISections.AISection section = new()
        {
            mId = id,
            miSpanIndex = 7,
            maCorners = [new(x, 0), new(x, 10), new(x + 10, 10), new(x + 10, 0)],
            maNoGoLines = [new(x, 0, x, 10)]
        };

        return section;
    }

    private static AISections AISample()
    {
        AISections data = new()
        {
            mafSectionMinSpeeds = [1, 2, 3, 4, 5],
            mafSectionMaxSpeeds = [6, 7, 8, 9, 10],
            maSections = [AISection(0x100, 0), AISection(0x200, 10), AISection(0x300, 20)]
        };

        for (int i = 0; i < 3; i++)
        {
            data.maSections[i].maPortals.Add(new()
            {
                mPosition = new(i * 10 + 10, 2, 5),
                maBoundaryLines = [new(i * 10 + 10, 0, i * 10 + 10, 10)],
                mLinkSection = data.maSections[(i + 1) % 3]
            });
        }

        data.maSectionResetPairs.Add(new()
        {
            meResetSpeed = AISections.EResetSpeedType.E_RESET_SPEED_TYPE_SLOW_REVERSE,
            mStartSection = data.maSections[0],
            mResetSection = data.maSections[2]
        });

        return data;
    }

    private static PropPhysics.PropTypeData PropType(uint uri)
    {
        PropPhysics.PropTypeData type = new()
        {
            muSceneUriId = uri,
            mResourceId = uri,
            mfMass = 50,
            mfSphereRadius = 2
        };

        type.maCollisionVolumes.Add(new()
        {
            mExtent = new(1, 2, 3)
        });

        return type;
    }

    private static PropPhysics PropSample()
    {
        PropPhysics data = new()
        {
            muTimeStamp = 0x47AAE3EA,
            NoPartsPointer = 0xFDD1F800,
            maPropTypes = [PropType(1), PropType(2), PropType(3)]
        };

        data.maPropTypes[1].maParts.Add(new()
        {
            mOffset = new(0, 1, 0, 0),
            mfMass = 5,
            mfSphereRadius = 1,
            maCollisionVolumes = [new()
            {
                meType = PropPhysics.VolumeType.E_VOLUMETYPE_CAPSULE,
                mExtent = new(2, 0, 0),
                mfRadius = 0.5f
            }]
        });

        return data;
    }

    private static void CheckAIAndProps()
    {
        foreach ((BundlePlatform platform, bool bigEndian, int pointerSize) in ((BundlePlatform, bool, int)[])[(BundlePlatform.PC, false, 4), (BundlePlatform.X360, true, 4), (BundlePlatform.PCx64, false, 8)])
        {
            byte[] saved = AISample().Serialize(bigEndian, pointerSize);
            AISections read = new();
            read.Read(Entry(saved, platform));
            Check(read.maSections.Count == 3 && read.maSections[0].maPortals[0].mLinkSection == read.maSections[1] && read.maSectionResetPairs[0].mResetSection == read.maSections[2]
                && read.maSections[1].maNoGoLines[0] == new Vector4(10, 0, 10, 10) && read.maSections[2].maCorners[3] == new Vector2(30, 0) && read.mafSectionMaxSpeeds[4] == 10,
                $"AI sections keep their links and geometry when saved for {platform}");
            Equal(read.Serialize(bigEndian, pointerSize), saved, $"AI sections save the same bytes after reopening for {platform}");

            byte[] savedProps = PropSample().Serialize(bigEndian, pointerSize);
            PropPhysics readProps = new();
            readProps.Read(Entry(savedProps, platform));
            Check(readProps.maPropTypes.Count == 3 && readProps.maPropTypes[1].maParts[0].maCollisionVolumes[0].meType == PropPhysics.VolumeType.E_VOLUMETYPE_CAPSULE
                && readProps.maPropTypes[2].maCollisionVolumes[0].mExtent == new Vector3(1, 2, 3) && readProps.muTimeStamp == 0x47AAE3EA && readProps.NoPartsPointer == 0xFDD1F800,
                $"Prop physics keeps its parts and volumes when saved for {platform}");
            Equal(readProps.Serialize(bigEndian, pointerSize), savedProps, $"Prop physics saves the same bytes after reopening for {platform}");
        }

        void aiRefused(string what, Action<AISections> edit, string expect)
        {
            AISections data = AISample();
            edit(data);
            Check(data.Problems().FirstOrDefault()?.Message.Contains(expect) == true, what);
            Check(Throws<InvalidDataException>(() => data.Serialize(false, 4)), what + " can't be saved");
        }

        aiRefused("A section with no portals is reported", data => data.maSections[1].maPortals.Clear(), "1 to 32");
        aiRefused("A portal with no boundary line is reported", data => data.maSections[0].maPortals[0].maBoundaryLines = [], "boundary lines");
        aiRefused("A portal leading nowhere is reported", data => data.maSections[0].maPortals[0].mLinkSection = null, "doesn't lead");
        aiRefused("A portal into a deleted section is reported", data => data.maSections[0].maPortals[0].mLinkSection = new(), "doesn't lead");
        aiRefused("A speed outside the bands is reported", data => data.maSections[0].muSpeed = (AISections.SectionSpeed)5, "5 bands");
        aiRefused("A section without 4 corners is reported", data => data.maSections[0].maCorners = new Vector2[3], "4 corners");
        aiRefused("A reset pair with no section is reported", data => data.maSectionResetPairs[0].mResetSection = null, "reset to");

        void propRefused(string what, Action<PropPhysics> edit, string expect)
        {
            PropPhysics data = PropSample();
            edit(data);
            Check(data.Problems().FirstOrDefault()?.Message.Contains(expect) == true, what);
            Check(Throws<InvalidDataException>(() => data.Serialize(false, 4)), what + " can't be saved");
        }

        propRefused("Too many prop types are reported", data => data.maPropTypes.AddRange(Enumerable.Range(0, 498).Select(i => PropType((uint)i))), "room for 500");
        propRefused("A volume type the game can't load is reported", data => data.maPropTypes[0].maCollisionVolumes[0].meType = (PropPhysics.VolumeType)6, "type the game");
        propRefused("A massless prop is reported", data => data.maPropTypes[0].mfMass = 0, "mass above 0");
        propRefused("More volumes than the game can number are reported", data => data.maPropTypes[0].maCollisionVolumes.AddRange(Enumerable.Range(0, 63).Select(_ => new PropPhysics.Volume())), "6 bits");

        AISections aiSource = AISample();
        BundleEntry aiEntry = Entry(aiSource.Serialize(false, 4));
        byte[] aiBefore = [.. aiEntry.GetPrimaryWithoutImports()];
        using (AISectionsEditor editor = new(aiSource, aiEntry, false))
        {
            Hidden(editor);
            AISections draft = Field<AISections>(editor, "data");
            ObjectTreeEditor tree = Field<ObjectTreeEditor>(editor, "tree");
            Check(!ReferenceEquals(draft, aiSource) && draft.maSections[0].maPortals[0].mLinkSection == draft.maSections[1], "The AI editor works on a draft with its links kept inside it");

            AISections.AISection first = draft.maSections[0];
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(first);
            PropertyDescriptor shortcut = properties["Shortcut"];
            shortcut.SetValue(first, true);
            Check((first.mx8Flags & 1) == 1 && (bool)shortcut.GetValue(first), "A flag switch sets its bit in the section's flags");
            shortcut.SetValue(first, false);
            Check(first.mx8Flags == 0, "A flag switch clears only its bit");
            TypeConverter speed = properties["muSpeed"].Converter;
            Check(speed.ConvertToString(AISections.SectionSpeed.E_SECTION_SPEED_VERY_SLOW) == "Very Slow" && speed.ConvertToString(AISections.SectionSpeed.E_SECTION_SPEED_SLOW) == "Slow"
                && (AISections.SectionSpeed)speed.ConvertFromString("Very Slow") == AISections.SectionSpeed.E_SECTION_SPEED_VERY_SLOW, "Similar speed names stay apart and convert back");
            Check(properties["mu8eDistrict"].DisplayName == "District" && properties["muSpeed"].Category == "Properties", "AI fields use their own names and categories");

            AISections.Portal portal = first.maPortals[0];
            Check(TrafficReferences.Candidates(portal, "mLinkSection", typeof(AISections.AISection)).Count == 3
                && TrafficReferences.Resolve(portal, "mLinkSection", typeof(AISections.AISection), "2: anything") == draft.maSections[2]
                && TrafficReferences.Label(draft.maSections[1], portal, "mLinkSection", typeof(AISections.AISection)).StartsWith("1: 0x200"), "A portal's link is chosen from the sections by index and shows the section");

            tree.SelectObject(draft.maSections[1]);
            Check(!tree.TryDeleteSelected(out List<ObjectGraph.Usage> uses) && uses.Count > 0, "A section other sections link to can't be deleted");
            first.mId = 0x999;
            tree.RecordChange();
            Check(aiSource.maSections[0].mId == 0x100 && aiEntry.GetPrimaryWithoutImports().SequenceEqual(aiBefore), "AI edits stay in the draft until they're applied");
            Check((bool)Call(editor, "ApplyChanges"), "AI Apply accepts valid edits");
            AISections applied = new();
            applied.Read(aiEntry);
            Check(applied.maSections[0].mId == 0x999, "AI Apply writes the edit to the resource");
            tree.Undo();
            Check(first.mId == 0x100 && tree.Modified, "Undo restores an applied AI edit");
            draft.maSections[2].maPortals.Clear();
            tree.RecordChange();
            Check(Field<ListBox>(editor, "issues").Items.Count > 0 && !(bool)Call(editor, "ApplyChanges"), "AI Apply is blocked by what would crash the game, and the issue is listed");
        }

        AISections aiView = AISample();
        using (AISectionsView view = new(aiView))
        {
            List<Vertex>[] layers = Field<List<Vertex>[]>(view, "layers");
            Check(layers[0].Count == 3 * 4 * 2 && layers[1].Count > 0 && layers[2].Count == 3 * 2 && layers[3].Count == 3 * 2 && layers[4].Count == 3 * 2 && layers[5].Count > 0, "Every kind of AI data has a layer in the 3D view");
            Check(layers[0].Min(v => v.Position.Y) == 2, "A section is drawn at its first portal's height, as the game takes it");
            GLControl gl = Field<GLControl>(view, "gl");
            gl.Size = new(800, 500);
            OpenTK.Mathematics.Matrix4 matrix = (OpenTK.Mathematics.Matrix4)Call(view, "ViewProjection");
            OpenTK.Mathematics.Vector4 projected = new OpenTK.Mathematics.Vector4(25, 2, 0, 1) * matrix;
            object[] picked = null;
            view.Picked += path => picked = path;
            Call(view, "Pick", new Point((int)((projected.X / projected.W + 1) * 400), (int)((1 - projected.Y / projected.W) * 250)));
            Check(picked?.Last() == aiView.maSections[2], "Clicking a section's edge selects it");
            view.Highlight(aiView.maSections[0]);
            Check(Field<List<Vertex>>(view, "highlight").Count > 8, "Selecting a section highlights it and what it links to");
        }

        PropPhysics propSource = PropSample();
        BundleEntry propEntry = Entry(propSource.Serialize(false, 4));
        using (PropPhysicsEditor editor = new(propSource, propEntry))
        {
            Hidden(editor);
            PropPhysics draft = Field<PropPhysics>(editor, "data");
            ObjectTreeEditor tree = Field<ObjectTreeEditor>(editor, "tree");
            tree.SelectObject(draft.maPropTypes[0]);
            Check(!tree.TryDeleteSelected(out List<ObjectGraph.Usage> uses) && uses.Count == 0 && draft.maPropTypes.Count == 3, "A prop type other resources find by position can't be deleted from the middle");
            Check(!Field<ToolStripMenuItem>(tree, "upItem").Enabled && !Field<ToolStripMenuItem>(tree, "downItem").Enabled, "Prop types can't be reordered");
            PropPhysics.PropTypeData original = draft.maPropTypes[0];
            Call(tree, "Duplicate");
            Check(draft.maPropTypes.Count == 4 && draft.maPropTypes[0] == original && draft.maPropTypes[3] != original && draft.maPropTypes[3].muSceneUriId == 1
                && draft.maPropTypes[3].maCollisionVolumes[0] != original.maCollisionVolumes[0], "Duplicating a prop type adds a separate copy at the end");
            Check(tree.SelectedObject == draft.maPropTypes[3], "The copy is selected");
            Check(tree.TryDeleteSelected(out _) && draft.maPropTypes.Count == 3, "The last prop type can be deleted");
            tree.SelectObject(draft.maPropTypes[0].maCollisionVolumes);
            Call(tree, "Add");
            Check(draft.maPropTypes[0].maCollisionVolumes.Count == 2 && !Field<ListBox>(editor, "issues").Visible, "Volumes can be added to a prop type");
            Check((bool)Call(editor, "ApplyChanges"), "Prop Apply accepts valid edits");
            PropPhysics applied = new();
            applied.Read(propEntry);
            Check(applied.maPropTypes[0].maCollisionVolumes.Count == 2 && propSource.maPropTypes[0].maCollisionVolumes.Count == 1, "Prop Apply writes the resource, not the object it was opened from");
        }

        CheckDevelopmentLayouts();
        CheckBnd1Saving();
        CheckClusteredMeshBuilder();

        PropPhysics propView = PropSample();
        using (PropPhysicsView view = new(propView))
        {
            List<Vertex>[] layers = Field<List<Vertex>[]>(view, "layers");
            Check(layers[0].Count == 3 * 24 && layers[1].Count > 0 && layers[2].Count > 0, "Prop volumes and part volumes are drawn");
            Check(!Field<CheckBox[]>(view, "layerBoxes")[4].Checked, "Bounding spheres start hidden");
            object beforeTarget = FindField(typeof(PropPhysicsView), "target").GetValue(view);
            view.Highlight(propView.maPropTypes[2]);
            Check(!Equals(beforeTarget, FindField(typeof(PropPhysicsView), "target").GetValue(view)) && Field<float>(view, "distance") < 25, "Selecting a prop type brings it into view");
            object framed = FindField(typeof(PropPhysicsView), "target").GetValue(view);
            view.Highlight(propView.maPropTypes[2].maCollisionVolumes[0]);
            Check(Equals(framed, FindField(typeof(PropPhysicsView), "target").GetValue(view)), "Moving around within a prop type keeps the camera");
            OpenTK.Mathematics.Vector3[] highlighted = [.. Field<List<Vertex>>(view, "highlight").Select(v => v.Position)];
            view.Highlight(propView.maPropTypes[1].maParts[0]);
            Check(highlighted.Length == 24 && Field<List<Vertex>>(view, "highlight").Count > 24, "A capsule part is drawn with its rings");
        }
    }

    private static BundleEntry Bnd1Entry(byte[] bytes)
    {
        BundleEntry entry = Entry(bytes, BundlePlatform.X360);
        entry.Archive.BNDVersion = 1;
        return entry;
    }

    private static void CheckDevelopmentLayouts()
    {
        AISections ai = AISample();
        ai.Development = true;
        ai.muVersion = 4;
        ai.maSectionResetPairs.Clear();
        ai.maSections[1].miSpanIndex = 100157;
        ai.maSections[2].muDangerRating = AISections.DangerRating.E_DANGER_RATING_DANGEROUS;
        ai.maSections[2].mx8Flags = 4;

        byte[] savedAI = ai.Serialize(true, 4);
        AISections readAI = new();
        BundleEntry aiEntry = Bnd1Entry(savedAI);
        readAI.Read(aiEntry);
        Check(readAI.Development && readAI.muVersion == 4 && readAI.maSections[1].miSpanIndex == 100157 && readAI.maSections[2].muDangerRating == AISections.DangerRating.E_DANGER_RATING_DANGEROUS
            && readAI.maSections[0].maPortals[0].mLinkSection == readAI.maSections[1] && readAI.maSections[2].maCorners[3] == new Vector2(30, 0) && readAI.maSections[0].maNoGoLines[0] == new Vector4(0, 0, 0, 10),
            "A BND1 bundle's AI sections keep their links and geometry");
        Equal(readAI.Serialize(true, 4), savedAI, "A BND1 bundle's AI sections save the same bytes after reopening");
        Check(savedAI.Length > 0x10 && BitConverter.ToUInt32([.. savedAI[0x10..0x14].Reverse()]) == 0xB0, "Development AI sections are 0x34 bytes each, with the portals at the next 16 bytes after them");

        byte[] olderAI = [.. savedAI];
        olderAI[0x13] = 0xA0;
        Check(Throws<ReadFailedError>(() => new AISections().Read(Bnd1Entry(olderAI))), "The smaller sections of the Nov 2006 builds are refused rather than misread");

        AISections releaseSpan = AISample();
        releaseSpan.maSections[0].miSpanIndex = 40000;
        Check(releaseSpan.Problems().Any(p => p.Message.Contains("span index")), "A span index too big for the release layout is reported");
        ai.maSections[0].miSpanIndex = 40000;
        Check(!ai.Problems().Any(), "The development layout has room for a big span index");
        ai.maSections[0].muDangerRating = (AISections.DangerRating)3;
        Check(ai.Problems().First().Message.Contains("danger rating"), "A danger rating outside the 3 is reported");

        using (AISectionsEditor editor = new(readAI, aiEntry, false))
        {
            Hidden(editor);
            AISections draft = Field<AISections>(editor, "data");
            AISections.AISection section = draft.maSections[2];
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(section);
            Check(draft.Development && properties["muDangerRating"] != null && properties["mId"] == null && properties["muSpeed"] == null, "Only the development layout's fields are shown");
            Check(properties["Junction"] is PropertyDescriptor junction && (bool)junction.GetValue(section) && properties["Split"] == null, "The development layout has its own flags");
            Check(TypeDescriptor.GetProperties(draft)["mafSectionMinSpeeds"] == null, "The speed bands don't exist in the development layout");
            Check(section.ToString() == "dangerous, 1 portals" && section.maPortals[0].ToString() == "To 0", "Sections without IDs are named by what they are and where they are");
            Check(Field<ObjectTreeEditor>(editor, "tree").CollectionLimit(draft, nameof(AISections.maSectionResetPairs)) == 0, "Reset pairs can't be added to the development layout");
            section.muDangerRating = AISections.DangerRating.E_DANGER_RATING_NORMAL;
            Field<ObjectTreeEditor>(editor, "tree").RecordChange();
            Check((bool)Call(editor, "ApplyChanges"), "Apply accepts an edit to the development layout");
            AISections applied = new();
            applied.Read(aiEntry);
            Check(applied.Development && applied.maSections[2].muDangerRating == AISections.DangerRating.E_DANGER_RATING_NORMAL, "Apply writes the development layout back");
        }

        Check(!TrafficLabels.Development && TypeDescriptor.GetProperties(AISample().maSections[0])["mId"] != null, "Closing the editor goes back to the release fields");

        AISections aiView = new()
        {
            Development = true
        };

        aiView.maSections.Add(AISample().maSections[0]);
        aiView.maSections[0].muDangerRating = AISections.DangerRating.E_DANGER_RATING_FREEWAY;
        using (AISectionsView view = new(aiView))
            Check(Field<List<Vertex>[]>(view, "layers")[0][0].Colour == ((uint[])typeof(AISectionsView).GetField("speedColours", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null))[4], "A freeway section is drawn as the fastest");

        PropPhysics props = PropSample();
        props.Development = true;
        props.muTimeStamp = 0;
        props.SizeIsLittleEndian = true;
        props.maPropTypes[0].muCrashModeScore = 7;
        props.maPropTypes[0].mfaCollisionThresholds = [1, 2, 3];
        props.maPropTypes[0].muaResponses = [1, 2, 3, 4, 5, 6, 7];
        props.maPropTypes[0].muNumberOfJoints = 2;
        props.maPropTypes[1].maParts[0].mOffset = new(1, 2, 3, BitConverter.UInt32BitsToSingle(0x48));

        byte[] savedProps = props.Serialize(true, 4);
        PropPhysics readProps = new();
        BundleEntry propEntry = Bnd1Entry(savedProps);
        readProps.Read(propEntry);
        Check(readProps.Development && readProps.SizeIsLittleEndian && readProps.maPropTypes[0].muCrashModeScore == 7 && readProps.maPropTypes[0].mfaCollisionThresholds[2] == 3 && readProps.maPropTypes[0].muaResponses[6] == 7 && readProps.maPropTypes[0].muNumberOfJoints == 2
            && readProps.maPropTypes[1].maParts[0].mOffset.Y == 2 && readProps.maPropTypes[1].maParts[0].maCollisionVolumes[0].meType == PropPhysics.VolumeType.E_VOLUMETYPE_CAPSULE && readProps.NoPartsPointer == 0xFDD1F800, "A BND1 bundle's prop physics keeps its parts and volumes");
        Equal(readProps.Serialize(true, 4), savedProps, "A BND1 bundle's prop physics saves the same bytes after reopening");

        byte[] olderProps = [.. savedProps];
        olderProps[0x12] ^= 1;
        Check(Throws<ReadFailedError>(() => new PropPhysics().Read(Bnd1Entry(olderProps))), "Prop physics in another development layout is refused rather than misread");
        props.maPropTypes[0].muaResponses = new uint[6];
        Check(props.Problems().First().Message.Contains("7 responses"), "The fixed arrays of a development prop type are checked");

        using (PropPhysicsEditor editor = new(readProps, propEntry))
        {
            Hidden(editor);
            PropPhysics.PropTypeData type = Field<PropPhysics>(editor, "data").maPropTypes[0];
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(type);
            Check(properties["muCrashModeScore"] != null && properties["mfLeanThreshold"] == null && properties["muSceneUriId"] == null && properties["mfaCollisionThresholds"].DisplayName == "Collision thresholds", "Only the development layout's prop fields are shown");
            Check(type.ToString().StartsWith("0x1,"), "A development prop type is named by its resource ID");
        }

        Check(!TrafficLabels.Development, "Closing the prop editor goes back to the release fields");

        PropPhysics spaced = PropSample();
        spaced.maPropTypes[0].mfSphereRadius = 1;
        spaced.maPropTypes[0].maCollisionVolumes[0].mExtent = new(20, 0, 0);
        using (PropPhysicsView view = new(spaced))
        {
            Dictionary<PropPhysics.PropTypeData, OpenTK.Mathematics.Vector3> origins = Field<Dictionary<PropPhysics.PropTypeData, OpenTK.Mathematics.Vector3>>(view, "origins");
            Check((origins[spaced.maPropTypes[1]] - origins[spaced.maPropTypes[0]]).Length > 20, "A prop gets room for its volumes even when its sphere radius is a placeholder");
        }
    }

    private static bool Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
            return false;
        }
        catch (T)
        {
            return true;
        }
    }
}
