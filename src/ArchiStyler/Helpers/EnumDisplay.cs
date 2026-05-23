using ArchiStyler.Models;

namespace ArchiStyler.Helpers;

public static class EnumDisplay
{
    public static string FormatRole(ClassRole role) =>
        role switch
        {
            ClassRole.Dao => "DAO",
            ClassRole.Dto => "DTO",
            ClassRole.HttpClient => "HTTP Client",
            ClassRole.DatabaseClient => "DB Client",
            ClassRole.OrmContext => "ORM Context",
            ClassRole.AuthService => "Auth Service",
            ClassRole.EntryPoint => "Entry Point",
            ClassRole.ErrorHandler => "Error Handler",
            _ => SplitPascal(role.ToString())
        };

    public static string FormatLanguage(TargetLanguage lang) =>
        lang switch
        {
            TargetLanguage.CSharp => "C#",
            TargetLanguage.Java => "Java",
            _ => lang.ToString()
        };

    private static string SplitPascal(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return string.Concat(value.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? " " + c : c.ToString()));
    }
}
