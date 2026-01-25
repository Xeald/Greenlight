using UnityEngine;
using Greenlight.Core.Events;

namespace Greenlight.Core.SceneManagement
{
    /// <summary>
    /// Base class for scene objects that react to Global Game State.
    /// Implements both initial state query (via IStateObserver) and runtime changes (via event subscription).
    /// 
    /// Inherit from this class and override OnStateQueried() and/or OnFlagChanged() to define behavior.
    /// </summary>
    [RequireComponent(typeof(Transform))]
    public abstract class StateResponder : MonoBehaviour, IStateObserver, IGameEventListener<FlagChangePayload>
    {
        [Header("State Connection")]
        [SerializeField, Tooltip("The Global Game State to query and watch.")]
        protected GameStateSO _gameState;

        [SerializeField, Tooltip("Event channel for runtime flag changes.")]
        protected WorldFlagChangedEventSO _onFlagChanged;

        [Header("Flag Filtering")]
        [SerializeField, Tooltip("Flag keys this responder watches. Leave empty to respond to all flags.")]
        protected string[] _watchedFlagKeys;

        [Header("Debug")]
        [SerializeField]
        protected bool _debugLog;

        protected bool _isSubscribed;

        /// <summary>
        /// The flags this responder is watching.
        /// </summary>
        public string[] WatchedFlagKeys
        {
            get => _watchedFlagKeys;
            set => _watchedFlagKeys = value;
        }

        /// <summary>
        /// The Game State this responder queries.
        /// </summary>
        public GameStateSO GameState
        {
            get => _gameState;
            set => _gameState = value;
        }

        protected virtual void OnEnable()
        {
            SubscribeToEvents();
        }

        protected virtual void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        /// <summary>
        /// Subscribes to the flag changed event channel.
        /// </summary>
        protected void SubscribeToEvents()
        {
            if (_onFlagChanged != null && !_isSubscribed)
            {
                _onFlagChanged.RegisterListener(this);
                _isSubscribed = true;
            }
        }

        /// <summary>
        /// Unsubscribes from the flag changed event channel.
        /// </summary>
        protected void UnsubscribeFromEvents()
        {
            if (_onFlagChanged != null && _isSubscribed)
            {
                _onFlagChanged.UnregisterListener(this);
                _isSubscribed = false;
            }
        }

        /// <summary>
        /// Called by SceneInitializer when the scene loads.
        /// Override to configure this object based on current state.
        /// </summary>
        /// <param name="state">The current game state.</param>
        public virtual void OnStateQueried(GameStateSO state)
        {
            _gameState = state;

            if (_debugLog)
            {
                Debug.Log($"[StateResponder] '{gameObject.name}' queried state.");
            }
        }

        /// <summary>
        /// Called when a flag change event is received.
        /// Filters by watched keys before calling OnFlagChanged().
        /// </summary>
        public void OnEventRaised(FlagChangePayload payload)
        {
            // If no filter is set, respond to all
            if (_watchedFlagKeys == null || _watchedFlagKeys.Length == 0)
            {
                OnFlagChanged(payload);
                return;
            }

            // Check if this flag is in our watch list
            foreach (string key in _watchedFlagKeys)
            {
                if (payload.FlagKey == key)
                {
                    OnFlagChanged(payload);
                    return;
                }
            }
        }

        /// <summary>
        /// Called when a watched flag changes at runtime.
        /// Override to react to state changes.
        /// </summary>
        /// <param name="payload">Details about the flag change.</param>
        protected virtual void OnFlagChanged(FlagChangePayload payload)
        {
            if (_debugLog)
            {
                Debug.Log($"[StateResponder] '{gameObject.name}' received flag change: {payload}");
            }
        }

        /// <summary>
        /// Utility method to check if a specific flag key is being watched.
        /// </summary>
        protected bool IsWatching(string flagKey)
        {
            if (_watchedFlagKeys == null || _watchedFlagKeys.Length == 0)
                return true;

            foreach (string key in _watchedFlagKeys)
            {
                if (key == flagKey)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Utility to safely get a bool flag value.
        /// </summary>
        protected bool GetBool(string key, bool defaultValue = false)
        {
            return _gameState != null ? _gameState.GetBool(key, defaultValue) : defaultValue;
        }

        /// <summary>
        /// Utility to safely get an int flag value.
        /// </summary>
        protected int GetInt(string key, int defaultValue = 0)
        {
            return _gameState != null ? _gameState.GetInt(key, defaultValue) : defaultValue;
        }

        /// <summary>
        /// Utility to safely get a string flag value.
        /// </summary>
        protected string GetString(string key, string defaultValue = "")
        {
            return _gameState != null ? _gameState.GetString(key, defaultValue) : defaultValue;
        }
    }
}
