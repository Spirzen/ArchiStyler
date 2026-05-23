using ArchiStyler.Models;

namespace ArchiStyler.Helpers;

public static class RelationKindHelper
{
    public static string EdgeLabel(RelationKind kind, RelationDefinition? rel, TargetLanguage language)
    {
        var name = DisplayName(kind, language);
        if (rel?.MemberName is { } member &&
            kind is RelationKind.FieldReference or RelationKind.MethodReference)
            return $"{name}: {member}";
        return name;
    }

    public static string DisplayName(RelationKind kind, TargetLanguage language) =>
        kind switch
        {
            RelationKind.Inherits => "Наследование",
            RelationKind.Implements => "Реализация интерфейса",
            RelationKind.Uses => "Зависимость (uses)",
            RelationKind.Aggregates => "Агрегация",
            RelationKind.Composes => "Композиция",
            RelationKind.FieldReference => "Поле",
            RelationKind.MethodReference => "Метод",
            RelationKind.UsingImport => language == TargetLanguage.CSharp ? "using" : "import",
            _ => kind.ToString()
        };

    public static IReadOnlyList<RelationKind> GetAvailableKinds(ClassDefinition from, ClassDefinition to)
    {
        var list = new List<RelationKind>
        {
            RelationKind.Uses,
            RelationKind.Aggregates,
            RelationKind.Composes,
            RelationKind.FieldReference,
            RelationKind.MethodReference,
            RelationKind.UsingImport
        };

        if (to.IsInterface && !from.IsInterface && !from.IsEnum)
            list.Insert(0, RelationKind.Implements);

        if (!from.IsInterface && !from.IsEnum && !to.IsInterface && !to.IsEnum)
            list.Insert(0, RelationKind.Inherits);

        return list;
    }

    public static void ApplyToModel(
        RelationDefinition rel,
        ClassDefinition from,
        ClassDefinition to,
        TargetLanguage language)
    {
        switch (rel.Kind)
        {
            case RelationKind.Inherits:
                from.BaseType = to.Name;
                break;
            case RelationKind.Implements:
                if (!from.ImplementedInterfaces.Contains(to.Name, StringComparer.Ordinal))
                    from.ImplementedInterfaces.Add(to.Name);
                break;
            case RelationKind.UsingImport:
                var import = language == TargetLanguage.CSharp
                    ? (to.Namespace ?? to.Name)
                    : (to.Package ?? to.Name);
                if (!string.IsNullOrWhiteSpace(import) &&
                    !from.Usings.Contains(import, StringComparer.Ordinal))
                    from.Usings.Add(import);
                break;
            case RelationKind.FieldReference:
                if (rel.CreateNewMember && !string.IsNullOrWhiteSpace(rel.MemberName))
                {
                    from.Members.Add(new MemberDefinition
                    {
                        Kind = MemberKind.Field,
                        Name = rel.MemberName!,
                        Type = to.Name,
                        Access = AccessModifier.Private
                    });
                }
                break;
            case RelationKind.MethodReference:
                if (rel.CreateNewMember && !string.IsNullOrWhiteSpace(rel.MemberName))
                {
                    from.Members.Add(new MemberDefinition
                    {
                        Kind = MemberKind.Method,
                        Name = rel.MemberName!,
                        ReturnType = "void",
                        Access = AccessModifier.Public,
                        GenerateStub = true,
                        Parameters =
                        [
                            new ParameterDefinition { Name = "arg", Type = to.Name }
                        ]
                    });
                }
                break;
        }
    }

    public static IEnumerable<MemberDefinition> GetFieldCandidates(ClassDefinition cls) =>
        cls.Members.Where(m => m.Kind is MemberKind.Field or MemberKind.Property);

    public static IEnumerable<MemberDefinition> GetMethodCandidates(ClassDefinition cls) =>
        cls.Members.Where(m => m.Kind is MemberKind.Method or MemberKind.Constructor);
}
