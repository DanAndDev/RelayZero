using UnityEngine;

namespace RelayZero.Arena.Authoring
{
    [DisallowMultipleComponent]
    public sealed class ArenaBoundsAuthoring : ArenaElementAuthoring
    {
        [SerializeField]
        private Vector2 size = new Vector2(28f, 20f);

        public Vector2 Size
        {
            get { return size; }
        }

        public void Configure(string id, Vector2 value)
        {
            ConfigureStableId(id);
            size = value;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = ArenaGizmoPalette.Bounds;
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, 0.05f, size.y));
        }
    }
}
