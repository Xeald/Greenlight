using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Greenlight.Core
{
    /// <summary>
    /// Handles serialization and deserialization of GameStateSO to JSON.
    /// Designed for cross-platform cloud saves (PC/Android).
    /// 
    /// Uses Newtonsoft.Json for robust, cross-platform serialization in Unity.
    /// </summary>
    public static class StateSerializer
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            Formatting = Formatting.None,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        /// Serializes the GameStateSO to a JSON string.
        /// </summary>
        /// <param name="state">The game state to serialize.</param>
        /// <returns>JSON string representation of the state.</returns>
        public static string Serialize(GameStateSO state)
        {
            if (state == null)
            {
                Debug.LogError("[StateSerializer] Cannot serialize null GameStateSO.");
                return null;
            }

            try
            {
                var saveData = new SaveData
                {
                    Version = SaveData.CurrentVersion,
                    Timestamp = DateTime.UtcNow.ToString("o"), // ISO 8601 format
                    BoolFlags = new Dictionary<string, bool>(state.BoolFlags),
                    IntFlags = new Dictionary<string, int>(state.IntFlags),
                    StringFlags = new Dictionary<string, string>(state.StringFlags)
                };

                return JsonConvert.SerializeObject(saveData, _jsonSettings);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StateSerializer] Serialization failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deserializes a JSON string and loads it into the GameStateSO.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <param name="state">The game state to load data into.</param>
        /// <param name="fireEvents">If true, fires change events for each loaded flag.</param>
        /// <returns>True if deserialization succeeded, false otherwise.</returns>
        public static bool Deserialize(string json, GameStateSO state, bool fireEvents = false)
        {
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[StateSerializer] Cannot deserialize empty JSON.");
                return false;
            }

            if (state == null)
            {
                Debug.LogError("[StateSerializer] Cannot deserialize into null GameStateSO.");
                return false;
            }

            try
            {
                var saveData = JsonConvert.DeserializeObject<SaveData>(json, _jsonSettings);

                if (saveData == null)
                {
                    Debug.LogError("[StateSerializer] Deserialization returned null.");
                    return false;
                }

                // Version migration (future-proofing)
                if (saveData.Version < SaveData.CurrentVersion)
                {
                    saveData = MigrateSaveData(saveData);
                }

                state.LoadFromData(
                    saveData.BoolFlags ?? new Dictionary<string, bool>(),
                    saveData.IntFlags ?? new Dictionary<string, int>(),
                    saveData.StringFlags ?? new Dictionary<string, string>(),
                    fireEvents
                );

                Debug.Log($"[StateSerializer] Loaded save from {saveData.Timestamp}. " +
                         $"Flags: {saveData.BoolFlags?.Count ?? 0} bool, " +
                         $"{saveData.IntFlags?.Count ?? 0} int, " +
                         $"{saveData.StringFlags?.Count ?? 0} string.");

                return true;
            }
            catch (JsonException ex)
            {
                Debug.LogError($"[StateSerializer] JSON parsing failed: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StateSerializer] Deserialization failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Migrates save data from an older version to the current version.
        /// Override this method to handle breaking changes in save format.
        /// </summary>
        private static SaveData MigrateSaveData(SaveData oldData)
        {
            Debug.Log($"[StateSerializer] Migrating save from v{oldData.Version} to v{SaveData.CurrentVersion}.");

            // Version 1 -> 2 migration example (when needed):
            // if (oldData.Version == 1)
            // {
            //     // Rename flags, convert types, etc.
            //     if (oldData.BoolFlags.TryGetValue("OldFlagName", out bool value))
            //     {
            //         oldData.BoolFlags.Remove("OldFlagName");
            //         oldData.BoolFlags["NewFlagName"] = value;
            //     }
            // }

            oldData.Version = SaveData.CurrentVersion;
            return oldData;
        }

        /// <summary>
        /// Creates an empty save data structure (for new games).
        /// </summary>
        public static string CreateEmptySave()
        {
            var saveData = new SaveData
            {
                Version = SaveData.CurrentVersion,
                Timestamp = DateTime.UtcNow.ToString("o"),
                BoolFlags = new Dictionary<string, bool>(),
                IntFlags = new Dictionary<string, int>(),
                StringFlags = new Dictionary<string, string>()
            };

            return JsonConvert.SerializeObject(saveData, _jsonSettings);
        }

        /// <summary>
        /// Validates a JSON string without loading it.
        /// Useful for checking save file integrity.
        /// </summary>
        public static bool ValidateJson(string json, out int version)
        {
            version = 0;

            if (string.IsNullOrEmpty(json))
                return false;

            try
            {
                var saveData = JsonConvert.DeserializeObject<SaveData>(json, _jsonSettings);
                if (saveData != null)
                {
                    version = saveData.Version;
                    return true;
                }
            }
            catch
            {
                // Invalid JSON
            }

            return false;
        }
    }

    /// <summary>
    /// Internal data structure for save file serialization. 
    /// Flat structure optimized for cloud save diff/merge operations.
    /// </summary>
    [Serializable]
    internal class SaveData
    {
        /// <summary>
        /// Current save format version. Increment when making breaking changes.
        /// </summary>
        public const int CurrentVersion = 1;

        /// <summary>
        /// Save format version for migration compatibility.
        /// </summary>
        [JsonProperty("version")]
        public int Version { get; set; }

        /// <summary>
        /// UTC timestamp when save was created (ISO 8601 format).
        /// </summary>
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        /// <summary>
        /// All boolean world flags.
        /// </summary>
        [JsonProperty("boolFlags")]
        public Dictionary<string, bool> BoolFlags { get; set; }

        /// <summary>
        /// All integer world flags.
        /// </summary>
        [JsonProperty("intFlags")]
        public Dictionary<string, int> IntFlags { get; set; }

        /// <summary>
        /// All string world flags.
        /// </summary>
        [JsonProperty("stringFlags")]
        public Dictionary<string, string> StringFlags { get; set; }
    }
}
