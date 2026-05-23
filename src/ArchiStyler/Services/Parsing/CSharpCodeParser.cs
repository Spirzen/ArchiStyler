using System.Text.RegularExpressions;
using ArchiStyler.Models;

namespace ArchiStyler.Services.Parsing;

public sealed partial class CSharpCodeParser : CodeParserBase, ICodeParser
{
    public TargetLanguage Language => TargetLanguage.CSharp;

    public ParseResult Parse(string code)
    {
        var result = new ParseResult();
        try
        {
            var src = StripComments(code);
            foreach (Match m in UsingRegex().Matches(src))
                result.Usings.Add(m.Groups[1].Value.Trim());

            var ns = NamespaceRegex().Match(src);
            if (ns.Success) result.Namespace = ns.Groups[1].Value.Trim();

            var decl = TypeDeclRegex().Match(src);
            if (!decl.Success)
            {
                result.Error = "Не найдено объявление class/interface/enum/record.";
                return result;
            }

            result.Access = ParseAccess(decl.Value);
            var kind = decl.Groups["kind"].Value;
            result.IsInterface = kind == "interface";
            result.IsEnum = kind == "enum";
            result.IsAbstract = decl.Value.Contains("abstract", StringComparison.Ordinal);
            result.IsSealed = decl.Value.Contains("sealed", StringComparison.Ordinal);
            result.Name = decl.Groups["name"].Value;

            var bases = decl.Groups["bases"].Value.Trim();
            if (!string.IsNullOrEmpty(bases))
            {
                var parts = bases.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    if (result.IsInterface)
                        result.Implements.AddRange(parts);
                    else
                    {
                        result.BaseType = parts[0];
                        for (var i = 1; i < parts.Length; i++)
                            result.Implements.Add(parts[i]);
                    }
                }
            }

            var brace = src.IndexOf('{', decl.Index);
            if (brace < 0)
            {
                result.Success = true;
                return result;
            }

            var body = ExtractBalancedBody(src, brace);
            if (body is null)
            {
                result.Error = "Не удалось разобрать тело типа.";
                return result;
            }

            ParseMembers(body, result);
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }

        return result;
    }

    private static void ParseMembers(string body, ParseResult result)
    {
        foreach (var raw in body.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var line = Regex.Replace(raw, @"\s+", " ").Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('[')) continue;

            if (PropertyRegex().IsMatch(line))
            {
                var pm = PropertyRegex().Match(line);
                result.Members.Add(new MemberDefinition
                {
                    Kind = MemberKind.Property,
                    Access = ParseAccess(line),
                    Type = pm.Groups["type"].Value.Trim(),
                    Name = pm.Groups["name"].Value.Trim(),
                    IsStatic = line.Contains("static", StringComparison.Ordinal)
                });
                continue;
            }

            if (CtorRegex().IsMatch(line))
            {
                var cm = CtorRegex().Match(line);
                var member = new MemberDefinition
                {
                    Kind = MemberKind.Constructor,
                    Access = ParseAccess(line),
                    Name = cm.Groups["name"].Value
                };
                ParseParams(cm.Groups["pars"].Value, member);
                result.Members.Add(member);
                continue;
            }

            if (MethodRegex().IsMatch(line))
            {
                var mm = MethodRegex().Match(line);
                var member = new MemberDefinition
                {
                    Kind = MemberKind.Method,
                    Access = ParseAccess(line),
                    ReturnType = mm.Groups["ret"].Value.Trim(),
                    Name = mm.Groups["name"].Value.Trim(),
                    IsStatic = line.Contains("static", StringComparison.Ordinal),
                    IsAbstract = line.Contains("abstract", StringComparison.Ordinal),
                    GenerateStub = line.Contains('{')
                };
                ParseParams(mm.Groups["pars"].Value, member);
                result.Members.Add(member);
                continue;
            }

            if (FieldRegex().IsMatch(line))
            {
                var fm = FieldRegex().Match(line);
                result.Members.Add(new MemberDefinition
                {
                    Kind = MemberKind.Field,
                    Access = ParseAccess(line),
                    Type = fm.Groups["type"].Value.Trim(),
                    Name = fm.Groups["name"].Value.Trim(),
                    IsStatic = line.Contains("static", StringComparison.Ordinal),
                    IsReadOnly = line.Contains("readonly", StringComparison.Ordinal)
                });
            }
        }
    }

    private static void ParseParams(string pars, MemberDefinition member)
    {
        if (string.IsNullOrWhiteSpace(pars)) return;
        foreach (var p in pars.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var bits = p.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (bits.Length >= 2)
                member.Parameters.Add(new ParameterDefinition
                {
                    Type = bits[^2],
                    Name = bits[^1].TrimEnd(')')
                });
        }
    }

    public void ApplyToClass(ClassDefinition target, ParseResult result, ProjectModel project)
    {
        if (!result.Success) return;
        target.Name = result.Name;
        target.Namespace = result.Namespace ?? project.DefaultNamespace;
        target.IsInterface = result.IsInterface;
        target.IsAbstract = result.IsAbstract;
        target.IsEnum = result.IsEnum;
        target.IsSealed = result.IsSealed;
        target.Access = result.Access;
        target.BaseType = result.BaseType;
        target.ImplementedInterfaces = result.Implements.ToList();
        target.Usings.Clear();
        target.Usings.AddRange(result.Usings);
        target.Members.Clear();
        target.Members.AddRange(result.Members);
    }

    [GeneratedRegex(@"^\s*using\s+([^;]+)\s*;", RegexOptions.Multiline)]
    private static partial Regex UsingRegex();

    [GeneratedRegex(@"namespace\s+([\w.]+)\s*;", RegexOptions.Multiline)]
    private static partial Regex NamespaceRegex();

    [GeneratedRegex(
        @"(?:(?:public|private|protected|internal)\s+)*(?:(?:static|abstract|sealed|partial)\s+)*(?<kind>class|interface|enum|record)\s+(?<name>\w+)(?:\s*:\s*(?<bases>[^{]+))?",
        RegexOptions.Multiline)]
    private static partial Regex TypeDeclRegex();

    [GeneratedRegex(@"^(?:(?:public|private|protected|internal|static|virtual|override|abstract)\s+)*(?<type>[\w.<>,\[\]?]+)\s+(?<name>\w+)\s*\{\s*get", RegexOptions.Multiline)]
    private static partial Regex PropertyRegex();

    [GeneratedRegex(@"^(?:(?:public|private|protected|internal)\s+)*(?<name>\w+)\s*\((?<pars>[^)]*)\)", RegexOptions.Multiline)]
    private static partial Regex CtorRegex();

    [GeneratedRegex(@"^(?:(?:public|private|protected|internal|static|virtual|override|abstract)\s+)*(?<ret>[\w.<>,\[\]?]+)\s+(?<name>\w+)\s*\((?<pars>[^)]*)\)", RegexOptions.Multiline)]
    private static partial Regex MethodRegex();

    [GeneratedRegex(@"^(?:(?:public|private|protected|internal|static|readonly)\s+)*(?<type>[\w.<>,\[\]?]+)\s+(?<name>\w+)\s*$", RegexOptions.Multiline)]
    private static partial Regex FieldRegex();
}
