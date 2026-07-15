using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace RelayZero.Tests.EditMode.Baseline
{
    public sealed class ToolchainBaselineTests
    {
        [Test]
        public void ProjectUsesInputSystemPackageOnly()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string projectSettings = File.ReadAllText(Path.Combine(projectRoot, "ProjectSettings", "ProjectSettings.asset"));

            StringAssert.Contains("activeInputHandler: 1", projectSettings);
        }

        [Test]
        public void ManifestPinsRequiredBaselinePackages()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string manifest = File.ReadAllText(Path.Combine(projectRoot, "Packages", "manifest.json"));

            StringAssert.Contains("\"com.unity.inputsystem\": \"1.19.0\"", manifest);
            StringAssert.Contains("\"com.unity.transport\": \"2.7.4\"", manifest);
            StringAssert.Contains("\"com.unity.burst\": \"1.8.29\"", manifest);
            StringAssert.Contains("\"com.unity.mathematics\": \"1.3.3\"", manifest);
            StringAssert.DoesNotContain("com.unity.purchasing", manifest);
        }
    }
}
