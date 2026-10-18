using System;
using System.Windows.Forms;

namespace PVSFormat;

public partial class AddZone : Form
{
    ulong zone;

    public AddZone()
    {
        InitializeComponent();
    }

    public ulong GetNewZoneId()
    {
        ShowDialog();
        return zone;
    }

    private void okButton_Click(object sender, EventArgs e)
    {
        zone = (ulong)zoneIdNumericUpDown.Value;
        Close();
    }

    private void cancelButton_Click(object sender, EventArgs e)
    {
        zone = ulong.MaxValue;
        Close();
    }
}
