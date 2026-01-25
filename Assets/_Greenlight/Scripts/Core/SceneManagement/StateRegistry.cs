using System.Collections.Generic;

namespace Greenlight.Core.SceneManagement
{
    /// <summary>
    /// Static registry for all active IStateObserver components in the scene.
    /// Used by SceneInitializer to avoid expensive scene-wide searches.
    /// </summary>
    public static class StateRegistry
    {
        private static readonly List<IStateObserver> _observers = new();

        /// <summary>
        /// Currently active observers.
        /// </summary>
        public static IReadOnlyList<IStateObserver> Observers => _observers;

        /// <summary>
        /// Registers an observer. Called by StateResponder in OnEnable.
        /// </summary>
        public static void Register(IStateObserver observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }

        /// <summary>
        /// Unregisters an observer. Called by StateResponder in OnDisable.
        /// </summary>
        public static void Unregister(IStateObserver observer)
        {
            _observers.Remove(observer);
        }

        /// <summary>
        /// Clears the registry. Typically not needed as observers unregister themselves.
        /// </summary>
        public static void Clear()
        {
            _observers.Clear();
        }
    }
}
