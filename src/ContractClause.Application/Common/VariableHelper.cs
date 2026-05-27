using System.Text.RegularExpressions;

namespace ContractClause.Application.Common;

public static partial class VariableHelper
{
    [GeneratedRegex(@"\{\{([^}]+)\}\}", RegexOptions.Compiled)]
    private static partial Regex VariablePattern();

    public static List<string> Extract(string text)
    {
        return VariablePattern()
            .Matches(text)
            .Select(m => "{{" + m.Groups[1].Value.Trim() + "}}")
            .Distinct()
            .ToList();
    }

    public static string Render(string content, IReadOnlyDictionary<string, string> variables)
    {
        var result = content;
        foreach (var (key, value) in variables)
        {
            result = result.Replace(key, value, StringComparison.Ordinal);
        }
        return result;
    }
}
