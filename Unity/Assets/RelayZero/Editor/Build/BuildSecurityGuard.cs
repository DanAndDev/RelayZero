using UnityEngine;

namespace RelayZero.Editor.Build
{
    public static class BuildSecurityGuard
    {
        public static void Validate(RelayZeroBuildProfile profile)
        {
            if (profile == RelayZeroBuildProfile.ClientRelease ||
                profile == RelayZeroBuildProfile.ServerRelease)
            {
                Debug.Log("Relay Zero release security guard is inactive until hosted security configuration exists.");
            }
        }
    }
}
