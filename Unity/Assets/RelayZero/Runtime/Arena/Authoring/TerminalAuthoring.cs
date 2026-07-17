using UnityEngine;

namespace RelayZero.Arena.Authoring
{
    [DisallowMultipleComponent]
    public sealed class TerminalAuthoring : ArenaElementAuthoring
    {
        [SerializeField]
        private float interactionRadius = 1.2f;

        public float InteractionRadius
        {
            get { return interactionRadius; }
        }

        public void Configure(string id, float radius)
        {
            ConfigureStableId(id);
            interactionRadius = Mathf.Max(0.05f, radius);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = ArenaGizmoPalette.Terminal;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }
}
