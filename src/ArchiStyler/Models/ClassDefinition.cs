namespace ArchiStyler.Models;

public sealed class ClassDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "NewClass";
    public string? Namespace { get; set; }
    public string? Package { get; set; }
    public ClassRole Role { get; set; } = ClassRole.None;
    public AccessModifier Access { get; set; } = AccessModifier.Public;
    public bool IsAbstract { get; set; }
    public bool IsSealed { get; set; }
    public bool IsStatic { get; set; }
    public bool IsInterface { get; set; }
    public bool IsEnum { get; set; }
    public bool IsRecord { get; set; }
    public List<string> Usings { get; set; } = [];
    public List<MemberDefinition> Members { get; set; } = [];
    public Guid? FolderId { get; set; }
    public double X { get; set; } = 40;
    public double Y { get; set; } = 40;
    public string? BaseType { get; set; }
    public List<string> ImplementedInterfaces { get; set; } = [];
    public string? Summary { get; set; }

    public string DisplayTitle => string.IsNullOrWhiteSpace(Name) ? "Unnamed" : Name;
}
