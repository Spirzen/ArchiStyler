namespace ArchiStyler.Controls;

public enum RelationVisualPart
{
    HitLine,
    Shape,
    Label
}

public sealed class RelationVisualTag
{
    public Guid? RelationId { get; init; }
    public RelationVisualPart Part { get; init; }
}
