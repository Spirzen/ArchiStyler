using System.Text;
using ArchiStyler.Helpers;
using ArchiStyler.Models;

namespace ArchiStyler.Services;

public sealed class CSharpCodeGenerator : ICodeGenerator
{
    public TargetLanguage Language => TargetLanguage.CSharp;

    public string GetFileExtension() => ".cs";

    public string GetRelativeFilePath(ClassDefinition cls, ProjectModel project) =>
        ProjectPathHelper.GetRelativeFilePath(cls, project, GetFileExtension());

    public string GenerateClass(ClassDefinition cls, ProjectModel project)
    {
        var sb = new StringBuilder();
        foreach (var u in cls.Usings.Distinct().OrderBy(x => x))
            sb.AppendLine($"using {u};");

        if (cls.Usings.Count > 0)
            sb.AppendLine();

        var ns = ProjectPathHelper.GetEffectiveNamespace(cls, project);
        sb.AppendLine($"namespace {ns};");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(cls.Summary))
            sb.AppendLine($"/// <summary>{cls.Summary}</summary>");

        sb.Append(BuildClassDeclaration(cls));
        sb.AppendLine();
        sb.AppendLine("{");

        if (cls.IsEnum)
        {
            foreach (var m in cls.Members)
                sb.AppendLine($"    {m.Name},");
        }
        else
        {
            foreach (var member in cls.Members)
                sb.Append(GenerateMember(cls, member));
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string BuildClassDeclaration(ClassDefinition cls)
    {
        var parts = new List<string>();
        parts.Add(LanguageHelper.FormatAccess(cls.Access, TargetLanguage.CSharp));

        if (cls.IsStatic) parts.Add("static ");
        if (cls.IsSealed) parts.Add("sealed ");
        if (cls.IsAbstract && !cls.IsInterface) parts.Add("abstract ");

        if (cls.IsInterface) parts.Add("interface ");
        else if (cls.IsEnum) parts.Add("enum ");
        else if (cls.IsRecord) parts.Add("record ");
        else parts.Add("class ");

        parts.Add(cls.Name);

        var inheritance = new List<string>();
        if (!string.IsNullOrWhiteSpace(cls.BaseType))
            inheritance.Add(cls.BaseType);
        inheritance.AddRange(cls.ImplementedInterfaces);
        if (inheritance.Count > 0)
            parts.Add($": {string.Join(", ", inheritance)}");

        return string.Concat(parts);
    }

    private static string GenerateMember(ClassDefinition cls, MemberDefinition m)
    {
        var sb = new StringBuilder();
        var access = LanguageHelper.FormatAccess(m.Access, TargetLanguage.CSharp);
        var type = LanguageHelper.MapType(m.Type, TargetLanguage.CSharp);

        switch (m.Kind)
        {
            case MemberKind.Field:
                sb.Append($"    {access}");
                if (m.IsStatic) sb.Append("static ");
                if (m.IsReadOnly) sb.Append("readonly ");
                sb.AppendLine($"{type} {m.Name};");
                break;

            case MemberKind.Property:
                sb.Append($"    {access}");
                if (m.IsStatic) sb.Append("static ");
                if (m.IsVirtual) sb.Append("virtual ");
                if (m.IsAbstract) sb.Append("abstract ");
                sb.AppendLine($"{type} {m.Name} {{ get; set; }}");
                break;

            case MemberKind.Constructor:
                {
                    var pars = string.Join(", ", m.Parameters.Select(p =>
                        $"{LanguageHelper.MapType(p.Type, TargetLanguage.CSharp)} {p.Name}"));
                    sb.AppendLine($"    {access}{cls.Name}({pars})");
                    sb.AppendLine("    {");
                    sb.AppendLine(GenerateStubBody(m));
                    sb.AppendLine("    }");
                }
                break;

            case MemberKind.Method:
                sb.Append($"    {access}");
                if (m.IsStatic) sb.Append("static ");
                if (m.IsVirtual) sb.Append("virtual ");
                if (m.IsAbstract) sb.Append("abstract ");

                var ret = LanguageHelper.MapType(m.ReturnType, TargetLanguage.CSharp);
                var methodPars = string.Join(", ", m.Parameters.Select(p =>
                    $"{LanguageHelper.MapType(p.Type, TargetLanguage.CSharp)} {p.Name}"));
                sb.AppendLine($"{ret} {m.Name}({methodPars})");

                if (m.IsAbstract)
                    sb.AppendLine(";");
                else
                {
                    sb.AppendLine("    {");
                    sb.AppendLine(GenerateStubBody(m));
                    sb.AppendLine("    }");
                }
                break;
        }

        sb.AppendLine();
        return sb.ToString();
    }

    private static string GenerateStubBody(MemberDefinition m)
    {
        if (!string.IsNullOrWhiteSpace(m.StubBody))
            return $"        {m.StubBody}";

        if (!m.GenerateStub)
            return "        throw new NotImplementedException();";

        return m.ReturnType switch
        {
            "void" => "        // TODO: implement",
            "bool" => "        return false;",
            "int" => "        return 0;",
            "string" => "        return string.Empty;",
            _ => "        throw new NotImplementedException();"
        };
    }
}
