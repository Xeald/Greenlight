using System;

namespace Greenlight.Core.Events
{
    /// <summary>
    /// Payload structure for world flag change events.
    /// Contains all information needed for listeners to react to state changes.
    /// </summary>
    [Serializable]
    public struct FlagChangePayload
    {
        /// <summary>
        /// The unique key of the flag that changed (e.g., "Village_House1_Burnt").
        /// </summary>
        public string FlagKey;

        /// <summary>
        /// The type of the flag value (Bool, Int, String).
        /// </summary>
        public FlagType Type;

        /// <summary>
        /// The new boolean value (only valid when Type == Bool).
        /// </summary>
        public bool BoolValue;

        /// <summary>
        /// The new integer value (only valid when Type == Int).
        /// </summary>
        public int IntValue;

        /// <summary>
        /// The new string value (only valid when Type == String).
        /// </summary>
        public string StringValue;

        /// <summary>
        /// Creates a payload for a boolean flag change.
        /// </summary>
        public static FlagChangePayload Bool(string key, bool value) => new()
        {
            FlagKey = key,
            Type = FlagType.Bool,
            BoolValue = value
        };

        /// <summary>
        /// Creates a payload for an integer flag change.
        /// </summary>
        public static FlagChangePayload Int(string key, int value) => new()
        {
            FlagKey = key,
            Type = FlagType.Int,
            IntValue = value
        };

        /// <summary>
        /// Creates a payload for a string flag change.
        /// </summary>
        public static FlagChangePayload String(string key, string value) => new()
        {
            FlagKey = key,
            Type = FlagType.String,
            StringValue = value
        };

        public override string ToString()
        {
            return Type switch
            {
                FlagType.Bool => $"{FlagKey} = {BoolValue}",
                FlagType.Int => $"{FlagKey} = {IntValue}",
                FlagType.String => $"{FlagKey} = \"{StringValue}\"",
                _ => $"{FlagKey} (unknown type)"
            };
        }
    }

    /// <summary>
    /// Enumeration of supported flag value types.
    /// </summary>
    public enum FlagType
    {
        Bool,
        Int,
        String
    }
}
