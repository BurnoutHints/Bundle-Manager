using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using BundleFormat;
using BundleUtilities;
using MathLib;
using OpenTK.Mathematics;
using PluginAPI;

namespace BaseHandlers;

public class AptColor
{
    public float Red;
    public float Green;
    public float Blue;
    public float Alpha;

    public AptColor(float red, float green, float blue, float alpha)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }
}

public enum AptConstType
{
    String = 1,
    RegisterNumber = 4
}

public class AptConst
{
    public AptConstType Type;
    public uint Data;
    public string String;

    public override string ToString()
    {
        if (Type == AptConstType.String)
        {
            return String;
        }
        else
        {
            return "Register Number: 0x" + Data.ToString("X2");
        }
    }
}

public class Import
{
    public string ImportMovie;
    public string ImportName;
    public int CharacterID;
    public uint Unknown1;

    public override string ToString()
    {
        return "ID: " + CharacterID + ", Movie: " + ImportMovie + ", Name: " + ImportName + ", Unk1: " + Unknown1.ToString("X8");
    }
}

public class Export
{
    public string ExportName;
    public int CharacterID;

    public override string ToString()
    {
        return "ID: " + CharacterID + ", Name: " + ExportName;
    }
}

public class ClipActionRecord
{
    // TODO: Clip Action Record
}

public class ClipAction
{
    public uint ClipActionRecordCount;
    public ClipActionRecord[] ClipActionRecords;
}

public enum FrameType
{
    Action = 1,
    FrameLabel = 2,
    PlaceObject = 3,
    RemoveObject = 4,
    BackgroundColor = 5,
    StartSound = 6,
    StartSoundStream = 7,
    InitAction = 8
}

public class FrameItem
{
    public FrameType Type;
}

public class FrameItemAction : FrameItem
{
    public uint ActionPtr;
    public ActionRecord[] Actions;
}

public class FrameItemLabel : FrameItem
{
    public string Label;
    public uint Flags;
    public uint FrameID;
}

public class FrameItemPlaceObject : FrameItem
{
    /**
     * The flags are:
     *      • Move – bit 1 (LSB) – a bit different than the other flags.
     *        This determines whether a previous instance of the character to
     *        "place" (specified by the depth parameter) should be moved, rather than
     *        simply created as new. There is special behaviour if combined with
     *        HasCharacter: a new character is created and the previous character
     *        at the given depth is removed.
     *      • HasCharacter – bit 2 – signifies whether the characterID field is set.
     *      • HasMatrix – bit 3 – signifies whether the matrix field is set.
     *      • HasColourTransform – bit 4 – signifies whether the colourTransform field is set.
     *      • HasRatio – bit 5 – signifies whether the ratio field is set.
     *      • HasName – bit 6 – signifies whether the name field is set.
     *      • HasClipDepth – bit 7 – signifies whether the clipDepth field is set.
     *      • HasClipActions – bit 8 – signifies whether the clipActions field is set.
     * */
    public uint PlaceFlags;

    public uint Depth;
    public uint CharacterID;
    public Matrix3x2 Matrix;
    public AptColor ColorTransform;
    public uint Unknown1;
    public uint Ratio;
    public uint ClipDepth;
    public uint ClipActionsPtr;
    
    public string Name;
    public ClipAction[] ClipActions;
}

public class Frame
{
    public FrameItem[] FrameItems;
}

public class ButtonRecord
{
    // TODO: Button Record
}

public class ButtonSound
{
    // TODO: Button Sound
}

public class ActionRecord
{
    
}

public enum TextAlignment
{
    Left = 0,
    Right = 1,
    Center = 2,
    None = 3,
    Justify = 4
}

public enum CharacterType
{
    Shape = 1,
    EditText = 2,
    Font = 3,
    Button = 4,
    Sprite = 5,
    Sound = 6,
    Image = 7,
    Morph = 8,
    Movie = 9,
    StaticText = 10,
    None = 11,
    Video = 12
}

public abstract class CharacterData
{

}

public class CharacterShape : CharacterData
{
    public RectF Bounds;
    public uint GeometryID;
}

