using UnityEngine;

namespace Greenlight.AI
{
    /// <summary>
    /// Death state for enemies - handles death animation, Luni drops, and cleanup.
    /// Terminal state that leads to enemy destruction.
    /// </summary>
    public class DeathState : EnemyState
    {
        private bool _deathProcessed;
        private bool _luniDropped;
        private float _deathAnimationDuration;

        public override void Enter()
        {
            base.Enter();

            _deathProcessed = false;
            _luniDropped = false;
            _deathAnimationDuration = 1f; // Default death animation duration

            // Stop all movement immediately
            Owner.StopMovement();

            // Disable combat components
            DisableCombatComponents();

            // Start death sequence
            StartDeathSequence();
        }

        public override void Execute()
        {
            base.Execute();

            // Process death effects if not already done
            if (!_deathProcessed)
            {
                ProcessDeath();
                _deathProcessed = true;
            }

            // Drop Luni after a brief delay
            if (!_luniDropped && TimeInState > 0.3f)
            {
                DropLuni();
                _luniDropped = true;
            }

            // Destroy enemy after death animation completes
            if (TimeInState > _deathAnimationDuration)
            {
                DestroyEnemy();
            }
        }

        public override EnemyState CheckTransitions()
        {
            // Death is a terminal state - no transitions out
            return null;
        }

        /// <summary>
        /// Disable combat-related components to prevent further interactions.
        /// </summary>
        private void DisableCombatComponents()
        {
            // Disable health component
            var healthComponent = Owner.GetComponent<HealthComponent>();
            if (healthComponent != null)
            {
                healthComponent.enabled = false;
            }

            // Disable colliders to prevent further interactions
            var colliders = Owner.GetComponentsInChildren<Collider2D>();
            foreach (var collider in colliders)
            {
                // Keep trigger colliders for Luni pickup, disable others
                if (!collider.isTrigger)
                {
                    collider.enabled = false;
                }
            }

            // Disable knockback receiver
            var knockbackReceiver = Owner.GetComponent<KnockbackReceiver>();
            if (knockbackReceiver != null)
            {
                knockbackReceiver.enabled = false;
            }
        }

        /// <summary>
        /// Start the death sequence with animation and effects.
        /// </summary>
        private void StartDeathSequence()
        {
            // Play death animation
            if (Owner.EnemyVisuals != null)
            {
                Owner.EnemyVisuals.PlayDeathAnimation();
                _deathAnimationDuration = Owner.EnemyVisuals.GetDeathAnimationDuration();
            }

            // Spawn death VFX if available
            if (Owner.Definition?.DeathVFXPrefab != null)
            {
                GameObject deathVFX = Object.Instantiate(
                    Owner.Definition.DeathVFXPrefab,
                    Owner.transform.position,
                    Quaternion.identity
                );

                // Auto-destroy VFX after a delay
                Object.Destroy(deathVFX, 3f);
            }

            // Play death sound effect
            // In a full implementation, this would be handled by an audio manager
        }

        /// <summary>
        /// Process death effects and notifications.
        /// </summary>
        private void ProcessDeath()
        {
            // Fire death events for other systems to react
            // This would trigger:
            // - Combat feedback (screen shake for defeat)
            // - Achievement/statistics tracking  
            // - Quest progress updates
            // - Audio cues

            // Trigger enhanced feedback for enemy defeat
            CombatFeedbackManager.FeedbackAPI.ApplyDefeatFeedback(Owner.transform);

            // Notify game state of enemy defeat if needed
            // This could increment kill counters, progress objectives, etc.
        }

        /// <summary>
        /// Drop Luni currency for the player to collect.
        /// </summary>
        private void DropLuni()
        {
            if (Owner.Definition == null)
                return;

            // Calculate Luni drop amount (100% drop rate as per plan)
            int luniAmount = Owner.Definition.RandomLuniDrop;

            // In a full implementation, this would spawn a LuniPickup prefab
            // For now, we'll add directly to GameState if available
            if (Owner.GameState != null)
            {
                int currentLuni = Owner.GameState.GetInt("Player_Luni", 0);
                Owner.GameState.SetInt("Player_Luni", currentLuni + luniAmount);

                // Fire Luni collected event for UI feedback
                // Owner.OnLuniCollected?.Raise();

                Debug.Log($"[DeathState] {Owner.name} dropped {luniAmount} Luni");
            }

            // TODO: Spawn actual LuniPickup prefab when economy system is implemented
            // This should be handled by a LuniDropper component
        }

        /// <summary>
        /// Destroy the enemy GameObject.
        /// </summary>
        private void DestroyEnemy()
        {
            if (Owner != null)
            {
                Debug.Log($"[DeathState] Destroying enemy: {Owner.name}");
                Object.Destroy(Owner.gameObject);
            }
        }

        public override void OnDamageTaken(int damage, Transform source)
        {
            // Dead enemies don't take damage
            // Ignore any damage dealt to corpse
        }

        public override void OnHealthDepleted()
        {
            // Already dead - ignore
        }

#if UNITY_EDITOR
        public override string GetStateName()
        {
            return $"Death (T:{TimeInState:F1}s)";
        }
#endif
    }
}