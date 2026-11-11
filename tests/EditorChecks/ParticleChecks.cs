using System.Buffers.Binary;
using System.ComponentModel;
using System.Numerics;
using System.Text;
using BaseHandlers;
using BundleFormat;
using BundleUtilities;
using static BaseHandlers.VFXPropCollection;

internal static partial class Program
{
    private static VFXPropCollection VFXSample()
    {
        VFXCoronaTypeData blink = new()
        {
            mnID = 575009,
            mType = eVFXCoronaType.VFXCorona_Texture_3,
            mrTimeOn = 0.5f,
            mrSizeMax = 1.7f
        };

        VFXPropCollection data = new()
        {
            mpCoronaTypeDataTable = [blink, new()
            {
                mnID = 7,
                mbSynchronised = true
            }]
        };

        VFXLocator locator(uint effect, float x) => new()
        {
            mPosition = new(x, 1, 2),
            PositionPadding = 0x7FC00001,
            mHashedName = effect,
            macDebugLefName = "Effects/Prop_Glass.lef.BurnoutFXLionEffectFile?ID=1"
        };

        data.mpPropTable.Add(new()
        {
            mPropID = 0xDEE01C66,
            mpPropStates = [new()
            {
                mpVFXMaterial = [new()
                {
                    mType = eVFXMaterialType.VFXMaterialTypeMetal,
                    mpLocators = [locator(0x1597597B, 1), locator(0x03F6F1F4, 2)]
                }, new()]
            }, new()
            {
                mpVFXMaterial = [new()],
                mpCoronaType = [new()
                {
                    mpTypeData = blink,
                    mrTimeOffset = 0.25f
                }]
            }]
        });

        data.mpPropTable.Add(new()
        {
            mPropID = 0xC3A0416D,
            mpPropStates = [new()
            {
                mpVFXMaterial = [new()
                {
                    mType = eVFXMaterialType.VFXMaterialTypeStuntYellow,
                    mpLocators = [locator(0x1597597B, 3)]
                }],
                mpCoronaType = [new()
                {
                    mpTypeData = blink
                }, new()
                {
                    mpTypeData = data.mpCoronaTypeDataTable[1]
                }]
            }]
        });

        return data;
    }

    // ParticleDescription: hash, pointer, and cLionEffectDefinition header
    private static BundleEntry Effect(BundleArchive archive, ulong id, string name)
    {
        byte[] bytes = new byte[0x60];
        bool big = archive.Platform == BundlePlatform.X360;
        (big
            ? (Action<Span<byte>, uint>)BinaryPrimitives.WriteUInt32BigEndian
            : BinaryPrimitives.WriteUInt32LittleEndian)(bytes.AsSpan(16), 0x10003);
        (big
            ? Encoding.BigEndianUnicode
            : Encoding.Unicode).GetBytes(name).CopyTo(bytes, 0x18);
        BundleEntry entry = new(archive)
        {
            ID = id,
            Type = EntryType.ParticleDescription,
            Platform = archive.Platform,
            EntryBlocks = [new()
            {
                Data = bytes
            }, new()
            {
                Data = []
            }, new()
            {
                Data = []
            }]
        };

        archive.Entries.Add(entry);
        return entry;
    }

