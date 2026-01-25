using UnityEngine;
using UnityEditor;
using Greenlight.Core;
using Greenlight.Core.Events;

namespace Greenlight.Editor
{
    /// <summary>
    /// Custom editor for WorldFlagDefinitionSO that provides:
    /// - Visual feedback based on flag type
    /// - Quick test functionality
    /// - Integration with GameStateSO
    /// </summary>
    [CustomEditor(typeof(WorldFlagDefinitionSO))]
    public class WorldFlagDefinitionEditor : UnityEditor.Editor
    {
        private WorldFlagDefinitionSO _target;

        private void OnEnable()
        {
            _target = (WorldFlagDefinitionSO)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawEditorHeader();
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            DrawTestTools();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawEditorHeader()
        {
            EditorGUILayout.BeginHorizontal();

            // Color-coded category indicator
            GUI.color = GetCategoryColor(_target.Category);
            EditorGUILayout.LabelField("", GUILayout.Width(10), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            GUI.color = Color.white;

            EditorGUILayout.LabelField($"Flag: {_target.FlagKey}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTestTools()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test this flag against a GameStateSO.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Test Tools", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Find all GameStateSO in project
            var gameStates = FindGameStateAssets();

            if (gameStates.Length == 0)
            {
                EditorGUILayout.HelpBox("No GameStateSO assets found in project.", MessageType.Warning);
            }
            else
            {
                foreach (var gs in gameStates)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(gs.name, GUILayout.MinWidth(100));

                    if (GUILayout.Button("Apply Default", GUILayout.Width(100)))
                    {
                        _target.ApplyDefault(gs, silent: false);
                        Debug.Log($"[FlagDefinition] Applied default for '{_target.FlagKey}' to '{gs.name}'");
                    }

                    if (GUILayout.Button("Query", GUILayout.Width(60)))
                    {
                        LogCurrentValue(gs);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void LogCurrentValue(GameStateSO state)
        {
            string value;
            switch (_target.FlagType)
            {
                case FlagType.Bool:
                    value = state.GetBool(_target.FlagKey).ToString();
                    break;
                case FlagType.Int:
                    value = state.GetInt(_target.FlagKey).ToString();
                    break;
                case FlagType.String:
                    value = $"\"{state.GetString(_target.FlagKey)}\"";
                    break;
                default:
                    value = "Unknown";
                    break;
            }

            Debug.Log($"[FlagDefinition] '{_target.FlagKey}' in '{state.name}' = {value}");
        }

        private GameStateSO[] FindGameStateAssets()
        {
            var guids = AssetDatabase.FindAssets("t:GameStateSO");
            var results = new GameStateSO[guids.Length];

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                results[i] = AssetDatabase.LoadAssetAtPath<GameStateSO>(path);
            }

            return results;
        }

        private Color GetCategoryColor(FlagCategory category)
        {
            return category switch
            {
                FlagCategory.World => new Color(0.4f, 0.8f, 0.4f),   // Green
                FlagCategory.Story => new Color(0.8f, 0.6f, 0.2f),   // Orange
                FlagCategory.Gadgets => new Color(0.4f, 0.6f, 0.9f), // Blue
                FlagCategory.Player => new Color(0.9f, 0.4f, 0.4f),  // Red
                FlagCategory.System => new Color(0.7f, 0.7f, 0.7f),  // Gray
                _ => Color.white
            };
        }
    }
}
