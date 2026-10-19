using System.Collections.Generic;
using System.Drawing;
using System.IO;
using BundleFormat;
using BundleUtilities;

namespace BaseHandlers;

public class MaterialEntry
{
    public List<TextureState> TextureStates;

    public Image DiffuseMap { get; set; }
    public Image NormalMap { get; set; }
    public Image SpecularMap { get; set; }
    public Color Color { get; set; }

    public MaterialEntry()
    {
        TextureStates = [];
    }

    public static MaterialEntry Read(BundleEntry entry)
    {
        MaterialEntry result = new();
        
        List<BundleDependency> dependencies = entry.GetDependencies();
        foreach (BundleDependency dependency in dependencies)
        {
            ulong id = dependency.EntryID;

            BundleEntry descEntry1 = entry.Archive.GetEntryByID(id) ?? BundleCache.GetEntryByID(id);

            //DebugTimer t2 = DebugTimer.Start("LoadTextureState");
            if (descEntry1 != null && descEntry1.Type == EntryType.TextureState)
            {
                TextureState state = TextureState.Read(descEntry1);

                result.TextureStates.Add(state);
            }
            //t2.StopLog();
        }

        MemoryStream ms = entry.MakeStream();
        BinaryReader2 br = new(ms);
        br.BigEndian = entry.Console;

        // TODO: Read Material

        br.Close();
        ms.Close();

        result.Color = Color.White;

        if (result.TextureStates.Count > 0)
            result.DiffuseMap = result.TextureStates[0].Texture;
        if (result.TextureStates.Count > 1)
            result.NormalMap = result.TextureStates[1].Texture;
        if (result.TextureStates.Count > 2)
            result.SpecularMap = result.TextureStates[2].Texture;

        return result;
    }
}
