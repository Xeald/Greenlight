using System.Collections.Generic;
using UnityEngine;

namespace Greenlight.Core.Events
{
    /// <summary>
    /// ScriptableObject-based event channel for decoupled communication.
    /// Listeners register at runtime; the event can be raised from any system.
    /// Supports hot-reload testing in the editor without recompilation.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameEvent", menuName = "Greenlight/Events/Game Event")]
    public class GameEventSO : ScriptableObject
    {
        [SerializeField, TextArea(2, 4)]
        private string _description = "Describe what this event signals.";

        /// <summary>
        /// Optional description for designer documentation.
        /// </summary>
        public string Description => _description;

        private readonly List<IGameEventListener> _listeners = new();

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField, Tooltip("Number of times this event has been raised (editor only).")]
        private int _raiseCount;
#endif

        /// <summary>
        /// Raises the event, notifying all registered listeners.
        /// </summary>
        public virtual void Raise()
        {
#if UNITY_EDITOR
            _raiseCount++;
            Debug.Log($"[GameEvent] '{name}' raised. Listeners: {_listeners.Count}");
#endif
            // Iterate backwards to safely handle listeners that unregister during callback
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i].OnEventRaised();
            }
        }

        /// <summary>
        /// Registers a listener to receive event notifications.
        /// </summary>
        public void RegisterListener(IGameEventListener listener)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        /// <summary>
        /// Unregisters a listener from event notifications.
        /// </summary>
        public void UnregisterListener(IGameEventListener listener)
        {
            _listeners.Remove(listener);
        }

        /// <summary>
        /// Returns the current number of registered listeners.
        /// Useful for debugging and editor tooling.
        /// </summary>
        public int ListenerCount => _listeners.Count;

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only: Resets the raise count for debugging purposes.
        /// </summary>
        public void EditorResetRaiseCount() => _raiseCount = 0;
#endif
    }

    /// <summary>
    /// Interface for objects that listen to GameEventSO.
    /// Implement this to receive parameterless event notifications.
    /// </summary>
    public interface IGameEventListener
    {
        void OnEventRaised();
    }
}
