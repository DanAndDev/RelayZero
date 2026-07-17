using System.Collections.Generic;
using System.Linq;
using RelayZero.Arena;
using RelayZero.Arena.Baking;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace RelayZero.Editor.Arena
{
    [InitializeOnLoad]
    internal static class ArenaBakePreviewGizmos
    {
        static ArenaBakePreviewGizmos()
        {
            SceneView.duringSceneGui += Draw;
        }

        private static void Draw(SceneView sceneView)
        {
            if (!ArenaValidationWindow.PreviewEnabled)
            {
                return;
            }

            ArenaBakeAsset asset = AssetDatabase.LoadAssetAtPath<ArenaBakeAsset>(ArenaBakeProcessor.SwitchyardBakeAssetPath);
            if (asset == null)
            {
                return;
            }

            ArenaBakeData data = asset.CreateRuntimeData();
            Dictionary<ArenaElementId, float2> positions = data.NavigationNodes.ToDictionary(node => node.Id, node => node.Position);
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            Handles.color = new Color(0.25f, 0.95f, 1f, 0.72f);
            foreach (BakedNavigationEdge edge in data.NavigationEdges)
            {
                Handles.DrawAAPolyLine(3f, ToWorld(positions[edge.NodeA], 0.2f), ToWorld(positions[edge.NodeB], 0.2f));
            }

            Handles.color = new Color(1f, 0.25f, 0.2f, 0.8f);
            foreach (BakedConvexObstacle obstacle in data.Obstacles)
            {
                DrawRectangle(obstacle.PlayerExpandedBounds, 0.24f);
            }

            Handles.color = new Color(1f, 0.85f, 0.12f, 0.9f);
            foreach (BakedShockGate gate in data.Gates)
            {
                float extent = math.abs(gate.SafeSideDirection.x) * gate.HalfExtents.x +
                               math.abs(gate.SafeSideDirection.y) * gate.HalfExtents.y;
                float2 target = gate.Center + gate.SafeSideDirection * (extent + 1.2f + ArenaBakeCompiler.PlayerRadius);
                Handles.DrawDottedLine(ToWorld(gate.Center, 0.28f), ToWorld(target, 0.28f), 4f);
                Handles.SphereHandleCap(0, ToWorld(target, 0.28f), Quaternion.identity, 0.22f, EventType.Repaint);
            }
        }

        private static void DrawRectangle(ArenaAabb bounds, float height)
        {
            Vector3[] points =
            {
                ToWorld(new float2(bounds.Minimum.x, bounds.Minimum.y), height),
                ToWorld(new float2(bounds.Maximum.x, bounds.Minimum.y), height),
                ToWorld(new float2(bounds.Maximum.x, bounds.Maximum.y), height),
                ToWorld(new float2(bounds.Minimum.x, bounds.Maximum.y), height),
                ToWorld(new float2(bounds.Minimum.x, bounds.Minimum.y), height),
            };
            Handles.DrawAAPolyLine(2f, points);
        }

        private static Vector3 ToWorld(float2 point, float height)
        {
            return new Vector3(point.x, height, point.y);
        }
    }
}
