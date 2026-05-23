using ArchiStyler.Models;

namespace ArchiStyler.Helpers;

public static class RoleCatalog
{
    public static IReadOnlyList<ClassRoleItem> All { get; } = Build();

    private static List<ClassRoleItem> Build() =>
        Enum.GetValues<ClassRole>()
            .Select(r => new ClassRoleItem { Role = r, DisplayName = Format(r) })
            .OrderBy(x => x.DisplayName)
            .ToList();

    private static string Format(ClassRole role) =>
        role switch
        {
            ClassRole.None => "— Без роли —",
            ClassRole.Dao => "DAO",
            ClassRole.Dto => "DTO",
            ClassRole.HttpClient => "HTTP Client",
            ClassRole.DatabaseClient => "DB Client",
            ClassRole.OrmContext => "ORM Context",
            ClassRole.AuthService => "Auth Service",
            ClassRole.EntryPoint => "Entry Point",
            ClassRole.ErrorHandler => "Error Handler",
            ClassRole.AbstractClass => "Abstract Class",
            _ => EnumDisplay.FormatRole(role)
        };
}
