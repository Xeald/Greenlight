using UnityEngine;

namespace Greenlight.AI
{
    /// <summary>
    /// Chase state for enemies - pursues the player until in attack range or loses sight.
    /// </summary>
    public class ChaseState : EnemyState
    {
        private float _lastPlayerSeenTime;
        private Vector3 _lastKnownPlayerPosition;
        private float _nextDetectionCheck;

        public override void Enter()
        {
            base.Enter();

            // Reset tracking variables
            _lastPlayerSeenTime = Time.time;
            _lastKnownPlayerPosition = GetPlayerTransform()?.position ?? Owner.transform.position;
            _nextDetectionCheck = 0f;

            // Start chase animation/effects
            Owner.EnemyVisuals?.PlayChaseAnimation();
            
            // Set movement speed to chase speed
            Owner.SetMoveSpeedMultiplier(Owner.Definition.ChaseSpeedMultiplier);
        }

        public override void Execute()
        {
            base.Execute();

            // Update player detection at intervals
            if (Time.time >= _nextDetectionCheck)
            {
                UpdatePlayerTracking();
                _nextDetectionCheck = Time.time + (Owner.BehaviorSettings?.DetectionUpdateInterval ?? 0.2f);
            }

            // Move toward player (or last known position)
            MoveTowardPlayer();
        }

        public override EnemyState CheckTransitions()
        {
            // Transition to Attack if close enough to player
            if (IsPlayerInRange(Owner.Definition.AttackRange))
            {
                return StateMachine.GetState<AttackState>();
            }

            // Transition back to Idle if player is lost for too long
            float timeSincePlayerSeen = Time.time - _lastPlayerSeenTime;
            float lostTimeout = Owner.BehaviorSettings?.PlayerLostTimeout ?? 3f;
            
            if (timeSincePlayerSeen > lostTimeout)
            {
                return StateMachine.GetState<IdleState>();
            }

            // Stay in chase if player is still in detection range
            if (IsPlayerInRange(Owner.Definition.DetectionRange) && HasLineOfSightToPlayer(Owner.Definition.DetectionRange))
            {
                return null; // Stay in chase
            }

            // Player out of range but recently seen - continue chasing to last known position
            float recentTimeout = lostTimeout * 0.5f; // Chase to last position for half the timeout
            if (timeSincePlayerSeen < recentTimeout)
            {
                return null; // Continue chase
            }

            // Give up and return to idle
            return StateMachine.GetState<IdleState>();
        }

        /// <summary>
        /// Update player tracking and last known position.
        /// </summary>
        private void UpdatePlayerTracking()
        {
            Transform player = GetPlayerTransform();
            if (player == null)
                return;

            bool playerInRange = IsPlayerInRange(Owner.Definition.DetectionRange);
            bool hasLineOfSight = playerInRange && HasLineOfSightToPlayer(Owner.Definition.DetectionRange);

            if (hasLineOfSight)
            {
                // Update tracking info
                _lastPlayerSeenTime = Time.time;
                _lastKnownPlayerPosition = player.position;
            }
        }

        /// <summary>
        /// Move toward the player or last known player position.
        /// </summary>
        private void MoveTowardPlayer()
        {
            Transform player = GetPlayerTransform();
            Vector3 targetPosition;

            // Use current player position if visible, otherwise use last known position
            if (player != null && HasLineOfSightToPlayer(Owner.Definition.DetectionRange))
            {
                targetPosition = player.position;
            }
            else
            {
                targetPosition = _lastKnownPlayerPosition;
            }

            // Calculate direction to target
            Vector2 direction = (targetPosition - Owner.transform.position).normalized;

            // Move toward target
            if (direction.sqrMagnitude > 0.01f)
            {
                Owner.MoveInDirection(direction);
                
                // Update facing direction
                Owner.EnemyVisuals?.SetFacingDirection(direction);
            }
            else
            {
                // Reached last known position, stop moving
                Owner.StopMovement();
            }

            // Check if we're close to the last known position but player isn't there
            float distanceToLastKnown = Vector2.Distance(Owner.transform.position, _lastKnownPlayerPosition);
            if (distanceToLastKnown < 0.5f && player != null)
            {
                float distanceToActualPlayer = Vector2.Distance(Owner.transform.position, player.position);
                if (distanceToActualPlayer > Owner.Definition.DetectionRange)
                {
                    // We've reached the last known position but player isn't here - lose track sooner
                    _lastPlayerSeenTime = Time.time - (Owner.BehaviorSettings?.PlayerLostTimeout ?? 3f) * 0.8f;
                }
            }
        }

        public override void Exit()
        {
            base.Exit();
            
            // Reset movement speed multiplier
            Owner.SetMoveSpeedMultiplier(1f);
            
            // Stop movement
            Owner.StopMovement();
        }

        public override void OnDamageTaken(int damage, Transform source)
        {
            // When taking damage during chase, update player tracking if damage came from player
            Transform player = GetPlayerTransform();
            if (source == player)
            {
                _lastPlayerSeenTime = Time.time;
                _lastKnownPlayerPosition = source.position;
            }

            // Call base implementation (likely transitions to stunned)
            base.OnDamageTaken(damage, source);
        }

#if UNITY_EDITOR
        public override string GetStateName()
        {
            float timeSincePlayerSeen = Time.time - _lastPlayerSeenTime;
            return $"Chase (Lost:{timeSincePlayerSeen:F1}s)";
        }
#endif
    }
}