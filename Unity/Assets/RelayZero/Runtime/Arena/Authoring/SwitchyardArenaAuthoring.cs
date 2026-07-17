using UnityEngine;

namespace RelayZero.Arena.Authoring
{
    [DisallowMultipleComponent]
    public sealed class SwitchyardArenaAuthoring : ArenaElementAuthoring
    {
        [SerializeField]
        private int authoringVersion = 1;

        public int AuthoringVersion
        {
            get { return authoringVersion; }
        }

        public void Configure(string id, int version)
        {
            ConfigureStableId(id);
            authoringVersion = Mathf.Max(1, version);
        }
    }
}
