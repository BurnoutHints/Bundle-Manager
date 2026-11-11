using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text.RegularExpressions;

namespace CodeStyle;

internal static class LayoutPass
{
    public static SourceText Apply(SyntaxNode original, StyleSettings settings)
    {
        SyntaxNode root = settings.InlineProperties
            ? InlineProperties(original)
            : original;
        SourceText source = SourceText.From(root.ToFullString());
        string text = source.ToString();
        SortedDictionary<int, int> breaks = [];
        List<TextChange> changes = [];

        foreach (SyntaxNode node in root.DescendantNodes())
        {
            switch (node)
            {
                case BlockSyntax block when settings.ExpandBlocks && block.Statements.Count > 0:
                    breakLine(block.OpenBraceToken.SpanStart);

                    foreach (StatementSyntax statement in block.Statements)
                        if (!ClosureSemicolon(statement))
                            breakLine(Anchor(statement));

                    breakLine(block.CloseBraceToken.SpanStart);
                    break;
                case InitializerExpressionSyntax initializer when settings.ExpandInitializers && initializer.Expressions.Count > 0:
                    breakLine(initializer.OpenBraceToken.SpanStart);

                    foreach (ExpressionSyntax expression in initializer.Expressions)
                        breakLine(expression.SpanStart);

                    breakLine(initializer.CloseBraceToken.SpanStart);
                    break;
                case ConditionalExpressionSyntax conditional when settings.TernaryLines:
                    breakLine(conditional.QuestionToken.SpanStart);
                    breakLine(conditional.ColonToken.SpanStart);
                    break;
                case TypeDeclarationSyntax type when settings.ExpandBlocks && type.Members.Count > 0:
                    breakLine(type.OpenBraceToken.SpanStart);

                    foreach (MemberDeclarationSyntax member in type.Members)
                        breakLine(Anchor(member));

                    breakLine(type.CloseBraceToken.SpanStart);
                    break;
                case EnumDeclarationSyntax enumeration when settings.ExpandBlocks && enumeration.Members.Count > 0:
                    breakLine(enumeration.OpenBraceToken.SpanStart);

                    foreach (EnumMemberDeclarationSyntax member in enumeration.Members)
                        breakLine(Anchor(member));

                    breakLine(enumeration.CloseBraceToken.SpanStart);
                    break;
            }

            if (settings.ExpandBlocks)
                foreach (StatementSyntax child in EmbeddedStatements(node))
                    if (child is not BlockSyntax)
                        breakLine(Anchor(child));
        }

        if (settings.StepSpacing)
        {
            foreach (BlockSyntax block in root.DescendantNodes().OfType<BlockSyntax>())
                for (int index = 1; index < block.Statements.Count; index++)
                {
                    StatementSyntax current = block.Statements[index], previous = block.Statements[index - 1];

                    if (!ClosureSemicolon(current) && (Step(current) || Step(previous) || previous is LocalDeclarationStatementSyntax local &&
                        local.DescendantNodes().OfType<InitializerExpressionSyntax>().Any(initializer => initializer.Expressions.Count > 0)))
                        breakLine(Anchor(current), 2);
                }

            foreach (TypeDeclarationSyntax type in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
                for (int index = 1; index < type.Members.Count; index++)
                {
                    MemberDeclarationSyntax current = type.Members[index], previous = type.Members[index - 1];
                    bool simpleFields = current is FieldDeclarationSyntax && previous is FieldDeclarationSyntax && SingleLine(current) && SingleLine(previous);

                    if (!simpleFields)
                        breakLine(Anchor(current), 2);
                }
        }

        foreach (SyntaxToken token in root.DescendantTokens().Where(token => token.IsKind(SyntaxKind.SemicolonToken)))
        {
            SyntaxToken previous = token.GetPreviousToken();

            if (previous.IsKind(SyntaxKind.CloseBraceToken) && token.SpanStart > previous.Span.End &&
                string.IsNullOrWhiteSpace(text[previous.Span.End..token.SpanStart]))
                changes.Add(new(TextSpan.FromBounds(previous.Span.End, token.SpanStart), ""));
        }

        foreach ((int position, int count) in breaks)
        {
            int lineStart = source.Lines.GetLineFromPosition(position).Start;
            string indentation = text[lineStart..position];

            if (!string.IsNullOrWhiteSpace(indentation))
                indentation = "";

            changes.Add(new(new(position, 0), string.Concat(Enumerable.Repeat(settings.NewLine, count)) + indentation));
        }

        return source.WithChanges(changes);

        void breakLine(int position, int desired = 1)
        {
            int before = position;

            while (before > 0 && char.IsWhiteSpace(text[before - 1]))
                before--;

            int existing = Regex.Matches(text[before..position], "\\r\\n|\\n|\\r").Count;
            int needed = Math.Max(0, desired - existing);

            if (needed > 0)
                breaks[position] = Math.Max(breaks.GetValueOrDefault(position), needed);
        }
    }

    private static SyntaxNode InlineProperties(SyntaxNode root)
    {
        AccessorListSyntax[] lists = [.. root.DescendantNodes().OfType<PropertyDeclarationSyntax>().Select(property => property.AccessorList)
            .OfType<AccessorListSyntax>().Where(CanInline)];
        root = root.ReplaceNodes(lists, (_, list) => list
            .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space))
            .WithAccessors(SyntaxFactory.List(list.Accessors.Select(accessor => accessor.NormalizeWhitespace().WithLeadingTrivia().WithTrailingTrivia(SyntaxFactory.Space))))
            .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken).WithTrailingTrivia(list.CloseBraceToken.TrailingTrivia)));
        SyntaxToken[] names = [.. root.DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .Where(property => property.AccessorList is { } list && CanInline(list))
            .Select(property => property.Identifier)
            .Where(token => token.TrailingTrivia.All(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia) || trivia.IsKind(SyntaxKind.EndOfLineTrivia)))];
        return root.ReplaceTokens(names, (_, token) => token.WithTrailingTrivia(SyntaxFactory.Space));
    }

    private static bool CanInline(AccessorListSyntax list) =>
        list.Accessors.All(accessor => accessor.Body == null && accessor.ExpressionBody == null && accessor.AttributeLists.Count == 0 && accessor.DescendantTrivia().All(Whitespace)) &&
        list.OpenBraceToken.LeadingTrivia.All(Whitespace) && list.OpenBraceToken.TrailingTrivia.All(Whitespace) &&
        list.CloseBraceToken.LeadingTrivia.All(Whitespace) && list.OpenBraceToken.GetPreviousToken().TrailingTrivia.All(Whitespace);

    private static bool Whitespace(SyntaxTrivia trivia) => trivia.IsKind(SyntaxKind.WhitespaceTrivia) || trivia.IsKind(SyntaxKind.EndOfLineTrivia);

    private static IEnumerable<StatementSyntax> EmbeddedStatements(SyntaxNode node) => node switch
    {
        IfStatementSyntax statement => [statement.Statement],
        ElseClauseSyntax { Statement: not IfStatementSyntax } clause => [clause.Statement],
        ForStatementSyntax statement => [statement.Statement],
        CommonForEachStatementSyntax statement => [statement.Statement],
        WhileStatementSyntax statement => [statement.Statement],
        DoStatementSyntax statement => [statement.Statement],
        UsingStatementSyntax statement => [statement.Statement],
        LockStatementSyntax statement => [statement.Statement],
        FixedStatementSyntax statement => [statement.Statement],
        SwitchSectionSyntax section => section.Statements.AsEnumerable(),
        _ => []
    };

    private static bool Step(StatementSyntax statement) => statement is LocalFunctionStatementSyntax or IfStatementSyntax or ForStatementSyntax or
        CommonForEachStatementSyntax or WhileStatementSyntax or TryStatementSyntax or SwitchStatementSyntax;

    private static bool ClosureSemicolon(StatementSyntax statement) => statement is EmptyStatementSyntax empty &&
        empty.SemicolonToken.GetPreviousToken().IsKind(SyntaxKind.CloseBraceToken);

    private static bool SingleLine(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line == node.GetLocation().GetLineSpan().EndLinePosition.Line;

    private static int Anchor(SyntaxNode node)
    {
        SyntaxTrivia comment = node.GetLeadingTrivia().FirstOrDefault(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
            trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) || trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia));
        return comment.RawKind == 0
            ? node.SpanStart
            : comment.FullSpan.Start;
    }
}
