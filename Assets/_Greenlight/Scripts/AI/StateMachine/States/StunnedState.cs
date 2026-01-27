using UnityEngine;

namespace Greenlight.AI
{
    /// <summary>
    /// Stunned state for enemies - temporary immobilization after taking damage.
    /// Critical for IPullable integration (grapple hook pulls end here).
    /// </summary>
    public class StunnedState : EnemyState
    {
        private float _stunEndTime;
        private bool _wasKnockedBack;

        public override void Enter()
        {
            base.Enter();

            // Calculate stun duration
            float stunDuration = Owner.Definition?.StunDuration ?? 0.5f;
            _stunEndTime = Time.time + stunDuration;

            // Check if this stun was caused by knockback
            var knockbackReceiver = Owner.GetComponent<KnockbackReceiver>();
            _wasKnockedBack = knockbackReceiver != null && knockbackReceiver.IsKnockedBack;

            // Stop all movement
            Owner.StopMovement();

            // Play stun animation and effects
            Owner.EnemyVisuals?.PlayStunnedAnimation();
            
            // Show stun visual effects (stars, dizzy effect, etc.)
            ShowStunEffects();
        }

        public override void Execute()
        {
            base.Execute();

            // Remain immobilized during stun
            // The enemy should not move or attack while stunned

            // Update stun visual effects
            UpdateStunEffects();
        }

        public override EnemyState CheckTransitions()
        {
            // Check if knockback is still active
            var knockbackReceiver = Owner.GetComponent<KnockbackReceiver>();
            bool stillKnockedBack = knockbackReceiver != null && knockbackReceiver.IsKnockedBack;

            // Don't exit stun while still being knocked back
            if (stillKnockedBack)
            {
                return null;
            }

            // Check if stun duration has ended
            if (Time.time >= _stunEndTime)
            {
                // Stun over - decide next state based on player proximity
                if (IsPlayerInRange(Owner.Definition.AttackRange))
                {
                    // Player very close - go directly to attack
                    return StateMachine.GetState<AttackState>();
                }
                else if (IsPlayerInRange(Owner.Definition.DetectionRange))
                {
                    // Player in detection range - chase
                    return StateMachine.GetState<ChaseState>();
                }
                else
                {
                    // No player detected - return to idle
                    return StateMachine.GetState<IdleState>();
                }
            }

            return null; // Stay stunned
        }

        /// <summary>
        /// Show initial stun visual effects.
        /// </summary>
        private void ShowStunEffects()
        {
            // In a full implementation, this might:
            // - Spawn "stars" particle effect above enemy
            // - Change sprite tint to indicate stun
            // - Play stun sound effect
            // - Disable enemy weapon/shield visuals temporarily

            if (Owner.EnemyVisuals != null)
            {
                // Change tint to indicate stunned state
                Owner.EnemyVisuals.SetTemporaryTint(Color.yellow, 0.3f);
            }
        }

        /// <summary>
        /// Update stun visual effects each frame.
        /// </summary>
        private void UpdateStunEffects()
        {
            // Update any ongoing stun effects
            // For example, animate dizzy stars, update tint fade, etc.
        }

        /// <summary>
        /// Clean up stun effects.
        /// </summary>
        private void CleanupStunEffects()
        {
            if (Owner.EnemyVisuals != null)
            {
                // Remove stun tint
                Owner.EnemyVisuals.ClearTemporaryTint();
            }
        }

        public override void Exit()
        {
            base.Exit();

            // Clean up stun effects
            CleanupStunEffects();

            // Play recovery animation
            Owner.EnemyVisuals?.PlayRecoveryAnimation();
        }

        public override void OnDamageTaken(int damage, Transform source)
        {
            // Taking damage while stunned extends the stun duration slightly
            float additionalStun = 0.2f;
            _stunEndTime = Mathf.Max(_stunEndTime, Time.time + additionalStun);

            // Update knockback status if new knockback applied
            var knockbackReceiver = Owner.GetComponent<KnockbackReceiver>();
            if (knockbackReceiver != null && knockbackReceiver.IsKnockedBack)
            {
                _wasKnockedBack = true;
            }

            // Don't transition to another stunned state - just extend current one
            // This prevents stun state stacking
        }

        public override void OnHealthDepleted()
        {
            // If health reaches zero while stunned, go directly to death
            // Don't wait for stun to end
            base.OnHealthDepleted();
        }

        /// <summary>
        /// Get remaining stun time in seconds.
        /// </summary>
        public float RemainingStunTime => Mathf.Max(0f, _stunEndTime - Time.time);

        /// <summary>
        /// Check if this stun was caused by knockback.
        /// </summary>
        public bool WasKnockedBack => _wasKnockedBack;

#if UNITY_EDITOR
        public override string GetStateName()
        {
            return $"Stunned ({RemainingStunTime:F1}s)";
        }
#endif
    }
}