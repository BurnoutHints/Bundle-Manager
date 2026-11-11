using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeStyle;

internal sealed class StyleSettings(AnalyzerConfigOptions options)
{
    public int IndentSize => int.TryParse(Get("indent_size", "4"), out int size) && size > 0
        ? size
        : int.TryParse(Get("tab_width", "4"), out int width) && width > 0
            ? width
            : 4;

    public string NewLine => Get("end_of_line", "crlf").ToLowerInvariant() switch
    {
        "lf" => "\n",
        "cr" => "\r",
        _ => "\r\n"
    };

    public bool InlineProperties => Enabled("codestyle_inline_auto_properties");

    public bool ExpandBlocks => Enabled("codestyle_expand_nonempty_blocks");

    public bool ExpandInitializers => Enabled("codestyle_expand_initializers");

    public bool TernaryLines => Enabled("codestyle_ternary_new_lines");

    public bool CommentSpacing => Enabled("codestyle_blank_line_before_comments");

    public bool AllowMultipleBlankLines => Enabled("dotnet_style_allow_multiple_blank_lines_experimental", false);

    public bool StepSpacing => Enabled("codestyle_separate_logical_steps");

    public bool CheckNaming => Enabled("codestyle_check_naming");

    public bool PreserveMembers => Enabled("codestyle_preserve_member_names", false);

    public bool PreserveHandlers => Enabled("codestyle_preserve_winforms_handlers");

    public bool Excluded => Enabled("codestyle_exclude", false);

    public bool Generated => Enabled("generated_code", false);

    public bool FormatGenerated => Enabled("codestyle_format_generated_code", false);

    public bool EmitBOM => Get("charset", "utf-8").ToLowerInvariant() switch
    {
        "utf-8" => false,
        "utf-8-bom" => true,
        _ => throw new NotSupportedException("CodeStyle supports charset = utf-8 or utf-8-bom. Convert other source encodings explicitly before formatting.")
    };

    private bool Tabs => Get("indent_style", "space").Equals("tab", StringComparison.OrdinalIgnoreCase);

    private int TabWidth => int.TryParse(Get("tab_width", IndentSize.ToString()), out int width) && width > 0
        ? width
        : IndentSize;

    public int LeadingIndent(string line)
    {
        int columns = 0;

        foreach (char character in line)
        {
            if (character == ' ')
                columns++;
            else if (character == '\t')
                columns += TabWidth - columns % TabWidth;
            else
                break;
        }

        return columns;
    }

    public string Indentation(int columns) => Tabs
        ? new string('\t', columns / TabWidth) + new string(' ', columns % TabWidth)
        : new string(' ', columns);

    public string[] Acronyms => Words("codestyle_acronyms", "RGB,HSV,CRC,PVS,ID");

    public string[] PreservedWords => Words("codestyle_preserved_words", "CgsID");

    public string[] IdentifierExceptions => Words("codestyle_identifier_exceptions", "");

    public bool NamingRuleEnabled(string rule) => !Get("dotnet_naming_rule." + rule + ".severity", "suggestion").Equals("none", StringComparison.OrdinalIgnoreCase);

    private bool Enabled(string key, bool fallback = true) => bool.TryParse(Get(key, fallback.ToString()), out bool enabled) && enabled;

    private string Get(string key, string fallback) => options.TryGetValue(key, out string? value)
        ? value
        : fallback;

    private string[] Words(string key, string fallback) => Get(key, fallback).Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