    // Mock effect fixture with QUAD descriptor, behaviour, waveform, and material
    private static BundleEntry LionEffectEntry(BundlePlatform platform, Action<Action<int, uint>, Action<int, float>> tweak = null)
    {
        byte[] d = new byte[0x900];
        bool big = platform == BundlePlatform.X360;

        void w(int at, uint v)
        {
            if (big)
                BinaryPrimitives.WriteUInt32BigEndian(d.AsSpan(at), v);
            else
                BinaryPrimitives.WriteUInt32LittleEndian(d.AsSpan(at), v);
        }

        void f(int at, float v) => w(at, BitConverter.SingleToUInt32Bits(v));

        const int effect = 0x70, descriptor = 0x80, behaviour = 0x100, wave = 0x600, material = 0x700, strings = 0x800;
        w(0, 0xABCD1234);
        w(4, 16);
        w(16, 0x10003);
        (big
            ? Encoding.BigEndianUnicode
            : Encoding.Unicode).GetBytes("Test.lef").CopyTo(d, 0x18);
        w(16 + 0x48, effect - 16);
        w(effect + 4, descriptor - effect);
        w(descriptor + 44, 1);
        w(descriptor + 56, strings - descriptor);
        w(descriptor + 60, 1);
        w(descriptor + 64, behaviour - descriptor);
        w(descriptor + 76, material - descriptor);
        f(behaviour + 4, -0.5f);
        w(behaviour + 544, 0xFF3259CD);
        w(behaviour + 708, 0x2000);
        w(behaviour + 724, wave - behaviour);
        w(wave + 4, 6);
        f(wave + 20, 2);
        d[material + 58] = 1;
        w(material + 16, strings + 6 - material);
        w(material + 96, 0xDEADBEEF);
        Encoding.ASCII.GetBytes("LAYER\0glow\0").CopyTo(d, strings);
        tweak?.Invoke(w, f);
        BundleEntry entry = Entry(d, platform);
        entry.Type = EntryType.ParticleDescription;
        return entry;
    }

