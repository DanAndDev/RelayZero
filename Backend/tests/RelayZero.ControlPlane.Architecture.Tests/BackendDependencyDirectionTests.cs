using System.Xml.Linq;
using Xunit;

namespace RelayZero.ControlPlane.Architecture.Tests;

public sealed class BackendDependencyDirectionTests
{
    private static readonly Dictionary<string, string[]> AllowedReferences = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RelayZero.ControlPlane.Domain"] = [],
        ["RelayZero.ControlPlane.Application"] = ["RelayZero.ControlPlane.Domain"],
        ["RelayZero.ControlPlane.Infrastructure"] = ["RelayZero.ControlPlane.Application", "RelayZero.ControlPlane.Domain"],
        ["RelayZero.ControlPlane.Api"] = ["RelayZero.ControlPlane.Application", "RelayZero.ControlPlane.Domain", "RelayZero.ControlPlane.Infrastructure"],
    };

    [Fact]
    public void ProjectReferencesFollowApprovedDependencyDirection()
    {
        string sourceRoot = Path.Combine(FindRepositoryRoot(), "Backend", "src");

        foreach ((string projectName, string[] allowedReferences) in AllowedReferences)
        {
            string projectPath = Path.Combine(sourceRoot, projectName, $"{projectName}.csproj");
            Assert.True(File.Exists(projectPath), $"Missing project file: {projectPath}");

            string[] references = ReadProjectReferences(projectPath);
            string[] forbiddenReferences = references
                .Where(reference => !allowedReferences.Contains(reference, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            Assert.Empty(forbiddenReferences);
        }
    }

    private static string[] ReadProjectReferences(string projectPath)
    {
        XDocument document = XDocument.Load(projectPath);
        string projectDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"Unable to resolve project directory for {projectPath}");

        return document
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => Path.GetFullPath(Path.Combine(projectDirectory, include!)))
            .Select(Path.GetFileNameWithoutExtension)
            .Where(projectName => !string.IsNullOrWhiteSpace(projectName))
            .ToArray()!;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "global.json")) &&
                Directory.Exists(Path.Combine(directory.FullName, "Backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate repository root.");
    }
}
