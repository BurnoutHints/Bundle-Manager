using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CodeStyle;

internal static class IndentationPass
{
    public static SourceText Apply(SyntaxNode root, SourceText text, StyleSettings settings)
    {
        Dictionary<int, int> indentation = [];

        foreach (SyntaxNode node in root.DescendantNodes().OrderBy(node => node.SpanStart))
        {
            int depth;

            switch (node)
            {
                case TypeDeclarationSyntax type when type.Members.Count > 0:
                    depth = getDepth(type.SpanStart);
                    set(type.OpenBraceToken.SpanStart, depth);
                    set(type.CloseBraceToken.SpanStart, depth);

                    foreach (MemberDeclarationSyntax member in type.Members)
                        set(member.SpanStart, depth + settings.IndentSize);

                    break;
                case BlockSyntax block when block.Statements.Count > 0:
                    SyntaxNode owner = block.Parent!;
                    TextLine line = text.Lines.GetLineFromPosition(owner.SpanStart);

                    if (owner is LambdaExpressionSyntax && line.ToString().TrimStart().StartsWith(':'))
                        owner = owner.Ancestors().FirstOrDefault(ancestor => ancestor is StatementSyntax and not BlockSyntax) ?? owner;

                    depth = getDepth(owner.SpanStart);
                    set(block.OpenBraceToken.SpanStart, depth);
                    set(block.CloseBraceToken.SpanStart, depth);

                    foreach (StatementSyntax statement in block.Statements)
                        set(statement.SpanStart, depth + settings.IndentSize);

                    break;
                case InitializerExpressionSyntax initializer when initializer.Expressions.Count > 0:
                    depth = getDepth(initializer.Parent!.SpanStart);
                    set(initializer.OpenBraceToken.SpanStart, depth);
                    set(initializer.CloseBraceToken.SpanStart, depth);

                    foreach (ExpressionSyntax expression in initializer.Expressions)
                        set(expression.SpanStart, depth + settings.IndentSize);

                    break;
                case SwitchStatementSyntax statement:
                    depth = getDepth(statement.SpanStart);
                    set(statement.OpenBraceToken.SpanStart, depth);
                    set(statement.CloseBraceToken.SpanStart, depth);

                    foreach (SwitchSectionSyntax section in statement.Sections)
                    {
                        foreach (SwitchLabelSyntax label in section.Labels)
                            set(label.SpanStart, depth + settings.IndentSize);

                        foreach (StatementSyntax child in section.Statements)
                            set(child.SpanStart, depth + settings.IndentSize * 2);
                    }

                    break;
            }
        }

        return text.WithChanges(indentation.Select(pair =>
        {
            TextLine line = text.Lines[pair.Key];
            int prefix = line.ToString().TakeWhile(character => character is ' ' or '\t').Count();
            return new TextChange(new(line.Start, prefix), settings.Indentation(pair.Value));
        }));

        int getDepth(int position)
        {
            TextLine line = text.Lines.GetLineFromPosition(position);
            return indentation.GetValueOrDefault(line.LineNumber, settings.LeadingIndent(line.ToString()));
        }

        void set(int position, int depth)
        {
            TextLine line = text.Lines.GetLineFromPosition(position);

            if (string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(line.Start, position))))
                indentation[line.LineNumber] = depth;
        }
    }
}
