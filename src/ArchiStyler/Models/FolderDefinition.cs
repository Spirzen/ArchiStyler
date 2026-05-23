namespace ArchiStyler.Models;

public sealed class FolderDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Folder";
    /// <summary>Сегмент пути (Models, Handlers). Пусто — берётся из Name.</summary>
    public string? Segment { get; set; }
    public Guid? ParentFolderId { get; set; }
    public double X { get; set; } = 40;
    public double Y { get; set; } = 40;
    public double Width { get; set; } = 320;
    public double Height { get; set; } = 240;
}
