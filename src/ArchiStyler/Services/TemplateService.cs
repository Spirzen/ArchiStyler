using System.Text.Json;
using ArchiStyler.Helpers;
using ArchiStyler.Models;

namespace ArchiStyler.Services;

public sealed class TemplateService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PatternTemplateFile LoadPatterns()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Assets", "Templates");
        if (!Directory.Exists(dir))
            return GetBuiltInPatterns();

        var merged = new PatternTemplateFile();
        foreach (var file in Directory.GetFiles(dir, "*.json").OrderBy(f => f))
        {
            try
            {
                var json = File.ReadAllText(file);
                var part = JsonSerializer.Deserialize<PatternTemplateFile>(json, JsonOptions);
                if (part?.Patterns is { Count: > 0 })
                    merged.Patterns.AddRange(part.Patterns);
            }
            catch
            {
                // skip invalid template files
            }
        }

        return merged.Patterns.Count > 0 ? merged : GetBuiltInPatterns();
    }

    public void ApplyPattern(ProjectModel project, PatternTemplate pattern, double originX = 80, double originY = 80)
    {
        var nameToId = new Dictionary<string, Guid>();
        var folderNameToId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var tf in pattern.Folders)
        {
            var folder = new FolderDefinition
            {
                Name = tf.Name,
                Segment = tf.Segment ?? tf.Name,
                X = originX + tf.OffsetX,
                Y = originY + tf.OffsetY,
                Width = tf.Width > 0 ? tf.Width : 320,
                Height = tf.Height > 0 ? tf.Height : 240
            };
            if (!string.IsNullOrWhiteSpace(tf.Parent) &&
                folderNameToId.TryGetValue(tf.Parent, out var parentId))
                folder.ParentFolderId = parentId;

            project.Folders.Add(folder);
            folderNameToId[tf.Name] = folder.Id;
        }

        foreach (var tc in pattern.Classes)
        {
            double classX = originX + tc.OffsetX;
            double classY = originY + tc.OffsetY;
            Guid? folderId = null;

            if (!string.IsNullOrWhiteSpace(tc.Folder) &&
                folderNameToId.TryGetValue(tc.Folder, out var fid))
            {
                folderId = fid;
                var folder = project.Folders.First(f => f.Id == fid);
                classX = folder.X + 20 + tc.OffsetX;
                classY = folder.Y + 44 + tc.OffsetY;
            }

            var cls = new ClassDefinition
            {
                Name = tc.Name,
                X = classX,
                Y = classY,
                FolderId = folderId,
                IsInterface = tc.IsInterface,
                IsAbstract = tc.IsAbstract,
                BaseType = tc.BaseType,
                ImplementedInterfaces = tc.Implements.ToList(),
                Usings = tc.Usings.ToList()
            };

            if (Enum.TryParse<ClassRole>(tc.Role, true, out var role))
                LanguageHelper.ApplyRoleDefaults(cls, role);

            if (folderId is not null)
                ProjectPathHelper.ApplyFolderAssignment(cls, project, folderId);
            else if (project.Language == TargetLanguage.CSharp)
                cls.Namespace = project.DefaultNamespace;
            else
                cls.Package = project.DefaultPackage;

            foreach (var tm in tc.Members)
            {
                var member = new MemberDefinition
                {
                    Name = tm.Name,
                    Type = tm.Type,
                    ReturnType = tm.ReturnType,
                    IsAbstract = tm.IsAbstract,
                    IsStatic = tm.IsStatic,
                    GenerateStub = tm.GenerateStub,
                    Kind = Enum.TryParse<MemberKind>(tm.Kind, true, out var mk) ? mk : MemberKind.Method
                };
                if (Enum.TryParse<AccessModifier>(tm.Access, true, out var acc))
                    member.Access = acc;
                foreach (var p in tm.Parameters)
                    member.Parameters.Add(new ParameterDefinition { Name = p.Name, Type = p.Type });
                cls.Members.Add(member);
            }

            project.Classes.Add(cls);
            nameToId[tc.Name] = cls.Id;
        }

        foreach (var tr in pattern.Relations)
        {
            if (!nameToId.TryGetValue(tr.From, out var fromId) ||
                !nameToId.TryGetValue(tr.To, out var toId))
                continue;

            project.Relations.Add(new RelationDefinition
            {
                FromClassId = fromId,
                ToClassId = toId,
                Kind = Enum.TryParse<RelationKind>(tr.Kind, true, out var rk) ? rk : RelationKind.Implements
            });

            var fromClass = project.Classes.First(c => c.Id == fromId);
            var toClass = project.Classes.First(c => c.Id == toId);

            if (tr.Kind.Equals("Inherits", StringComparison.OrdinalIgnoreCase))
                fromClass.BaseType = toClass.Name;
            else if (tr.Kind.Equals("Implements", StringComparison.OrdinalIgnoreCase) && !fromClass.ImplementedInterfaces.Contains(toClass.Name))
                fromClass.ImplementedInterfaces.Add(toClass.Name);
        }
    }

    public ClassDefinition CreateFromRole(ClassRole role, ProjectModel project, double x, double y)
    {
        var name = role == ClassRole.None ? "NewClass" : role.ToString();
        var cls = new ClassDefinition
        {
            Name = name,
            X = x,
            Y = y
        };

        LanguageHelper.ApplyRoleDefaults(cls, role);

        if (project.Language == TargetLanguage.CSharp)
            cls.Namespace = project.DefaultNamespace;
        else
            cls.Package = project.DefaultPackage;

        ApplyRoleScaffold(cls, role);
        return cls;
    }

    private static void ApplyRoleScaffold(ClassDefinition cls, ClassRole role)
    {
        switch (role)
        {
            case ClassRole.Dao:
                cls.Members.Add(new MemberDefinition { Name = "GetById", ReturnType = cls.Name.Replace("Dao", ""), Kind = MemberKind.Method, Access = AccessModifier.Public, GenerateStub = true });
                cls.Members.Add(new MemberDefinition { Name = "Save", ReturnType = "void", Kind = MemberKind.Method, Parameters = [new ParameterDefinition { Name = "entity", Type = "object" }] });
                break;
            case ClassRole.Service:
                cls.Members.Add(new MemberDefinition { Name = "Execute", ReturnType = "void", Kind = MemberKind.Method, Access = AccessModifier.Public, GenerateStub = true });
                break;
            case ClassRole.Dto:
                cls.Members.Add(new MemberDefinition { Name = "Id", Type = "string", Kind = MemberKind.Property, Access = AccessModifier.Public });
                break;
            case ClassRole.Repository:
                cls.Members.Add(new MemberDefinition { Name = "FindAll", ReturnType = "IEnumerable<object>", Kind = MemberKind.Method, GenerateStub = true });
                cls.Members.Add(new MemberDefinition { Name = "Add", ReturnType = "void", Kind = MemberKind.Method, Parameters = [new ParameterDefinition { Name = "item", Type = "object" }] });
                break;
            case ClassRole.Logger:
                cls.Members.Add(new MemberDefinition { Name = "Log", ReturnType = "void", Kind = MemberKind.Method, Parameters = [new ParameterDefinition { Name = "message", Type = "string" }] });
                break;
        }
    }

    private static PatternTemplateFile GetBuiltInPatterns() => new() { Patterns = [] };
}
