using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace CodeStyle;

internal static class StyleFormatter
{
    public static async Task<SourceText> FormatAsync(Document document, StyleSettings settings)
    {
        SyntaxNode original = (await document.GetSyntaxRootAsync())!;
        CSharpParseOptions parseOptions = (CSharpParseOptions?)document.Project.ParseOptions ?? CSharpParseOptions.Default;
        SourceText prepared = LayoutPass.Apply(original, settings);
        Document formatted = await Formatter.FormatAsync(document.WithText(prepared));
        SyntaxNode root = (await formatted.GetSyntaxRootAsync())!;
        SourceText indented = IndentationPass.Apply(root, await formatted.GetTextAsync(), settings);
        SyntaxNode indentedRoot = CSharpSyntaxTree.ParseText(indented, parseOptions).GetRoot();
        SourceText output = TriviaPass.Apply(indentedRoot, indented, settings, parseOptions);
        SyntaxNode after = CSharpSyntaxTree.ParseText(output, parseOptions).GetRoot();

        SyntaxToken[] beforeTokens = [.. original.DescendantTokens()], afterTokens = [.. after.DescendantTokens()];

        if (!beforeTokens.Select(token => (token.RawKind, token.ValueText)).SequenceEqual(afterTokens.Select(token => (token.RawKind, token.ValueText))))
        {
            int mismatch = Enumerable.Range(0, Math.Min(beforeTokens.Length, afterTokens.Length))
                .FirstOrDefault(index => beforeTokens[index].RawKind != afterTokens[index].RawKind || beforeTokens[index].ValueText != afterTokens[index].ValueText);
            throw new InvalidOperationException($"Formatting would change code or a literal in {document.FilePath} near token {mismatch}. No formatting was written to this file.");
        }

        return output;
    }
}
