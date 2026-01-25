namespace Greenlight.Core
{
    /// <summary>
    /// Interface for components that contribute to save data.
    /// Used by systems that need to persist state beyond the Global Game State flags.
    /// </summary>
    public interface ISerializableState
    {
        /// <summary>
        /// Returns a unique identifier for this saveable component.
        /// Should be stable across sessions (e.g., based on scene + object name).
        /// </summary>
        string SaveKey { get; }

        /// <summary>
        /// Serializes the component's state to a string (typically JSON).
        /// </summary>
        string SerializeState();

        /// <summary>
        /// Restores the component's state from a previously serialized string.
        /// </summary>
        /// <param name="data">The serialized state data.</param>
        void DeserializeState(string data);
    }
}
