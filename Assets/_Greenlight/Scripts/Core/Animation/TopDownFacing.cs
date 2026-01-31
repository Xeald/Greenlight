using UnityEngine;

namespace Greenlight.Core.Animation
{
    /// <summary>
    /// Cardinal direction for top-down character facing.
    /// Used to determine which directional animation state to play.
    /// </summary>
    public enum FacingCardinal
    {
        Down,
        Up,
        Side
    }

    /// <summary>
    /// Utility class for converting Vector2 movement directions into cardinal facing directions
    /// and sprite flip decisions for top-down 2D characters.
    /// 
    /// Design Philosophy:
    /// - Side direction if horizontal component dominates
    /// - Up/Down based on vertical component when vertical dominates
    /// - FlipX = true only when facing left (Side + negative X)
    /// </summary>
    public static class TopDownFacing
    {
        /// <summary>
        /// Converts a Vector2 direction into cardinal facing and sprite flip decision.
        /// </summary>
        /// <param name="direction">Movement or facing direction (does not need to be normalized)</param>
        /// <returns>Cardinal direction and whether to flip sprite horizontally</returns>
        public static (FacingCardinal cardinal, bool flipX) FromVector(Vector2 direction)
        {
            // Handle zero/near-zero vectors - default to Down facing
            if (direction.sqrMagnitude < 0.01f)
            {
                return (FacingCardinal.Down, false);
            }

            // Get absolute values for comparison
            float absX = Mathf.Abs(direction.x);
            float absY = Mathf.Abs(direction.y);

            // Side direction if horizontal component is greater than or equal to vertical
            if (absX >= absY)
            {
                bool flipX = direction.x < 0f; // Flip when facing left
                return (FacingCardinal.Side, flipX);
            }
            
            // Vertical movement - Up or Down based on Y component
            FacingCardinal cardinal = direction.y > 0f ? FacingCardinal.Up : FacingCardinal.Down;
            return (cardinal, false); // Never flip for up/down facing
        }

        /// <summary>
        /// Gets the default facing direction and flip state.
        /// Used for initialization or when no movement direction is available.
        /// </summary>
        /// <returns>Default cardinal (Down) and no flip</returns>
        public static (FacingCardinal cardinal, bool flipX) GetDefault()
        {
            return (FacingCardinal.Down, false);
        }

        /// <summary>
        /// Converts FacingCardinal to a readable string for debugging.
        /// </summary>
        /// <param name="cardinal">The cardinal direction</param>
        /// <returns>Human-readable direction name</returns>
        public static string CardinalToString(FacingCardinal cardinal)
        {
            return cardinal switch
            {
                FacingCardinal.Down => "Down",
                FacingCardinal.Up => "Up", 
                FacingCardinal.Side => "Side",
                _ => "Unknown"
            };
        }

#if UNITY_EDITOR
        /// <summary>
        /// Debug utility for visualizing direction calculations in editor.
        /// </summary>
        /// <param name="direction">Input direction vector</param>
        /// <returns>Debug string with direction analysis</returns>
        public static string DebugDirection(Vector2 direction)
        {
            var (cardinal, flipX) = FromVector(direction);
            return $"Dir: {direction:F2} → {CardinalToString(cardinal)} (Flip: {flipX})";
        }
#endif
    }
}