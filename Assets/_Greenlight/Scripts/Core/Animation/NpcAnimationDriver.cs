using UnityEngine;

namespace Greenlight.Core.Animation
{
    /// <summary>
    /// Reusable animation driver for NPCs that can be controlled by any NPC controller.
    /// Drives directional Idle and Move animations based on movement input.
    /// 
    /// Design Philosophy:
    /// - Reusable across all NPC types (merchants, villagers, dialogue NPCs, etc.)
    /// - Self-contained animation and sprite flip logic
    /// - Uses same state naming contract as Player for consistency
    /// - Graceful fallback if animation states don't exist
    /// </summary>
    [AddComponentMenu("Greenlight/Core/NPC Animation Driver")]
    public class NpcAnimationDriver : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField, Tooltip("Movement threshold to consider NPC as 'moving' (units/sec).")]
        private float _movementThreshold = 0.1f;

        [SerializeField, Tooltip("Should the sprite flip horizontally based on movement direction?")]
        private bool _flipSpriteOnDirection = true;

        [Header("Debug")]
        [SerializeField, Tooltip("Enable debug logging for animation state changes.")]
        private bool _debugLog = false;

        // Cached component references
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;

        // Animation and movement state
        private Vector2 _currentVelocity;
        private FacingCardinal _currentCardinal = FacingCardinal.Down;
        private bool _isMoving = false;
        private int _currentStateHash = -1;
        private string _currentStateName = "";
        private System.Collections.Generic.HashSet<int> _availableStateHashes;

        // Animation state name hashes (cached for performance)
        private static readonly int IdleDownHash = Animator.StringToHash("IdleDown");
        private static readonly int IdleUpHash = Animator.StringToHash("IdleUp");
        private static readonly int IdleSideHash = Animator.StringToHash("IdleSide");
        private static readonly int MoveDownHash = Animator.StringToHash("MoveDown");
        private static readonly int MoveUpHash = Animator.StringToHash("MoveUp");
        private static readonly int MoveSideHash = Animator.StringToHash("MoveSide");

        /// <summary>
        /// Current movement velocity of the NPC.
        /// </summary>
        public Vector2 CurrentVelocity => _currentVelocity;

        /// <summary>
        /// Current cardinal facing direction.
        /// </summary>
        public FacingCardinal CurrentCardinal => _currentCardinal;

        /// <summary>
        /// Whether the NPC is currently considered to be moving.
        /// </summary>
        public bool IsMoving => _isMoving;

        private void Awake()
        {
            // Cache component references (look in children for standard 2D workflow)
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            _animator = GetComponentInChildren<Animator>();

            if (_animator == null)
            {
                Debug.LogWarning($"[NpcAnimationDriver] {name}: No Animator found in children. " +
                               "Add an Animator component to a child object or this object for animations to work.", this);
            }

            if (_spriteRenderer == null)
            {
                Debug.LogError($"[NpcAnimationDriver] {name}: SpriteRenderer component not found in children! " +
                             "Add a SpriteRenderer to the Visuals child object.", this);
            }

            // Initialize to default state
            var (defaultCardinal, _) = TopDownFacing.GetDefault();
            _currentCardinal = defaultCardinal;

            // Cache available animator states for performance
            CacheAvailableStates();
        }

        private void Start()
        {
            // Play initial idle animation to avoid T-pose on scene load
            if (_animator != null)
            {
                var (stateHash, stateName) = GetAnimationState(false, _currentCardinal);
                PlayAnimationState(stateHash, stateName);
            }
        }

        /// <summary>
        /// Sets the movement velocity and updates animation state accordingly.
        /// Call this from any NPC controller to drive animations.
        /// </summary>
        /// <param name="velocity">Current movement velocity in units per second</param>
        public void SetMovement(Vector2 velocity)
        {
            _currentVelocity = velocity;
            _isMoving = velocity.magnitude > _movementThreshold;

            // Update facing direction and cardinal if moving
            if (_isMoving)
            {
                var (cardinal, flipX) = TopDownFacing.FromVector(velocity);
                _currentCardinal = cardinal;

                // Update sprite flip
                if (_flipSpriteOnDirection && _spriteRenderer != null)
                {
                    // Only flip for Side direction when facing left
                    if (_currentCardinal == FacingCardinal.Side)
                    {
                        _spriteRenderer.flipX = flipX;
                    }
                    else
                    {
                        // Up/Down: always face right (no flip)
                        _spriteRenderer.flipX = false;
                    }
                }
            }

            // Update animation state
            UpdateAnimationState();
        }

        /// <summary>
        /// Forces the NPC to face a specific direction without changing movement state.
        /// Useful for dialogue or interaction scenarios.
        /// </summary>
        /// <param name="direction">Direction to face</param>
        public void SetFacingDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.01f)
                return;

            var (cardinal, flipX) = TopDownFacing.FromVector(direction);
            _currentCardinal = cardinal;

            // Update sprite flip
            if (_flipSpriteOnDirection && _spriteRenderer != null)
            {
                if (_currentCardinal == FacingCardinal.Side)
                {
                    _spriteRenderer.flipX = flipX;
                }
                else
                {
                    _spriteRenderer.flipX = false;
                }
            }

            // Update animation to reflect new facing direction
            UpdateAnimationState();
        }

        /// <summary>
        /// Stops all movement and transitions to idle animation.
        /// </summary>
        public void Stop()
        {
            _currentVelocity = Vector2.zero;
            _isMoving = false;
            UpdateAnimationState();
        }

        /// <summary>
        /// Updates the animation state based on current movement and facing direction.
        /// </summary>
        private void UpdateAnimationState()
        {
            if (_animator == null)
                return;

            // Determine desired animation state
            (int stateHash, string stateName) = GetAnimationState(_isMoving, _currentCardinal);

            // Only play animation if state has changed
            if (stateHash != _currentStateHash)
            {
                PlayAnimationState(stateHash, stateName);
            }
        }

        /// <summary>
        /// Gets the animation state hash and name based on movement and cardinal direction.
        /// </summary>
        /// <param name="isMoving">Whether the NPC is currently moving</param>
        /// <param name="cardinal">Current cardinal facing direction</param>
        /// <returns>State hash and name for the desired animation</returns>
        private (int hash, string name) GetAnimationState(bool isMoving, FacingCardinal cardinal)
        {
            if (isMoving)
            {
                return cardinal switch
                {
                    FacingCardinal.Down => (MoveDownHash, "MoveDown"),
                    FacingCardinal.Up => (MoveUpHash, "MoveUp"),
                    FacingCardinal.Side => (MoveSideHash, "MoveSide"),
                    _ => (MoveDownHash, "MoveDown") // Fallback to Down
                };
            }
            else
            {
                return cardinal switch
                {
                    FacingCardinal.Down => (IdleDownHash, "IdleDown"),
                    FacingCardinal.Up => (IdleUpHash, "IdleUp"),
                    FacingCardinal.Side => (IdleSideHash, "IdleSide"),
                    _ => (IdleDownHash, "IdleDown") // Fallback to Down
                };
            }
        }

        /// <summary>
        /// Plays the specified animation state with graceful fallback handling.
        /// </summary>
        /// <param name="stateHash">Hash of the animation state to play</param>
        /// <param name="stateName">Name of the animation state (for debugging)</param>
        private void PlayAnimationState(int stateHash, string stateName)
        {
            // Check if the state exists in the animator
            if (!HasAnimatorState(stateHash))
            {
                if (_debugLog)
                {
                    Debug.LogWarning($"[NpcAnimationDriver] {name}: Animation state '{stateName}' not found in " +
                                   "Animator Controller. Add this state for proper NPC animations.", this);
                }
                return;
            }

            // Play the animation state
            _animator.Play(stateHash);
            _currentStateHash = stateHash;
            _currentStateName = stateName;

            if (_debugLog)
            {
                Debug.Log($"[NpcAnimationDriver] {name}: Playing animation state '{stateName}'", this);
            }
        }

        /// <summary>
        /// Caches all available animator states for performance.
        /// Called once in Awake to avoid repeated layer iteration.
        /// </summary>
        private void CacheAvailableStates()
        {
            _availableStateHashes = new System.Collections.Generic.HashSet<int>();
            
            if (_animator == null || _animator.runtimeAnimatorController == null)
                return;

            // Cache all state hashes we care about
            int[] statesToCheck = { IdleDownHash, IdleUpHash, IdleSideHash, MoveDownHash, MoveUpHash, MoveSideHash };
            
            foreach (int hash in statesToCheck)
            {
                for (int i = 0; i < _animator.layerCount; i++)
                {
                    if (_animator.HasState(i, hash))
                    {
                        _availableStateHashes.Add(hash);
                        break; // Found in this layer, no need to check other layers
                    }
                }
            }
        }

        /// <summary>
        /// Checks if the animator has a state with the given hash.
        /// Uses cached state set for performance.
        /// </summary>
        /// <param name="stateHash">Hash of the state to check</param>
        /// <returns>True if the state exists in the animator</returns>
        private bool HasAnimatorState(int stateHash)
        {
            return _availableStateHashes?.Contains(stateHash) ?? false;
        }

        /// <summary>
        /// Forces a specific animation state to play (for external control).
        /// Useful for custom animations like talking, crafting, etc.
        /// </summary>
        /// <param name="stateName">Name of the animation state to play</param>
        public void ForcePlayState(string stateName)
        {
            if (_animator == null)
                return;

            int stateHash = Animator.StringToHash(stateName);
            PlayAnimationState(stateHash, stateName);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-assign components in editor (look in children for standard 2D workflow)
            if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
        }

        /// <summary>
        /// Gets debug information about the current animation state.
        /// </summary>
        /// <returns>Debug string with current state info</returns>
        public string GetDebugInfo()
        {
            if (_animator == null)
                return "Animator not found";

            return $"State: {_currentStateName} | Moving: {_isMoving} | " +
                   $"Cardinal: {TopDownFacing.CardinalToString(_currentCardinal)} | " +
                   $"Velocity: {_currentVelocity:F2}";
        }
#endif
    }
}