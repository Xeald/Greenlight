using UnityEngine;
using Greenlight.Core;
using Greenlight.Combat;
using Greenlight.Core.Physics;

namespace Greenlight.AI
{
    /// <summary>
    /// Base controller for all enemies - orchestrates AI, combat, and visual systems.
    /// Integrates with the "Nervous System" for world state awareness.
    /// 
    /// Architecture:
    /// - Data-driven design using EnemyDefinitionSO
    /// - State machine for AI behavior
    /// - Integration with combat and gadget systems
    /// - Event-driven communication with other systems
    /// </summary>
    [AddComponentMenu("Greenlight/AI/Enemy Controller")]
    public class EnemyController : MonoBehaviour, ICollisionChecker
    {
        [Header("Enemy Configuration")]
        [SerializeField, Tooltip("Enemy definition (Brain) - contains all data-driven parameters.")]
        private EnemyDefinitionSO _definition;

        [SerializeField, Tooltip("Behavior settings for AI logic.")]
        private EnemyBehaviorSettingsSO _behaviorSettings;

        [Header("Core Components")]
        [SerializeField, Tooltip("Health component for damage handling.")]
        private HealthComponent _healthComponent;

        [SerializeField, Tooltip("Knockback receiver for impact physics.")]
        private KnockbackReceiver _knockbackReceiver;

        [SerializeField, Tooltip("Enemy visuals manager.")]
        private EnemyVisuals _enemyVisuals;

        [SerializeField, Tooltip("Telegraph system for attack warnings.")]
        private EnemyTelegraph _enemyTelegraph;

        [Header("Physics & Movement")]
        [SerializeField, Tooltip("Rigidbody2D for movement (should be kinematic).")]
        private Rigidbody2D _rigidbody;

        [SerializeField, Tooltip("Layers that block enemy movement (typically Environment).")]
        private LayerMask _obstacleLayers;

        [Header("Game State Integration")]
        [SerializeField, Tooltip("Game state for world awareness.")]
        private GameStateSO _gameState;

        [Header("Debug")]
        [SerializeField, Tooltip("Enable debug logging and gizmos.")]
        private bool _debugMode = false;

        // State Machine
        private EnemyStateMachine _stateMachine;
        private float _currentMoveSpeedMultiplier = 1f;

        // Movement state
        private Vector2 _currentVelocity;
        private bool _movementPaused;

        // Physics components
        private BoxCollider2D _collider;

        /// <summary>
        /// Enemy definition containing all data-driven parameters.
        /// </summary>
        public EnemyDefinitionSO Definition => _definition;

        /// <summary>
        /// Behavior settings for AI logic.
        /// </summary>
        public EnemyBehaviorSettingsSO BehaviorSettings => _behaviorSettings;

        /// <summary>
        /// Health component reference.
        /// </summary>
        public HealthComponent HealthComponent => _healthComponent;

        /// <summary>
        /// Enemy visuals manager.
        /// </summary>
        public EnemyVisuals EnemyVisuals => _enemyVisuals;

        /// <summary>
        /// Telegraph system reference.
        /// </summary>
        public EnemyTelegraph EnemyTelegraph => _enemyTelegraph;

        /// <summary>
        /// Game state reference.
        /// </summary>
        public GameStateSO GameState => _gameState;

        /// <summary>
        /// Current state machine instance.
        /// </summary>
        public EnemyStateMachine StateMachine => _stateMachine;

        /// <summary>
        /// Is the enemy currently alive?
        /// </summary>
        public bool IsAlive => _healthComponent != null && !_healthComponent.IsDead;

        protected virtual void Awake()
        {
            // Auto-assign components if not set
            AutoAssignComponents();

            // Initialize state machine
            InitializeStateMachine();

            // Configure components
            ConfigureComponents();
        }

        protected virtual void Start()
        {
            // Initialize health from definition
            InitializeHealth();

            // Start the AI state machine
            StartAI();
        }

        protected virtual void Update()
        {
            // Update state machine
            _stateMachine?.Update();

            // Handle movement
            UpdateMovement();
        }

        protected virtual void FixedUpdate()
        {
            // Apply movement in FixedUpdate for consistent physics
            ApplyMovement();
        }

        #region Initialization