public class CharacterTextEdit : CharacterData
{
    public RectF Bounds;
    public uint FontID;
    public TextAlignment Alignment;
    public uint Color;
    public float FontHeight;
    public uint ReadOnly;
    public uint Multiline;
    public uint Wordwrap;
    public string Text;
    public string Variable;
}

public class CharacterFont : CharacterData
{
    public string Name;

    public Character[] Glyphs;
}

public class CharacterButton : CharacterData
{
    public uint MenuButton;
    public RectF Bounds;
    public int TriangleCount;
    public int VertexCount;
    public uint VertexPtr;
    public uint TrianglePtr;
    public int RecordCount;
    public uint RecordPtr;
    public int ActionCount;
    public uint ActionPtr;
    public uint SoundPtr;

    public Vector2[] Vertices;
    public Triangle[] Triangles;
    public ButtonRecord[] Records;
    public ActionRecord[] Actions;
    public ButtonSound Sound;
}

public class CharacterSprite : CharacterData
{
    public uint Unknown1;

    public Frame[] Frames;
}

public class CharacterSound : CharacterData
{
    // TODO: Sound
}

public class CharacterImage : CharacterData
{
    public uint ImageID;
}

public class CharacterMorph : CharacterData
{
    // TODO: Morph
}

public class CharacterMovie : CharacterData
{
    public uint Unknown1;
    public int ScreenWidth;
    public int ScreenHeight;
    public int MillisecondsPerFrame;
    public uint Unknown2;

    public Frame[] Frames;
    public Character[] Characters;
    public Import[] Imports;
    public Export[] Exports;
}

public class CharacterStaticText : CharacterData
{
    // TODO: Static Text
}

public class CharacterVideo : CharacterData
{
    // TODO: Video
}

public class Character
{
    // Header
    public CharacterType Type;
    public uint Signature;
    public ushort Unknown1;
    public ushort ID;
    public uint Unknown2;

    // Data
    public CharacterData Data;

    private static Frame ReadFrame(BinaryReader2 br, uint dataStart)
    {
        Frame frame = new();

        uint itemCount = br.ReadUInt32();
        uint frameItemPtr = dataStart + br.ReadUInt32();

        long fPos = br.BaseStream.Position;

        br.BaseStream.Position = frameItemPtr;
        //MessageBox.Show("Pos: " + frameItemPtr.ToString("X8"));

        uint[] framePtrs = new uint[itemCount];
        for (int j = 0; j < framePtrs.Length; j++)
        {
            framePtrs[j] = dataStart + br.ReadUInt32();
        }

        frame.FrameItems = new FrameItem[itemCount];
        for (int j = 0; j < frame.FrameItems.Length; j++)
        {
            br.BaseStream.Position = framePtrs[j];

            FrameItem item = null;
            FrameType type = (FrameType) br.ReadUInt32();
            //MessageBox.Show("Type: " + type);
            switch (type)
            {
                case FrameType.Action:
                    FrameItemAction actionFrame = new();
                    actionFrame.Type = type;
                    actionFrame.ActionPtr = br.ReadUInt32();

                    // TODO: ActionRecords

                    item = actionFrame;
                    break;
                case FrameType.FrameLabel:
                    FrameItemLabel frameLabel = new();
                    frameLabel.Type = type;
                    uint labelPtr = dataStart + br.ReadUInt32();
                    frameLabel.Flags = br.ReadUInt32();
                    frameLabel.FrameID = br.ReadUInt32();

                    long blaPos = br.BaseStream.Position;
                    br.BaseStream.Position = labelPtr;
                    frameLabel.Label = br.ReadCStr();
                    br.BaseStream.Position = blaPos;

                    item = frameLabel;
                    break;
                case FrameType.PlaceObject:
                    FrameItemPlaceObject framePlaceObject = new();
                    framePlaceObject.Type = type;

                    // TODO: Place Object
                    framePlaceObject.PlaceFlags = br.ReadUInt32();
                    framePlaceObject.Depth = br.ReadUInt32();
                    framePlaceObject.CharacterID = br.ReadUInt32();
                    framePlaceObject.Matrix = br.ReadMatrix3x2();
                    float r = br.ReadSingle();
                    float g = br.ReadSingle();
                    float b = br.ReadSingle();
                    float a = br.ReadSingle();
                    framePlaceObject.ColorTransform = new AptColor(r, g, b, a);
                    framePlaceObject.Unknown1 = br.ReadUInt32();
                    framePlaceObject.Ratio = br.ReadUInt32();
                    uint namePtr = br.ReadUInt32();
                    long cblaPos = br.BaseStream.Position;
                    br.BaseStream.Position = namePtr;
                    framePlaceObject.Name = br.ReadCStr();
                    br.BaseStream.Position = cblaPos;
                    framePlaceObject.ClipDepth = br.ReadUInt32();
                    framePlaceObject.ClipActionsPtr = br.ReadUInt32();

                    // TODO: ClipActions

                    item = framePlaceObject;
                    break;
                case FrameType.RemoveObject:
                    break;
                case FrameType.BackgroundColor:
                    break;
                case FrameType.StartSound:
                    break;
                case FrameType.StartSoundStream:
                    break;
                case FrameType.InitAction:
                    break;
                default:
                    item = null;
                    break;
            }

            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem
            // TODO: FrameItem

            frame.FrameItems[j] = item;
        }

        br.BaseStream.Position = fPos;

        return frame;
    }

