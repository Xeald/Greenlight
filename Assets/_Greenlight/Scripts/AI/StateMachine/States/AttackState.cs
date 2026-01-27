using UnityEngine;

namespace Greenlight.AI
{
    /// <summary>
    /// Attack state for enemies - handles telegraph, attack execution, and recovery.
    /// Implements "Combat as Conversation" pillar with clear telegraphs.
    /// </summary>
    public class AttackState : EnemyState
    {
        /// <summary>
        /// Phases of the attack state.
        /// </summary>
        public enum AttackPhase
        {
            Telegraph,  // Warning phase - enemy signals attack
            Execute,    // Attack execution - damage dealt
            Recovery    // Cooldown phase - enemy is vulnerable
        }

        private AttackPhase _currentPhase;
        private float _phaseStartTime;
        private bool _attackExecuted;

        /// <summary>
        /// Current phase of the attack.
        /// </summary>
        public AttackPhase CurrentPhase => _currentPhase;

        public override void Enter()
        {
            base.Enter();

            // Start with telegraph phase
            _currentPhase = AttackPhase.Telegraph;
            _phaseStartTime = Time.time;
            _attackExecuted = false;

            // Stop movement during attack
            Owner.StopMovement();

            // Start telegraph effects
            StartTelegraphPhase();
        }

        public override void Execute()
        {
            base.Execute();

            switch (_currentPhase)
            {
                case AttackPhase.Telegraph:
                    HandleTelegraphPhase();
                    break;
                
                case AttackPhase.Execute:
                    HandleExecutePhase();
                    break;
                
                case AttackPhase.Recovery:
                    HandleRecoveryPhase();
                    break;
            }
        }

        public override EnemyState CheckTransitions()
        {
            // Only allow transitions during recovery phase or if attack is complete
            if (_currentPhase == AttackPhase.Recovery)
            {
                float recoveryDuration = Owner.BehaviorSettings?.AttackRecoveryTime ?? 0.8f;
                float timeInRecovery = Time.time - _phaseStartTime;

                if (timeInRecovery >= recoveryDuration)
                {
                    // Attack complete - decide next state based on player position
                    if (IsPlayerInRange(Owner.Definition.AttackRange))
                    {
                        // Player still in range - attack again after cooldown
                        float timeSinceAttackStart = Time.time - (TimeInState - timeInRecovery);
                        if (timeSinceAttackStart >= Owner.Definition.AttackCooldown)
                        {
                            // Ready for another attack - restart this state
                            return StateMachine.GetState<AttackState>();
                        }
                    }
                    else if (IsPlayerInRange(Owner.Definition.DetectionRange))
                    {
                        // Player moved out of attack range but still detectable - chase
                        return StateMachine.GetState<ChaseState>();
                    }
                    else
                    {
                        // Player is gone - return to idle
                        return StateMachine.GetState<IdleState>();
                    }
                }
            }

            return null; // Stay in current state
        }

        /// <summary>
        /// Start the telegraph phase.
        /// </summary>
        private void StartTelegraphPhase()
        {
            // Play telegraph animation and effects
            Owner.EnemyVisuals?.PlayTelegraphAnimation();
            
            // Start telegraph effects through the Telegraph system
            if (Owner.EnemyTelegraph != null)
            {
                Owner.EnemyTelegraph.StartTelegraph();
            }

            // Face the player
            Vector2 directionToPlayer = GetDirectionToPlayer();
            if (directionToPlayer != Vector2.zero)
            {
                Owner.EnemyVisuals?.SetFacingDirection(directionToPlayer);
            }
        }

        /// <summary>
        /// Handle telegraph phase logic.
        /// </summary>
        private void HandleTelegraphPhase()
        {
            float telegraphDuration = Owner.BehaviorSettings?.TelegraphDuration ?? 0.8f;
            float timeInTelegraph = Time.time - _phaseStartTime;

            // Continue facing player during telegraph
            Vector2 directionToPlayer = GetDirectionToPlayer();
            if (directionToPlayer != Vector2.zero)
            {
                Owner.EnemyVisuals?.SetFacingDirection(directionToPlayer);
            }

            // Check if telegraph phase is complete
            if (timeInTelegraph >= telegraphDuration)
            {
                TransitionToExecutePhase();
            }
        }

        /// <summary>
        /// Transition from telegraph to execute phase.
        /// </summary>
        private void TransitionToExecutePhase()
        {
            _currentPhase = AttackPhase.Execute;
            _phaseStartTime = Time.time;

            // Stop telegraph effects
            if (Owner.EnemyTelegraph != null)
            {
                Owner.EnemyTelegraph.StopTelegraph();
            }

            // Start attack animation
            Owner.EnemyVisuals?.PlayAttackAnimation();
        }

        /// <summary>
        /// Handle attack execution phase.
        /// </summary>
        private void HandleExecutePhase()
        {
            // Execute attack immediately when entering this phase
            if (!_attackExecuted)
            {
                ExecuteAttack();
                _attackExecuted = true;
            }

            // Transition to recovery phase quickly (attack execution is brief)
            float executeTime = Time.time - _phaseStartTime;
            if (executeTime >= 0.1f) // Brief execution window
            {
                TransitionToRecoveryPhase();
            }
        }

        /// <summary>
        /// Execute the actual attack.
        /// </summary>
        private void ExecuteAttack()
        {
            // Check if player is still in attack range
            Transform player = GetPlayerTransform();
            if (player == null || !IsPlayerInRange(Owner.Definition.AttackRange))
                return;

            // Deal damage to player
            var playerHealth = player.GetComponent<HealthComponent>();
            if (playerHealth != null)
            {
                bool damageDealt = playerHealth.TakeDamage(
                    Owner.Definition.AttackDamage,
                    Owner.transform,
                    DamageType.Physical
                );

                if (damageDealt)
                {
                    // Trigger combat feedback
                    CombatFeedbackManager.FeedbackAPI.ApplyDamageFeedback(
                        Owner.Definition.AttackDamage,
                        player,
                        Owner.transform
                    );
                }
            }
        }

        /// <summary>
        /// Transition to recovery phase.
        /// </summary>
        private void TransitionToRecoveryPhase()
        {
            _currentPhase = AttackPhase.Recovery;
            _phaseStartTime = Time.time;

            // Play recovery animation
            Owner.EnemyVisuals?.PlayRecoveryAnimation();
        }

        /// <summary>
        /// Handle recovery phase logic.
        /// </summary>
        private void HandleRecoveryPhase()
        {
            // Recovery phase is handled mainly in CheckTransitions()
            // Enemy is vulnerable during this time
        }

        public override void Exit()
        {
            base.Exit();

            // Clean up any ongoing effects
            if (Owner.EnemyTelegraph != null)
            {
                Owner.EnemyTelegraph.StopTelegraph();
            }

            // Reset attack state
            _attackExecuted = false;
        }

        public override void OnDamageTaken(int damage, Transform source)
        {
            // During telegraph and recovery phases, enemy can be interrupted
            if (_currentPhase == AttackPhase.Telegraph || _currentPhase == AttackPhase.Recovery)
            {
                // Interrupt attack and go to stunned state
                base.OnDamageTaken(damage, source);
            }
            else
            {
                // During execute phase, enemy might be immune to interruption
                // This depends on the specific enemy design
                base.OnDamageTaken(damage, source);
            }
        }

#if UNITY_EDITOR
        public override string GetStateName()
        {
            return $"Attack ({_currentPhase})";
        }
#endif
    }
}