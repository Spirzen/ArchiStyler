using ArchiStyler.Models;

namespace ArchiStyler.Services.Parsing;

public static class CodeParserFactory
{
    public static ICodeParser Create(TargetLanguage language) =>
        language switch
        {
            TargetLanguage.Java => new JavaCodeParser(),
            _ => new CSharpCodeParser()
        };
}