    public static Character Read(BinaryReader2 br, uint dataStart)
    {
        Character result = new();

        long startPos = br.BaseStream.Position;

        result.Type = (CharacterType)br.ReadUInt32();
        result.Signature = br.ReadUInt32();
        result.Unknown1 = br.ReadUInt16();
        result.ID = br.ReadUInt16();
        result.Unknown2 = br.ReadUInt32();

        switch (result.Type)
        {
            case CharacterType.Shape:
                CharacterShape shape = new();
                shape.Bounds = br.ReadRectF();
                shape.GeometryID = br.ReadUInt32();
                result.Data = shape;
                break;
            case CharacterType.EditText:
                CharacterTextEdit textEdit = new();
                textEdit.Bounds = br.ReadRectF();
                textEdit.FontID = br.ReadUInt32();
                textEdit.Alignment = (TextAlignment)br.ReadUInt32();
                textEdit.Color = br.ReadUInt32();
                textEdit.FontHeight = br.ReadSingle();
                textEdit.ReadOnly = br.ReadUInt32();
                textEdit.Multiline = br.ReadUInt32();
                textEdit.Wordwrap = br.ReadUInt32();
                uint textPtr = dataStart + br.ReadUInt32();
                uint variableNamePtr = dataStart + br.ReadUInt32();

                long thePos = br.BaseStream.Position;
                br.BaseStream.Position = textPtr;
                textEdit.Text = br.ReadCStr();
                br.BaseStream.Position = variableNamePtr;
                textEdit.Variable = br.ReadCStr();
                br.BaseStream.Position = thePos;

                result.Data = textEdit;
                break;
            case CharacterType.Font:
                CharacterFont font = new();
                uint namePtr = dataStart + br.ReadUInt32();
                long cPos = br.BaseStream.Position;
                br.BaseStream.Position = namePtr;
                font.Name = br.ReadCStr();
                br.BaseStream.Position = cPos;
                uint glyphCount = br.ReadUInt32();
                uint glyphPtr = br.ReadUInt32();

                cPos = br.BaseStream.Position;
                br.BaseStream.Position = dataStart + glyphPtr;

                font.Glyphs = new Character[glyphCount];
                for (int i = 0; i < font.Glyphs.Length; i++)
                {
                    font.Glyphs[i] = Character.Read(br, dataStart);
                }

                br.BaseStream.Position = cPos;

                result.Data = font;
                break;
            case CharacterType.Button:
                CharacterButton button = new();
                button.MenuButton = br.ReadUInt32();
                button.Bounds = br.ReadRectF();
                button.TriangleCount = br.ReadInt32();
                button.VertexCount = br.ReadInt32();
                button.VertexPtr = br.ReadUInt32();
                button.TrianglePtr = br.ReadUInt32();
                button.RecordCount = br.ReadInt32();
                button.RecordPtr = br.ReadUInt32();
                button.ActionCount = br.ReadInt32();
                button.ActionPtr = br.ReadUInt32();
                button.SoundPtr = br.ReadUInt32();
                result.Data = button;
                break;
            case CharacterType.Sprite:
                CharacterSprite sprite = new();
                uint frameCount2 = br.ReadUInt32();
                uint framePtr2 = br.ReadUInt32();
                sprite.Unknown1 = br.ReadUInt32();
                
                // TODO: Verify Frames
                br.BaseStream.Position = dataStart + framePtr2;
                sprite.Frames = new Frame[frameCount2];
                for (int i = 0; i < sprite.Frames.Length; i++)
                {
                    sprite.Frames[i] = ReadFrame(br, dataStart);
                }

                result.Data = sprite;
                break;
            case CharacterType.Sound:
                CharacterSound sound = new();
                // TODO: Sound
                result.Data = sound;
                break;
            case CharacterType.Image:
                CharacterImage image = new();
                image.ImageID = br.ReadUInt32(); // TODO: Verify
                result.Data = image;
                break;
            case CharacterType.Morph:
                CharacterMorph morph = new();
                // TODO: Morph
                result.Data = morph;
                break;
            case CharacterType.Movie:
                CharacterMovie movie = new();

                uint frameCount = br.ReadUInt32();
                uint framePtr = br.ReadUInt32();
                movie.Unknown1 = br.ReadUInt32();
                uint characterCount = br.ReadUInt32();
                uint characterPtr = br.ReadUInt32();
                movie.ScreenWidth = br.ReadInt32();
                movie.ScreenHeight = br.ReadInt32();
                movie.MillisecondsPerFrame = br.ReadInt32();
                uint importCount = br.ReadUInt32();
                uint importPtr = br.ReadUInt32();
                uint exportCount = br.ReadUInt32();
                uint exportPtr = br.ReadUInt32();
                movie.Unknown2 = br.ReadUInt32();

                //MessageBox.Show("Pos: " + br.BaseStream.Position.ToString("X8"));
                //MessageBox.Show("Pos: " + characterPtr.ToString("X8"));

                long myPos = br.BaseStream.Position;


            br.BaseStream.Position = dataStart + framePtr;
            movie.Frames = new Frame[frameCount];
                for (int i = 0; i < movie.Frames.Length; i++)
                {
                    movie.Frames[i] = ReadFrame(br, dataStart);
                }

                br.BaseStream.Position = dataStart + characterPtr;

                uint[] CharacterPtrs = new uint[characterCount];
                for (int i = 0; i < characterCount; i++)
                {
                    CharacterPtrs[i] = dataStart + br.ReadUInt32();
                }

                movie.Characters = new Character[characterCount];
                for (int i = 0; i < movie.Characters.Length; i++)
                {
                    if (CharacterPtrs[i] == startPos)
                    {
                        movie.Characters[i] = result;
                    }
                    else
                    {
                        br.BaseStream.Position = CharacterPtrs[i];
                        movie.Characters[i] = Character.Read(br, dataStart);
                    }
                }

                br.BaseStream.Position = dataStart + importPtr;
                movie.Imports = new Import[importCount];
                for (int i = 0; i < movie.Imports.Length; i++)
                {
                    Import import = new();
                    
                    uint importMoviePtr = dataStart + br.ReadUInt32();
                    uint importNamePtr = dataStart + br.ReadUInt32();
                    import.CharacterID = br.ReadInt32();
                    import.Unknown1 = br.ReadUInt32();

                    long pos = br.BaseStream.Position;

                    br.BaseStream.Position = importMoviePtr;
                    import.ImportMovie = br.ReadCStr();

                    br.BaseStream.Position = importNamePtr;
                    import.ImportName = br.ReadCStr();

                    br.BaseStream.Position = pos;

                    movie.Imports[i] = import;
                }
                
                br.BaseStream.Position = dataStart + exportPtr;
                movie.Exports = new Export[exportCount];
                for (int i = 0; i < movie.Exports.Length; i++)
                {
                    Export export = new();

                    uint exportNamePtr = dataStart + br.ReadUInt32();
                    export.CharacterID = br.ReadInt32();
                    
                    long pos = br.BaseStream.Position;

                    br.BaseStream.Position = exportNamePtr;
                    export.ExportName = br.ReadCStr();

                    br.BaseStream.Position = pos;

                    movie.Exports[i] = export;
                }

                br.BaseStream.Position = myPos;

                // TODO: Init Arrays

                result.Data = movie;
                break;
            case CharacterType.StaticText:
                CharacterStaticText staticText = new();
                // TODO: Static Text
                result.Data = staticText;
                break;
            case CharacterType.None:
                break;
            case CharacterType.Video:
                CharacterVideo video = new();
                // TODO: Video
                result.Data = video;
                break;
            default:
                MessageBox.Show("Out of range Character Type: " + result.Type);
                break;
        }

        // TODO: Finish Read

        return result;
    }

