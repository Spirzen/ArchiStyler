using ArchiStyler.Models;

namespace ArchiStyler.Services;

public interface ICodeGenerator
{
    TargetLanguage Language { get; }
    string GenerateClass(ClassDefinition cls, ProjectModel project);
    string GetFileExtension();
    string GetRelativeFilePath(ClassDefinition cls, ProjectModel project);
}
