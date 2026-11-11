using System.Reflection;
using System.Runtime.Loader;

namespace CodeStyle;

internal static class SdkHost
{
    private static bool initialized;

    public static async Task InitializeAsync()
    {
        if (initialized)
            return;

        string directory = Directory.GetCurrentDirectory();
        ProcessResult selected = await ProcessRunner.RunAsync("dotnet", ["--version"], directory);
        ProcessResult installed = await ProcessRunner.RunAsync("dotnet", ["--list-sdks"], directory);

        if (selected.ExitCode != 0 || installed.ExitCode != 0)
            throw new InvalidOperationException("Install the .NET 10 SDK and run this command from a compatible SDK directory.");

        string version = selected.Output.Trim();
        string? sdk = null;

        foreach (string line in installed.Output.Split('\n'))
        {
            if (!line.StartsWith(version + " [", StringComparison.Ordinal))
                continue;

            int opening = line.IndexOf('['), closing = line.LastIndexOf(']');
            sdk = Path.Combine(line[(opening + 1)..closing], version);
            break;
        }

        if (sdk == null || !Version.TryParse(version.Split('-')[0], out Version? sdkVersion) || sdkVersion.Major < 10)
            throw new InvalidOperationException("CodeStyle requires the .NET 10 SDK or later.");

        string tools = Path.Combine(sdk, "DotnetTools", "dotnet-format");

        if (!File.Exists(Path.Combine(tools, "Microsoft.CodeAnalysis.CSharp.Workspaces.dll")))
            throw new InvalidOperationException("The selected SDK does not contain the Roslyn formatting tools.");

        AssemblyLoadContext.Default.Resolving += (_, name) =>
        {
            string path = Path.Combine(tools, name.Name + ".dll");
            return File.Exists(path)
                ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path)
                : null;
        };
        Assembly locator = Assembly.LoadFrom(Path.Combine(tools, "BuildHost-netcore", "Microsoft.Build.Locator.dll"));
        Type registration = locator.GetType("Microsoft.Build.Locator.MSBuildLocator", true)!;
        registration.GetMethod("RegisterMSBuildPath", [typeof(string)])!.Invoke(null, [sdk]);
        Assembly.Load("Microsoft.CodeAnalysis.CSharp.Workspaces");
        initialized = true;
    }
}
