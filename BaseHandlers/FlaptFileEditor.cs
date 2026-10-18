using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using BundleFormat;
using PluginAPI;

namespace BaseHandlers.Flapt;

public class FlaptFileEditor : Form, IEntryEditor
{
    public FlaptFileEditor(FlaptFile flapt, BundleEntry entry)
    {
        Controls.Add(new PropertyGrid { Dock = DockStyle.Fill, SelectedObject = flapt });

        Text = "Flapt File Editor";
        ClientSize = new Size(800, 600);

        // Collection editors change lists in place without telling the grid, so compare what would be saved
        byte[] opened = flapt.Serialize(entry, out _);

        Activated += (_, _) => FlaptFile.Editing = flapt;
        FormClosing += (_, e) =>
        {
            try
            {
                if (!flapt.Serialize(entry, out int _).AsSpan().SequenceEqual(opened))
                    flapt.Write(entry);
            }
            catch (InvalidDataException ex)
            {
                e.Cancel = MessageBox.Show(this, $"{ex.Message}\n\nClose without saving?", "Can't save", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No;
            }
        };
    }
}
