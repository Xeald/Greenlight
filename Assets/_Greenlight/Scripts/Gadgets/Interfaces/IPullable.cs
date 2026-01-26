using UnityEngine;

namespace Greenlight.Gadgets
{
    /// <summary>
    /// Interface for objects that can be pulled by the grappling hook.
    /// Implement on enemies or physics objects.
    /// </summary>
    public interface IPullable
    {
        /// <summary>
        /// Pull this object toward the specified position.
        /// </summary>
        /// <param name="targetPosition">Position to pull toward.</param>
        /// <param name="speed">Pull speed in units per second.</param>
        void PullToward(Vector2 targetPosition, float speed);

        /// <summary>
        /// Called when the pull completes or is interrupted.
        /// </summary>
        void OnPullComplete();
    }
}
