using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PluginAPI;

namespace BaseHandlers;

// Edits a resource in a property grid and saves it on close if it changed
public class PropertyGridEditor : Form, IEntryEditor
{
    // serialize gives the bytes write would save. Collection editors change lists in place without telling the grid,
    // so changes are found by comparing those bytes from opening and closing.
    public PropertyGridEditor(string title, object data, Func<byte[]> serialize, Action write)
    {
        Controls.Add(new PropertyGrid { Dock = DockStyle.Fill, SelectedObject = data });

        Text = title;
        ClientSize = new Size(800, 600);

        byte[] opened = serialize();

        FormClosing += (_, e) =>
        {
            try
            {
                if (!serialize().AsSpan().SequenceEqual(opened))
                    write();
            }
            catch (InvalidDataException ex)
            {
                e.Cancel = MessageBox.Show(this, $"{ex.Message}\n\nClose without saving?", "Can't save", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No;
            }
        };
    }
}
