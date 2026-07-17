using RelayZero.Arena.Authoring;
using UnityEditor;
using UnityEngine;

namespace RelayZero.Editor.Arena
{
    [InitializeOnLoad]
    public static class SwitchyardAuthoringGizmos
    {
        private static GUIStyle labelStyle;

        private static GUIStyle LabelStyle
        {
            get
            {
                if (labelStyle == null)
                {
                    labelStyle = new GUIStyle(EditorStyles.miniBoldLabel);
                    labelStyle.normal.textColor = new Color(0.92f, 0.96f, 1f);
                }

                return labelStyle;
            }
        }

        static SwitchyardAuthoringGizmos()
        {
            SceneView.duringSceneGui += DrawLegend;
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        private static void DrawStableId(ArenaElementAuthoring element, GizmoType gizmoType)
        {
            if (element == null || string.IsNullOrWhiteSpace(element.StableId))
            {
                return;
            }

            Handles.Label(element.transform.position + Vector3.up * 0.45f, element.StableId, LabelStyle);
        }

        private static void DrawLegend(SceneView sceneView)
        {
            SwitchyardSceneLegendAuthoring legend = Object.FindAnyObjectByType<SwitchyardSceneLegendAuthoring>();
            if (legend == null || !legend.Visible)
            {
                return;
            }

            Handles.BeginGUI();
            Rect area = new Rect(legend.ScreenPosition.x, legend.ScreenPosition.y, 228f, 260f);
            GUILayout.BeginArea(area, "SWITCHYARD AUTHORING", GUI.skin.window);
            DrawLegendItem("Playable bounds", ArenaGizmoPalette.Bounds);
            DrawLegendItem("Walls / convex pylons", ArenaGizmoPalette.StaticCollision);
            DrawLegendItem("Relay Alpha", ArenaGizmoPalette.RelayAlpha);
            DrawLegendItem("Relay Beta", ArenaGizmoPalette.RelayBeta);
            DrawLegendItem("Terminal interaction", ArenaGizmoPalette.Terminal);
            DrawLegendItem("Shock Gate + safe side", ArenaGizmoPalette.ShockGate);
            DrawLegendItem("Boost pad", ArenaGizmoPalette.Boost);
            DrawLegendItem("Player spawn + facing", ArenaGizmoPalette.Spawn);
            DrawLegendItem("Core reset", ArenaGizmoPalette.CoreReset);
            DrawLegendItem("Barrier-forbidden", ArenaGizmoPalette.BarrierForbidden);
            DrawLegendItem("Navigation hint", ArenaGizmoPalette.Navigation);
            DrawLegendItem("Camera bounds", ArenaGizmoPalette.CameraBounds);
            GUILayout.Space(4f);
            GUILayout.Label("Labels are stable bake IDs.", EditorStyles.miniLabel);
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private static void DrawLegendItem(string label, Color color)
        {
            GUILayout.BeginHorizontal();
            Color previous = GUI.color;
            GUI.color = color;
            GUILayout.Box(GUIContent.none, GUILayout.Width(15f), GUILayout.Height(10f));
            GUI.color = previous;
            GUILayout.Label(label, EditorStyles.miniLabel);
            GUILayout.EndHorizontal();
        }
    }
}
