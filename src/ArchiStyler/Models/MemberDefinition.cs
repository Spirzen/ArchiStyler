namespace ArchiStyler.Models;

public sealed class MemberDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public MemberKind Kind { get; set; } = MemberKind.Field;
    public string Name { get; set; } = "member";
    public string Type { get; set; } = "string";
    public AccessModifier Access { get; set; } = AccessModifier.Private;
    public bool IsStatic { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsVirtual { get; set; }
    public bool IsAbstract { get; set; }
    public bool GenerateStub { get; set; }
    public string? StubBody { get; set; }
    public List<ParameterDefinition> Parameters { get; set; } = [];
    public string ReturnType { get; set; } = "void";
}
