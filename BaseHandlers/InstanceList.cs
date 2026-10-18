using System.Collections.Generic;
using System.IO;
using BundleFormat;
using BundleUtilities;
using MathLib;
using ModelViewer.SceneData;
using OpenTK.Mathematics;
using PluginAPI;

namespace BaseHandlers;

public class ModelInstance
{
    public ulong ModelEntryID;

    public int ModelEntryPtr;
    public int Unknown2;
    public int Unknown3;
    public int Unknown4;
    public Matrix4 Transform;

    public Vector3 Translation => Transform.ExtractTranslation();

    public Vector3 Scale => Transform.ExtractScale();

    public Vector4 Rotation => Transform.ExtractRotation().ToAxisAngle();

    public static ModelInstance Read(BinaryReader2 br)
    {
        ModelInstance result = new();

        result.ModelEntryPtr = br.ReadInt32();
        result.Unknown2 = br.ReadInt32();
        result.Unknown3 = br.ReadInt32();
        result.Unknown4 = br.ReadInt32();
        result.Transform = br.ReadMatrix4();

        return result;
    }

    public void Write(BinaryWriter2 bw)
    {
        bw.Write(ModelEntryPtr);
        bw.Write(Unknown2);
        bw.Write(Unknown3);
        bw.Write(Unknown4);
        bw.Write(Transform);
    }

    public override string ToString()
    {
        return "Model ID: 0x" + ModelEntryID.ToString("X8") + ", Translation: (" + Translation + "), Scale: (" + Scale + "), Rotation: (" + Rotation + ")";
    }
}

public class InstanceList : IEntryData
{
    //private Scene _scene;

    public BundleEntry Entry;

    public int Unknown1;
    public List<ModelInstance> Instances;
    public int Unknown2;
    public int Unknown3;

    public byte[] RemainingBytes;

    public InstanceList()
    {
        Instances = [];
    }

    private void Clear()
    {
        //_scene = null;

        Entry = default;
        Unknown1 = default;
        Unknown2 = default;
        Unknown3 = default;

        Instances.Clear();
    }

    public bool Read(BundleEntry entry, ILoader loader = null)
    {
        Clear();

        Entry = entry;

        MemoryStream ms = entry.MakeStream();
        BinaryReader2 br = new(ms);
        br.BigEndian = entry.Console;

        Unknown1 = br.ReadInt32();
        int instanceCount = br.ReadInt32();
        Unknown2 = br.ReadInt32();
        Unknown3 = br.ReadInt32();

        for (int i = 0; i < instanceCount; i++)
        {
            ModelInstance instance = ModelInstance.Read(br);

            instance.ModelEntryID = entry.GetDependencies()[i].EntryID;

            Instances.Add(instance);
        }

        RemainingBytes = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));

        br.Close();
        ms.Close();

        //_scene = MakeScene(loader);

        return true;
    }

    public bool Write(BundleEntry entry)
    {
        MemoryStream ms = new();
        BinaryWriter2 bw = new(ms);
        bw.BigEndian = entry.Console;

        bw.Write(Unknown1);
        bw.Write(Instances.Count);
        bw.Write(Unknown2);
        bw.Write(Unknown3);

        for (int i = 0; i < Instances.Count; i++)
        {
            ModelInstance instance = Instances[i];

            instance.Write(bw);
        }

        bw.Write(RemainingBytes);

        bw.Flush();
        byte[] data = ms.GetBuffer();
        bw.Close();
        ms.Close();

        entry.EntryBlocks[0].Data = data;
        entry.Dirty = true;

        return true;
    }

    public Scene MakeScene(ILoader loader = null)
    {
        //TextureState.ResetCache();

        Scene scene = new();

        Dictionary<ulong, Renderable> models = [];

        int i = 0;
        int index = 1;
        foreach (ModelInstance instance in Instances)
        {
            int progress = (index - 1) * 100 / Instances.Count;
            loader?.SetProgress(progress);

            loader?.SetStatus("Loading(" + progress.ToString("D2") + "%): ModelInstance: " + index + "/" + Instances.Count);
            //DebugTimer t = DebugTimer.Start("ModelInstance[" + index + "/" + Instances.Count + "]");
            index++;

            if (!InRange(instance))
                continue;

            if (models.ContainsKey(instance.ModelEntryID))
            {
                Renderable renderable = models[instance.ModelEntryID];
                SceneObject sceneObject = new(instance.ModelEntryID.ToString("X8"), renderable.Model);
                sceneObject.ID = i.ToString();
                sceneObject.Transform = instance.Transform;

                scene.AddObject(sceneObject);
            }
            else
            {
                BundleEntry modelEntry = Entry.Archive.GetEntryByID(instance.ModelEntryID);
                if (modelEntry == null)
                {
                    string file = BundleCache.GetFileByEntryID(instance.ModelEntryID);
                    if (!string.IsNullOrEmpty(file))
                    {
                        BundleArchive archive = BundleArchive.Read(file);
                        modelEntry = archive.GetEntryByID(instance.ModelEntryID);
                    }
                }

                if (modelEntry != null)
                {
                    BundleEntry renderableEntry = modelEntry.GetDependencies()[0].Entry;
                    Renderable renderable = new();
                    renderable.Read(renderableEntry, null); // TODO: Null Loader
                    models.Add(instance.ModelEntryID, renderable);
                    SceneObject sceneObject =
                        new(instance.ModelEntryID.ToString("X8"), renderable.Model);
                    sceneObject.ID = i.ToString();
                    sceneObject.Transform = instance.Transform;

                    scene.AddObject(sceneObject);
                }
            }
            i++;
            //t.StopLog();
        }
        loader?.SetProgress(100);

        //TextureState.ResetCache();

        return scene;
    }

    private bool InRange(ModelInstance instance)
    {
        return true;
    }

    public EntryType GetEntryType(BundleEntry entry)
    {
        return EntryType.InstanceList;
    }

    public IEntryEditor GetEditor(BundleEntry entry)
    {
        InstanceListEditor editor = new();
        editor.InstanceList = this;
        editor.Changed += () =>
        {
            Write(entry);
        };

        return editor;
    }
}
