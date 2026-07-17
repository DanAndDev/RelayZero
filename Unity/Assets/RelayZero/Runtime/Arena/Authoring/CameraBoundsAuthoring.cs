using UnityEngine;

namespace RelayZero.Arena.Authoring
{
    [DisallowMultipleComponent]
    public sealed class CameraBoundsAuthoring : ArenaElementAuthoring
    {
        [SerializeField]
        private Vector2 size = new Vector2(30f, 22f);

        [SerializeField]
        private float orthographicSize = 11.5f;

        public Vector2 Size
        {
            get { return size; }
        }

        public float OrthographicSize
        {
            get { return orthographicSize; }
        }

        public void Configure(string id, Vector2 value, float cameraOrthographicSize)
        {
            ConfigureStableId(id);
            size = value;
            orthographicSize = Mathf.Max(0.1f, cameraOrthographicSize);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = ArenaGizmoPalette.CameraBounds;
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, 0.08f, size.y));
        }
    }
}
