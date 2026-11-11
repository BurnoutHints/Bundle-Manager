using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO.Enumeration;
using System.Text.RegularExpressions;

namespace CodeStyle;

internal sealed record StyleIssue(string Path, int Line, int Column, string Rule, string Message)
{
    public override string ToString() => $"{Path}({Line},{Column}): error {Rule}: {Message}";
}

internal static class NamingRules
{
    public static Dictionary<string, HashSet<string>> EventBindings(IEnumerable<SyntaxNode> roots)
    {
        Dictionary<string, HashSet<string>> bindings = [];

        foreach (AssignmentExpressionSyntax assignment in roots.SelectMany(root => root.DescendantNodes().OfType<AssignmentExpressionSyntax>()))
        {
            if (!assignment.IsKind(SyntaxKind.AddAssignmentExpression) || Handler(assignment.Right) is not string handler)
                continue;

            string? control = assignment.Left switch
            {
                MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax identifier } => identifier.Identifier.ValueText,
                MemberAccessExpressionSyntax { Expression: MemberAccessExpressionSyntax member } => member.Name.Identifier.ValueText,
                IdentifierNameSyntax => assignment.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText,
                _ => null
            };
            string? eventName = assignment.Left switch
            {
                MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                _ => null
            };

            if (control == null || eventName == null)
                continue;

            string key = Owner(assignment) + "." + handler;

            if (!bindings.TryGetValue(key, out HashSet<string>? names))
                bindings.Add(key, names = []);

            names.Add(control + "_" + eventName);
        }

