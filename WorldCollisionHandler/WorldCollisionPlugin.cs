using BundleFormat;
using BundleUtilities;
using ModelViewer.SceneData;
using PluginAPI;
using System.IO;
using System.Windows.Forms;

namespace WorldCollisionHandler
{
    public class WorldCollisionPlugin : IPlugin
    {
        public string Id => "worldcolplugin";
        public string Name => "World Collision Handler";
        
        public static void Init()
        {
            EntryTypeRegistry.Register(EntryType.PolygonSoupList, new PolygonSoupList());

            PluginCommandRegistry.Register("dump_all_collisions", "Dump All Collisions", DumpAllCollisions, IsWorldCol);
            PluginCommandRegistry.Register("remove_wreck_surfaces", "Remove Wreck Surfaces", RemoveWreckSurfaces, IsWorldCol);
        }

        #region Extra Tools

        private static bool IsWorldCol(BundleArchive archive)
        {
            for (int i = 0; i < archive.Entries.Count; i++)
            {
                BundleEntry entry = archive.Entries[i];

                if (entry.Type == EntryType.PolygonSoupList)
                    return true;
            }

            return false;
        }

        private static void DumpAllCollisions(IWin32Window window, BundleArchive archive)
        {
            if (archive == null)
                return;

            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog(window);
            if (result == DialogResult.OK)
            {
                string path = fbd.SelectedPath;

                for (int i = 0; ; i++)
                {
                    string idListName = "trk_clil" + i;
                    string polyName = "trk_col_" + i;

                    ulong idListID = Crc32.HashCrc32B(idListName);
                    ulong polyID = Crc32.HashCrc32B(polyName);

                    BundleEntry entry = archive.GetEntryByID(idListID);
                    if (entry == null)
                        break;
                    Stream outFile = File.Open(path + "/" + idListName + ".bin", FileMode.Create, FileAccess.Write);
                    BinaryWriter2 bw = new BinaryWriter2(outFile);
                    bw.Write(entry.EntryBlocks[0].Data);
                    bw.Flush();
                    bw.Close();
                    outFile.Close();

                    BundleEntry polyEntry = archive.GetEntryByID(polyID);
                    if (polyEntry == null)
                        break;
                    Stream outFilePoly = File.Open(path + "/" + polyName + ".bin", FileMode.Create, FileAccess.Write);
                    BinaryWriter2 bwPoly = new BinaryWriter2(outFilePoly);
                    bwPoly.Write(polyEntry.EntryBlocks[0].Data);
                    bwPoly.Flush();
                    bwPoly.Close();
                    outFilePoly.Close();

                    PolygonSoupList poly = new PolygonSoupList();
                    poly.Read(polyEntry);
                    Scene scene = poly.MakeScene();
                    scene.ExportWavefrontObj(path + "/" + polyName + ".obj");
                }

                MessageBox.Show(window, "Done!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private static void RemoveWreckSurfaces(IWin32Window window, BundleArchive archive)
        {
            for (int i = 0; i < archive.Entries.Count; i++)
            {
                BundleEntry entry = archive.Entries[i];

                if (entry.Type == EntryType.PolygonSoupList)
                {
                    PolygonSoupList list = new PolygonSoupList();
                    list.Read(entry);
                    list.RemoveWreckSurfaces();
                    list.Write(entry);
                }
            }
        }

        #endregion
    }
}