        /// <summary>
        /// Auto-assign components if not manually set.
        /// </summary>
        private void AutoAssignComponents()
        {
            if (_healthComponent == null)
                _healthComponent = GetComponent<HealthComponent>();

            if (_knockbackReceiver == null)
                _knockbackReceiver = GetComponent<KnockbackReceiver>();

            if (_enemyVisuals == null)
                _enemyVisuals = GetComponent<EnemyVisuals>();

            if (_enemyTelegraph == null)
                _enemyTelegraph = GetComponent<EnemyTelegraph>();

            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody2D>();

            if (_collider == null)
                _collider = GetComponent<BoxCollider2D>();

            // Ensure rigidbody is kinematic for manual control
            if (_rigidbody != null && _rigidbody.bodyType != RigidbodyType2D.Kinematic)
            {
                _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        /// <summary>
        /// Initialize the state machine with all available states.
        /// </summary>
        private void InitializeStateMachine()
        {
            _stateMachine = new EnemyStateMachine();
            _stateMachine.Initialize(this);

            // Add all states
            _stateMachine.AddState(new IdleState());
            _stateMachine.AddState(new ChaseState());
            _stateMachine.AddState(new AttackState());
            _stateMachine.AddState(new StunnedState());
            _stateMachine.AddState(new DeathState());
        }

        /// <summary>
        /// Configure components with enemy definition data.
        /// </summary>
        private void ConfigureComponents()
        {
            if (_definition == null)
            {
                Debug.LogError($"[EnemyController] {name}: No EnemyDefinitionSO assigned!");
                return;
            }

            // Configure knockback receiver
            if (_knockbackReceiver != null)
            {
                // Knockback settings are typically handled by CombatSettingsSO
                // The knockback receiver will use those settings when ApplyKnockback is called
            }
        }

        /// <summary>
        /// Initialize health from enemy definition.
        /// </summary>
        private void InitializeHealth()
        {
            if (_healthComponent != null && _definition != null)
            {
                // Set health to max from definition
                // Note: HealthComponent typically initializes its own health,
                // but we may want to override it with enemy-specific values
            }
        }

        /// <summary>
        /// Start the AI state machine.
        /// </summary>
        private void StartAI()
        {
            if (_stateMachine != null)
            {
                // Start in Idle state
                _stateMachine.Start<IdleState>();

                if (_debugMode)
                {
                    Debug.Log($"[EnemyController] {name}: AI started in Idle state");
                }
            }
        }

        #endregion

        #region Movement Control

        /// <summary>
        /// Move in a specific direction.
        /// </summary>
        /// <param name="direction">Normalized movement direction</param>
        public void MoveInDirection(Vector2 direction)
        {
            if (_movementPaused || _definition == null)
            {
                _currentVelocity = Vector2.zero;
                return;
            }

            // Calculate target velocity
            float moveSpeed = _definition.MoveSpeedUnits * _currentMoveSpeedMultiplier;
            _currentVelocity = direction.normalized * moveSpeed;
        }

        /// <summary>
        /// Stop all movement.
        /// </summary>
        public void StopMovement()
        {
            _currentVelocity = Vector2.zero;
        }

        /// <summary>
        /// Set movement speed multiplier.
        /// </summary>
        /// <param name="multiplier">Speed multiplier (e.g., 1.5 for chase speed)</param>
        public void SetMoveSpeedMultiplier(float multiplier)
        {
            _currentMoveSpeedMultiplier = Mathf.Max(0f, multiplier);
        }

        /// <summary>
        /// Pause or unpause movement.
        /// </summary>
        /// <param name="paused">True to pause movement</param>
        public void SetMovementPaused(bool paused)
        {
            _movementPaused = paused;
            if (paused)
            {
                _currentVelocity = Vector2.zero;
            }
        }

        /// <summary>
        /// Update movement logic (called in Update).
        /// </summary>
        private void UpdateMovement()
        {
            // Check if movement should be paused due to knockback
            if (_knockbackReceiver != null && _knockbackReceiver.IsKnockedBack)
            {
                _currentVelocity = Vector2.zero;
                return;
            }
        }

        /// <summary>
        /// Implements ICollisionChecker. Checks if movement is blocked by obstacles.
        /// </summary>
        /// <param name="delta">The movement vector to check.</param>
        /// <returns>True if movement is blocked by a collision.</returns>
        public bool CheckCollision(Vector2 delta)
        {
            if (_collider == null || _rigidbody == null) return false;

            float distance = delta.magnitude;
            if (distance < 0.0001f) return false;

            Vector2 direction = delta.normalized;
            Vector2 size = _collider.size * 0.95f; // Slightly smaller to avoid "snagging"
            Vector2 origin = _rigidbody.position + _collider.offset;

            // Small skin width to detect surfaces even if already touching
            float skinWidth = 0.015f; // ~0.5 pixels at 32 PPU

            // Cast a box to see if we hit anything on the obstacle layers
            RaycastHit2D hit = Physics2D.BoxCast(
                origin - direction * skinWidth,
                size,
                0f,
                direction,
                distance + skinWidth,
                _obstacleLayers
            );

            if (hit.collider != null)
            {
                // Only block if moving towards the surface (dot product < 0)
                float dot = Vector2.Dot(direction, hit.normal);
                if (dot < -0.01f)
                {
                    return true; // Blocked by wall
                }
            }

            return false;
        }

        /// <summary>
        /// Apply movement using Rigidbody2D (called in FixedUpdate).
        /// Uses collision checking to prevent walking through walls.
        /// </summary>
        private void ApplyMovement()
        {
            if (_rigidbody == null)
                return;

            Vector2 moveDelta = _currentVelocity * Time.fixedDeltaTime;
            Vector2 finalPos = _rigidbody.position;

            // Split movement into X and Y for sliding collision response
            // This allows the enemy to slide along walls smoothly
            if (Mathf.Abs(moveDelta.x) > 0.0001f)
            {
                if (!CheckCollision(new Vector2(moveDelta.x, 0)))
                {
                    finalPos.x += moveDelta.x;
                }
            }

            if (Mathf.Abs(moveDelta.y) > 0.0001f)
            {
                if (!CheckCollision(new Vector2(0, moveDelta.y)))
                {
                    finalPos.y += moveDelta.y;
                }
            }

            _rigidbody.MovePosition(finalPos);
        }

        #endregion

        #region Combat Integration

        /// <summary>
        /// Called when this enemy takes damage.
        /// </summary>
        /// <param name="damage">Amount of damage taken</param>
        /// <param name="source">Source of the damage</param>
        /// <param name="damageType">Type of damage</param>
        public void OnDamageTaken(int damage, Transform source, DamageType damageType)
        {
            // Apply damage resistances/weaknesses
            if (_definition != null)
            {
                damage = _definition.CalculateEffectiveDamage(damage, damageType);
            }

            // Notify state machine
            _stateMachine?.OnDamageTaken(damage, source);

            // Apply visual feedback
            _enemyVisuals?.StartFlicker(0.3f);

            if (_debugMode)
            {
                Debug.Log($"[EnemyController] {name}: Took {damage} damage from {source?.name}");
            }
        }

        /// <summary>
        /// Called when this enemy's health is depleted.
        /// </summary>
        public void OnHealthDepleted()
        {
            // Notify state machine
            _stateMachine?.OnHealthDepleted();

            if (_debugMode)
            {
                Debug.Log($"[EnemyController] {name}: Health depleted - transitioning to death");
            }
        }

        #endregion

        #region Event Handlers

        private void OnEnable()
        {
            // Subscribe to health component events
            if (_healthComponent != null)
            {
                // In a full implementation, you'd subscribe to health events here
                // _healthComponent.OnDamageTaken += OnDamageTaken;
                // _healthComponent.OnHealthDepleted += OnHealthDepleted;
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from events
            if (_healthComponent != null)
            {
                // _healthComponent.OnDamageTaken -= OnDamageTaken;
                // _healthComponent.OnHealthDepleted -= OnHealthDepleted;
            }
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            AutoAssignComponents();

            // Validate configuration
            if (_definition == null)
            {
                Debug.LogWarning($"[EnemyController] {name}: No EnemyDefinitionSO assigned!");
            }
        }

        private void OnDrawGizmos()
        {
            if (!_debugMode || _definition == null)
                return;

            // Draw detection range
            Gizmos.color = Color.yellow;
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, _definition.DetectionRange);

            // Draw attack range
            Gizmos.color = Color.red;
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, _definition.AttackRange);

            // Draw current state info
            if (Application.isPlaying && _stateMachine != null)
            {
                Vector3 labelPos = transform.position + Vector3.up * 2f;
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(labelPos, _stateMachine.CurrentStateName);
                #endif
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_definition == null)
                return;

            // Draw more detailed debug info when selected
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.2f);

            // Draw movement velocity
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Vector3 velocityEnd = transform.position + (Vector3)_currentVelocity;
                Gizmos.DrawLine(transform.position, velocityEnd);
                Gizmos.DrawSphere(velocityEnd, 0.1f);
            }
        }
#endif
    }
}