        return bindings;
    }

    public static IEnumerable<StyleIssue> Check(SyntaxNode root, string path, StyleSettings settings, Dictionary<string, HashSet<string>> handlers)
    {
        if (!settings.CheckNaming)
            yield break;

        foreach (SyntaxNode node in root.DescendantNodes())
        {
            SyntaxToken token = default;
            bool pascal = true;
            string rule = "";

            switch (node)
            {
                case BaseTypeDeclarationSyntax type:
                    token = type.Identifier;
                    break;
                case DelegateDeclarationSyntax declaration:
                    token = declaration.Identifier;
                    break;
                case PropertyDeclarationSyntax property when !settings.PreserveMembers && property.ExplicitInterfaceSpecifier == null && !ExternalContract(property.Modifiers):
                    token = property.Identifier;
                    rule = "properties_are_pascal_case";
                    break;
                case MethodDeclarationSyntax method when method.ExplicitInterfaceSpecifier == null && !ExternalContract(method.Modifiers):
                    token = method.Identifier;

                    bool eventSignature = method.ParameterList.Parameters.Count == 2 &&
                        method.ParameterList.Parameters[1].Type?.ToString().EndsWith("EventArgs", StringComparison.Ordinal) == true;

                    if (settings.PreserveHandlers && eventSignature)
                    {
                        handlers.TryGetValue(Owner(method) + "." + token.ValueText, out HashSet<string>? names);
                        string? expected = names?.FirstOrDefault(name => name.Equals(token.ValueText, StringComparison.OrdinalIgnoreCase) ||
                            name.Replace("_", "").Equals(token.ValueText, StringComparison.OrdinalIgnoreCase));

                        if (expected != null)
                        {
                            if (token.ValueText != expected)
                                yield return Issue(token, path, expected, "STYLE102");

                            continue;
                        }

                        if (token.ValueText.Contains('_'))
                            continue;
                    }

                    break;
                case LocalFunctionStatementSyntax function:
                    token = function.Identifier;
                    pascal = false;
                    rule = "locals_are_camel_case";
                    break;
                case EventDeclarationSyntax declaration when !ExternalContract(declaration.Modifiers):
                    token = declaration.Identifier;
                    break;
                case EnumMemberDeclarationSyntax member when !settings.PreserveMembers:
                    token = member.Identifier;
                    break;
                case ParameterSyntax parameter:
                    token = parameter.Identifier;
                    pascal = parameter.Parent?.Parent is RecordDeclarationSyntax;
                    rule = pascal
                        ? "properties_are_pascal_case"
                        : "parameters_are_camel_case";
                    break;
                case TypeParameterSyntax parameter:
                    token = parameter.Identifier;
                    break;
                case VariableDeclaratorSyntax variable when variable.Parent?.Parent is FieldDeclarationSyntax field && !settings.PreserveMembers:
                    token = variable.Identifier;
                    pascal = field.Modifiers.Any(SyntaxKind.ConstKeyword) || field.Modifiers.Any(SyntaxKind.PublicKeyword) || field.Modifiers.Any(SyntaxKind.ProtectedKeyword);
                    rule = pascal
                        ? "visible_fields_are_pascal_case"
                        : "private_fields_are_camel_case";
                    break;
                case VariableDeclaratorSyntax variable when variable.Parent?.Parent is EventFieldDeclarationSyntax:
                    token = variable.Identifier;
                    break;
                case VariableDeclaratorSyntax variable when variable.Parent?.Parent is not BaseFieldDeclarationSyntax:
                    token = variable.Identifier;
                    pascal = false;
                    rule = "locals_are_camel_case";
                    break;
                case SingleVariableDesignationSyntax variable:
                    token = variable.Identifier;
                    pascal = false;
                    rule = "locals_are_camel_case";
                    break;
                case ForEachStatementSyntax statement:
                    token = statement.Identifier;
                    pascal = false;
                    rule = "locals_are_camel_case";
                    break;
                case CatchDeclarationSyntax declaration:
                    token = declaration.Identifier;
                    pascal = false;
                    rule = "locals_are_camel_case";
                    break;
            }

            if (token.RawKind == 0 || token.ValueText is "" or "_" || rule.Length > 0 && !settings.NamingRuleEnabled(rule) ||
                settings.IdentifierExceptions.Any(pattern => FileSystemName.MatchesSimpleExpression(pattern, token.ValueText)))
                continue;

            string desired = Expected(token.ValueText, pascal, settings);

            if (desired != token.ValueText)
                yield return Issue(token, path, desired, "STYLE101");
        }
    }

    internal static string Expected(string name, bool pascal, StyleSettings settings)
    {
        string preserved = string.Join("|", settings.PreservedWords.OrderByDescending(word => word.Length)
            .Select(word => "(?i:" + Regex.Escape(word) + ")(?:s(?=[A-Z]|$))?"));
        string pattern = (preserved.Length > 0
            ? preserved + "|"
            : "") + "[A-Z]{2,}s(?=[A-Z]|_|$)|[A-Z]+(?=[A-Z][a-z]|[0-9]|_|$)|[A-Z]?[a-z]+|[0-9]+";
        string[] words = [.. Regex.Matches(name, pattern).Select(match => match.Value)];

        if (words.Length == 0)
            return name;

        for (int index = 0; index < words.Length; index++)
        {
            string word = words[index];

            if (word == "x" && index > 0 && index + 1 < words.Length && words[index - 1].All(char.IsDigit) && words[index + 1].All(char.IsDigit))
                continue;

            string? special = settings.PreservedWords.FirstOrDefault(value => value.Equals(word, StringComparison.OrdinalIgnoreCase) ||
                (value + "s").Equals(word, StringComparison.OrdinalIgnoreCase));
            string? acronym = settings.Acronyms.FirstOrDefault(value => value.Equals(word, StringComparison.OrdinalIgnoreCase) ||
                (value + "s").Equals(word, StringComparison.OrdinalIgnoreCase));

            if (special != null)
                word = special + (word.Length > special.Length
                    ? "s"
                    : "");
            else if (acronym != null)
                word = acronym.ToUpperInvariant() + (word.Length > acronym.Length
                    ? "s"
                    : "");
            else
                word = char.ToUpperInvariant(word[0]) + word[1..];

            if (index == 0 && !pascal)
                word = acronym != null || word.All(character => !char.IsLetter(character) || char.IsUpper(character))
                    ? word.ToLowerInvariant()
                    : char.ToLowerInvariant(word[0]) + word[1..];

            words[index] = word;
        }

        return string.Concat(words);
    }

    private static bool ExternalContract(SyntaxTokenList modifiers) => modifiers.Any(SyntaxKind.OverrideKeyword) || modifiers.Any(SyntaxKind.ExternKeyword);

    private static string? Handler(ExpressionSyntax expression) => expression switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
        ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 1 } creation => Handler(creation.ArgumentList.Arguments[0].Expression),
        ParenthesizedExpressionSyntax parentheses => Handler(parentheses.Expression),
        _ => null
    };

    private static StyleIssue Issue(SyntaxToken token, string path, string desired, string rule)
    {
        FileLinePositionSpan location = token.GetLocation().GetLineSpan();
        return new(path, location.StartLinePosition.Line + 1, location.StartLinePosition.Character + 1, rule,
            $"Name '{token.ValueText}' should be '{desired}'. Use a symbol rename to update its references.");
    }

    private static string Owner(SyntaxNode node) => string.Join(".", node.Ancestors().Reverse().Select(ancestor => ancestor switch
    {
        BaseNamespaceDeclarationSyntax declaration => declaration.Name.ToString(),
        TypeDeclarationSyntax type => type.Identifier.ValueText,
        _ => ""
    }).Where(name => name.Length > 0));
}
