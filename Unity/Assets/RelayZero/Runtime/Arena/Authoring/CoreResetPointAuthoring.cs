using UnityEngine;

namespace RelayZero.Arena.Authoring
{
    [DisallowMultipleComponent]
    public sealed class CoreResetPointAuthoring : ArenaElementAuthoring
    {
        [SerializeField]
        private float pedestalRadius = 0.65f;

        public float PedestalRadius
        {
            get { return pedestalRadius; }
        }

        public void Configure(string id, float radius)
        {
            ConfigureStableId(id);
            pedestalRadius = Mathf.Max(0.05f, radius);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = ArenaGizmoPalette.CoreReset;
            Gizmos.DrawWireSphere(transform.position, pedestalRadius);
            Gizmos.DrawLine(transform.position + Vector3.left, transform.position + Vector3.right);
            Gizmos.DrawLine(transform.position + Vector3.back, transform.position + Vector3.forward);
        }
    }
}
