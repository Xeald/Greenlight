using UnityEngine;
using Greenlight.Core.Animation;

namespace Greenlight.Player
{
    /// <summary>
    /// Drives player animation states based on movement and facing direction.
    /// Switches between directional Idle and Move animations based on PlayerMotor and PlayerVisuals state.
    /// 
    /// Design Philosophy:
    /// - Cache animator reference for performance (no GetComponent in Update)
    /// - Only play animation when state actually changes (prevents clip restart)
    /// - Graceful fallback if animation states don't exist (log warning, don't crash)
    /// - Uses TopDownFacing helper for consistent cardinal direction logic
    /// </summary>
    [AddComponentMenu("Greenlight/Player/Player Animation Driver")]
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerVisuals))]
    public class PlayerAnimationDriver : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField, Tooltip("Enable debug logging for animation state changes.")]
        private bool _debugLog = false;

        // Cached component references
        private Animator _animator;
        private PlayerMotor _motor;
        private PlayerVisuals _visuals;

        // Animation state tracking
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

        private void Awake()
        {
            // Cache component references
            _motor = GetComponent<PlayerMotor>();
            _visuals = GetComponent<PlayerVisuals>();
            _animator = GetComponentInChildren<Animator>();

            if (_animator == null)
            {
                Debug.LogError($"[PlayerAnimationDriver] {name}: No Animator found in children! " +
                              "Add an Animator component to the Visuals child object.", this);
            }

            if (_motor == null)
            {
                Debug.LogError($"[PlayerAnimationDriver] {name}: PlayerMotor component not found!", this);
            }

            if (_visuals == null)
            {
                Debug.LogError($"[PlayerAnimationDriver] {name}: PlayerVisuals component not found!", this);
            }

            // Cache available animator states for performance
            CacheAvailableStates();
        }

        private void Start()
        {
            // Play initial idle animation to avoid T-pose on scene load
            if (_animator != null && _visuals != null)
            {
                var (stateHash, stateName) = GetAnimationState(false, _visuals.CurrentCardinal);
                PlayAnimationState(stateHash, stateName);
            }
        }

        private void Update()
        {
            if (_animator == null || _motor == null || _visuals == null)
                return;

            // Determine desired animation state based on movement and facing
            bool isMoving = _motor.IsMoving;
            FacingCardinal cardinal = _visuals.CurrentCardinal;

            // Get the appropriate state hash and name
            (int stateHash, string stateName) = GetAnimationState(isMoving, cardinal);

            // Only play animation if state has changed
            if (stateHash != _currentStateHash)
            {
                PlayAnimationState(stateHash, stateName);
            }
        }

        /// <summary>
        /// Gets the animation state hash and name based on movement and cardinal direction.
        /// </summary>
        /// <param name="isMoving">Whether the player is currently moving</param>
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
                Debug.LogWarning($"[PlayerAnimationDriver] {name}: Animation state '{stateName}' not found in " +
                               $"Animator Controller. Please add this state to your Player.controller.", this);
                return;
            }

            // Play the animation state
            _animator.Play(stateHash);
            _currentStateHash = stateHash;
            _currentStateName = stateName;

            if (_debugLog)
            {
                Debug.Log($"[PlayerAnimationDriver] {name}: Playing animation state '{stateName}'", this);
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
            // Auto-assign components in editor
            if (_motor == null) _motor = GetComponent<PlayerMotor>();
            if (_visuals == null) _visuals = GetComponent<PlayerVisuals>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
        }

        /// <summary>
        /// Gets debug information about the current animation state.
        /// </summary>
        /// <returns>Debug string with current state info</returns>
        public string GetDebugInfo()
        {
            if (_animator == null || _motor == null || _visuals == null)
                return "Components not initialized";

            return $"State: {_currentStateName} | Moving: {_motor.IsMoving} | " +
                   $"Cardinal: {TopDownFacing.CardinalToString(_visuals.CurrentCardinal)}";
        }
#endif
    }
}