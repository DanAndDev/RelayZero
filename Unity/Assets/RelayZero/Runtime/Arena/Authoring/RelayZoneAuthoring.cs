using UnityEngine;

namespace RelayZero.Arena.Authoring
{
    public enum ArenaPowerSide
    {
        Alpha,
        Beta,
    }

    [DisallowMultipleComponent]
    public sealed class RelayZoneAuthoring : ArenaElementAuthoring
    {
        [SerializeField]
        private ArenaPowerSide side;

        [SerializeField]
        private float radius = 2.4f;

        public ArenaPowerSide Side
        {
            get { return side; }
        }

        public float Radius
        {
            get { return radius; }
        }

        public void Configure(string id, ArenaPowerSide powerSide, float zoneRadius)
        {
            ConfigureStableId(id);
            side = powerSide;
            radius = Mathf.Max(0.05f, zoneRadius);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = side == ArenaPowerSide.Alpha
                ? ArenaGizmoPalette.RelayAlpha
                : ArenaGizmoPalette.RelayBeta;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
