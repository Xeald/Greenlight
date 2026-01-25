using System.Collections.Generic;
using UnityEngine;

namespace Greenlight.Core.Events
{
    /// <summary>
    /// Generic ScriptableObject-based event channel that carries a payload.
    /// Use concrete implementations (e.g., BoolEventSO, IntEventSO) for inspector support.
    /// </summary>
    /// <typeparam name="T">The type of payload this event carries.</typeparam>
    public abstract class GameEventSO<T> : ScriptableObject
    {
        [SerializeField, TextArea(2, 4)]
        private string _description = "Describe what this event signals.";

        private readonly List<IGameEventListener<T>> _listeners = new();

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField, Tooltip("Number of times this event has been raised (editor only).")]
        private int _raiseCount;

        [SerializeField, Tooltip("Last payload value (editor only).")]
        private T _lastPayload;
#endif

        /// <summary>
        /// Raises the event with a payload, notifying all registered listeners.
        /// </summary>
        /// <param name="payload">The data to pass to listeners.</param>
        public virtual void Raise(T payload)
        {
#if UNITY_EDITOR
            _raiseCount++;
            _lastPayload = payload;
            Debug.Log($"[GameEvent<{typeof(T).Name}>] '{name}' raised with payload: {payload}. Listeners: {_listeners.Count}");
#endif
            // Iterate backwards to safely handle listeners that unregister during callback
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i].OnEventRaised(payload);
            }
        }

        /// <summary>
        /// Registers a listener to receive event notifications.
        /// </summary>
        public void RegisterListener(IGameEventListener<T> listener)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        /// <summary>
        /// Unregisters a listener from event notifications.
        /// </summary>
        public void UnregisterListener(IGameEventListener<T> listener)
        {
            _listeners.Remove(listener);
        }

        /// <summary>
        /// Returns the current number of registered listeners.
        /// </summary>
        public int ListenerCount => _listeners.Count;

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only: Returns the last payload for debugging.
        /// </summary>
        public T EditorLastPayload => _lastPayload;

        /// <summary>
        /// Editor-only: Resets the raise count for debugging purposes.
        /// </summary>
        public void EditorResetRaiseCount() => _raiseCount = 0;
#endif
    }

    /// <summary>
    /// Interface for objects that listen to typed GameEventSO.
    /// </summary>
    /// <typeparam name="T">The type of payload expected.</typeparam>
    public interface IGameEventListener<T>
    {
        void OnEventRaised(T payload);
    }
}
