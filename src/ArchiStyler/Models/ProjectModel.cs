namespace ArchiStyler.Models;

public sealed class ProjectModel
{
    public string Name { get; set; } = "MyArchitecture";
    public string RootPath { get; set; } = "";
    public TargetLanguage Language { get; set; } = TargetLanguage.CSharp;
    public string DefaultNamespace { get; set; } = "App";
    public string DefaultPackage { get; set; } = "app";
    public List<FolderDefinition> Folders { get; set; } = [];
    public List<ClassDefinition> Classes { get; set; } = [];
    public List<RelationDefinition> Relations { get; set; } = [];
}
