using RelayZero.Arena.Baking;
using UnityEditor;
using UnityEngine;

namespace RelayZero.Editor.Arena
{
    public sealed class ArenaValidationWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private GUIStyle titleStyle;
        private GUIStyle cardStyle;
        private GUIStyle pipelineStyle;

        public static bool PreviewEnabled { get; private set; } = true;

        [MenuItem("Relay Zero/Arena/Arena Validator")]
        public static void ShowWindow()
        {
            ArenaValidationWindow window = GetWindow<ArenaValidationWindow>("Arena Validator");
            window.minSize = new Vector2(720f, 540f);
            window.Show();
        }

        private void OnEnable()
        {
            titleStyle = null;
            cardStyle = null;
            pipelineStyle = null;
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawHeader();
            DrawPipeline();
            DrawToolbar();
            DrawComparison();
            DrawResults();
        }

        private void DrawHeader()
        {
            Rect header = GUILayoutUtility.GetRect(10f, 70f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(header, new Color(0.035f, 0.075f, 0.12f));
            GUI.Label(new Rect(header.x + 18f, header.y + 10f, header.width - 36f, 30f), "SWITCHYARD  /  ARENA DATA", titleStyle);
            GUI.Label(
                new Rect(header.x + 20f, header.y + 42f, header.width - 40f, 20f),
                "Deterministic authoring validation, immutable bake, and content hash",
                EditorStyles.miniLabel);
        }

        private void DrawPipeline()
        {
            GUILayout.Space(10f);
            EditorGUILayout.BeginHorizontal();
            DrawPipelineCard("AUTHORING SCENE", "52 stable-ID fixtures");
            GUILayout.Label("→", pipelineStyle, GUILayout.Width(30f));
            DrawPipelineCard("VALIDATION / BAKE", "15 deterministic gates");
            GUILayout.Label("→", pipelineStyle, GUILayout.Width(30f));
            DrawPipelineCard("SIMULATION · AI · PREDICTION", "Transform-free float2 data");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            GUILayout.Space(10f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate", GUILayout.Height(30f)))
            {
                ArenaBakeProcessor.ValidateSwitchyardMenu();
            }

            if (GUILayout.Button("Validate + Bake", GUILayout.Height(30f)))
            {
                ArenaBakeProcessor.ValidateAndBakeSwitchyardMenu();
            }

            if (GUILayout.Button("Select Bake Asset", GUILayout.Height(30f)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<ArenaBakeAsset>(ArenaBakeProcessor.SwitchyardBakeAssetPath);
            }

            bool preview = GUILayout.Toggle(PreviewEnabled, "Scene Preview", "Button", GUILayout.Height(30f), GUILayout.Width(120f));
            if (preview != PreviewEnabled)
            {
                PreviewEnabled = preview;
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawComparison()
        {
            ArenaValidationResult result = ArenaBakeProcessor.LastResult;
            ArenaBakeAsset asset = AssetDatabase.LoadAssetAtPath<ArenaBakeAsset>(ArenaBakeProcessor.SwitchyardBakeAssetPath);
            GUILayout.Space(10f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(cardStyle, GUILayout.MinHeight(92f));
            GUILayout.Label("AUTHORING", EditorStyles.boldLabel);
            GUILayout.Label(result == null || result.Payload == null
                ? "Run validation to inspect the loaded scene."
                : result.Payload.Elements.Length + " elements  ·  " + result.Payload.NavigationNodes.Length + " nav nodes  ·  " +
                  result.Payload.NavigationEdges.Length + " edges");
            GUILayout.Label(ArenaBakeProcessor.SwitchyardScenePath, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(cardStyle, GUILayout.MinHeight(92f));
            GUILayout.Label("IMMUTABLE BAKE", EditorStyles.boldLabel);
            GUILayout.Label(asset == null
                ? "No bake asset exists yet."
                : asset.ElementCount + " elements  ·  " + asset.NavigationNodeCount + " nav nodes  ·  " +
                  asset.NavigationEdgeCount + " edges  ·  0 Transform refs");
            GUILayout.Label(asset == null ? ArenaBakeProcessor.SwitchyardBakeAssetPath : "v" + asset.BakeVersion + "  ·  " + ShortHash(asset.ContentHash), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawResults()
        {
            ArenaValidationResult result = ArenaBakeProcessor.LastResult;
            GUILayout.Space(10f);
            if (result == null)
            {
                EditorGUILayout.HelpBox("Validate the authored Switchyard to populate the shared editor/batch report.", MessageType.Info);
                return;
            }

            MessageType messageType = result.Report.IsValid ? MessageType.Info : MessageType.Error;
            EditorGUILayout.HelpBox(
                result.Report.IsValid
                    ? "CLEAN  ·  " + result.Report.PassedCount + " checks passed  ·  " + ShortHash(result.ContentHash)
                    : "BLOCKED  ·  " + result.Report.FailedCount + " check(s) failed; no bake may be published.",
                messageType);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (ArenaValidationCheck check in result.Report.Checks)
            {
                EditorGUILayout.BeginVertical(cardStyle);
                EditorGUILayout.BeginHorizontal();
                Color previous = GUI.color;
                GUI.color = check.Passed ? new Color(0.35f, 1f, 0.62f) : new Color(1f, 0.35f, 0.35f);
                GUILayout.Label(check.Passed ? "PASS" : "FAIL", EditorStyles.boldLabel, GUILayout.Width(42f));
                GUI.color = previous;
                GUILayout.Label(check.Code + "  " + check.Name, EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
                GUILayout.Label(check.Details, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPipelineCard(string title, string subtitle)
        {
            EditorGUILayout.BeginVertical(cardStyle, GUILayout.Height(58f));
            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.Label(subtitle, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                normal = { textColor = new Color(0.35f, 0.92f, 1f) },
            };
            cardStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(4, 4, 3, 3),
            };
            pipelineStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
            };
        }

        private static string ShortHash(string value)
        {
            return string.IsNullOrEmpty(value) ? "no hash" : value.Substring(0, Mathf.Min(12, value.Length));
        }
    }
}
