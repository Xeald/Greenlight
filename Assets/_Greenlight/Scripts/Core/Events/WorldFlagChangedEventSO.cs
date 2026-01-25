using UnityEngine;

namespace Greenlight.Core.Events
{
    /// <summary>
    /// Specialized event channel for world flag changes.
    /// Raised by GameStateSO whenever a flag value is modified.
    /// Listeners can filter by flag key to react only to relevant changes.
    /// </summary>
    [CreateAssetMenu(fileName = "OnWorldFlagChanged", menuName = "Greenlight/Events/World Flag Changed Event")]
    public class WorldFlagChangedEventSO : GameEventSO<FlagChangePayload>
    {
        /// <summary>
        /// Convenience method to raise a boolean flag change event.
        /// </summary>
        /// <param name="flagKey">The flag that changed.</param>
        /// <param name="newValue">The new value of the flag.</param>
        public void RaiseBool(string flagKey, bool newValue)
        {
            Raise(FlagChangePayload.Bool(flagKey, newValue));
        }

        /// <summary>
        /// Convenience method to raise an integer flag change event.
        /// </summary>
        /// <param name="flagKey">The flag that changed.</param>
        /// <param name="newValue">The new value of the flag.</param>
        public void RaiseInt(string flagKey, int newValue)
        {
            Raise(FlagChangePayload.Int(flagKey, newValue));
        }

        /// <summary>
        /// Convenience method to raise a string flag change event.
        /// </summary>
        /// <param name="flagKey">The flag that changed.</param>
        /// <param name="newValue">The new value of the flag.</param>
        public void RaiseString(string flagKey, string newValue)
        {
            Raise(FlagChangePayload.String(flagKey, newValue));
        }
    }
}
