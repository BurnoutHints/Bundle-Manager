using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CodeStyle;

internal static class TriviaPass
{
    public static SourceText Apply(SyntaxNode root, SourceText text, StyleSettings settings, CSharpParseOptions parseOptions)
    {
        Dictionary<int, TextChange> changes = [];

        if (settings.TernaryLines)
            foreach (ConditionalExpressionSyntax conditional in root.DescendantNodes().OfType<ConditionalExpressionSyntax>())
            {
                ConditionalExpressionSyntax[] outer = [.. conditional.Ancestors().OfType<ConditionalExpressionSyntax>()];
                ConditionalExpressionSyntax top = outer.LastOrDefault() ?? conditional;
                TextLine sourceLine = text.Lines.GetLineFromPosition(top.SpanStart);
                int prefixLength = sourceLine.ToString().TakeWhile(char.IsWhiteSpace).Count();
                int leading = settings.LeadingIndent(sourceLine.ToString());

                if (root.FindToken(sourceLine.Start + prefixLength).Parent is ConditionalExpressionSyntax)
                {
                    SyntaxNode? owner = top.Ancestors().FirstOrDefault(node => node is StatementSyntax and not BlockSyntax)
                        ?? top.Ancestors().FirstOrDefault(node => node is MemberDeclarationSyntax);

                    if (owner != null)
                        leading = settings.LeadingIndent(text.Lines.GetLineFromPosition(owner.SpanStart).ToString());
                }

                int indentation = leading + settings.IndentSize * (outer.Length + 1);
                set(conditional.QuestionToken.SpanStart, indentation);
                set(conditional.ColonToken.SpanStart, indentation);
            }

        SourceText ternaries = text.WithChanges(changes.Values);
        SyntaxNode updated = CSharpSyntaxTree.ParseText(ternaries, parseOptions).GetRoot();
        List<TextChange> commentBreaks = [];

        if (settings.CommentSpacing)
        {
            SyntaxTrivia[] comments = [.. updated.DescendantTrivia().Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))];

            for (int index = 0; index < comments.Length; index++)
            {
                SyntaxTrivia comment = comments[index];
                TextLine line = ternaries.Lines.GetLineFromPosition(comment.FullSpan.Start);

                if (line.LineNumber == 0 || !string.IsNullOrWhiteSpace(ternaries.ToString(TextSpan.FromBounds(line.Start, comment.FullSpan.Start))))
                    continue;

                string previous = ternaries.Lines[line.LineNumber - 1].ToString().Trim();

                if (previous.Length == 0 || previous == "{")
                    continue;

                if (index > 0)
                {
                    SyntaxTrivia prior = comments[index - 1];
                    string gap = ternaries.ToString(TextSpan.FromBounds(prior.FullSpan.End, comment.FullSpan.Start));
                    TextLine priorLine = ternaries.Lines.GetLineFromPosition(prior.FullSpan.Start);
                    bool standalone = string.IsNullOrWhiteSpace(ternaries.ToString(TextSpan.FromBounds(priorLine.Start, prior.FullSpan.Start)));

                    if (standalone && string.IsNullOrWhiteSpace(gap) && gap.Count(character => character == '\n') <= 1)
                        continue;
                }

                commentBreaks.Add(new(new(line.Start, 0), settings.NewLine));
            }
        }

        SourceText spaced = ternaries.WithChanges(commentBreaks);
        SyntaxNode spacedRoot = CSharpSyntaxTree.ParseText(spaced, parseOptions).GetRoot();
        List<TextChange> semicolons = [];

        foreach (SyntaxToken token in spacedRoot.DescendantTokens().Where(token => token.IsKind(SyntaxKind.SemicolonToken)))
        {
            SyntaxToken previous = token.GetPreviousToken();

            if (previous.IsKind(SyntaxKind.CloseBraceToken) && previous.Span.End < token.SpanStart &&
                string.IsNullOrWhiteSpace(spaced.ToString(TextSpan.FromBounds(previous.Span.End, token.SpanStart))))
                semicolons.Add(new(TextSpan.FromBounds(previous.Span.End, token.SpanStart), ""));
        }

        spaced = spaced.WithChanges(semicolons);
        spacedRoot = CSharpSyntaxTree.ParseText(spaced, parseOptions).GetRoot();
        HashSet<SyntaxTrivia> redundantLines = [];
        int lineBreaks = 0, previousEnd = -1;

        if (!settings.AllowMultipleBlankLines)
            foreach (SyntaxTrivia trivia in spacedRoot.DescendantTrivia())
            {
                if (trivia.FullSpan.Start != previousEnd)
                    lineBreaks = 0;

                if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                {
                    lineBreaks++;

                    if (lineBreaks > 2)
                        redundantLines.Add(trivia);
                }
                else if (!trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                    lineBreaks = 0;

                previousEnd = trivia.FullSpan.End;
            }

        SyntaxTrivia[] layout = [.. spacedRoot.DescendantTrivia().Where(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia) ||
            trivia.IsKind(SyntaxKind.WhitespaceTrivia) || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
            trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
            trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.GetStructure() is DirectiveTriviaSyntax)];
        SyntaxNode finished = spacedRoot.ReplaceTrivia(layout, (before, _) =>
        {
            if (redundantLines.Contains(before))
                return default;

            if (before.IsKind(SyntaxKind.EndOfLineTrivia))
                return SyntaxFactory.EndOfLine(settings.NewLine);

            if (before.GetStructure() is DirectiveTriviaSyntax directive)
                return SyntaxFactory.Trivia(directive.ReplaceTrivia(directive.DescendantTrivia()
                    .Where(trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia)), (_, _) => SyntaxFactory.EndOfLine(settings.NewLine)));

            if (!before.IsKind(SyntaxKind.WhitespaceTrivia))
                return SyntaxFactory.ParseLeadingTrivia(string.Join(settings.NewLine, before.ToFullString().ReplaceLineEndings("\n")
                        .Split('\n').Select(line => line.TrimEnd(' ', '\t'))))
                    .First(trivia => trivia.RawKind == before.RawKind);

            int end = before.FullSpan.End;
            return end == spaced.Length || spaced[end] is '\r' or '\n'
                ? default
                : before;
        });
        return SourceText.From(finished.ToFullString().TrimEnd(' ', '\t', '\r', '\n') + settings.NewLine);

        void set(int position, int indentation)
        {
            TextLine line = text.Lines.GetLineFromPosition(position);
            string prefix = text.ToString(TextSpan.FromBounds(line.Start, position));

            if (string.IsNullOrWhiteSpace(prefix))
                changes[line.Start] = new(new(line.Start, position - line.Start), settings.Indentation(indentation));
            else
                changes[position] = new(new(position, 0), settings.NewLine + settings.Indentation(indentation));
        }
    }
}
