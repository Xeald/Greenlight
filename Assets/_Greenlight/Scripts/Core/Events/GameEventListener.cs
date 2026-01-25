using UnityEngine;
using UnityEngine.Events;

namespace Greenlight.Core.Events
{
    /// <summary>
    /// MonoBehaviour that listens to a GameEventSO and invokes a UnityEvent response.
    /// Attach to scene objects that need to react to game events without direct coupling.
    /// 
    /// Usage:
    /// 1. Create a GameEventSO asset (or use existing)
    /// 2. Add this component to a GameObject
    /// 3. Assign the event in the inspector
    /// 4. Configure the Response UnityEvent (e.g., call methods on other components)
    /// </summary>
    [AddComponentMenu("Greenlight/Events/Game Event Listener")]
    public class GameEventListener : MonoBehaviour, IGameEventListener
    {
        [Header("Event Configuration")]
        [SerializeField, Tooltip("The event to listen to.")]
        private GameEventSO _event;

        [SerializeField, Tooltip("Response invoked when the event is raised.")]
        private UnityEvent _response;

        /// <summary>
        /// The event this listener is subscribed to.
        /// </summary>
        public GameEventSO Event
        {
            get => _event;
            set
            {
                // Unregister from old event
                if (_event != null && enabled)
                {
                    _event.UnregisterListener(this);
                }

                _event = value;

                // Register with new event
                if (_event != null && enabled)
                {
                    _event.RegisterListener(this);
                }
            }
        }

        private void OnEnable()
        {
            if (_event != null)
            {
                _event.RegisterListener(this);
            }
        }

        private void OnDisable()
        {
            if (_event != null)
            {
                _event.UnregisterListener(this);
            }
        }

        /// <summary>
        /// Called by the GameEventSO when the event is raised.
        /// </summary>
        public void OnEventRaised()
        {
            _response?.Invoke();
        }
    }
}
