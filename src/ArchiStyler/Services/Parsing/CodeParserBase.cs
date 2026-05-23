using System.Text.RegularExpressions;
using ArchiStyler.Models;

namespace ArchiStyler.Services.Parsing;

public abstract partial class CodeParserBase
{
    protected static string StripComments(string code) =>
        CommentsRegex().Replace(code, "");

    protected static string? ExtractBalancedBody(string code, int startBrace)
    {
        var depth = 0;
        for (var i = startBrace; i < code.Length; i++)
        {
            if (code[i] == '{') depth++;
            else if (code[i] == '}')
            {
                depth--;
                if (depth == 0) return code[(startBrace + 1)..i];
            }
        }
        return null;
    }

    protected static AccessModifier ParseAccess(string line)
    {
        if (line.Contains("private", StringComparison.Ordinal)) return AccessModifier.Private;
        if (line.Contains("protected", StringComparison.Ordinal)) return AccessModifier.Protected;
        if (line.Contains("internal", StringComparison.Ordinal)) return AccessModifier.Internal;
        if (line.Contains("public", StringComparison.Ordinal)) return AccessModifier.Public;
        return AccessModifier.PackagePrivate;
    }

    [GeneratedRegex(@"//.*?$|/\*[\s\S]*?\*/", RegexOptions.Multiline)]
    private static partial Regex CommentsRegex();
}
