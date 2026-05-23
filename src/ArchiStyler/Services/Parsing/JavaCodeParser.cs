using System.Text.RegularExpressions;
using ArchiStyler.Models;

namespace ArchiStyler.Services.Parsing;

public sealed partial class JavaCodeParser : CodeParserBase, ICodeParser
{
    public TargetLanguage Language => TargetLanguage.Java;

    public ParseResult Parse(string code)
    {
        var result = new ParseResult();
        try
        {
            var src = StripComments(code);
            var pkg = PackageRegex().Match(src);
            if (pkg.Success) result.Package = pkg.Groups[1].Value.Trim();

            foreach (Match m in ImportRegex().Matches(src))
                result.Usings.Add(m.Groups[1].Value.Trim());

            var decl = TypeDeclRegex().Match(src);
            if (!decl.Success)
            {
                result.Error = "Не найдено объявление class/interface/enum.";
                return result;
            }

            result.Access = ParseAccess(decl.Value);
            var kind = decl.Groups["kind"].Value;
            result.IsInterface = kind == "interface";
            result.IsEnum = kind == "enum";
            result.IsAbstract = decl.Value.Contains("abstract", StringComparison.Ordinal);
            result.Name = decl.Groups["name"].Value;

            var ext = decl.Groups["extends"].Value.Trim();
            if (!string.IsNullOrEmpty(ext)) result.BaseType = ext;

            var impl = decl.Groups["implements"].Value.Trim();
            if (!string.IsNullOrEmpty(impl))
                result.Implements.AddRange(impl.Split(',', StringSplitOptions.TrimEntries));

            var brace = src.IndexOf('{', decl.Index);
            if (brace < 0)
            {
                result.Success = true;
                return result;
            }

            var body = ExtractBalancedBody(src, brace);
            if (body is not null) ParseMembers(body, result);
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
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (CtorRegex().IsMatch(line))
            {
                var cm = CtorRegex().Match(line);
                var m = new MemberDefinition { Kind = MemberKind.Constructor, Access = ParseAccess(line), Name = cm.Groups["name"].Value };
                ParseParams(cm.Groups["pars"].Value, m);
                result.Members.Add(m);
                continue;
            }

            if (MethodRegex().IsMatch(line))
            {
                var mm = MethodRegex().Match(line);
                var m = new MemberDefinition
                {
                    Kind = MemberKind.Method,
                    Access = ParseAccess(line),
                    ReturnType = mm.Groups["ret"].Value.Trim(),
                    Name = mm.Groups["name"].Value.Trim(),
                    IsStatic = line.Contains("static", StringComparison.Ordinal),
                    IsAbstract = line.Contains("abstract", StringComparison.Ordinal),
                    GenerateStub = true
                };
                ParseParams(mm.Groups["pars"].Value, m);
                result.Members.Add(m);
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
                    IsReadOnly = line.Contains("final", StringComparison.Ordinal)
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
                member.Parameters.Add(new ParameterDefinition { Type = bits[0], Name = bits[1] });
        }
    }

    public void ApplyToClass(ClassDefinition target, ParseResult result, ProjectModel project)
    {
        if (!result.Success) return;
        target.Name = result.Name;
        target.Package = result.Package ?? project.DefaultPackage;
        target.IsInterface = result.IsInterface;
        target.IsAbstract = result.IsAbstract;
        target.IsEnum = result.IsEnum;
        target.Access = result.Access;
        target.BaseType = result.BaseType;
        target.ImplementedInterfaces = result.Implements.ToList();
        target.Usings.Clear();
        target.Usings.AddRange(result.Usings);
        target.Members.Clear();
        target.Members.AddRange(result.Members);
    }

    [GeneratedRegex(@"package\s+([\w.]+)\s*;", RegexOptions.Multiline)]
    private static partial Regex PackageRegex();

    [GeneratedRegex(@"import\s+([^;]+)\s*;", RegexOptions.Multiline)]
    private static partial Regex ImportRegex();

    [GeneratedRegex(
        @"(?:(?:public|private|protected)\s+)*(?:(?:abstract|static)\s+)*(?<kind>class|interface|enum)\s+(?<name>\w+)(?:\s+extends\s+(?<extends>[\w.]+))?(?:\s+implements\s+(?<implements>[\w.,\s]+))?",
        RegexOptions.Multiline)]
    private static partial Regex TypeDeclRegex();

    [GeneratedRegex(@"^(?:(?:public|private|protected)\s+)*(?<name>\w+)\s*\((?<pars>[^)]*)\)", RegexOptions.Multiline)]
    private static partial Regex CtorRegex();

    [GeneratedRegex(@"^(?:(?:public|private|protected|static|abstract)\s+)*(?<ret>[\w.<>\[\]]+)\s+(?<name>\w+)\s*\((?<pars>[^)]*)\)", RegexOptions.Multiline)]
    private static partial Regex MethodRegex();

    [GeneratedRegex(@"^(?:(?:public|private|protected|static|final)\s+)*(?<type>[\w.<>\[\]]+)\s+(?<name>\w+)\s*$", RegexOptions.Multiline)]
    private static partial Regex FieldRegex();
}