    private static void CheckParticles(string[] args)
    {
        string pcText = LionEffect.FromEntry(LionEffectEntry(BundlePlatform.PC)).ToText();
        string[] lines = pcText.Split('\n');

        int at(string line) => Array.IndexOf(lines, line);

        Check(lines[0] == "<LION_EFFECT name=\"Test.lef\">" && lines[2] == "<LION_PARTICLES name=\"tool.lef\">" && lines[4] == "<DESCRIPTOR name =\"des\">"
            && at("NAME = LAYER") > 0 && at("SHAPE = QUAD") > 0 && at("ACC_BASE = 0.000000, -0.500000, 0.000000") > 0 && at("COLOUR0 = 205, 89, 50, 255") > 0
            && at("DO_WAVEALPHA = 1") > 0 && at("TYPE = SIN") > 0 && at("AMP = 2.000000") > 0 && at("BLEND_MODE = SRCALPHA") > 0 && at("TEXTURE = glow") > 0,
            "An effect is written as LION editor text with its enums by name and colours as R, G, B, A");
        Check(at("<BEHAVIOUR name =\"beh\">") < at("<WAVEFORM name =\"WAVEFORM_ALPHA\">") && at("<WAVEFORM name =\"WAVEFORM_ALPHA\">") < at("<MATERIAL name =\"beh\">")
            && lines.Count(l => l == "{") == lines.Count(l => l == "}") && lines.Count(l => l.Contains(" = ")) == 22 + 88 + 13 + 46,
            "Blocks nest as TextSave writes them: the waveform inside its behaviour, then the material, every token written");
        Check(at("MESH0 = (null)") > 0, "A mesh name the tools left as garbage, with its DO_MESH flag off, is written as none");
        Check(LionEffect.FromEntry(LionEffectEntry(BundlePlatform.X360)).ToText() == pcText, "A console effect is written the same as a PC one");

        // Verify round-trip export and compilation
        foreach (BundlePlatform platform in (BundlePlatform[])[BundlePlatform.PC, BundlePlatform.X360])
        {
            LionEffect original = LionEffect.FromEntry(LionEffectEntry(platform, (w, _) => w(0x80 + 32, 0x200)));
            List<string> roundNotes = [];
            byte[] compiled = original.Compile(original.ToText(), platform == BundlePlatform.X360, roundNotes);
            Check(LionEffect.FromEntry(Entry(compiled, platform)).ToText() == original.ToText() && roundNotes.Count == 0 && compiled.Length % 16 == 0,
                $"An exported effect imports back unchanged for {platform}");
        }

        // Verify import handling for editor-specific syntax and path textures
        LionEffect target = LionEffect.FromEntry(LionEffectEntry(BundlePlatform.PC, (w, _) => w(0x80 + 32, 0x200)));
        string edited = target.ToText().Replace("DO_PHYSICS = 1\n", "DO_LIGHTING = 1\n").Replace("EMISSION_RATE_BASE = 0.000000", "EMISSION_RATE_BASE = 25.000000")
            .Replace("TEXTURE = glow", "TEXTURE = \"..\\..\\Textures\\fire02_c.tga\"").Replace("SHAPE = QUAD", "SHAPE = TILT").Replace("COLOUR1 = 0, 0, 0, 0", "COLOUR1 = 10, 20, 30, 40");
        int behaviourAt = edited.IndexOf("<BEHAVIOUR", StringComparison.Ordinal), materialAt = edited.IndexOf("<MATERIAL", StringComparison.Ordinal);
        edited = edited.Insert(materialAt, edited[behaviourAt..materialAt].Replace("EMISSION_RATE_BASE = 25.000000", "EMISSION_RATE_BASE = 50.000000"));
        List<string> notes = [];
        LionEffect imported = LionEffect.FromEntry(Entry(target.Compile(edited, false, notes), BundlePlatform.PC));
        LionEffect.Descriptor first = imported.Descriptors[0];
        Check(first.Behaviours.Count == 2 && first.Behaviours[0].Fields.Float("EMISSION_RATE_BASE") == 25 && first.Behaviours[1].Fields.Float("EMISSION_RATE_BASE") == 50
            && first.Blended != null && first.Behaviours[0].Waveforms.Count == 1 && first.Behaviours[0].Waveforms[0].Wave.Number("TYPE") == 6,
            "An edited effect imports its values, with a second behaviour and the blended one the game fills in");
        Check(first.Material.Text("TEXTURE") == "FIRE02_C" && first.Fields.Number("SHAPE") == 3 && first.Behaviours[0].Fields.Colour("COLOUR1") == new Vector4(10, 20, 30, 40) / 255,
            "A texture the editor saved as a path imports as its name");
        Check(first.Fields.Flag("DO_PHYSICS") && notes.Any(n => n.Contains("DO_LIGHTING")), "Burnout's own fields the editor drops are kept, and the editor's own are reported");
        Check(Throws<InvalidDataException>(() => target.Compile("<LION_EFFECT name=\"x\">\n{\n}\n", false, [])) && Throws<InvalidDataException>(() => target.Compile(edited.Replace("SHAPE = TILT", "SHAPE = TRIANGLE"), false, []))
            && Throws<InvalidDataException>(() => target.Compile(edited[..^4], false, [])), "A file with no emitters, a value that isn't allowed, or a missing } is refused");

        // Verify streaming emitter simulation and alpha fade
        void stream(Action<int, uint> w, Action<int, float> f)
        {
            w(0x80 + 28, 1);
            w(0x100 + 708, 0);
            f(0x100 + 4, 0);
            f(0x100 + 1144, 1);
            f(0x100 + 680, 10);
            f(0x100 + 688, 1);
            f(0x100 + 404, 1);
            f(0x100 + 304, 0.5f);
            f(0x100 + 700, 1);
            w(0x100 + 528, 0xFFFFFFFF);
            f(0x100 + 636, 0.5f);
        }

        LionEffect streaming = LionEffect.FromEntry(LionEffectEntry(BundlePlatform.X360, stream));
        LionEffectSimulation simulation = new(streaming);

        for (int i = 0; i < 60; i++)
            simulation.Step(1 / 60f);

        List<LionEffectSimulation.Particle> live = simulation.Emitters[0].Particles;
        LionEffectSimulation.Settings settings = simulation.Emitters[0].Settings(0);
        Check(live.Count is >= 9 and <= 10 && live.All(p => MathF.Abs(p.Position.Y - p.Age) < 0.02f && p.Size.X == 0.5f), "Particles stream at the emission rate and move at their velocity");
        LionEffectSimulation.Particle oldest = live.MaxBy(p => p.Age);
        Check(MathF.Abs(LionEffectSimulation.ColourAt(oldest, settings).W - (1 - (oldest.Age - 0.5f) / 0.5f)) < 0.01f && LionEffectSimulation.ColourAt(live.MinBy(p => p.Age), settings).W == 1,
            "Alpha fades out from ALPHA_FADEOUT of the way through a particle's life");
        Check(!simulation.Finished, "An infinite emitter never finishes");

        LionEffect burst = LionEffect.FromEntry(LionEffectEntry(BundlePlatform.PC, (w, f) =>
        {
            stream(w, f);
            w(0x80 + 28, 0);
            f(0x80 + 20, 0.5f);
            w(0x100 + 708, 0x200);
            w(0x100 + 704, 5);
        }));
        LionEffectSimulation once = new(burst);
        once.Step(1 / 60f);
        int burstCount = once.Emitters[0].Particles.Count;

        for (int i = 0; i < 30; i++)
            once.Step(1 / 60f);

        Check(burstCount == 5 && once.Emitters[0].Particles.Count == 5 && !once.Finished, "DO_BURST makes EMISSION_COUNT_CLAMP particles once");

        for (int i = 0; i < 60; i++)
            once.Step(1 / 60f);

        Check(once.Emitters[0].Particles.Count == 0 && once.Finished, "A one-shot effect finishes when its emitter's life and its particles are over");

        LionEffectRenderer renderer = new(_ => null);
        renderer.Frame(streaming);
        using (Bitmap frame = new(64, 64, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
        {
            renderer.Grid = false;
            renderer.Render(simulation, frame);
            Color background = frame.GetPixel(0, 0), brightest = Enumerable.Range(0, 64 * 64).Select(i => frame.GetPixel(i % 64, i / 64)).MaxBy(c => c.R);
            Check(renderer.Distance > 0.5f && renderer.Target.Y is > 0.2f and < 0.8f && brightest.R > background.R + 60, "The previewer frames the particles and draws them");
        }

        // Verify TILT particle rendering
        LionEffectSimulation tilts = new(LionEffect.FromEntry(LionEffectEntry(BundlePlatform.PC, (w, f) =>
        {
            stream(w, f);
            w(0x80 + 44, 3);
            f(0x100 + 400, 1);
            f(0x100 + 404, 0);
            w(0x100 + 708, 0x80);
            f(0x100 + 304, 0.1f);
            f(0x100 + 308, 1);
        })));

        for (int i = 0; i < 60; i++)
            tilts.Step(1 / 60f);

        using (Bitmap frame = new(160, 120, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
        {
            LionEffectRenderer front = new(_ => null)
            {
                Grid = false,
                Yaw = 0,
                Pitch = 0,
                Distance = 4,
                Target = new(1, 0, 0)
            };

            front.Render(tilts, frame);
            List<Point> lit = [.. Enumerable.Range(0, 160 * 120).Select(i => new Point(i % 160, i / 160)).Where(p => frame.GetPixel(p.X, p.Y).R > 60)];
            int across = lit.Max(p => p.X) - lit.Min(p => p.X), up = lit.Max(p => p.Y) - lit.Min(p => p.Y);
            Check(across > 4 * Math.Max(up, 1), $"TILT particles are strips along their velocity, as RenderTilts draws them ({across} by {up} pixels)");
        }

        BundleEntry viewed = LionEffectEntry(BundlePlatform.PC, stream);
        LionEffect viewedEffect = LionEffect.FromEntry(viewed);
        using (LionEffectViewer viewer = (LionEffectViewer)viewedEffect.GetEditor(viewed))
        {
            Hidden(viewer);

            for (int i = 0; i < 5; i++)
            {
                System.Threading.Thread.Sleep(40);
                Application.DoEvents();
            }

            Check(viewer.Simulation.Time > 0 && viewer.Simulation.Emitters[0].Particles.Count > 0, "The previewer window plays the effect");
        }

        foreach ((BundlePlatform platform, bool bigEndian) in ((BundlePlatform, bool)[])[(BundlePlatform.PC, false), (BundlePlatform.X360, true)])
        {
            byte[] saved = VFXSample().Serialize(bigEndian);
            VFXPropCollection read = new();
            read.Read(Entry(saved, platform));
            VFXPropState smashed = read.mpPropTable[0].mpPropStates[1];
            Check(read.mpPropTable.Count == 2 && read.mpPropTable[0].mpPropStates[0].mpVFXMaterial[0].mpLocators[1].mPosition == new Vector3(2, 1, 2)
                && smashed.mpCoronaType[0].mpTypeData == read.mpCoronaTypeDataTable[0] && read.mpPropTable[1].mpPropStates[0].mpCoronaType[0].mpTypeData == read.mpCoronaTypeDataTable[0]
                && read.mpCoronaTypeDataTable[1].mbSynchronised && read.mpPropTable[1].mpPropStates[0].mpVFXMaterial[0].mType == eVFXMaterialType.VFXMaterialTypeStuntYellow,
                $"VFX props keep their states, effects and shared light glow types when saved for {platform}");
            Equal(read.Serialize(bigEndian), saved, $"VFX props save the same bytes after reopening for {platform}");

            int word(int at) => bigEndian
                ? BinaryPrimitives.ReadInt32BigEndian(saved.AsSpan(at))
                : BinaryPrimitives.ReadInt32LittleEndian(saved.AsSpan(at));

            int states = word(8), materials = word(16), locators = word(24);
            Check(word(48) == 3 && states == 0x60 && word(states + 8) == -1 && word(materials + 12 + 8) == -1 && word(materials + 8) == 0 && word(materials + 36 + 8) == 2,
                $"Empty lists are stored as -1 and the others as runs of the next table for {platform}");
            Check(BinaryPrimitives.ReadUInt32LittleEndian(saved.AsSpan(locators + 12)) == (bigEndian
                ? 0x0100C07Fu
                : 0x7FC00001u), $"A locator keeps what the tools left after its position for {platform}");
        }

        VFXPropCollection longName = VFXSample();
        longName.mpPropTable[0].mpPropStates[0].mpVFXMaterial[0].mpLocators[0].macDebugLefName = "gamedb://burnout5/Burnout/Effects/PropGenericFx.lef.BurnoutFXLionEffectFile?ID=425809";
        VFXPropCollection reread = new();
        reread.Read(Entry(longName.Serialize(false)));
        Check(reread.mpPropTable[0].mpPropStates[0].mpVFXMaterial[0].mpLocators[0].macDebugLefName == "Effects/PropGenericFx.lef.BurnoutFXLionEffectFile?ID=425809",
            "A long effect path keeps its last 59 characters, as the game's tools did");

        void vfxRefused(string what, Action<VFXPropCollection> edit, string expect)
        {
            VFXPropCollection data = VFXSample();
            edit(data);
            Check(data.Problems().FirstOrDefault()?.Message.Contains(expect) == true, what);
            Check(Throws<InvalidDataException>(() => data.Serialize(false)), what + " can't be saved");
        }

        vfxRefused("A material type the game doesn't have is reported", data => data.mpPropTable[1].mpPropStates[0].mpVFXMaterial[0].mType = (eVFXMaterialType)15, "type the game");
        vfxRefused("A light glow without a type is reported", data => data.mpPropTable[0].mpPropStates[1].mpCoronaType[0].mpTypeData = null, "light glow type");
        vfxRefused("A light glow type with a texture the game doesn't have is reported", data => data.mpCoronaTypeDataTable[1].mType = (eVFXCoronaType)17, "texture");
        byte[] version2 = VFXSample().Serialize(false);
        version2[48] = 2;
        Check(Throws<ReadFailedError>(() => new VFXPropCollection().Read(Entry(version2))), "Another version of VFX props is refused rather than misread");

        // Effects
        BundleEntry collectionEntry = Entry([], BundlePlatform.X360);
        BundleArchive archive = collectionEntry.Archive;
        Effect(archive, 0x1597597B, "Prop_Glass.lef");
        Effect(archive, 0x03F6F1F4, "Prop_Foilage.lef");
        ParticleEffects.Load(archive);
        Check(ParticleEffects.Names[0x03F6F1F4] == "Prop_Foilage.lef", "An effect's name comes from its LION definition");
        ParticleDescriptionCollection effects = new()
        {
            mpTable = [new()
            {
                mResourceId = 0x1597597B
            }, new()
            {
                mResourceId = 0x03F6F1F4
            }, new()
            {
                mResourceId = 0x1597597B
            }]
        };

        effects.Write(collectionEntry);
        byte[] table = collectionEntry.GetPrimaryWithoutImports();
        List<Dependency> imports = collectionEntry.GetImports();
        Check(table.Length == 32 && new ParticleDescriptionCollection
        {
            mpTable = [new(), new(), new(), new(), new()]
        }.Serialize(false).Data.Length == 48 && BinaryPrimitives.ReadUInt32BigEndian(table) == 8 && BinaryPrimitives.ReadUInt32BigEndian(table.AsSpan(4)) == 3
            && BinaryPrimitives.ReadUInt32LittleEndian(table.AsSpan(16)) == 3 && imports.Select(i => i.EntryPointerOffset).SequenceEqual([8u, 12u, 16u]) && imports[1].ID == 0x03F6F1F4,
            "Effects are slots of 4 bytes from +8 with an import each, numbered from 1 in little endian as the tools did");
        ParticleDescriptionCollection readEffects = new();
        readEffects.Read(collectionEntry);
        Check(readEffects.mpTable.Select(e => e.mResourceId).SequenceEqual([0x1597597BUL, 0x03F6F1F4UL, 0x1597597BUL]) && readEffects.mpTable[1].ToString() == "Prop_Foilage.lef",
            "Effects are read back from their imports and named");
        Check(!readEffects.Problems().Any(), "Effects that are in the bundle are fine");
        readEffects.mpTable.Add(new()
        {
            mResourceId = 0xABCDEF
        });
        Check(readEffects.Problems().Single().Message.Contains("isn't a ParticleDescription"), "An effect that isn't in the bundle is reported");
        collectionEntry.Archive.BNDVersion = 1;
        Check(Throws<ReadFailedError>(() => new ParticleDescriptionCollection().Read(collectionEntry)), "The inline effects of BND1 bundles are refused rather than misread");
        collectionEntry.Archive.BNDVersion = 2;

        // Effect textures
        TextureNameMap textures = new()
        {
            mpEntries = [new()
            {
                mpGDBTextureName = "gamedb://burnout5/Burnout/Effects/Textures/fxskid.TextureConfig2d?ID=226049"
            }, new()
            {
                mpGDBTextureName = "gamedb://burnout5/Burnout/Effects/Textures/SparkBlast.TextureConfig2d?ID=245985"
            }, new()
            {
                mpGDBTextureName = "gamedb://burnout5/Burnout/Effects/Textures/twinky.TextureConfig2d?ID=246672"
            }]
        };

        Check(textures.mpEntries[0].muHash == 0xF2CC8984 && textures.mpEntries[1].muHash == 0x8E6892F8 && textures.mpEntries[1].LionName == "SparkBlast",
            "An effect texture's hash is the FNV-1a of its lowercase name, as in the game's data");
        byte[] map = textures.Serialize(true);
        Check(BinaryPrimitives.ReadUInt32BigEndian(map.AsSpan(20)) == 0x30 && BinaryPrimitives.ReadUInt32BigEndian(map.AsSpan(28)) == 0x80 && map.Length % 16 == 0 && map[0x30] == (byte)'g',
            "Texture names start 16-byte aligned after the table");
        TextureNameMap readMap = new();
        readMap.Read(Entry(map, BundlePlatform.X360));
        Equal(readMap.Serialize(true), map, "Effect textures save the same bytes after reopening");
        readMap.mpEntries.Add(new()
        {
            mpGDBTextureName = "gamedb://elsewhere/FXSKID.TextureConfig2d?ID=1"
        });
        Check(readMap.Problems().Single().Message.Contains("only find one"), "Two textures with the same name are reported");

        // Particle editor verification
        VFXPropCollection source = VFXSample();
        BundleEntry vfxEntry = Entry(source.Serialize(false));
        Effect(vfxEntry.Archive, 0x1597597B, "Prop_Glass.lef");
        using (ParticleEditor editor = (ParticleEditor)source.GetEditor(vfxEntry))
        {
            Hidden(editor);
            VFXPropCollection draft = Field<VFXPropCollection>(editor, "data");
            ObjectTreeEditor tree = Field<ObjectTreeEditor>(editor, "tree");
            Check(!ReferenceEquals(draft, source) && draft.mpPropTable[0].mpPropStates[1].mpCoronaType[0].mpTypeData == draft.mpCoronaTypeDataTable[0], "The VFX editor works on a draft with its shared glow types kept inside it");
            Check(Field<TreeView>(tree, "tree").Nodes[0].Text == "VFX props" && draft.mpPropTable[0].mpPropStates[0].mpVFXMaterial[0].mpLocators[0].ToString() == "Prop_Glass.lef",
                "The VFX editor names its root and locators by their effects");
            VFXMaterial material = draft.mpPropTable[1].mpPropStates[0].mpVFXMaterial[0];
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(material);
            Check(properties["mType"].Converter.ConvertToString(material.mType) == "Stunt Yellow" && properties["mType"].DisplayName == "Type"
                && (eVFXMaterialType)properties["mType"].Converter.ConvertFromString("Red Barrel Explo") == eVFXMaterialType.VFXMaterialTypeRedBarrelExplo,
                "Material types read as words and convert back");
            Check(TypeDescriptor.GetProperties(draft.mpCoronaTypeDataTable[0])["mrTimeOn"].DisplayName == "Time on" && TypeDescriptor.GetProperties(material.mpLocators[0])["macDebugLefName"].DisplayName == "Effect path (debug)",
                "VFX fields lose their prefixes");
            VFXLocator locator = material.mpLocators[0];
            PropertyDescriptor effect = TypeDescriptor.GetProperties(locator)["mHashedName"];
            Check(effect.Converter.ConvertToString(locator.mHashedName) == "0x1597597B Prop_Glass.lef" && Convert.ToUInt32(effect.Converter.ConvertFromString("0x03F6F1F4 anything")) == 0x03F6F1F4
                && Convert.ToUInt64(effect.Converter.GetStandardValues().Cast<object>().Single()) == 0x1597597B, "A locator's effect shows its name and is chosen from the bundle's effects");
            VFXCoronaType corona = draft.mpPropTable[0].mpPropStates[1].mpCoronaType[0];
            Check(TrafficReferences.Candidates(corona, "mpTypeData", typeof(VFXCoronaTypeData)).Count == 2
                && TrafficReferences.Label(corona.mpTypeData, corona, "mpTypeData", typeof(VFXCoronaTypeData)) == "0: ID 575009, texture 3", "A light glow's type is chosen from the shared list");
            tree.SelectObject(draft.mpCoronaTypeDataTable[0]);
            Check(!tree.TryDeleteSelected(out List<ObjectGraph.Usage> uses) && uses.Count == 2, "A light glow type that glows use can't be deleted");
            tree.SelectObject(material.mpLocators);
            Call(tree, "Add");
            Check(material.mpLocators.Count == 2 && !Field<ListBox>(editor, "issues").Visible, "Effects can be added to a material");
            Check((bool)Call(editor, "ApplyChanges"), "VFX Apply accepts valid edits");
            VFXPropCollection applied = new();
            applied.Read(vfxEntry);
            Check(applied.mpPropTable[1].mpPropStates[0].mpVFXMaterial[0].mpLocators.Count == 2 && source.mpPropTable[1].mpPropStates[0].mpVFXMaterial[0].mpLocators.Count == 1,
                "VFX Apply writes the resource, not the object it was opened from");
            corona.mpTypeData = null;
            tree.RecordChange();
            Check(Field<ListBox>(editor, "issues").Items.Count > 0 && !(bool)Call(editor, "ApplyChanges"), "VFX Apply is blocked by a light glow without a type");
        }

        Check(TrafficReferences.VFXData == null, "Closing the VFX editor lets go of its draft");

        // Optional local bundle fixtures
        Dictionary<BundlePlatform, Dictionary<ulong, string>> lefs = [];

        foreach (string argument in args.Where(argument => argument.StartsWith("--particles-file=")))
        {
            string path = argument["--particles-file=".Length..];
            BundleArchive bundle = BundleArchive.Read(path);
            ParticleEffects.Load(bundle);
            string label = $"{Path.GetFileName(Path.GetDirectoryName(path))}/{Path.GetFileName(path)}";
            List<BundleEntry> effectEntries = [.. bundle.Entries.Where(e => e.Type == EntryType.ParticleDescription)];

            if (effectEntries.Count > 0)
            {
                Dictionary<ulong, string> texts = effectEntries.ToDictionary(e => e.ID, e => LionEffect.FromEntry(e).ToText());
                int same = effectEntries.Count(e =>
                {
                    LionEffect effect = LionEffect.FromEntry(e);
                    return effect.Compile(effect.ToText(), e.Console, []).AsSpan().SequenceEqual(e.GetPrimaryWithoutImports());
                });
                Check(effectEntries.All(e =>
                {
                    LionEffect effect = LionEffect.FromEntry(e);
                    return LionEffect.FromEntry(Entry(effect.Compile(effect.ToText(), e.Console, []), e.Platform)).ToText() == effect.ToText();
                }), $"{label}: every effect imports back from its export unchanged ({same} of {effectEntries.Count} byte for byte)");

                if (!bundle.Console)
                    Check(same == effectEntries.Count, $"{label}: every PC effect imports back byte for byte");

                Check(texts.Values.All(t => t.Split('\n').Count(l => l == "{") == t.Split('\n').Count(l => l == "}") && t.Contains("\nTEXTURE = ") && !t.Contains("TEXTURE = (null)")),
                    $"{label}: all {texts.Count} effects export as balanced LION text with a texture each");
                lefs.TryAdd(bundle.Platform, texts);
            }

            foreach (BundleEntry entry in bundle.Entries)
            {
                byte[] before = entry.GetPrimaryWithoutImports();
                List<Dependency> importsBefore = entry.GetImports();

                switch (entry.Type)
                {
                    case EntryType.VFXPropCollection:
                        VFXPropCollection vfx = new();
                        vfx.Read(entry);
                        List<VFXLocator> all = [.. vfx.mpPropTable.SelectMany(p => p.mpPropStates).SelectMany(s => s.mpVFXMaterial).SelectMany(m => m.mpLocators)];
                        Check(all.Count > 0 && all.All(l => ParticleEffects.Names.ContainsKey(l.mHashedName)) && !vfx.Problems().Any(), $"{label}: every VFX locator starts an effect of the bundle");
                        Equal(vfx.Serialize(entry.Console), before, $"{label}: VFX props save the same bytes ({vfx.mpPropTable.Count} props, {all.Count} effects)");
                        break;
                    case EntryType.ParticleDescriptionCollection when bundle.BNDVersion == 2:
                        ParticleDescriptionCollection collection = new();
                        collection.Read(entry);
                        (byte[] data, List<Dependency> written) = collection.Serialize(entry.Console);
                        Check(!collection.Problems().Any() && collection.mpTable.All(e => ParticleEffects.Names[e.mResourceId] != "?"), $"{label}: every effect is a named ParticleDescription of the bundle");
                        Check(data.SequenceEqual(before) && written.Select(i => (i.ID, i.EntryPointerOffset)).SequenceEqual(importsBefore.Select(i => (i.ID, i.EntryPointerOffset))),
                            $"{label}: effects save the same bytes and imports ({collection.mpTable.Count} effects)");
                        break;
                    case EntryType.TextureNameMap:
                        TextureNameMap names = new();
                        names.Read(entry);
                        Check(!names.Problems().Any(), $"{label}: effect texture names are all different");
                        Equal(names.Serialize(entry.Console), before, $"{label}: effect textures save the same bytes ({names.mpEntries.Count} textures)");
                        break;
                }
            }
        }

        // Cross-platform endianness equivalence check
        if (lefs.TryGetValue(BundlePlatform.PC, out Dictionary<ulong, string> pc) && lefs.TryGetValue(BundlePlatform.X360, out Dictionary<ulong, string> x360))
        {
            List<ulong> common = [.. pc.Keys.Intersect(x360.Keys)];
            List<ulong> differ = [.. common.Where(id => pc[id] != x360[id])];

            foreach (ulong id in differ)
            {
                string[] a = pc[id].Split('\n'), b = x360[id].Split('\n');
                Console.WriteLine($"  {id:X8} differs: " + string.Join("; ", a.Zip(b).Where(p => p.First != p.Second).Take(4).Select(p => $"{p.First} | {p.Second}")));
            }

            Check(common.Count > 0 && differ.Count * 2 < common.Count, $"PC and X360 export {common.Count - differ.Count} of {common.Count} shared effects identically");
        }
    }
}
