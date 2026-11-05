using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using BundleUtilities;

namespace BaseHandlers;

public sealed partial class TrafficEditor
{
    private bool worldLoading;

    private void InitializeWorldVisualization(MenuStrip menu, bool promptForWorld)
    {
        ToolStripMenuItem openWorld = new("&Open worldcol.bin…");
        ToolStripMenuItem removeWorld = new("&Remove world geometry");
        ToolStripMenuItem viewMenu = new("&View");
        viewMenu.DropDownItems.AddRange([openWorld, removeWorld]);
        menu.Items.Add(viewMenu);
        openWorld.Click += async (_, _) => await OpenWorldVisualization();
        removeWorld.Click += (_, _) => view.SetWorldGeometry(null);
        viewMenu.DropDownOpening += (_, _) =>
        {
            openWorld.Enabled = !worldLoading;
            removeWorld.Enabled = !worldLoading && view.HasWorldGeometry;
        };

        if (promptForWorld)
            Shown += async (_, _) =>
            {
                DialogResult choice = MessageBox.Show(this,
                    "Open worldcol.bin to show the world geometry behind the traffic data?\n\nYou can also open it later from the View menu.",
                    "World visualization", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (choice == DialogResult.Yes)
                    await OpenWorldVisualization();
            };
    }

    private async Task OpenWorldVisualization()
    {
        if (worldLoading)
            return;

        view.CancelMove();
        using OpenFileDialog dialog = new()
        {
            Title = "Open world collision for traffic visualization",
            FileName = "worldcol.bin",
            Filter = "World collision bundles (*.bin;*.bundle;*.bndl)|*.bin;*.bundle;*.bndl|All files (*.*)|*.*",
            CheckFileExists = true,
            InitialDirectory = Path.GetDirectoryName(entry.Archive?.Path) ?? ""
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        worldLoading = true;

        try
        {
            string path = dialog.FileName;
            TrafficWorldGeometry geometry = await LoadingDialog.RunAsync(this, "Loading world geometry…",
                (loader, cancel) => TrafficWorldGeometry.Load(path, loader, cancel));

            if (!IsDisposed)
                view.SetWorldGeometry(geometry);
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            MessageBox.Show(this, "Could not open the world geometry.\n\n" + exception.Message,
                "Cannot open world visualization", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            worldLoading = false;
        }
    }
}
