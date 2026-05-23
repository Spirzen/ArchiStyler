namespace ArchiStyler.Models;

public sealed class RelationDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FromClassId { get; set; }
    public Guid ToClassId { get; set; }
    public RelationKind Kind { get; set; } = RelationKind.Uses;
    public ConnectionPort? FromPort { get; set; }
    public ConnectionPort? ToPort { get; set; }
    /// <summary>Имя поля или метода для FieldReference / MethodReference.</summary>
    public string? MemberName { get; set; }
    public bool CreateNewMember { get; set; }
}
