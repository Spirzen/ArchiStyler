using System.Text.Json;
using ArchiStyler.Models;

namespace ArchiStyler.Services;

public sealed class ProjectExportService
{
    private readonly ProjectScaffoldService _scaffold = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExportResult ExportSources(ProjectModel project)
    {
        var generator = CodeGeneratorFactory.Create(project.Language);
        var result = new ExportResult();

        foreach (var cls in project.Classes)
        {
            try
            {
                var code = generator.GenerateClass(cls, project);
                var relative = generator.GetRelativeFilePath(cls, project);
                var fullPath = Path.Combine(project.RootPath, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllText(fullPath, code);
                result.WrittenFiles.Add(fullPath);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"{cls.Name}: {ex.Message}");
            }
        }

        return result;
    }

    public ExportResult ExportAll(ProjectModel project, bool includeScaffold = true)
    {
        var result = ExportSources(project);
        if (!includeScaffold) return result;

        try
        {
            var scaffoldPath = Path.Combine(
                project.RootPath,
                _scaffold.GetScaffoldFileName(project));
            var content = project.Language == TargetLanguage.CSharp
                ? _scaffold.GenerateCsProj(project)
                : _scaffold.GeneratePomXml(project);
            File.WriteAllText(scaffoldPath, content);
            result.WrittenFiles.Add(scaffoldPath);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Scaffold: {ex.Message}");
        }

        return result;
    }

    public void SaveProject(ProjectModel project, string path)
    {
        var json = JsonSerializer.Serialize(project, JsonOptions);
        File.WriteAllText(path, json);
    }

    public ProjectModel? LoadProject(string path)
    {
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ProjectModel>(json, JsonOptions);
    }
}

public sealed class ExportResult
{
    public List<string> WrittenFiles { get; } = [];
    public List<string> Errors { get; } = [];
    public bool Success => Errors.Count == 0;
}
