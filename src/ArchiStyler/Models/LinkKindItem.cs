using ArchiStyler.Models;

namespace ArchiStyler.Models;

public sealed class LinkKindItem
{
    public RelationKind Kind { get; init; }
    public string DisplayName { get; init; } = "";

    public LinkKindItem() { }

    public LinkKindItem(RelationKind kind, string displayName)
    {
        Kind = kind;
        DisplayName = displayName;
    }
}
