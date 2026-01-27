using UnityEngine;

namespace Greenlight.AI
{
    /// <summary>
    /// Abstract base class for enemy AI states.
    /// Follows the State pattern for clean state management.
    /// 
    /// State Lifecycle:
    /// - Enter: Called once when transitioning into this state
    /// - Execute: Called every frame while in this state
    /// - Exit: Called once when transitioning out of this state
    /// </summary>
    public abstract class EnemyState
    {
        /// <summary>
        /// Reference to the enemy controller that owns this state.
        /// </summary>
        protected EnemyController Owner { get; private set; }

        /// <summary>
        /// Reference to the state machine managing this state.
        /// </summary>
        protected EnemyStateMachine StateMachine { get; private set; }

        /// <summary>
        /// Time spent in this state (seconds).
        /// </summary>
        public float TimeInState { get; protected set; }

        /// <summary>
        /// Initialize this state with references to its owner and state machine.
        /// </summary>
        /// <param name="owner">The enemy controller that owns this state</param>
        /// <param name="stateMachine">The state machine managing transitions</param>
        public virtual void Initialize(EnemyController owner, EnemyStateMachine stateMachine)
        {
            Owner = owner;
            StateMachine = stateMachine;
        }

        /// <summary>
        /// Called once when entering this state.
        /// Override to implement state-specific setup logic.
        /// </summary>
        public virtual void Enter()
        {
            TimeInState = 0f;
        }

        /// <summary>
        /// Called every frame while in this state.
        /// Override to implement state-specific behavior.
        /// </summary>
        public virtual void Execute()
        {
            TimeInState += Time.deltaTime;
        }

        /// <summary>
        /// Called once when exiting this state.
        /// Override to implement state-specific cleanup logic.
        /// </summary>
        public virtual void Exit()
        {
            // Base implementation does nothing
        }

        /// <summary>
        /// Check if a state transition condition is met.
        /// Override to implement state-specific transition logic.
        /// </summary>
        /// <returns>The state to transition to, or null to stay in current state</returns>
        public virtual EnemyState CheckTransitions()
        {
            return null;
        }

        /// <summary>
        /// Called when the enemy takes damage while in this state.
        /// Override to implement damage-specific behavior.
        /// </summary>
        /// <param name="damage">Amount of damage taken</param>
        /// <param name="source">Source of the damage</param>
        public virtual void OnDamageTaken(int damage, Transform source)
        {
            // Default behavior: transition to stunned state if available
            if (StateMachine.HasState<StunnedState>())
            {
                StateMachine.TransitionTo<StunnedState>();
            }
        }

        /// <summary>
        /// Called when the enemy's health reaches zero while in this state.
        /// </summary>
        public virtual void OnHealthDepleted()
        {
            // Default behavior: transition to death state if available
            if (StateMachine.HasState<DeathState>())
            {
                StateMachine.TransitionTo<DeathState>();
            }
        }

        /// <summary>
        /// Get the player transform if available.
        /// Utility method for common state logic.
        /// </summary>
        /// <returns>Player transform or null if not found</returns>
        protected Transform GetPlayerTransform()
        {
            // Find player by tag - in a full implementation, this might be cached
            GameObject player = GameObject.FindWithTag("Player");
            return player?.transform;
        }

        /// <summary>
        /// Check if the player is within a certain range of the enemy.
        /// </summary>
        /// <param name="range">Range to check</param>
        /// <returns>True if player is within range</returns>
        protected bool IsPlayerInRange(float range)
        {
            Transform player = GetPlayerTransform();
            if (player == null || Owner == null)
                return false;

            float distanceToPlayer = Vector2.Distance(Owner.transform.position, player.position);
            return distanceToPlayer <= range;
        }

        /// <summary>
        /// Get the direction from the enemy to the player.
        /// </summary>
        /// <returns>Normalized direction vector, or Vector2.zero if player not found</returns>
        protected Vector2 GetDirectionToPlayer()
        {
            Transform player = GetPlayerTransform();
            if (player == null || Owner == null)
                return Vector2.zero;

            return (player.position - Owner.transform.position).normalized;
        }

        /// <summary>
        /// Get the distance to the player.
        /// </summary>
        /// <returns>Distance to player, or float.MaxValue if player not found</returns>
        protected float GetDistanceToPlayer()
        {
            Transform player = GetPlayerTransform();
            if (player == null || Owner == null)
                return float.MaxValue;

            return Vector2.Distance(Owner.transform.position, player.position);
        }

        /// <summary>
        /// Check if the enemy has line of sight to the player.
        /// </summary>
        /// <param name="maxDistance">Maximum sight distance</param>
        /// <returns>True if line of sight is clear</returns>
        protected bool HasLineOfSightToPlayer(float maxDistance)
        {
            Transform player = GetPlayerTransform();
            if (player == null || Owner?.BehaviorSettings == null)
                return false;

            Vector2 enemyPos = Owner.transform.position;
            Vector2 playerPos = player.position;

            return Owner.BehaviorSettings.HasLineOfSight(enemyPos, playerPos, maxDistance);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Get the name of this state for debugging purposes.
        /// </summary>
        public virtual string GetStateName()
        {
            return GetType().Name;
        }
#endif
    }
}