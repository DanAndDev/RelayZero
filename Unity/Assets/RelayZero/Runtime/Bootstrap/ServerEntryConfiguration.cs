using UnityEngine;

namespace RelayZero.Bootstrap
{
    [CreateAssetMenu(menuName = "Relay Zero/Server Entry Configuration")]
    public sealed class ServerEntryConfiguration : ScriptableObject
    {
        [SerializeField]
        private string bindAddress = "127.0.0.1";

        [SerializeField]
        private int port = 7777;

        [SerializeField]
        private string controlPlaneEndpoint = "http://127.0.0.1:8080";

        [SerializeField]
        private string environmentName = "Development";

        [SerializeField]
        private bool releaseSecurityGuardActive;

        public string BindAddress
        {
            get { return bindAddress; }
        }

        public int Port
        {
            get { return port; }
        }

        public string ControlPlaneEndpoint
        {
            get { return controlPlaneEndpoint; }
        }

        public string EnvironmentName
        {
            get { return environmentName; }
        }

        public bool ReleaseSecurityGuardActive
        {
            get { return releaseSecurityGuardActive; }
        }
    }
}
