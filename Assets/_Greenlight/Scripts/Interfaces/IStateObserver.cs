namespace Greenlight.Core
{
    /// <summary>
    /// Interface for objects that react to Global Game State queries.
    /// Implemented by scene objects that need to configure themselves based on world flags.
    /// </summary>
    public interface IStateObserver
    {
        /// <summary>
        /// Called by SceneInitializer when the scene loads, allowing the object
        /// to query the current state and configure itself accordingly.
        /// </summary>
        /// <param name="state">The global game state to query.</param>
        void OnStateQueried(GameStateSO state);
    }
}
