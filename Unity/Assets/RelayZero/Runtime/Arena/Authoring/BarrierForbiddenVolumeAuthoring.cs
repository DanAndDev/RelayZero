using UnityEngine;

namespace RelayZero.Arena.Authoring
{
    public enum BarrierForbiddenShape
    {
        Circle,
        Rectangle,
    }

    [DisallowMultipleComponent]
    public sealed class BarrierForbiddenVolumeAuthoring : ArenaElementAuthoring
    {
        [SerializeField]
        private BarrierForbiddenShape shape;

        [SerializeField]
        private float radius = 1f;

        [SerializeField]
        private Vector2 size = Vector2.one;

        [SerializeField]
        private string reason = string.Empty;

        public BarrierForbiddenShape Shape
        {
            get { return shape; }
        }

        public float Radius
        {
            get { return radius; }
        }

        public Vector2 Size
        {
            get { return size; }
        }

        public string Reason
        {
            get { return reason; }
        }

        public void ConfigureCircle(string id, float circleRadius, string description)
        {
            ConfigureStableId(id);
            shape = BarrierForbiddenShape.Circle;
            radius = Mathf.Max(0.05f, circleRadius);
            size = Vector2.one * (radius * 2f);
            reason = description ?? string.Empty;
        }

        public void ConfigureRectangle(string id, Vector2 rectangleSize, string description)
        {
            ConfigureStableId(id);
            shape = BarrierForbiddenShape.Rectangle;
            size = rectangleSize;
            radius = 0f;
            reason = description ?? string.Empty;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = ArenaGizmoPalette.BarrierForbidden;
            if (shape == BarrierForbiddenShape.Circle)
            {
                Gizmos.DrawWireSphere(transform.position, radius);
                return;
            }

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, 0.12f, size.y));
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
