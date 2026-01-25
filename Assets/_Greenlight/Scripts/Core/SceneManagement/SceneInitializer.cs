using System.Collections.Generic;
using UnityEngine;
using Greenlight.Core.Events;

namespace Greenlight.Core.SceneManagement
{
    /// <summary>
    /// Queries the Global Game State on scene load and notifies all IStateObserver components.
    /// Place one instance in each scene that has state-reactive objects.
    /// 
    /// Flow:
    /// 1. Scene loads, SceneInitializer.Awake() runs
    /// 2. Finds all IStateObserver components in scene
    /// 3. Calls OnStateQueried() on each, passing the GameStateSO
    /// 4. Fires OnSceneInitialized event for other systems
    /// </summary>
    [AddComponentMenu("Greenlight/Scene Management/Scene Initializer")]
    [DefaultExecutionOrder(-100)] // Run early to initialize state before other scripts
    public class SceneInitializer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("The Global Game State to query.")]
        private GameStateSO _gameState;

        [SerializeField, Tooltip("Event fired when scene initialization is complete.")]
        private GameEventSO _onSceneInitialized;

        [Header("Configuration")]
        [SerializeField, Tooltip("If true, automatically initialize on Awake. Disable for manual control.")]
        private bool _initializeOnAwake = true;

        [SerializeField, Tooltip("If true, find observers in child objects only. If false, search entire scene.")]
        private bool _childrenOnly;

        [Header("Debug")]
        [SerializeField, Tooltip("Log initialization details to console.")]
        private bool _debugLog = true;

        private List<IStateObserver> _observers = new();
        private bool _initialized;

        /// <summary>
        /// The Global Game State this initializer queries.
        /// </summary>
        public GameStateSO GameState => _gameState;

        /// <summary>
        /// Whether this scene has been initialized.
        /// </summary>
        public bool IsInitialized => _initialized;

        private void Awake()
        {
            if (_initializeOnAwake)
            {
                InitializeScene();
            }
        }

        /// <summary>
        /// Manually triggers scene initialization.
        /// Can be called multiple times to re-query state (e.g., after loading a save).
        /// </summary>
        public void InitializeScene()
        {
            if (_gameState == null)
            {
                Debug.LogError($"[SceneInitializer] No GameStateSO assigned on '{gameObject.name}'.");
                return;
            }

            FindObservers();
            NotifyObservers();

            _initialized = true;

            if (_debugLog)
            {
                Debug.Log($"[SceneInitializer] Scene '{gameObject.scene.name}' initialized. " +
                         $"Notified {_observers.Count} observers.");
            }

            _onSceneInitialized?.Raise();
        }

        /// <summary>
        /// Finds all IStateObserver components in the scene or hierarchy.
        /// </summary>
        private void FindObservers()
        {
            _observers.Clear();

            if (_childrenOnly)
            {
                // Only search children of this GameObject
                var childObservers = GetComponentsInChildren<IStateObserver>(includeInactive: true);
                _observers.AddRange(childObservers);
            }
            else
            {
                // Search entire scene
                var allObservers = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var mb in allObservers)
                {
                    if (mb is IStateObserver observer)
                    {
                        _observers.Add(observer);
                    }
                }
            }
        }

        /// <summary>
        /// Notifies all found observers of the current state.
        /// </summary>
        private void NotifyObservers()
        {
            foreach (var observer in _observers)
            {
                try
                {
                    observer.OnStateQueried(_gameState);
                }
                catch (System.Exception ex)
                {
                    var mb = observer as MonoBehaviour;
                    string objName = mb != null ? mb.gameObject.name : "Unknown";
                    Debug.LogError($"[SceneInitializer] Observer on '{objName}' threw exception: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Registers an observer manually (for dynamically spawned objects).
        /// </summary>
        /// <param name="observer">The observer to register.</param>
        /// <param name="notifyImmediately">If true and scene is initialized, queries state immediately.</param>
        public void RegisterObserver(IStateObserver observer, bool notifyImmediately = true)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);

                if (notifyImmediately && _initialized && _gameState != null)
                {
                    observer.OnStateQueried(_gameState);
                }
            }
        }

        /// <summary>
        /// Unregisters an observer (for objects being destroyed).
        /// </summary>
        public void UnregisterObserver(IStateObserver observer)
        {
            _observers.Remove(observer);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor button to manually trigger re-initialization during play mode.
        /// </summary>
        [ContextMenu("Re-initialize Scene")]
        private void EditorReinitialize()
        {
            if (Application.isPlaying)
            {
                InitializeScene();
            }
            else
            {
                Debug.LogWarning("[SceneInitializer] Re-initialization only works in Play mode.");
            }
        }
#endif
    }
}
