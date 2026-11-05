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

        System.Windows.Forms.Timer refresh = new(components)
        {
            Interval = 50,
            Enabled = true
        };

        refresh.Tick += (_, _) => ShowProgress();
    }

    // Shows the dialog while work runs on a worker thread
    public static async Task<T> RunAsync<T>(IWin32Window owner, string status, Func<ILoader, CancellationToken, T> work)
    {
        using LoadingDialog dialog = new()
        {
            Status = status
        };

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

    private volatile string status;
    private volatile int progress = -1;

    public void SetStatus(string newStatus) => status = newStatus;

    public void SetProgress(int newProgress) => progress = newProgress;

    private void ShowProgress()
    {
        string currentStatus = status;

        if (currentStatus != null && lblStatus.Text != currentStatus)
            lblStatus.Text = currentStatus;

        int currentProgress = progress;

        if (currentProgress >= 0 && pboMain.Value != currentProgress)
        {
            pboMain.Style = ProgressBarStyle.Blocks;
            pboMain.Value = currentProgress;
        }
    }
}
