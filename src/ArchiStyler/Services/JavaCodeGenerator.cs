using System.Text;
using ArchiStyler.Helpers;
using ArchiStyler.Models;

namespace ArchiStyler.Services;

public sealed class JavaCodeGenerator : ICodeGenerator
{
    public TargetLanguage Language => TargetLanguage.Java;

    public string GetFileExtension() => ".java";

    public string GetRelativeFilePath(ClassDefinition cls, ProjectModel project) =>
        ProjectPathHelper.GetRelativeFilePath(cls, project, GetFileExtension());

    public string GenerateClass(ClassDefinition cls, ProjectModel project)
    {
        var sb = new StringBuilder();
        var pkg = ProjectPathHelper.GetEffectivePackage(cls, project);
        sb.AppendLine($"package {pkg};");
        sb.AppendLine();

        foreach (var u in cls.Usings.Distinct().OrderBy(x => x))
            sb.AppendLine($"import {u};");

        if (cls.Usings.Count > 0)
            sb.AppendLine();

        sb.Append(BuildClassDeclaration(cls));
        sb.AppendLine(" {");

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
        parts.Add(LanguageHelper.FormatAccess(cls.Access, TargetLanguage.Java));

        if (cls.IsAbstract && !cls.IsInterface) parts.Add("abstract ");
        if (cls.IsInterface) parts.Add("interface ");
        else if (cls.IsEnum) parts.Add("enum ");
        else parts.Add("class ");

        parts.Add(cls.Name);

        var inheritance = new List<string>();
        if (!string.IsNullOrWhiteSpace(cls.BaseType))
            inheritance.Add(cls.BaseType);
        inheritance.AddRange(cls.ImplementedInterfaces);
        if (inheritance.Count > 0)
        {
            var keyword = cls.IsInterface ? "extends" : "implements";
            if (!cls.IsInterface && !string.IsNullOrWhiteSpace(cls.BaseType) && cls.ImplementedInterfaces.Count > 0)
            {
                parts.Add($"extends {cls.BaseType} implements {string.Join(", ", cls.ImplementedInterfaces)}");
            }
            else if (!cls.IsInterface && !string.IsNullOrWhiteSpace(cls.BaseType))
                parts.Add($"extends {cls.BaseType}");
            else
                parts.Add($"{keyword} {string.Join(", ", inheritance)}");
        }

        return string.Concat(parts);
    }

    private static string GenerateMember(ClassDefinition cls, MemberDefinition m)
    {
        var sb = new StringBuilder();
        var access = LanguageHelper.FormatAccess(m.Access, TargetLanguage.Java);
        var type = LanguageHelper.MapType(m.Type, TargetLanguage.Java);

        switch (m.Kind)
        {
            case MemberKind.Field:
                sb.Append($"    {access}");
                if (m.IsStatic) sb.Append("static ");
                if (m.IsReadOnly) sb.Append("final ");
                sb.AppendLine($"{type} {m.Name};");
                break;

            case MemberKind.Property:
                sb.Append($"    {access}");
                if (m.IsStatic) sb.Append("static ");
                sb.AppendLine($"{type} {m.Name};");
                sb.AppendLine();
                sb.Append($"    {access}");
                sb.AppendLine($"{type} get{m.Name}() {{ return {m.Name}; }}");
                sb.Append($"    {access}void set{m.Name}({type} value) {{ this.{m.Name} = value; }}");
                break;

            case MemberKind.Constructor:
                {
                    var pars = string.Join(", ", m.Parameters.Select(p =>
                        $"{LanguageHelper.MapType(p.Type, TargetLanguage.Java)} {p.Name}"));
                    sb.AppendLine($"    {access}{cls.Name}({pars}) {{");
                    sb.AppendLine(GenerateStubBody(m, TargetLanguage.Java));
                    sb.AppendLine("    }");
                }
                break;

            case MemberKind.Method:
                sb.Append($"    {access}");
                if (m.IsStatic) sb.Append("static ");
                if (m.IsAbstract) sb.Append("abstract ");

                var ret = LanguageHelper.MapType(m.ReturnType, TargetLanguage.Java);
                var methodPars = string.Join(", ", m.Parameters.Select(p =>
                    $"{LanguageHelper.MapType(p.Type, TargetLanguage.Java)} {p.Name}"));
                sb.AppendLine($"{ret} {m.Name}({methodPars})");

                if (m.IsAbstract)
                    sb.AppendLine(";");
                else
                {
                    sb.AppendLine("    {");
                    sb.AppendLine(GenerateStubBody(m, TargetLanguage.Java));
                    sb.AppendLine("    }");
                }
                break;
        }

        sb.AppendLine();
        return sb.ToString();
    }

    private static string GenerateStubBody(MemberDefinition m, TargetLanguage lang)
    {
        if (!string.IsNullOrWhiteSpace(m.StubBody))
            return $"        {m.StubBody}";

        if (!m.GenerateStub)
            return "        throw new UnsupportedOperationException();";

        return m.ReturnType switch
        {
            "void" => "        // TODO: implement",
            "bool" or "boolean" => "        return false;",
            "int" => "        return 0;",
            "string" or "String" => lang == TargetLanguage.Java
                ? "        return \"\";"
                : "        return string.Empty;",
            _ => "        throw new UnsupportedOperationException();"
        };
    }
}
