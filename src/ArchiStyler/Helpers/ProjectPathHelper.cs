using ArchiStyler.Models;
using Avalonia;

namespace ArchiStyler.Helpers;

public static class ProjectPathHelper
{
    public static string SanitizeSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Folder";
        var chars = value.Trim()
            .Where(c => char.IsLetterOrDigit(c) || c == '_')
            .ToArray();
        if (chars.Length == 0) return "Folder";
        var s = new string(chars);
        if (char.IsDigit(s[0]))
            s = "_" + s;
        return s;
    }

    public static string GetSegment(FolderDefinition folder) =>
        SanitizeSegment(string.IsNullOrWhiteSpace(folder.Segment) ? folder.Name : folder.Segment!);

    public static IReadOnlyList<string> GetFolderSegments(ProjectModel project, Guid? folderId)
    {
        var segments = new List<string>();
        var current = folderId;
        var guard = 0;
        while (current is { } id && guard++ < 32)
        {
            var folder = project.Folders.FirstOrDefault(f => f.Id == id);
            if (folder is null) break;
            segments.Insert(0, GetSegment(folder));
            current = folder.ParentFolderId;
        }
        return segments;
    }

    public static string CombineNamespace(string root, IReadOnlyList<string> segments)
    {
        if (segments.Count == 0)
            return root;
        var tail = string.Join('.', segments);
        return string.IsNullOrWhiteSpace(root) ? tail : $"{root.Trim()}.{tail}";
    }

    public static string GetEffectiveNamespace(ClassDefinition cls, ProjectModel project)
    {
        var segments = GetFolderSegments(project, cls.FolderId);
        if (segments.Count == 0)
            return cls.Namespace ?? project.DefaultNamespace;
        return CombineNamespace(project.DefaultNamespace, segments);
    }

    public static string GetEffectivePackage(ClassDefinition cls, ProjectModel project)
    {
        var segments = GetFolderSegments(project, cls.FolderId);
        if (segments.Count == 0)
            return cls.Package ?? project.DefaultPackage;
        return CombineNamespace(project.DefaultPackage, segments);
    }

    public static string GetEffectivePathRoot(ClassDefinition cls, ProjectModel project) =>
        project.Language == TargetLanguage.CSharp
            ? GetEffectiveNamespace(cls, project)
            : GetEffectivePackage(cls, project);

    public static string GetRelativeFilePath(ClassDefinition cls, ProjectModel project, string extension)
    {
        var root = GetEffectivePathRoot(cls, project);
        var dir = root.Replace('.', Path.DirectorySeparatorChar);
        return Path.Combine(dir, cls.Name + extension);
    }

    public static void ApplyFolderAssignment(ClassDefinition cls, ProjectModel project, Guid? folderId)
    {
        cls.FolderId = folderId;
        if (project.Language == TargetLanguage.CSharp)
            cls.Namespace = GetEffectiveNamespace(cls, project);
        else
            cls.Package = GetEffectivePackage(cls, project);
    }

    public static void SyncAllClassesInFolder(ProjectModel project, Guid folderId)
    {
        foreach (var cls in project.Classes.Where(c => c.FolderId == folderId))
            ApplyFolderAssignment(cls, project, folderId);
    }

    public static Rect GetFolderBounds(FolderDefinition folder) =>
        new(folder.X, folder.Y, Math.Max(120, folder.Width), Math.Max(100, folder.Height));

    public static FolderDefinition? FindInnermostFolderAt(ProjectModel project, Point point)
    {
        return project.Folders
            .Where(f => GetFolderBounds(f).Contains(point))
            .OrderBy(f => f.Width * f.Height)
            .FirstOrDefault();
    }

    public static string GetFolderDisplayPath(ProjectModel project, Guid? folderId)
    {
        var segments = GetFolderSegments(project, folderId);
        return segments.Count == 0
            ? "(корень)"
            : string.Join('/', segments);
    }

    public static string GetClassLocationLabel(ClassDefinition cls, ProjectModel project) =>
        $"{GetFolderDisplayPath(project, cls.FolderId)} → {GetEffectivePathRoot(cls, project)}";
}
