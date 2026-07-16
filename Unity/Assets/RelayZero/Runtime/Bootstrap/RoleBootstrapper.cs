using System;
using System.Collections;
using System.Reflection;
using RelayZero.Foundation;
using UnityEngine;

namespace RelayZero.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class RoleBootstrapper : MonoBehaviour
    {
        private const string SmokeExitArgument = "--rz-smoke-exit";
        private const string ClientRootTypeName = "RelayZero.Client.Application.ClientCompositionRoot, RelayZero.Client.Application";
        private const string ServerRootTypeName = "RelayZero.Server.DedicatedServerCompositionRoot, RelayZero.Server";

        private static RoleBootstrapper activeInstance;

        [SerializeField]
        private BootstrapRoleSelection roleSelection = BootstrapRoleSelection.Auto;

        [SerializeField]
        private bool persistAcrossScenes = true;

        [SerializeField]
        private bool showBuildInfoPanel = true;

        private IApplicationRoot applicationRoot;
        private BuildInfo buildInfo;
        private bool rootStarted;
        private bool shuttingDown;

        public BuildInfo BuildInfo
        {
            get { return buildInfo; }
        }

        public bool IsRootStarted
        {
            get { return rootStarted; }
        }

        public void Configure(
            BootstrapRoleSelection selection,
            bool persist,
            bool showPanel)
        {
            roleSelection = selection;
            persistAcrossScenes = persist;
            showBuildInfoPanel = showPanel;
        }

        private void Awake()
        {
            if (activeInstance != null && activeInstance != this)
            {
                Debug.LogWarning("Duplicate Relay Zero bootstrap root detected; destroying the newer instance.");
                Destroy(gameObject);
                return;
            }

            activeInstance = this;
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            if (activeInstance != this)
            {
                return;
            }

            StartRoot();
        }

        private void OnApplicationQuit()
        {
            shuttingDown = true;
            StopRoot();
        }

        private void OnDestroy()
        {
            if (activeInstance != this)
            {
                return;
            }

            StopRoot();
            activeInstance = null;
        }

        private void OnGUI()
        {
            if (!showBuildInfoPanel || !rootStarted || Application.isBatchMode)
            {
                return;
            }

            GUI.Box(new Rect(16f, 16f, 560f, 72f), "Relay Zero");
            GUI.Label(new Rect(28f, 42f, 536f, 24f), buildInfo.ToDisplayString());
            GUI.Label(new Rect(28f, 62f, 536f, 20f), "Bootstrap root: active");
        }

        private void StartRoot()
        {
            if (rootStarted)
            {
                return;
            }

            ApplicationRole role = ResolveRole(roleSelection);
            buildInfo = GeneratedBuildInfo.ForRole(role);
            applicationRoot = CreateRoot(role);
            applicationRoot.Start();
            rootStarted = true;

            Debug.Log($"Relay Zero shell started: {buildInfo.ToDisplayString()}");

            if (HasArgument(SmokeExitArgument))
            {
                StartCoroutine(QuitAfterSmokeFrame());
            }
        }

        private void StopRoot()
        {
            if (!rootStarted)
            {
                return;
            }

            applicationRoot.Stop();
            Debug.Log($"Relay Zero shell stopped: {buildInfo.ToDisplayString()}");
            rootStarted = false;
        }

        private static ApplicationRole ResolveRole(BootstrapRoleSelection selection)
        {
            if (selection == BootstrapRoleSelection.Client)
            {
                return ApplicationRole.Client;
            }

            if (selection == BootstrapRoleSelection.DedicatedServer)
            {
                return ApplicationRole.DedicatedServer;
            }

#if UNITY_SERVER
            return ApplicationRole.DedicatedServer;
#else
            return ApplicationRole.Client;
#endif
        }

        private static IApplicationRoot CreateRoot(ApplicationRole role)
        {
            string typeName = role == ApplicationRole.DedicatedServer
                ? ServerRootTypeName
                : ClientRootTypeName;

            Type type = Type.GetType(typeName, false);
            if (type == null)
            {
                throw new InvalidOperationException($"Unable to locate composition root type '{typeName}'.");
            }

            MethodInfo factory = type.GetMethod("CreateDefault", BindingFlags.Public | BindingFlags.Static);
            if (factory == null)
            {
                throw new InvalidOperationException($"{type.FullName} must expose public static CreateDefault().");
            }

            object root = factory.Invoke(null, null);
            IApplicationRoot applicationRoot = root as IApplicationRoot;
            if (applicationRoot == null)
            {
                throw new InvalidOperationException($"{type.FullName}.CreateDefault() did not return IApplicationRoot.");
            }

            return applicationRoot;
        }

        private IEnumerator QuitAfterSmokeFrame()
        {
            yield return null;

            if (!shuttingDown)
            {
                Debug.Log("Relay Zero smoke exit requested.");
                StopRoot();
                Application.Quit();
            }
        }

        private static bool HasArgument(string argument)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (string.Equals(arguments[i], argument, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
