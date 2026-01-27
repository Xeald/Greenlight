using UnityEngine;

namespace Greenlight.Combat
{
    /// <summary>
    /// Interface for entities that can take damage.
    /// Implement this on enemies, player, destructible objects, etc.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Apply damage to this entity.
        /// </summary>
        /// <param name="amount">Damage amount in heart units</param>
        /// <param name="source">Transform of the damage source (for knockback direction)</param>
        /// <param name="damageType">Type of damage being dealt</param>
        /// <returns>True if damage was applied, false if blocked/avoided</returns>
        bool TakeDamage(int amount, Transform source, DamageType damageType = DamageType.Physical);

        /// <summary>
        /// Current health of this entity.
        /// </summary>
        int CurrentHealth { get; }

        /// <summary>
        /// Maximum health of this entity.
        /// </summary>
        int MaxHealth { get; }

        /// <summary>
        /// Is this entity currently invulnerable to damage?
        /// </summary>
        bool IsInvulnerable { get; }
    }

    /// <summary>
    /// Types of damage that can be dealt.
    /// Used for resistance/weakness systems and visual feedback.
    /// </summary>
    public enum DamageType
    {
        Physical,    // Sword swings, collision damage
        Fire,        // Fire-based attacks
        Magic,       // Magical attacks
        Environmental // Spikes, pits, environmental hazards
    }
}