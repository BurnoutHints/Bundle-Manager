using System.Diagnostics;

namespace CodeStyle;

internal sealed record ProcessResult(int ExitCode, string Output, string Error);

internal static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(string command, IEnumerable<string> arguments, string directory, bool echo = false)
    {
        ProcessStartInfo start = new(command)
        {
            WorkingDirectory = directory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (string argument in arguments)
            start.ArgumentList.Add(argument);

        using Process process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start " + command);
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        ProcessResult result = new(process.ExitCode, await output, await error);

        if (echo)
        {
            Console.Write(result.Output);
            Console.Error.Write(result.Error);
        }

        return result;
    }
}
