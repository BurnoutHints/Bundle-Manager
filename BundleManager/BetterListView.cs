using System;
using System.Drawing;
using System.Windows.Forms;

namespace BundleManager;

class BetterListView : System.Windows.Forms.ListView
{
    public BetterListView()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        SetStyle(ControlStyles.EnableNotifyMessage, true);
    }

    protected override void OnNotifyMessage(Message m)
    {
        // Filter out the WM_ERASEBKGND message
        if (m.Msg != 0x14) // WM_ERASEBKGND
        {
            base.OnNotifyMessage(m);
        }
    }


    private void ScaleListViewColumns(SizeF factor)
    {
        foreach (ColumnHeader column in Columns)
        {
            column.Width = (int)Math.Round(column.Width * factor.Width);
        }
    }

    protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
    {
        base.ScaleControl(factor, specified);
        ScaleListViewColumns(factor);
    }
}
