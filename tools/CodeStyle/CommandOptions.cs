namespace CodeStyle;

internal sealed record CommandOptions(bool Fix, string Workspace, string[] Includes)
{
    public static CommandOptions? Parse(string[] args)
    {
        if (args.Length == 0 || args is ["--help"] or ["-h"])
        {
            Console.WriteLine("Usage: CodeStyle check|fix [--path solution-or-project] [--include file-or-directory ...]");
            Console.WriteLine("check reports violations without modifying sources; fix applies formatting and reports naming issues.");
            return null;
        }

        if (args[0] is not ("check" or "fix"))
            throw new ArgumentException("Choose check or fix. Use --help for usage.");

        string? path = null;
        List<string> includes = [];

        for (int index = 1; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--path" when index + 1 < args.Length:
                    path = Path.GetFullPath(args[++index]);
                    break;
                case "--include":
                    int count = includes.Count;

                    while (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
                        includes.Add(Path.GetFullPath(args[++index]));

                    if (includes.Count == count)
                        throw new ArgumentException("--include requires at least one file or directory.");

                    break;
                default:
                    throw new ArgumentException("Unknown or incomplete option: " + args[index]);
            }
        }

        path ??= FindWorkspace();

        if (Directory.Exists(path))
            path = WorkspaceIn(path) ?? throw new ArgumentException("Choose a solution or project using --path.");

        if (!File.Exists(path) || Path.GetExtension(path) is not (".sln" or ".slnx" or ".csproj"))
            throw new ArgumentException("--path must name an existing .sln, .slnx, or .csproj file.");

        return new(args[0] == "fix", path, [.. includes]);
    }

    private static string FindWorkspace()
    {
        for (DirectoryInfo? directory = new(Directory.GetCurrentDirectory()); directory != null; directory = directory.Parent)
        {
            string? workspace = WorkspaceIn(directory.FullName);

            if (workspace != null)
                return workspace;
        }

        throw new ArgumentException("No solution or project found. Use --path.");
    }

    private static string? WorkspaceIn(string directory)
    {
        string[] solutions = [.. Directory.EnumerateFiles(directory).Where(path => Path.GetExtension(path) is ".sln" or ".slnx")];

        if (solutions.Length == 1)
            return solutions[0];

        string[] projects = Directory.GetFiles(directory, "*.csproj");
        return solutions.Length == 0 && projects.Length == 1
            ? projects[0]
            : null;
    }
}
