using System;
using UnityEngine;

namespace RelayZero.Arena.Authoring
{
    [DisallowMultipleComponent]
    public sealed class ConvexPylonAuthoring : ArenaElementAuthoring
    {
        [SerializeField]
        private Vector2[] vertices = Array.Empty<Vector2>();

        public Vector2[] Vertices
        {
            get { return (Vector2[])vertices.Clone(); }
        }

        public void Configure(string id, Vector2[] localVertices)
        {
            ConfigureStableId(id);
            vertices = localVertices == null ? Array.Empty<Vector2>() : (Vector2[])localVertices.Clone();
        }

        private void OnDrawGizmos()
        {
            if (vertices == null || vertices.Length < 2)
            {
                return;
            }

            Gizmos.color = ArenaGizmoPalette.StaticCollision;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector2 a = vertices[i];
                Vector2 b = vertices[(i + 1) % vertices.Length];
                Gizmos.DrawLine(
                    transform.TransformPoint(new Vector3(a.x, 0f, a.y)),
                    transform.TransformPoint(new Vector3(b.x, 0f, b.y)));
            }
        }
    }
}
