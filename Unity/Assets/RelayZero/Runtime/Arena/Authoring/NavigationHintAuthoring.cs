using UnityEngine;

namespace RelayZero.Arena.Authoring
{
    public enum NavigationHintKind
    {
        Spawn,
        Core,
        Relay,
        Terminal,
        LaneJunction,
        PylonCorner,
    }

    [DisallowMultipleComponent]
    public sealed class NavigationHintAuthoring : ArenaElementAuthoring
    {
        [SerializeField]
        private NavigationHintKind kind;

        [SerializeField]
        private float connectionRadius = 8f;

        public NavigationHintKind Kind
        {
            get { return kind; }
        }

        public float ConnectionRadius
        {
            get { return connectionRadius; }
        }

        public void Configure(string id, NavigationHintKind hintKind, float radius)
        {
            ConfigureStableId(id);
            kind = hintKind;
            connectionRadius = Mathf.Max(0.1f, radius);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = ArenaGizmoPalette.Navigation;
            Gizmos.DrawWireSphere(transform.position, 0.18f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.75f);
        }
    }
}
