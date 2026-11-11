using CodeStyle;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Reflection;
using System.Text;

namespace CodeStyleTests;

internal static class FixtureChecks
{
    private sealed class Configuration(Dictionary<string, string> values) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value) => values.TryGetValue(key, out value!);
    }

    private const string ConfigurationText = """
        root = true
        [*.cs]
        indent_style = space
        indent_size = 4
        end_of_line = lf
        charset = utf-8
        csharp_preserve_single_line_blocks = true
        csharp_preserve_single_line_statements = false
        csharp_new_line_before_open_brace = all
        csharp_style_var_for_built_in_types = false
        csharp_style_var_when_type_is_apparent = false
        csharp_style_var_elsewhere = false
        dotnet_diagnostic.IDE0008.severity = suggestion
        """;

    private static int checks;

    public static async Task RunAsync()
    {
        string source = """
            namespace Demo;
            internal class Sample {
                public int Count
                {
                    get; protected set;
                } // stored offset
                private void Run(bool condition) {
                    State state = new() { A = 1, B = 2 }

                    ;
                    if (condition) state.A++;
                    int value = condition ? 1 : condition ? 2 : 3;
                    // Save the result
                    // Keep this explanation together
                    state.B = value;
                    void LocalHelper() { state.A++; }
                    LocalHelper();
                }
            }
            """;
        string formatted = await FormatAsync(source);
        Check(formatted.Contains("public int Count { get; protected set; } // stored offset"), "Auto-properties stay inline with accessor visibility and trailing comments");
        Check(formatted.Contains("A = 1,\n            B = 2\n        };"), "Initializer members expand and closing semicolons stay attached");
        Check(formatted.Contains("if (condition)\n            state.A++;"), "Unbraced statements start on a new, indented line");
        Check(formatted.Contains("int value = condition\n            ? 1\n            : condition\n                ? 2\n                : 3;"), "Nested ternaries use one continuation level per nesting level");
        Check(formatted.Contains("\n\n        // Save the result\n        // Keep this explanation together\n        state.B"), "Comment groups have a blank line above them without splitting consecutive comments");
        Check(formatted.Contains("\n\n        void LocalHelper()") && formatted.Contains("}\n\n        LocalHelper();"), "Local functions are separated from surrounding steps");
        Check(await FormatAsync(formatted) == formatted, "Formatting is idempotent");
        string attached = await FormatAsync("class Sample { void Run() { if (true) { Run(); }; } }");
        Check(attached.Contains("};") && await FormatAsync(attached) == attached, "Closing-brace semicolons remain attached across repeated formatting");

        string literals = """"
            namespace Demo;
            internal class Sample {
                private const string Text = """
                    { get; set; } ? preserve : these;
                    // This is text, not a comment
                    """;
                private void Run() {
                    // First statement in the block
                    string path = @"C:\A\B";
                    /* Keep this block comment. */
                    _ = path;
                }
            }
            """";
        string literalResult = await FormatAsync(literals);
        Check(Tokens(literals).SequenceEqual(Tokens(literalResult)), "Formatting preserves raw and verbatim string values and code tokens");
        Check(!literalResult.Contains("{\n\n        // First"), "A comment at the start of a block does not get an extra blank line");
        Check(literalResult.Contains("/* Keep this block comment. */"), "Block comments survive formatting");
        string mixedLines = "/// <summary>\r\n/// A documented type.\r\n/// </summary>\r\nclass Sample {\r\n\r\n\r\nint first;\r\n\r\n\r\n/* A block\r\ncomment. */\r\nint second;\r\n}\r\n";
        string normalizedLines = await FormatAsync(mixedLines);
        Check(!normalizedLines.Contains('\r') && !normalizedLines.Contains("\n\n\n"),
            "Documentation and block comments use configured line endings and surplus blank lines are removed");
        string multilineLiteral = "class Sample { private const string Text = @\"first\r\n\r\n\r\nsecond\"; }";
        Check(Tokens(multilineLiteral).SequenceEqual(Tokens(await FormatAsync(multilineLiteral))),
            "Line-ending and blank-line cleanup preserve multiline literal values");
        string regions = "#region Data\nclass Sample {\n//   \nint value;\n}\n#endregion\n";
        string formattedRegions = await FormatAsync(regions, extraConfiguration: "end_of_line = crlf");
        Check(!formattedRegions.Replace("\r\n", "").Contains('\n') && !formattedRegions.Contains("//   "),
            "Region directives use configured line endings and comments lose trailing whitespace");
        Check((await FormatAsync("class Sample {\nint first;\n\n\nint second;\n}\n",
            extraConfiguration: "dotnet_style_allow_multiple_blank_lines_experimental = true")).Contains("\n\n\n"),
            "Multiple blank lines are preserved when EditorConfig explicitly allows them");
        string directives = "namespace Demo;\ninternal class Sample {\n#if DEBUG\nprivate int value = 1;\n#else\nprivate int value = 2;\n#endif\n}\n";
        string directiveResult = await FormatAsync(directives);
        CSharpParseOptions debug = new CSharpParseOptions(LanguageVersion.Latest).WithPreprocessorSymbols("DEBUG");
        Check(CSharpSyntaxTree.ParseText(directives, debug).GetRoot().DescendantTokens().Select(token => token.ValueText)
            .SequenceEqual(CSharpSyntaxTree.ParseText(directiveResult, debug).GetRoot().DescendantTokens().Select(token => token.ValueText)),
            "Conditional-compilation symbols remain active during formatting");

        string narrow = await FormatAsync("class Sample { public int Value { get; set; } private void Run() { int value = true ? 1 : 2; } }", 2);
        Check(narrow.Contains("\n  public int Value") && narrow.Contains("\n      ? 1"), "Indent size is read from EditorConfig");

        StyleSettings names = new(new Configuration([]));
        Check(NamingRules.Expected("ReadIds", true, names) == "ReadIDs" && NamingRules.Expected("rgbValue", false, names) == "rgbValue", "Configured acronyms respect PascalCase and camelCase positions");
        Check(NamingRules.Expected("DecodeCGSID", true, names) == "DecodeCgsID", "CgsID keeps its explicit spelling");
        Check(NamingRules.Expected("HTTPServer", false, names) == "httpServer", "Initial uppercase acronym words become valid camelCase even outside the configured dictionary");
        Check(NamingRules.Expected("ReadMatrix3x2", true, names) == "ReadMatrix3x2", "Dimension notation does not produce a false acronym warning");
        SyntaxNode localFunctions = CSharpSyntaxTree.ParseText("class Sample { void Run() { void writeCgsID() { } void WriteBytes() { } void _readBytes() { } } }").GetRoot();
        StyleIssue[] localFunctionIssues = [.. NamingRules.Check(localFunctions, "Sample.cs", names, [])];
        Check(localFunctionIssues.Length == 2 && localFunctionIssues.Any(issue => issue.Message.Contains("'writeBytes'")) &&
            localFunctionIssues.Any(issue => issue.Message.Contains("'readBytes'")),
            "Local functions use camelCase without an underscore while member methods use PascalCase");
        SyntaxNode model = CSharpSyntaxTree.ParseText("class Model { public int mpaSpans { get; set; } public int muID; void Run(int Bad) { int _value = Bad; } }").GetRoot();
        StyleSettings preserve = new(new Configuration(new()
        {
            ["codestyle_preserve_member_names"] = "true"
        }));

        StyleIssue[] modelIssues = [.. NamingRules.Check(model, "Model.cs", preserve, [])];
        Check(modelIssues.Length == 2 && modelIssues.All(issue => !issue.Message.Contains("mpaSpans") && !issue.Message.Contains("muID")), "Game member exemptions preserve schema names while still checking parameters and locals");

        SyntaxNode handlers = CSharpSyntaxTree.ParseText("""
            partial class Window {
                void InitializeComponent() { btnCancel.Click += BtnCancel_Click; debugToolStripMenuItem.Click += DebugToolStripMenuItemClick; }
                void BtnCancel_Click(object sender, System.EventArgs e) { }
                void DebugToolStripMenuItemClick(object sender, System.EventArgs e) { }
            }
            """).GetRoot();
        StyleIssue[] handlerIssues = [.. NamingRules.Check(handlers, "Window.cs", names, NamingRules.EventBindings([handlers]))];
        Check(handlerIssues.Any(issue => issue.Message.Contains("btnCancel_Click")) && handlerIssues.Any(issue => issue.Message.Contains("debugToolStripMenuItem_Click")), "Handler diagnostics preserve the control's casing and event separator");
        SyntaxNode designer = CSharpSyntaxTree.ParseText("partial class Window { void InitializeComponent() { this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click); openHeader.Click += importData_Click; editor.OnDone += EditorOnDone; } }").GetRoot();
        SyntaxNode codeBehind = CSharpSyntaxTree.ParseText("partial class Window { void BtnCancel_Click(object sender, System.EventArgs e) { } void importData_Click(object sender, System.EventArgs e) { } void EditorOnDone(object value) { } }").GetRoot();
        StyleIssue[] designerIssues = [.. NamingRules.Check(codeBehind, "Window.cs", names, NamingRules.EventBindings([designer]))];
        Check(designerIssues.Length == 1 && designerIssues[0].Message.Contains("btnCancel_Click"), "Designer delegate constructors and legacy handler aliases are recognized without rewriting helper callbacks");

        await CheckCLIAsync();
        Console.WriteLine($"All {checks} CodeStyle checks passed.");
    }

    private static async Task<string> FormatAsync(string source, int indent = 4, string extraConfiguration = "")
    {
        using AdhocWorkspace workspace = new();
        string directory = Path.Combine(Path.GetTempPath(), "CodeStyle.UnitFixtures");
        Project project = workspace.AddProject("Fixture", LanguageNames.CSharp)
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.Latest).WithPreprocessorSymbols("DEBUG"));
        TextDocument configuration = project.AddAnalyzerConfigDocument(".editorconfig", SourceText.From(ConfigurationText.Replace("indent_size = 4", "indent_size = " + indent) + "\n" + extraConfiguration),
            filePath: Path.Combine(directory, ".editorconfig"));
        Document document = configuration.Project.AddDocument("Fixture.cs", SourceText.From(source), filePath: Path.Combine(directory, "Fixture.cs"));
        SyntaxTree tree = (await document.GetSyntaxTreeAsync())!;
        StyleSettings settings = new(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree));
        return (await StyleFormatter.FormatAsync(document, settings)).ToString();
    }

    private static async Task CheckCLIAsync()
    {
        string temporary = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "CodeStyle.Tests-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(temporary);

        try
        {
            string project = Path.Combine(temporary, "Fixture.csproj"), source = Path.Combine(temporary, "Fixture.cs");
            await File.WriteAllTextAsync(project, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>");
            await File.WriteAllTextAsync(Path.Combine(temporary, "NuGet.Config"), "<configuration><packageSources><clear /></packageSources></configuration>");
            await File.WriteAllTextAsync(Path.Combine(temporary, ".editorconfig"), ConfigurationText);
            await File.WriteAllTextAsync(source, "namespace Fixture; internal class Sample { public int Read(bool condition) { var value = condition ? 1 : 2; return value; } }");
            ProcessResult restore = await ProcessRunner.RunAsync("dotnet", ["restore", project, "-p:NuGetAudit=false", "--verbosity", "quiet"], temporary);
            Check(restore.ExitCode == 0, "The integration fixture restores without external packages");
            byte[] before = await File.ReadAllBytesAsync(source);
            ProcessResult check = await CLIAsync("check", project, temporary);
            Check(check.ExitCode == 1 && (await File.ReadAllBytesAsync(source)).SequenceEqual(before), "check fails for standard and custom violations without modifying source bytes");
            ProcessResult fix = await CLIAsync("fix", project, temporary);
            Check(fix.ExitCode == 0 && (await File.ReadAllTextAsync(source)).Contains("int value"), "fix applies standard explicit-type and custom layout rules");
            byte[] fixedSource = await File.ReadAllBytesAsync(source);
            ProcessResult repeated = await CLIAsync("fix", project, temporary);
            ProcessResult clean = await CLIAsync("check", project, temporary);
            Check(repeated.ExitCode == 0 && clean.ExitCode == 0 && (await File.ReadAllBytesAsync(source)).SequenceEqual(fixedSource), "Repeated fix and check agree on a clean file");
            await File.WriteAllTextAsync(source, "namespace Fixture; internal class Sample { private int BadField; }");
            ProcessResult naming = await CLIAsync("fix", project, temporary);
            Check(naming.ExitCode == 1 && (await File.ReadAllTextAsync(source)).Contains("BadField"), "fix reports naming problems without renaming symbols");
            await File.WriteAllTextAsync(source, "namespace Fixture; internal class Sample { private int value; }\n", Encoding.Unicode);
            string generated = Path.Combine(temporary, "Generated.cs");
            const string generatedSource = "// -----\n// <auto-generated>\nnamespace Fixture; internal class Generated { private int BAD; }";
            await File.WriteAllTextAsync(generated, generatedSource);
            await File.WriteAllTextAsync(Path.Combine(temporary, ".editorconfig"), ConfigurationText.Replace("charset = utf-8", "charset = utf-8-bom"));
            ProcessResult encodingCheck = await CLIAsync("check", project, temporary);
            Check(encodingCheck.ExitCode == 1 && (await File.ReadAllBytesAsync(source)) is [0xFF, 0xFE, ..], "check reports a non-UTF-8 file without rewriting its encoding");
            ProcessResult encoding = await CLIAsync("fix", project, temporary);
            Check(encoding.ExitCode == 0 && (await File.ReadAllBytesAsync(source)) is [0xEF, 0xBB, 0xBF, ..] && await File.ReadAllTextAsync(generated) == generatedSource,
                "Encoding preferences are applied while generated sources remain byte-for-byte untouched");
            string designer = Path.Combine(temporary, "Window.designer.cs");
            const string designerSource = "namespace Fixture { partial class Window { private object BAD; void InitializeComponent() { BAD = new object(); if (BAD != null) BAD = null; } } }";
            await File.WriteAllTextAsync(designer, designerSource);
            string generatedConfiguration = ConfigurationText.Replace("charset = utf-8", "charset = utf-8-bom") + "\n[*.{Designer,designer}.cs]\ngenerated_code = true\ncodestyle_format_generated_code = true\n";
            await File.WriteAllTextAsync(Path.Combine(temporary, ".editorconfig"), generatedConfiguration);
            byte[] designerBefore = await File.ReadAllBytesAsync(designer);
            ProcessResult designerCheck = await CLIAsync("check", project, temporary);
            Check(designerCheck.ExitCode == 1 && (await File.ReadAllBytesAsync(designer)).SequenceEqual(designerBefore),
                "Opted-in designer layout is checked without changing source bytes");
            ProcessResult designerFix = await CLIAsync("fix", project, temporary);
            string designerFormatted = await File.ReadAllTextAsync(designer);
            Check(designerFix.ExitCode == 0 && designerFormatted != designerSource && Tokens(designerSource).SequenceEqual(Tokens(designerFormatted)) &&
                designerFormatted.Contains("BAD = new object();"), "Designer formatting preserves generated declarations and constructor syntax");
            ProcessResult designerClean = await CLIAsync("check", project, temporary);
            Check(designerClean.ExitCode == 0 && await File.ReadAllTextAsync(generated) == generatedSource,
                "Opted-in designer layout stays clean while other generated files remain untouched");
            ProcessResult invalid = await ProcessRunner.RunAsync("dotnet", ["exec", "--runtimeconfig", RuntimeConfiguration(), typeof(SdkHost).Assembly.Location, "unknown-command"], temporary);
            Check(invalid.ExitCode == 2, "Invalid commands return a distinct usage failure");
        }
        finally
        {
            string parent = Path.GetDirectoryName(temporary)!;

            if (!Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar).Equals(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) ||
                !Path.GetFileName(temporary).StartsWith("CodeStyle.Tests-", StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing to clean a fixture outside its temporary directory.");

            Directory.Delete(temporary, true);
        }
    }

    private static Task<ProcessResult> CLIAsync(string command, string project, string directory) => ProcessRunner.RunAsync("dotnet",
        ["exec", "--runtimeconfig", RuntimeConfiguration(), typeof(SdkHost).Assembly.Location, command, "--path", project], directory);

    private static string RuntimeConfiguration() => Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, ".runtimeconfig.json");

    private static IEnumerable<(int Kind, string Value)> Tokens(string source) => CSharpSyntaxTree.ParseText(source).GetRoot().DescendantTokens().Select(token => (token.RawKind, token.ValueText));

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);

        checks++;
        Console.WriteLine("PASS " + message);
    }
}
