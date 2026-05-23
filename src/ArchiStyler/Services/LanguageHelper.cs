using ArchiStyler.Models;

namespace ArchiStyler.Services;

internal static class LanguageHelper
{
    public static string MapType(string type, TargetLanguage language) =>
        language switch
        {
            TargetLanguage.Java => type switch
            {
                "string" => "String",
                "int" => "int",
                "bool" => "boolean",
                "object" => "Object",
                "void" => "void",
                _ => type
            },
            _ => type
        };

    public static string FormatAccess(AccessModifier access, TargetLanguage language, bool forInterface = false)
    {
        if (forInterface && language == TargetLanguage.CSharp)
            return access == AccessModifier.Public ? "public " : "";

        return (access, language) switch
        {
            (AccessModifier.Public, TargetLanguage.Java) => "public ",
            (AccessModifier.Private, TargetLanguage.Java) => "private ",
            (AccessModifier.Protected, TargetLanguage.Java) => "protected ",
            (AccessModifier.PackagePrivate, TargetLanguage.Java) => "",
            (AccessModifier.Internal, TargetLanguage.CSharp) => "internal ",
            (AccessModifier.Public, TargetLanguage.CSharp) => "public ",
            (AccessModifier.Private, TargetLanguage.CSharp) => "private ",
            (AccessModifier.Protected, TargetLanguage.CSharp) => "protected ",
            _ => "public "
        };
    }

    public static void ApplyRoleDefaults(ClassDefinition cls, ClassRole role)
    {
        cls.Role = role;
        switch (role)
        {
            case ClassRole.Interface:
                cls.IsInterface = true;
                cls.IsAbstract = false;
                cls.IsEnum = false;
                break;
            case ClassRole.AbstractClass:
                cls.IsAbstract = true;
                cls.IsInterface = false;
                break;
            case ClassRole.Enum:
                cls.IsEnum = true;
                cls.IsInterface = false;
                break;
            case ClassRole.Dto:
                cls.IsRecord = true;
                break;
        }
    }
}
