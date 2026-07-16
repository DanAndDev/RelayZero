using UnityEngine;

namespace RelayZero.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class RelayZeroSceneMarker : MonoBehaviour
    {
        [SerializeField]
        private RelayZeroSceneKind sceneKind;

        [SerializeField]
        private string sceneId;

        public RelayZeroSceneKind SceneKind
        {
            get { return sceneKind; }
        }

        public string SceneId
        {
            get { return sceneId; }
        }

        public void Configure(
            RelayZeroSceneKind kind,
            string id)
        {
            sceneKind = kind;
            sceneId = id;
        }
    }
}
