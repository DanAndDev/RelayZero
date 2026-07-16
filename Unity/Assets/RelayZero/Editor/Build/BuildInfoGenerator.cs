using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace RelayZero.Editor.Build
{
    [InitializeOnLoad]
    public static class BuildInfoGenerator
    {
        private const string GeneratedAssetPath = "Assets/RelayZero/Runtime/Foundation/BuildInfo.Generated.cs";
        private const string ProtocolPlaceholder = "protocol-placeholder";
        private const string ConfigurationPlaceholder = "configuration-placeholder";

        static BuildInfoGenerator()
        {
            Generate();
        }

        [MenuItem("Relay Zero/Build/Regenerate BuildInfo")]
        public static void Generate()
        {
            string unityProjectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string repositoryRoot = Path.GetFullPath(Path.Combine(unityProjectRoot, ".."));
            string generatedPath = Path.Combine(unityProjectRoot, GeneratedAssetPath);

            GeneratedValues values = new GeneratedValues(
                ReadBundleVersion(unityProjectRoot),
                RunGit(repositoryRoot, "rev-parse", "--short", "HEAD", "unknown"),
                IsDirty(repositoryRoot));

            string contents = CreateSource(values);
            string currentContents = File.Exists(generatedPath)
                ? File.ReadAllText(generatedPath)
                : string.Empty;

            if (string.Equals(currentContents, contents, StringComparison.Ordinal))
            {
                return;
            }

            File.WriteAllText(generatedPath, contents, Encoding.UTF8);
            AssetDatabase.ImportAsset(GeneratedAssetPath, ImportAssetOptions.ForceUpdate);
        }

        private static string ReadBundleVersion(string unityProjectRoot)
        {
            string projectSettingsPath = Path.Combine(unityProjectRoot, "ProjectSettings", "ProjectSettings.asset");
            if (!File.Exists(projectSettingsPath))
            {
                return "0.0.0-dev";
            }

            foreach (string line in File.ReadLines(projectSettingsPath))
            {
                const string prefix = "  bundleVersion: ";
                if (line.StartsWith(prefix, StringComparison.Ordinal))
                {
                    string version = line.Substring(prefix.Length).Trim();
                    return string.IsNullOrWhiteSpace(version) ? "0.0.0-dev" : version;
                }
            }

            return "0.0.0-dev";
        }

        private static bool IsDirty(string repositoryRoot)
        {
            string status = RunGit(
                repositoryRoot,
                "status",
                "--porcelain",
                "--untracked-files=all",
                string.Empty);

            return !string.IsNullOrWhiteSpace(status);
        }

        private static string RunGit(
            string repositoryRoot,
            string command,
            string argument,
            string fallback)
        {
            return RunGit(repositoryRoot, command, argument, null, fallback);
        }

        private static string RunGit(
            string repositoryRoot,
            string command,
            string argument,
            string argument2,
            string fallback)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo("git")
                {
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                };

                startInfo.ArgumentList.Add("-c");
                startInfo.ArgumentList.Add($"safe.directory={NormalizePath(repositoryRoot)}");
                startInfo.ArgumentList.Add("-C");
                startInfo.ArgumentList.Add(repositoryRoot);
                startInfo.ArgumentList.Add(command);
                startInfo.ArgumentList.Add(argument);
                if (!string.IsNullOrEmpty(argument2))
                {
                    startInfo.ArgumentList.Add(argument2);
                }

                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return fallback;
                    }

                    string output = process.StandardOutput.ReadToEnd().Trim();
                    string error = process.StandardError.ReadToEnd().Trim();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        Debug.LogWarning($"BuildInfo git command failed: git {command} {argument} {argument2}. {error}");
                        return fallback;
                    }

                    return string.IsNullOrWhiteSpace(output) ? fallback : output;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"BuildInfo git command failed: {exception.Message}");
                return fallback;
            }
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        private static string CreateSource(GeneratedValues values)
        {
            return
                "// <auto-generated />\n" +
                "// Generated by RelayZero.Editor.Build.BuildInfoGenerator. Do not edit by hand.\n\n" +
                "namespace RelayZero.Foundation\n" +
                "{\n" +
                "    public static partial class GeneratedBuildInfo\n" +
                "    {\n" +
                "        static partial void Fill(ref GeneratedBuildInfoValues values)\n" +
                "        {\n" +
                "            values = new GeneratedBuildInfoValues(\n" +
                $"                \"{Escape(values.Version)}\",\n" +
                $"                \"{Escape(values.Commit)}\",\n" +
                $"                \"{ProtocolPlaceholder}\",\n" +
                $"                \"{ConfigurationPlaceholder}\",\n" +
                $"                {values.IsDirty.ToString().ToLowerInvariant()});\n" +
                "        }\n" +
                "    }\n" +
                "}\n";
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private readonly struct GeneratedValues
        {
            public GeneratedValues(string version, string commit, bool isDirty)
            {
                Version = version;
                Commit = commit;
                IsDirty = isDirty;
            }

            public string Version { get; }

            public string Commit { get; }

            public bool IsDirty { get; }
        }
    }
}
