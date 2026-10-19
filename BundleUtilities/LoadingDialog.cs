using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BundleUtilities;

public partial class LoadingDialog : Form, ILoader
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string Status
    {
        get => lblStatus.Text;
        set => lblStatus.Text = value;
    }

    public LoadingDialog()
    {
        InitializeComponent();

        System.Windows.Forms.Timer refresh = new(components) { Interval = 50, Enabled = true };
        refresh.Tick += (_, _) => ShowProgress();
    }

    // Shows the dialog while work runs on a worker thread
    public static async Task<T> RunAsync<T>(IWin32Window owner, string status, Func<ILoader, CancellationToken, T> work)
    {
        using LoadingDialog dialog = new() { Status = status };
        CancellationTokenSource cancel = new(); // Not disposed, as abandoned work may still read its token
        Task<T> task = Task.Run(() => work(dialog, cancel.Token));

        dialog.Shown += async (_, _) =>
        {
            try
            {
                await task;
            }
            catch
            {
                // The caller gets it below
            }
            if (dialog.Visible)
                dialog.Close();
        };
        await dialog.ShowDialogAsync(owner);

        if (!task.IsCompleted)
        {
            cancel.Cancel();
            throw new OperationCanceledException();
        }
        return await task;
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        Close();
    }

    private volatile string _status;
    private volatile int _progress = -1;

    public void SetStatus(string status) => _status = status;

    public void SetProgress(int progress) => _progress = progress;

    private void ShowProgress()
    {
        string status = _status;
        if (status != null && lblStatus.Text != status)
            lblStatus.Text = status;

        int progress = _progress;
        if (progress >= 0 && pboMain.Value != progress)
        {
            pboMain.Style = ProgressBarStyle.Blocks;
            pboMain.Value = progress;
        }
    }
}
