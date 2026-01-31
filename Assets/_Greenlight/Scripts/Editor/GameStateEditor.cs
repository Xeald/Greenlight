using UnityEngine;
using UnityEditor;
using Greenlight.Core;
using Greenlight.Core.Events;
using Greenlight.Core.SceneManagement;
using System.Collections.Generic;
using System.Linq;

namespace Greenlight.Editor
{
    /// <summary>
    /// Custom editor for GameStateSO that provides:
    /// - Real-time flag visualization
    /// - One-click flag toggling for testing
    /// - Manual event firing for hot-reload testing
    /// - Save/Load preview functionality
    /// </summary>
    [CustomEditor(typeof(GameStateSO))]
    public class GameStateEditor : UnityEditor.Editor
    {
        private GameStateSO _target;
        private bool _showBoolFlags = true;
        private bool _showIntFlags = true;
        private bool _showStringFlags = true;
        private bool _showDebugTools = true;

        private string _newFlagKey = "";
        private FlagType _newFlagType = FlagType.Bool;
        private bool _newBoolValue;
        private int _newIntValue;
        private string _newStringValue = "";

        private Vector2 _scrollPosition;

        private void OnEnable()
        {
            _target = (GameStateSO)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawEditorHeader();
            DrawEventChannelField();

            EditorGUILayout.Space(10);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (Application.isPlaying)
            {
                DrawRuntimeFlagViewer();
                DrawDebugTools();
            }
            else
            {
                DrawEditModeMessage();
            }

            EditorGUILayout.EndScrollView();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawEditorHeader()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Global Game State", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("LIVE", EditorStyles.miniLabel, GUILayout.Width(40));
                GUI.color = Color.white;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "The single source of truth for Greenlight's reactive world. " +
                "All world flags are stored here and propagated via events.",
                MessageType.Info);
        }

