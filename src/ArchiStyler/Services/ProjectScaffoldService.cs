using System.Text;
using ArchiStyler.Models;

namespace ArchiStyler.Services;

public sealed class ProjectScaffoldService
{
    public string GenerateCsProj(ProjectModel project)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <OutputType>Library</OutputType>");
        sb.AppendLine("    <TargetFramework>net8.0</TargetFramework>");
        sb.AppendLine("    <Nullable>enable</Nullable>");
        sb.AppendLine("    <ImplicitUsings>enable</ImplicitUsings>");
        sb.AppendLine($"    <RootNamespace>{project.DefaultNamespace}</RootNamespace>");
        sb.AppendLine($"    <AssemblyName>{Sanitize(project.Name)}</AssemblyName>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine("</Project>");
        return sb.ToString();
    }

    public string GeneratePomXml(ProjectModel project)
    {
        var artifact = Sanitize(project.Name).ToLowerInvariant();
        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <project xmlns="http://maven.apache.org/POM/4.0.0"
                     xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
                     xsi:schemaLocation="http://maven.apache.org/POM/4.0.0 https://maven.apache.org/xsd/maven-4.0.0.xsd">
              <modelVersion>4.0.0</modelVersion>
              <groupId>{project.DefaultPackage}</groupId>
              <artifactId>{artifact}</artifactId>
              <version>1.0.0-SNAPSHOT</version>
              <name>{project.Name}</name>
              <properties>
                <project.build.sourceEncoding>UTF-8</project.build.sourceEncoding>
                <maven.compiler.release>17</maven.compiler.release>
              </properties>
            </project>
            """;
    }

    private static string Sanitize(string name) =>
        string.Concat(name.Where(c => char.IsLetterOrDigit(c) || c is '_' or '-'));

    public string GetScaffoldFileName(ProjectModel project) =>
        project.Language == TargetLanguage.CSharp
            ? $"{Sanitize(project.Name)}.csproj"
            : "pom.xml";
}
