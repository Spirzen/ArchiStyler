using ArchiStyler.Models;

namespace ArchiStyler.Services;

public static class CodeGeneratorFactory
{
    public static ICodeGenerator Create(TargetLanguage language) =>
        language switch
        {
            TargetLanguage.Java => new JavaCodeGenerator(),
            _ => new CSharpCodeGenerator()
        };
}