        private void DrawEventChannelField()
        {
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty("_onFlagChanged"),
                new GUIContent("Flag Changed Event", "Event raised when any flag changes."));
        }

        private void DrawEditModeMessage()
        {
            EditorGUILayout.HelpBox(
                "Enter Play Mode to view and modify flags in real-time.\n\n" +
                "The debug tools below allow hot-reload testing without recompilation.",
                MessageType.Warning);
        }

        private void DrawRuntimeFlagViewer()
        {
            EditorGUILayout.LabelField("Current Flags", EditorStyles.boldLabel);

            // Bool Flags
            _showBoolFlags = EditorGUILayout.BeginFoldoutHeaderGroup(_showBoolFlags,
                $"Boolean Flags ({_target.BoolFlags.Count})");

            if (_showBoolFlags)
            {
                EditorGUI.indentLevel++;
                foreach (var kvp in _target.BoolFlags.OrderBy(x => x.Key).ToList())
                {
                    DrawBoolFlag(kvp.Key, kvp.Value);
                }

                if (_target.BoolFlags.Count == 0)
                {
                    EditorGUILayout.LabelField("(no boolean flags set)", EditorStyles.miniLabel);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            // Int Flags
            _showIntFlags = EditorGUILayout.BeginFoldoutHeaderGroup(_showIntFlags,
                $"Integer Flags ({_target.IntFlags.Count})");

            if (_showIntFlags)
            {
                EditorGUI.indentLevel++;
                foreach (var kvp in _target.IntFlags.OrderBy(x => x.Key).ToList())
                {
                    DrawIntFlag(kvp.Key, kvp.Value);
                }

                if (_target.IntFlags.Count == 0)
                {
                    EditorGUILayout.LabelField("(no integer flags set)", EditorStyles.miniLabel);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            // String Flags
            _showStringFlags = EditorGUILayout.BeginFoldoutHeaderGroup(_showStringFlags,
                $"String Flags ({_target.StringFlags.Count})");

            if (_showStringFlags)
            {
                EditorGUI.indentLevel++;
                foreach (var kvp in _target.StringFlags.OrderBy(x => x.Key).ToList())
                {
                    DrawStringFlag(kvp.Key, kvp.Value);
                }

                if (_target.StringFlags.Count == 0)
                {
                    EditorGUILayout.LabelField("(no string flags set)", EditorStyles.miniLabel);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void DrawBoolFlag(string key, bool value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(key, GUILayout.MinWidth(150));

            bool newValue = EditorGUILayout.Toggle(value, GUILayout.Width(20));
            if (newValue != value)
            {
                _target.SetBool(key, newValue);
            }

            GUI.color = Color.red;
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                _target.RemoveBool(key);
            }

            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawIntFlag(string key, int value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(key, GUILayout.MinWidth(150));

            int newValue = EditorGUILayout.IntField(value, GUILayout.Width(80));
            if (newValue != value)
            {
                _target.SetInt(key, newValue);
            }

            GUI.color = Color.red;
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                _target.RemoveInt(key);
            }

            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStringFlag(string key, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(key, GUILayout.MinWidth(150));

            string newValue = EditorGUILayout.TextField(value, GUILayout.MinWidth(100));
            if (newValue != value)
            {
                _target.SetString(key, newValue);
            }

            GUI.color = Color.red;
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                _target.RemoveString(key);
            }

            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawDebugTools()
        {
            EditorGUILayout.Space(10);

            _showDebugTools = EditorGUILayout.BeginFoldoutHeaderGroup(_showDebugTools, "Debug Tools");

            if (_showDebugTools)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Add New Flag
                EditorGUILayout.LabelField("Add New Flag", EditorStyles.boldLabel);

                _newFlagKey = EditorGUILayout.TextField("Key", _newFlagKey);
                _newFlagType = (FlagType)EditorGUILayout.EnumPopup("Type", _newFlagType);

                switch (_newFlagType)
                {
                    case FlagType.Bool:
                        _newBoolValue = EditorGUILayout.Toggle("Value", _newBoolValue);
                        break;
                    case FlagType.Int:
                        _newIntValue = EditorGUILayout.IntField("Value", _newIntValue);
                        break;
                    case FlagType.String:
                        _newStringValue = EditorGUILayout.TextField("Value", _newStringValue);
                        break;
                }

                GUI.enabled = !string.IsNullOrEmpty(_newFlagKey);
                if (GUILayout.Button("Add Flag"))
                {
                    AddNewFlag();
                }

                GUI.enabled = true;

                EditorGUILayout.Space(10);

                // Quick Actions
                EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Clear All Flags"))
                {
                    if (EditorUtility.DisplayDialog("Clear All Flags",
                        "This will remove all flags from the game state. Are you sure?",
                        "Clear", "Cancel"))
                    {
                        _target.ClearAll();
                    }
                }

                if (GUILayout.Button("Log State JSON"))
                {
                    string json = StateSerializer.Serialize(_target);
                    Debug.Log($"[GameStateEditor] Current State:\n{json}");
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // Scene Re-initialization
                if (GUILayout.Button("Re-initialize All Scene Observers"))
                {
                    var initializers = Object.FindObjectsByType<SceneInitializer>(
                        FindObjectsInactive.Exclude, FindObjectsSortMode.None);

                    foreach (var init in initializers)
                    {
                        init.InitializeScene();
                    }

                    Debug.Log($"[GameStateEditor] Re-initialized {initializers.Length} scene(s).");
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private void AddNewFlag()
        {
            switch (_newFlagType)
            {
                case FlagType.Bool:
                    _target.SetBool(_newFlagKey, _newBoolValue);
                    break;
                case FlagType.Int:
                    _target.SetInt(_newFlagKey, _newIntValue);
                    break;
                case FlagType.String:
                    _target.SetString(_newFlagKey, _newStringValue);
                    break;
            }

            Debug.Log($"[GameStateEditor] Added flag: {_newFlagKey} ({_newFlagType})");

            // Reset form
            _newFlagKey = "";
            _newBoolValue = false;
            _newIntValue = 0;
            _newStringValue = "";
        }
    }
}
