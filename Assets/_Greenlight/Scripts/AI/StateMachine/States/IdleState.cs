using UnityEngine;

namespace Greenlight.AI
{
    /// <summary>
    /// Idle state for enemies - patrols, waits, and watches for the player.
    /// Entry point for most enemy behavior trees.
    /// </summary>
    public class IdleState : EnemyState
    {
        private float _idleDuration;
        private float _nextDetectionCheck;
        private Vector3 _initialPosition;
        private bool _isPatrolling;

        public override void Enter()
        {
            base.Enter();

            // Set random idle duration based on behavior settings
            _idleDuration = Owner.BehaviorSettings?.RandomIdleTime ?? 2f;
            
            // Store initial position for potential patrol behavior
            _initialPosition = Owner.transform.position;
            
            // Reset detection timer
            _nextDetectionCheck = 0f;
            _isPatrolling = false;

            // Stop any movement
            Owner.StopMovement();

            // Play idle animation if available
            Owner.EnemyVisuals?.PlayIdleAnimation();
        }

        public override void Execute()
        {
            base.Execute();

            // Check for player detection at intervals
            if (Time.time >= _nextDetectionCheck)
            {
                CheckForPlayer();
                _nextDetectionCheck = Time.time + (Owner.BehaviorSettings?.DetectionUpdateInterval ?? 0.2f);
            }

            // Handle idle behavior (could be standing still or light patrol)
            HandleIdleBehavior();
        }

        public override EnemyState CheckTransitions()
        {
            // Transition to Chase if player is detected
            if (IsPlayerInRange(Owner.Definition.DetectionRange) && HasLineOfSightToPlayer(Owner.Definition.DetectionRange))
            {
                return StateMachine.GetState<ChaseState>();
            }

            return null;
        }

        /// <summary>
        /// Check for player presence and react accordingly.
        /// </summary>
        private void CheckForPlayer()
        {
            if (Owner?.Definition == null)
                return;

            // Simple detection check - in a full implementation, this might include
            // field of view, sound detection, etc.
            bool playerInRange = IsPlayerInRange(Owner.Definition.DetectionRange);
            bool hasLineOfSight = playerInRange && HasLineOfSightToPlayer(Owner.Definition.DetectionRange);

            if (hasLineOfSight)
            {
                // Player detected - let the state machine handle the transition
                // in CheckTransitions() method
                Owner.EnemyVisuals?.PlayAlertAnimation();
            }
        }

        /// <summary>
        /// Handle basic idle behavior like standing still or light movement.
        /// </summary>
        private void HandleIdleBehavior()
        {
            // For now, just play idle animation and stay still
            // In a full implementation, this could include:
            // - Patrol between waypoints
            // - Random movement around spawn point
            // - Looking around behavior
            // - Playing ambient animations

            if (Owner?.EnemyVisuals != null)
            {
                // Ensure idle animation is playing
                if (!Owner.EnemyVisuals.IsPlayingIdleAnimation())
                {
                    Owner.EnemyVisuals.PlayIdleAnimation();
                }
            }
        }

        public override void Exit()
        {
            base.Exit();
            
            // Clean up idle-specific state
            _isPatrolling = false;
        }

#if UNITY_EDITOR
        public override string GetStateName()
        {
            return $"Idle (T:{TimeInState:F1}s)";
        }
#endif
    }
}