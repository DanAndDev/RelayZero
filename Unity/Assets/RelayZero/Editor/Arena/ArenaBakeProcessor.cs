using System;
using RelayZero.Arena.Baking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RelayZero.Editor.Arena
{
    public static class ArenaBakeProcessor
    {
        public const string SwitchyardScenePath = "Assets/Scenes/Switchyard.unity";
        public const string SwitchyardBakeAssetPath = "Assets/RelayZero/Arena/SwitchyardArenaBake.asset";

        public static ArenaValidationResult LastResult { get; private set; }

        [MenuItem("Relay Zero/Arena/Validate Switchyard")]
        public static void ValidateSwitchyardMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ArenaValidationResult result = ValidateSwitchyard();
            Log(result);
            ArenaValidationWindow.ShowWindow();
        }

        [MenuItem("Relay Zero/Arena/Validate and Bake Switchyard")]
        public static void ValidateAndBakeSwitchyardMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ArenaValidationResult result = ValidateAndBakeSwitchyard();
            Log(result);
            ArenaValidationWindow.ShowWindow();
        }

        public static ArenaValidationResult ValidateSwitchyard()
        {
            Scene scene = OpenSwitchyard();
            LastResult = ValidateScene(scene);
            return LastResult;
        }

        public static ArenaValidationResult ValidateScene(Scene scene)
        {
            ArenaAuthoringSnapshot snapshot = ArenaAuthoringSnapshot.Capture(scene);
            LastResult = ArenaValidator.Validate(snapshot);
            return LastResult;
        }

        public static ArenaValidationResult ValidateAndBakeSwitchyard()
        {
            ArenaValidationResult result = ValidateSwitchyard();
            if (!result.Report.IsValid)
            {
                return result;
            }

            EnsureFolderPath("Assets/RelayZero/Arena");
            ArenaBakeAsset asset = AssetDatabase.LoadAssetAtPath<ArenaBakeAsset>(SwitchyardBakeAssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ArenaBakeAsset>();
                AssetDatabase.CreateAsset(asset, SwitchyardBakeAssetPath);
            }

            asset.ApplyBake(result.Payload, result.ContentHash);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(SwitchyardBakeAssetPath, ImportAssetOptions.ForceUpdate);

            ArenaBakeAsset persisted = AssetDatabase.LoadAssetAtPath<ArenaBakeAsset>(SwitchyardBakeAssetPath);
            string persistedHash = persisted == null ? string.Empty : persisted.ContentHash;
            if (!string.Equals(persistedHash, result.ContentHash, StringComparison.Ordinal))
            {
                result.Report.Add("ARENA-014", "Persisted bake round-trip", false, "Asset hash changed during serialization.");
            }
            else
            {
                result.Report.Add("ARENA-014", "Persisted bake round-trip", true, SwitchyardBakeAssetPath);
            }

            LastResult = result;
            return result;
        }

        public static void ValidateAndBakeSwitchyardBatch()
        {
            ArenaValidationResult first = ValidateAndBakeSwitchyard();
            Log(first);
            if (!first.Report.IsValid)
            {
                throw new InvalidOperationException(first.Report.FormatForLog());
            }

            string firstHash = first.ContentHash;
            ArenaValidationResult second = ValidateAndBakeSwitchyard();
            bool stable = second.Report.IsValid && string.Equals(firstHash, second.ContentHash, StringComparison.Ordinal);
            second.Report.Add(
                "ARENA-015",
                "Deterministic rebake",
                stable,
                stable ? firstHash : firstHash + " != " + second.ContentHash);
            Log(second);
            if (!second.Report.IsValid)
            {
                throw new InvalidOperationException(second.Report.FormatForLog());
            }
        }

        private static Scene OpenSwitchyard()
        {
            Scene scene = SceneManager.GetSceneByPath(SwitchyardScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(SwitchyardScenePath, OpenSceneMode.Single);
            }

            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException("Could not load " + SwitchyardScenePath);
            }

            return scene;
        }

        private static void EnsureFolderPath(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static void Log(ArenaValidationResult result)
        {
            string text = result.Report.FormatForLog();
            if (result.Report.IsValid)
            {
                Debug.Log(text);
            }
            else
            {
                Debug.LogError(text);
            }
        }
    }
}