    public void Write(BinaryWriter2 bw)
    {
        bw.Write((uint)Type);
        bw.Write(Signature);
        bw.Write(Unknown1);
        bw.Write(ID);
        bw.Write(Unknown2);

        // TODO: Finish Write
    }

    public override string ToString()
    {
        return "Type: " + Type + ", Signature: " + Signature.ToString("X8") + ", Unk1: " + Unknown1.ToString("X4") + ", ID: " + ID.ToString("X4") + ", Unk2: " + Unknown2.ToString("X8");
    }
}

public class AptData : IEntryData
{
    public string Component1Name;
    public string Component2Name;

    public Character RootMovie;

    public List<AptConst> Constants;

    public AptData()
    {
        Constants = [];
    }

    private void Clear()
    {
        Component1Name = default;
        Component2Name = default;

        RootMovie = default;

        Constants.Clear();
    }

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        Clear();

        MemoryStream ms = entry.MakeStream();
        BinaryReader2 br = new(ms);
        br.BigEndian = entry.Console;

        uint componentName2Ptr = br.ReadUInt32();
        uint componentName1Ptr = br.ReadUInt32();
        uint aptDataOffset = br.ReadUInt32();
        uint constOffset = br.ReadUInt32();
        uint geometryOffset = br.ReadUInt32();
        uint fileSize = br.ReadUInt32();

