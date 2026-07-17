using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RelayZero.Foundation;
using UnityEngine;

namespace RelayZero.Tests.EditMode.Architecture
{
    public sealed class UnityAssemblyBoundaryTests
    {
        private static readonly Dictionary<string, string[]> ExpectedRuntimeReferences = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["RelayZero.Arena"] = Array.Empty<string>(),
            ["RelayZero.Arena.Authoring"] = Array.Empty<string>(),
            ["RelayZero.Arena.Baking"] = new[] { "RelayZero.Arena" },
            ["RelayZero.Foundation"] = Array.Empty<string>(),
            ["RelayZero.Simulation"] = new[] { "RelayZero.Foundation" },
            ["RelayZero.Protocol"] = new[] { "RelayZero.Foundation", "RelayZero.Simulation" },
            ["RelayZero.AI"] = new[] { "RelayZero.Foundation", "RelayZero.Simulation" },
            ["RelayZero.Transport"] = new[] { "RelayZero.Foundation", "RelayZero.Protocol" },
            ["RelayZero.Server"] = new[] { "RelayZero.Foundation", "RelayZero.Simulation", "RelayZero.Protocol", "RelayZero.AI", "RelayZero.Transport" },
            ["RelayZero.Client.Prediction"] = new[] { "RelayZero.Foundation", "RelayZero.Simulation", "RelayZero.Protocol" },
            ["RelayZero.Client.Application"] = new[] { "RelayZero.Foundation", "RelayZero.Protocol" },
            ["RelayZero.Client.Presentation"] = new[] { "RelayZero.Foundation", "RelayZero.Simulation", "RelayZero.Client.Prediction" },
            ["RelayZero.Client.UI"] = new[] { "RelayZero.Client.Application", "RelayZero.Client.Presentation" },
            ["RelayZero.Infrastructure"] = new[] { "RelayZero.Foundation", "RelayZero.Client.Application" },
            ["RelayZero.Bootstrap"] = new[] { "RelayZero.Foundation" },
        };

        private static readonly string[] ClientOnlyAssemblies =
        {
            "RelayZero.Client.Prediction",
            "RelayZero.Client.Application",
            "RelayZero.Client.Presentation",
            "RelayZero.Client.UI",
            "RelayZero.Infrastructure",
        };

        [Test]
        public void RuntimeAssembliesFollowApprovedDependencyDirection()
        {
            IReadOnlyDictionary<string, AssemblyDefinition> definitions = LoadAssemblyDefinitions();

            foreach ((string assemblyName, string[] allowedReferences) in ExpectedRuntimeReferences)
            {
                Assert.That(definitions.ContainsKey(assemblyName), Is.True, $"Missing asmdef for {assemblyName}.");

                string[] relayZeroReferences = definitions[assemblyName].References
                    .Where(reference => reference.StartsWith("RelayZero.", StringComparison.Ordinal))
                    .ToArray();

                string[] forbiddenReferences = relayZeroReferences
                    .Where(reference => !allowedReferences.Contains(reference, StringComparer.Ordinal))
                    .ToArray();

                Assert.That(
                    forbiddenReferences,
                    Is.Empty,
                    $"{assemblyName} has forbidden Relay Zero references: {string.Join(", ", forbiddenReferences)}");
            }
        }

        [Test]
        public void SimulationAssemblyExcludesUnityEngineAndRoleAssemblies()
        {
            AssemblyDefinition simulation = LoadAssemblyDefinitions()["RelayZero.Simulation"];

            Assert.That(simulation.NoEngineReferences, Is.True, "RelayZero.Simulation must keep noEngineReferences enabled.");

            string[] forbiddenReferences = simulation.References
                .Where(reference =>
                    reference.StartsWith("UnityEngine", StringComparison.Ordinal) ||
                    reference.Contains(".Client", StringComparison.Ordinal) ||
                    reference.Contains(".Server", StringComparison.Ordinal) ||
                    reference == "RelayZero.Protocol" ||
                    reference == "RelayZero.Infrastructure")
                .ToArray();

            Assert.That(forbiddenReferences, Is.Empty);
        }

