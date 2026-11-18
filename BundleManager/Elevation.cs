using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BundleManager;

public static class Elevation
{
    public const string CopyArgument = "--elevated-copy";

    public static bool Offer(IWin32Window owner, string path) =>
        !path.StartsWith(@"\\", StringComparison.Ordinal)
        && new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path))).DriveType != DriveType.Network
        && MessageBox.Show(owner, $"Windows won't let Bundle Manager write to {path}.\n\nSave it with administrator rights?", "Save",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

    public static async Task Copy(string source, string target)
    {
        Process process;

        try
        {
            process = Process.Start(new ProcessStartInfo(Environment.ProcessPath, $"{CopyArgument} \"{source}\" \"{target}\"")
            {
                UseShellExecute = true,
                Verb = "runas"
            });
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223) // ERROR_CANCELLED
        {
            throw new OperationCanceledException();
        }

        using (process)
        {
            await process.WaitForExitAsync();
            string error = source + ".error";

            if (process.ExitCode != 0)
                throw new IOException(File.Exists(error)
                    ? File.ReadAllText(error)
                    : $"The administrator copy of Bundle Manager couldn't save it (exit code {process.ExitCode}).");
        }
    }

    public static int RunCopy(string source, string target)
    {
        try
        {
            File.Copy(source, target, true);
            return 0;
        }
        catch (Exception ex)
        {
            File.WriteAllText(source + ".error", ex.Message);
            return 1;
        }
    }
}
