using ArchiStyler.Models;

namespace ArchiStyler.Services.Parsing;

public interface ICodeParser
{
    TargetLanguage Language { get; }
    ParseResult Parse(string code);
    void ApplyToClass(ClassDefinition target, ParseResult result, ProjectModel project);
}

public sealed class ParseResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Namespace { get; set; }
    public string? Package { get; set; }
    public string Name { get; set; } = "";
    public bool IsInterface { get; set; }
    public bool IsAbstract { get; set; }
    public bool IsEnum { get; set; }
    public bool IsSealed { get; set; }
    public AccessModifier Access { get; set; } = AccessModifier.Public;
    public string? BaseType { get; set; }
    public List<string> Usings { get; } = [];
    public List<string> Implements { get; } = [];
    public List<MemberDefinition> Members { get; } = [];
}
