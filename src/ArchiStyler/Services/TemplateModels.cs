namespace ArchiStyler.Services;

public sealed class PatternTemplateFile
{
    public List<PatternTemplate> Patterns { get; set; } = [];
}

public sealed class PatternTemplate
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public List<TemplateFolder> Folders { get; set; } = [];
    public List<TemplateClass> Classes { get; set; } = [];
    public List<TemplateRelation> Relations { get; set; } = [];
}

public sealed class TemplateFolder
{
    public string Name { get; set; } = "";
    public string? Segment { get; set; }
    public string? Parent { get; set; }
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public double Width { get; set; } = 320;
    public double Height { get; set; } = 240;
}

public sealed class TemplateClass
{
    public string Name { get; set; } = "";
    public string Role { get; set; } = "None";
    public bool IsInterface { get; set; }
    public bool IsAbstract { get; set; }
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public string? Folder { get; set; }
    public List<string> Usings { get; set; } = [];
    public List<TemplateMember> Members { get; set; } = [];
    public string? BaseType { get; set; }
    public List<string> Implements { get; set; } = [];
}

public sealed class TemplateMember
{
    public string Kind { get; set; } = "Method";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "void";
    public string ReturnType { get; set; } = "void";
    public string Access { get; set; } = "Public";
    public bool IsAbstract { get; set; }
    public bool IsStatic { get; set; }
    public bool GenerateStub { get; set; } = true;
    public List<TemplateParameter> Parameters { get; set; } = [];
}

public sealed class TemplateParameter
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "object";
}

public sealed class TemplateRelation
{
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string Kind { get; set; } = "Implements";
}