        /*int numPadding = (int)(16 - br.BaseStream.Position % 16);
        for (int i = 0; i < numPadding; i++)
            br.ReadByte();*/

        br.BaseStream.Position = componentName1Ptr;
        Component1Name = br.ReadCStr();
        br.BaseStream.Position = componentName2Ptr;
        Component2Name = br.ReadCStr();

        br.BaseStream.Position = constOffset;
        byte[] constMagic = br.ReadBytes(20);

        uint movieOffset = br.ReadUInt32();

        int constCount = br.ReadInt32();
        uint constantStartOffset = br.ReadUInt32();

        br.BaseStream.Position = constOffset + constantStartOffset;
        for (int i = 0; i < constCount; i++)
        {
            AptConst constant = new();

            constant.Type = (AptConstType)br.ReadUInt32();
            constant.Data = br.ReadUInt32();

            Constants.Add(constant);
        }

        for (int i = 0; i < Constants.Count; i++)
        {
            if (Constants[i].Type == AptConstType.String)
            {
                br.BaseStream.Position = constOffset + Constants[i].Data;
                Constants[i].String = br.ReadCStr(); // 4 byte aligned
            }
        }

        br.BaseStream.Position = aptDataOffset;
        byte[] dataMagic = br.ReadBytes(16);

        br.BaseStream.Position = aptDataOffset + movieOffset;
        RootMovie = Character.Read(br, aptDataOffset);

        // TODO: Finish Read

        br.Close();
        ms.Close();

        return true;
    }

    public bool Write(BundleEntry entry)
    {
        MemoryStream ms = new();
        BinaryWriter2 bw = new(ms);
        bw.BigEndian = entry.Console;

        // TODO: Write

        bw.Flush();
        byte[] data = ms.ToArray();
        bw.Close();
        ms.Close();

        entry.EntryBlocks[0].Data = data;
        entry.Dirty = true;

        return true;
    }

    public EntryType GetEntryType(BundleEntry entry)
    {
        return EntryType.AptData;
    }

    public IEntryEditor GetEditor(BundleEntry entry)
    {
        return null;
    }
}
