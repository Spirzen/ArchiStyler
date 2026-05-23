namespace ArchiStyler.Models;

public sealed class ClassRoleItem
{
    public ClassRole Role { get; init; }
    public string DisplayName { get; init; } = "";

    public override string ToString() => DisplayName;
}
