using RelayZero.Arena.Baking;
using UnityEditor;
using UnityEngine;

namespace RelayZero.Editor.Arena
{
    [CustomEditor(typeof(ArenaBakeAsset))]
    public sealed class ArenaBakeAssetInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            ArenaBakeAsset asset = (ArenaBakeAsset)target;
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("SWITCHYARD RUNTIME BAKE", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Immutable, canonical float2 arena data. Runtime consumers do not require the authoring scene or Transform references.", MessageType.Info);
            EditorGUILayout.LabelField("Bake version", asset.BakeVersion.ToString());
            EditorGUILayout.LabelField("Content hash", asset.ContentHash, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("Source scene", asset.SourceScene);
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Elements", asset.ElementCount.ToString());
            EditorGUILayout.LabelField("Walls / obstacles", asset.WallCount + " / " + asset.ObstacleCount);
            EditorGUILayout.LabelField("Strategic circles / gates", asset.CircleCount + " / " + asset.GateCount);
            EditorGUILayout.LabelField("Navigation nodes / edges", asset.NavigationNodeCount + " / " + asset.NavigationEdgeCount);
            EditorGUILayout.LabelField("Transform references", asset.TransformReferenceCount.ToString());
            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Validate + Rebake Switchyard", GUILayout.Height(30f)))
            {
                ArenaBakeProcessor.ValidateAndBakeSwitchyardMenu();
            }

            if (GUILayout.Button("Open Arena Validator"))
            {
                ArenaValidationWindow.ShowWindow();
            }
        }
    }
}
