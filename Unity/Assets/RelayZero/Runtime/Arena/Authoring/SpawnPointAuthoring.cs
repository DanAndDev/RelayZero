using UnityEngine;

namespace RelayZero.Arena.Authoring
{
    [DisallowMultipleComponent]
    public sealed class SpawnPointAuthoring : ArenaElementAuthoring
    {
        [SerializeField]
        private int playerSlot;

        [SerializeField]
        private Vector2 facingDirection = Vector2.down;

        public int PlayerSlot
        {
            get { return playerSlot; }
        }

        public Vector2 FacingDirection
        {
            get { return facingDirection; }
        }

        public void Configure(string id, int slot, Vector2 facing)
        {
            ConfigureStableId(id);
            playerSlot = Mathf.Max(0, slot);
            facingDirection = facing.sqrMagnitude > 0f ? facing.normalized : Vector2.down;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = ArenaGizmoPalette.Spawn;
            Gizmos.DrawWireSphere(transform.position, 0.45f);
            Gizmos.DrawLine(
                transform.position,
                transform.position + new Vector3(facingDirection.x, 0f, facingDirection.y) * 1.25f);
        }
    }
}
