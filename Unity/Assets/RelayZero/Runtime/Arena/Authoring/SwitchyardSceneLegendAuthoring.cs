using UnityEngine;

namespace RelayZero.Arena.Authoring
{
    [DisallowMultipleComponent]
    public sealed class SwitchyardSceneLegendAuthoring : MonoBehaviour
    {
        [SerializeField]
        private bool visible = true;

        [SerializeField]
        private Vector2 screenPosition = new Vector2(12f, 12f);

        public bool Visible
        {
            get { return visible; }
        }

        public Vector2 ScreenPosition
        {
            get { return screenPosition; }
        }

        public void Configure(bool show, Vector2 position)
        {
            visible = show;
            screenPosition = position;
        }
    }
}
