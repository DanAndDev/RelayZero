using UnityEngine;

namespace RelayZero.Arena.Authoring
{
    [DisallowMultipleComponent]
    public sealed class ArenaWallAuthoring : ArenaElementAuthoring
    {
        [SerializeField]
        private Vector2 start;

        [SerializeField]
        private Vector2 end;

        [SerializeField]
        private float thickness = 0.5f;

        public Vector2 Start
        {
            get { return start; }
        }

        public Vector2 End
        {
            get { return end; }
        }

        public float Thickness
        {
            get { return thickness; }
        }

        public void Configure(string id, Vector2 startPoint, Vector2 endPoint, float wallThickness)
        {
            ConfigureStableId(id);
            start = startPoint;
            end = endPoint;
            thickness = Mathf.Max(0.05f, wallThickness);
        }

        private void OnDrawGizmos()
        {
            Vector3 worldStart = new Vector3(start.x, transform.position.y, start.y);
            Vector3 worldEnd = new Vector3(end.x, transform.position.y, end.y);
            Gizmos.color = ArenaGizmoPalette.StaticCollision;
            Gizmos.DrawLine(worldStart, worldEnd);
            Gizmos.DrawWireSphere(worldStart, thickness * 0.5f);
            Gizmos.DrawWireSphere(worldEnd, thickness * 0.5f);
        }
    }
}
