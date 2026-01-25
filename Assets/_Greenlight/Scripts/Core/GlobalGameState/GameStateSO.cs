using System.Collections.Generic;
using UnityEngine;
using Greenlight.Core.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Greenlight.Core
{
    /// <summary>
    /// The Global Game State - the single source of truth for Greenlight's reactive world.
    /// Stores all world flags that drive dynamic scene composition, dialogue, and NPC behavior.
    /// 
    /// Design Philosophy:
    /// - Flags represent world events (e.g., "ForestShrine_Cleansed", "Village_BurntDown")
    /// - Any system can query state without tight coupling
    /// - Changes fire events for reactive systems to respond
    /// - Serializes to JSON for cross-platform cloud saves
    /// </summary>
    [CreateAssetMenu(fileName = "MasterGameState", menuName = "Greenlight/Core/Game State")]
    public class GameStateSO : ScriptableObject, ISerializationCallbackReceiver
    {
        [Header("Event Channel")]
        [SerializeField, Tooltip("Event raised whenever any flag changes. Listeners filter by flag key.")]
        private WorldFlagChangedEventSO _onFlagChanged;

        [Header("Debug View (Runtime)")]
        [SerializeField, Tooltip("Current boolean flags (read-only in inspector).")]
        private List<FlagEntry<bool>> _debugBoolFlags = new();

        [SerializeField, Tooltip("Current integer flags (read-only in inspector).")]
        private List<FlagEntry<int>> _debugIntFlags = new();

        [SerializeField, Tooltip("Current string flags (read-only in inspector).")]
        private List<FlagEntry<string>> _debugStringFlags = new();

        // Runtime storage - dictionaries for O(1) lookup
        private Dictionary<string, bool> _boolFlags = new();
        private Dictionary<string, int> _intFlags = new();
        private Dictionary<string, string> _stringFlags = new();

        /// <summary>
        /// Event raised when any flag changes. Can be null if no event channel is assigned.
        /// </summary>
        public WorldFlagChangedEventSO OnFlagChanged => _onFlagChanged;

        #region Boolean Flags

        /// <summary>
        /// Gets a boolean flag value. Returns false if the flag doesn't exist.
        /// </summary>
        /// <param name="key">The flag key (e.g., "Village_House1_Burnt").</param>
        public bool GetBool(string key)
        {
            return _boolFlags.TryGetValue(key, out bool value) && value;
        }

        /// <summary>
        /// Gets a boolean flag value with a custom default.
        /// </summary>
        /// <param name="key">The flag key.</param>
        /// <param name="defaultValue">Value to return if flag doesn't exist.</param>
        public bool GetBool(string key, bool defaultValue)
        {
            return _boolFlags.TryGetValue(key, out bool value) ? value : defaultValue;
        }

        /// <summary>
        /// Sets a boolean flag and raises a change event if the value changed.
        /// </summary>
        /// <param name="key">The flag key.</param>
        /// <param name="value">The new value.</param>
        /// <param name="silent">If true, suppresses the change event.</param>
        public void SetBool(string key, bool value, bool silent = false)
        {
            bool hadValue = _boolFlags.TryGetValue(key, out bool oldValue);
            
            if (!hadValue || oldValue != value)
            {
                _boolFlags[key] = value;
                SyncDebugView();

                if (!silent)
                {
                    _onFlagChanged?.RaiseBool(key, value);
                }
            }
        }

        /// <summary>
        /// Checks if a boolean flag exists (has been set at least once).
        /// </summary>
        public bool HasBool(string key) => _boolFlags.ContainsKey(key);

        #endregion

        #region Integer Flags

        /// <summary>
        /// Gets an integer flag value. Returns 0 if the flag doesn't exist.
        /// </summary>
        public int GetInt(string key)
        {
            return _intFlags.TryGetValue(key, out int value) ? value : 0;
        }

        /// <summary>
        /// Gets an integer flag value with a custom default.
        /// </summary>
        public int GetInt(string key, int defaultValue)
        {
            return _intFlags.TryGetValue(key, out int value) ? value : defaultValue;
        }

        /// <summary>
        /// Sets an integer flag and raises a change event if the value changed.
        /// </summary>
        public void SetInt(string key, int value, bool silent = false)
        {
            bool hadValue = _intFlags.TryGetValue(key, out int oldValue);

            if (!hadValue || oldValue != value)
            {
                _intFlags[key] = value;
                SyncDebugView();

                if (!silent)
                {
                    _onFlagChanged?.RaiseInt(key, value);
                }
            }
        }

        /// <summary>
        /// Increments an integer flag by a delta value.
        /// </summary>
        public void AddInt(string key, int delta, bool silent = false)
        {
            int current = GetInt(key);
            SetInt(key, current + delta, silent);
        }

        /// <summary>
        /// Checks if an integer flag exists.
        /// </summary>
        public bool HasInt(string key) => _intFlags.ContainsKey(key);

        #endregion

        #region String Flags

        /// <summary>
        /// Gets a string flag value. Returns empty string if the flag doesn't exist.
        /// </summary>
        public string GetString(string key)
        {
            return _stringFlags.TryGetValue(key, out string value) ? value : string.Empty;
        }

        /// <summary>
        /// Gets a string flag value with a custom default.
        /// </summary>
        public string GetString(string key, string defaultValue)
        {
            return _stringFlags.TryGetValue(key, out string value) ? value : defaultValue;
        }

        /// <summary>
        /// Sets a string flag and raises a change event if the value changed.
        /// </summary>
        public void SetString(string key, string value, bool silent = false)
        {
            bool hadValue = _stringFlags.TryGetValue(key, out string oldValue);

            if (!hadValue || oldValue != value)
            {
                _stringFlags[key] = value;
                SyncDebugView();

                if (!silent)
                {
                    _onFlagChanged?.RaiseString(key, value);
                }
            }
        }

        /// <summary>
        /// Checks if a string flag exists.
        /// </summary>
        public bool HasString(string key) => _stringFlags.ContainsKey(key);

        #endregion

        #region State Management

        /// <summary>
        /// Clears all flags and resets to a fresh state.
        /// Typically called when starting a new game.
        /// </summary>
        public void ClearAll()
        {
            _boolFlags.Clear();
            _intFlags.Clear();
            _stringFlags.Clear();
            SyncDebugView();
        }

        /// <summary>
        /// Returns read-only access to all boolean flags.
        /// Used by serialization and editor tooling.
        /// </summary>
        public IReadOnlyDictionary<string, bool> BoolFlags => _boolFlags;

        /// <summary>
        /// Returns read-only access to all integer flags.
        /// </summary>
        public IReadOnlyDictionary<string, int> IntFlags => _intFlags;

        /// <summary>
        /// Returns read-only access to all string flags.
        /// </summary>
        public IReadOnlyDictionary<string, string> StringFlags => _stringFlags;

        /// <summary>
        /// Loads flag data from dictionaries (used by StateSerializer during load).
        /// </summary>
        /// <param name="boolFlags">Boolean flags to load.</param>
        /// <param name="intFlags">Integer flags to load.</param>
        /// <param name="stringFlags">String flags to load.</param>
        /// <param name="fireEvents">If true, fires change events for each flag.</param>
        public void LoadFromData(
            Dictionary<string, bool> boolFlags,
            Dictionary<string, int> intFlags,
            Dictionary<string, string> stringFlags,
            bool fireEvents = false)
        {
            _boolFlags = boolFlags ?? new Dictionary<string, bool>();
            _intFlags = intFlags ?? new Dictionary<string, int>();
            _stringFlags = stringFlags ?? new Dictionary<string, string>();
            SyncDebugView();

            if (fireEvents && _onFlagChanged != null)
            {
                foreach (var kvp in _boolFlags)
                    _onFlagChanged.RaiseBool(kvp.Key, kvp.Value);
                foreach (var kvp in _intFlags)
                    _onFlagChanged.RaiseInt(kvp.Key, kvp.Value);
                foreach (var kvp in _stringFlags)
                    _onFlagChanged.RaiseString(kvp.Key, kvp.Value);
            }
        }

        #endregion

        #region Debug View Sync

        /// <summary>
        /// Synchronizes runtime dictionaries to serialized lists for inspector viewing.
        /// Only runs in editor for debugging purposes.
        /// </summary>
        private void SyncDebugView()
        {
#if UNITY_EDITOR
            _debugBoolFlags.Clear();
            foreach (var kvp in _boolFlags)
                _debugBoolFlags.Add(new FlagEntry<bool> { Key = kvp.Key, Value = kvp.Value });

            _debugIntFlags.Clear();
            foreach (var kvp in _intFlags)
                _debugIntFlags.Add(new FlagEntry<int> { Key = kvp.Key, Value = kvp.Value });

            _debugStringFlags.Clear();
            foreach (var kvp in _stringFlags)
                _debugStringFlags.Add(new FlagEntry<string> { Key = kvp.Key, Value = kvp.Value });

            EditorUtility.SetDirty(this);
#endif
        }

        #endregion

        #region Unity Lifecycle & Serialization

        private void OnEnable()
        {
            // Initialize dictionaries if needed
            _boolFlags ??= new Dictionary<string, bool>();
            _intFlags ??= new Dictionary<string, int>();
            _stringFlags ??= new Dictionary<string, string>();

#if UNITY_EDITOR
            // In the editor, we want to clear the state when exiting play mode
            // to prevent "Play Mode State Pollution" where changes in Play Mode
            // persist in the asset.
            if (!Application.isPlaying)
            {
                ClearAll();
            }
#endif
        }

        public void OnBeforeSerialize()
        {
            // Optional: Ensure debug lists are synced before serialization
            // SyncDebugView(); 
        }

        public void OnAfterDeserialize()
        {
            // After deserialization (e.g., when the asset is loaded), 
            // we don't necessarily want to clear flags if they were set in edit mode,
            // but for runtime state, we typically start fresh.
        }

        #endregion
    }

    /// <summary>
    /// Serializable entry for displaying flags in the inspector.
    /// </summary>
    [System.Serializable]
    public struct FlagEntry<T>
    {
        public string Key;
        public T Value;
    }
}
