using UnityEngine;

namespace RelayZero.Arena.Authoring
{
    [DisallowMultipleComponent]
    public sealed class ShockGateAuthoring : ArenaElementAuthoring
    {
        [SerializeField]
        private ArenaPowerSide safeWhenPoweredSide;

        [SerializeField]
        private Vector2 triggerSize = new Vector2(0.8f, 3.2f);

        [SerializeField]
        private Vector2 safeSideDirection = Vector2.right;

        public ArenaPowerSide SafeWhenPoweredSide
        {
            get { return safeWhenPoweredSide; }
        }

        public Vector2 TriggerSize
        {
            get { return triggerSize; }
        }

        public Vector2 SafeSideDirection
        {
            get { return safeSideDirection; }
        }

        public void Configure(
            string id,
            ArenaPowerSide side,
            Vector2 size,
            Vector2 safeDirection)
        {
            ConfigureStableId(id);
            safeWhenPoweredSide = side;
            triggerSize = size;
            safeSideDirection = safeDirection.sqrMagnitude > 0f ? safeDirection.normalized : Vector2.right;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = ArenaGizmoPalette.ShockGate;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(triggerSize.x, 0.25f, triggerSize.y));
            Gizmos.DrawLine(
                Vector3.zero,
                new Vector3(safeSideDirection.x, 0f, safeSideDirection.y) * 1.5f);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
