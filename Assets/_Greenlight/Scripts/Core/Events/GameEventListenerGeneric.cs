using UnityEngine;
using UnityEngine.Events;

namespace Greenlight.Core.Events
{
    /// <summary>
    /// Generic MonoBehaviour that listens to typed GameEventSO and invokes a UnityEvent with payload.
    /// Concrete implementations (WorldFlagChangedListener) provide inspector support.
    /// </summary>
    /// <typeparam name="T">The payload type expected from the event.</typeparam>
    public abstract class GameEventListenerGeneric<T> : MonoBehaviour, IGameEventListener<T>
    {
        [Header("Event Configuration")]
        [SerializeField, Tooltip("The typed event to listen to.")]
        protected GameEventSO<T> _event;

        [SerializeField, Tooltip("Response invoked when the event is raised, receiving the payload.")]
        protected UnityEvent<T> _response;

        /// <summary>
        /// The event this listener is subscribed to.
        /// </summary>
        public GameEventSO<T> Event
        {
            get => _event;
            set
            {
                if (_event != null && enabled)
                {
                    _event.UnregisterListener(this);
                }

                _event = value;

                if (_event != null && enabled)
                {
                    _event.RegisterListener(this);
                }
            }
        }

        protected virtual void OnEnable()
        {
            if (_event != null)
            {
                _event.RegisterListener(this);
            }
        }

        protected virtual void OnDisable()
        {
            if (_event != null)
            {
                _event.UnregisterListener(this);
            }
        }

        /// <summary>
        /// Called by the event when raised. Override to add filtering logic.
        /// </summary>
        public virtual void OnEventRaised(T payload)
        {
            _response?.Invoke(payload);
        }
    }

    /// <summary>
    /// Concrete listener for WorldFlagChangedEventSO with optional flag key filtering.
    /// Only invokes the response if the changed flag matches the filter (or filter is empty).
    /// </summary>
    [AddComponentMenu("Greenlight/Events/World Flag Changed Listener")]
    public class WorldFlagChangedListener : GameEventListenerGeneric<FlagChangePayload>
    {
        [Header("Filtering")]
        [SerializeField, Tooltip("Only respond to changes for these flag keys. Leave empty to respond to all.")]
        private string[] _watchedFlagKeys;

        /// <summary>
        /// Flag keys this listener responds to. Empty array means respond to all flags.
        /// </summary>
        public string[] WatchedFlagKeys
        {
            get => _watchedFlagKeys;
            set => _watchedFlagKeys = value;
        }

        public override void OnEventRaised(FlagChangePayload payload)
        {
            // If no filter is set, respond to all
            if (_watchedFlagKeys == null || _watchedFlagKeys.Length == 0)
            {
                _response?.Invoke(payload);
                return;
            }

            // Check if this flag is in our watch list
            foreach (string key in _watchedFlagKeys)
            {
                if (payload.FlagKey == key)
                {
                    _response?.Invoke(payload);
                    return;
                }
            }
        }
    }
}
