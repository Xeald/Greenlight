using UnityEngine;

namespace Greenlight.Core.Physics
{
    /// <summary>
    /// Interface for components that can check for collisions before movement.
    /// Allows systems like knockback and gadgets to respect collision detection
    /// without needing direct assembly references to player-specific code.
    /// </summary>
    public interface ICollisionChecker
    {
        /// <summary>
        /// Checks if the given movement delta would result in a collision.
        /// </summary>
        /// <param name="delta">The movement vector to check.</param>
        /// <returns>True if movement is blocked by a collision.</returns>
        bool CheckCollision(Vector2 delta);
    }
}