        [Test]
        public void ClientOnlyAssembliesAreExcludedFromUnityServerBuilds()
        {
            IReadOnlyDictionary<string, AssemblyDefinition> definitions = LoadAssemblyDefinitions();

            foreach (string assemblyName in ClientOnlyAssemblies)
            {
                AssemblyDefinition definition = definitions[assemblyName];

                Assert.That(
                    definition.DefineConstraints,
                    Does.Contain("!UNITY_SERVER"),
                    $"{assemblyName} must be excluded from dedicated server builds.");
            }
        }

        [Test]
        public void ServerAssemblyDoesNotReferenceClientOrUiAssemblies()
        {
            AssemblyDefinition server = LoadAssemblyDefinitions()["RelayZero.Server"];

            string[] forbiddenReferences = server.References
                .Where(reference =>
                    reference.StartsWith("RelayZero.Client.", StringComparison.Ordinal) ||
                    reference == "RelayZero.Infrastructure")
                .ToArray();

            Assert.That(forbiddenReferences, Is.Empty);
        }

        [Test]
        public void RuntimeSourcesDoNotIntroduceAServiceLocator()
        {
            string runtimeRoot = Path.Combine(Application.dataPath, "RelayZero", "Runtime");
            string[] offenders = Directory
                .GetFiles(runtimeRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => File.ReadAllText(path).Contains("ServiceLocator", StringComparison.Ordinal))
                .Select(path => MakeProjectRelative(path))
                .ToArray();

            Assert.That(offenders, Is.Empty);
        }

        [Test]
        public void GeneratedBuildInfoContainsRequiredIdentityFields()
        {
            BuildInfo buildInfo = GeneratedBuildInfo.ForRole(ApplicationRole.Test);

            Assert.That(buildInfo.Version, Is.Not.Empty);
            Assert.That(buildInfo.Commit, Is.Not.Empty);
            Assert.That(buildInfo.ProtocolVersion, Is.EqualTo("protocol-placeholder"));
            Assert.That(buildInfo.ConfigurationVersion, Is.EqualTo("configuration-placeholder"));
            Assert.That(buildInfo.Role, Is.EqualTo(ApplicationRole.Test));
        }

        private static IReadOnlyDictionary<string, AssemblyDefinition> LoadAssemblyDefinitions()
        {
            string relayZeroRoot = Path.Combine(Application.dataPath, "RelayZero");

            return Directory
                .GetFiles(relayZeroRoot, "*.asmdef", SearchOption.AllDirectories)
                .Select(path =>
                {
                    string json = File.ReadAllText(path);
                    AssemblyDefinitionFile file = JsonUtility.FromJson<AssemblyDefinitionFile>(json);
                    return new AssemblyDefinition(path, file);
                })
                .ToDictionary(definition => definition.Name, StringComparer.Ordinal);
        }

        private static string MakeProjectRelative(string path)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string fullPath = Path.GetFullPath(path);
            return fullPath.Substring(projectRoot.Length + 1).Replace('\\', '/');
        }

        [Serializable]
        private sealed class AssemblyDefinitionFile
        {
            public string name;
            public string[] references;
            public string[] defineConstraints;
            public bool noEngineReferences;
        }

        private sealed class AssemblyDefinition
        {
            public AssemblyDefinition(string path, AssemblyDefinitionFile file)
            {
                Path = path;
                Name = file.name;
                References = file.references ?? Array.Empty<string>();
                DefineConstraints = file.defineConstraints ?? Array.Empty<string>();
                NoEngineReferences = file.noEngineReferences;
            }

            public string Path { get; }

            public string Name { get; }

            public string[] References { get; }

            public string[] DefineConstraints { get; }

            public bool NoEngineReferences { get; }
        }
    }
}
