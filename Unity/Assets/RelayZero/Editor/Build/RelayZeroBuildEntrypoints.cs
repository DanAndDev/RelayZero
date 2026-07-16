using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RelayZero.Editor.Build
{
    public static class RelayZeroBuildEntrypoints
    {
        [MenuItem("Relay Zero/Build/Client Development")]
        public static void BuildClientDevelopment()
        {
            Build(RelayZeroBuildProfile.ClientDevelopment);
        }

        [MenuItem("Relay Zero/Build/Client Release")]
        public static void BuildClientRelease()
        {
            Build(RelayZeroBuildProfile.ClientRelease);
        }

        [MenuItem("Relay Zero/Build/Server Development")]
        public static void BuildServerDevelopment()
        {
            Build(RelayZeroBuildProfile.ServerDevelopment);
        }

        [MenuItem("Relay Zero/Build/Server Release")]
        public static void BuildServerRelease()
        {
            Build(RelayZeroBuildProfile.ServerRelease);
        }

        [MenuItem("Relay Zero/Build/Switch Active Target/Windows Client")]
        public static void SwitchActiveTargetToWindowsClient()
        {
            EditorUserBuildSettings.standaloneBuildSubtarget = StandaloneBuildSubtarget.Player;
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Standalone,
                    BuildTarget.StandaloneWindows64);
            }

            Debug.Log("Relay Zero active Unity target is Windows client.");
        }

        public static void Build(RelayZeroBuildProfile profile)
        {
            RoleSceneSetup.GenerateRoleScenesAndConfiguration();
            BuildSecurityGuard.Validate(profile);

            BuildTarget originalTarget = EditorUserBuildSettings.activeBuildTarget;
            StandaloneBuildSubtarget originalSubtarget = EditorUserBuildSettings.standaloneBuildSubtarget;
            BuildPlayerOptions options = CreateOptions(profile);
            ValidateTargetSupport(profile, options.target);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(options.locationPathName));

                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Relay Zero {profile} build failed: {report.summary.result} ({report.summary.totalErrors} errors).");
                }

                Debug.Log($"Relay Zero {profile} build succeeded: {options.locationPathName}");
            }
            finally
            {
                EditorUserBuildSettings.standaloneBuildSubtarget = originalSubtarget;
                if (EditorUserBuildSettings.activeBuildTarget != originalTarget &&
                    BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, originalTarget))
                {
                    EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, originalTarget);
                }
            }
        }

        private static BuildPlayerOptions CreateOptions(RelayZeroBuildProfile profile)
        {
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = GetScenes(profile),
                locationPathName = GetOutputPath(profile),
                target = IsServer(profile) ? BuildTarget.StandaloneLinux64 : BuildTarget.StandaloneWindows64,
                options = IsDevelopment(profile) ? BuildOptions.Development : BuildOptions.None,
            };

            if (IsServer(profile))
            {
                options.subtarget = (int)StandaloneBuildSubtarget.Server;
            }

            return options;
        }

        private static void ValidateTargetSupport(RelayZeroBuildProfile profile, BuildTarget target)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
            {
                throw new InvalidOperationException(
                    $"Relay Zero {profile} requires Unity build target {target}, but that module is not installed or enabled in this Editor.");
            }
        }

        private static string[] GetScenes(RelayZeroBuildProfile profile)
        {
            if (IsServer(profile))
            {
                return new[] { RoleSceneSetup.BootstrapScenePath };
            }

            return new[]
            {
                RoleSceneSetup.BootstrapScenePath,
                RoleSceneSetup.FrontendScenePath,
                RoleSceneSetup.SwitchyardScenePath,
            };
        }

        private static string GetOutputPath(RelayZeroBuildProfile profile)
        {
            string repositoryRoot = GetRepositoryRoot();
            string buildRoot = Path.Combine(repositoryRoot, "Builds");

            switch (profile)
            {
                case RelayZeroBuildProfile.ClientDevelopment:
                    return Path.Combine(buildRoot, "ClientDevelopment", "RelayZero.exe");
                case RelayZeroBuildProfile.ClientRelease:
                    return Path.Combine(buildRoot, "ClientRelease", "RelayZero.exe");
                case RelayZeroBuildProfile.ServerDevelopment:
                    return Path.Combine(buildRoot, "ServerDevelopment", "RelayZeroServer.x86_64");
                case RelayZeroBuildProfile.ServerRelease:
                    return Path.Combine(buildRoot, "ServerRelease", "RelayZeroServer.x86_64");
                default:
                    throw new ArgumentOutOfRangeException(nameof(profile), profile, null);
            }
        }

        private static string GetRepositoryRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        }

        private static bool IsDevelopment(RelayZeroBuildProfile profile)
        {
            return profile == RelayZeroBuildProfile.ClientDevelopment ||
                profile == RelayZeroBuildProfile.ServerDevelopment;
        }

        private static bool IsServer(RelayZeroBuildProfile profile)
        {
            return profile == RelayZeroBuildProfile.ServerDevelopment ||
                profile == RelayZeroBuildProfile.ServerRelease;
        }
    }
}
