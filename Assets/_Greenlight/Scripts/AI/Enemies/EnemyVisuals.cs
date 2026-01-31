using UnityEngine;
using Greenlight.Core.Animation;

namespace Greenlight.AI
{
    /// <summary>
    /// Handles visual aspects of enemies - animations, sprites, and visual effects.
    /// Separates visual logic from AI/combat logic for clean architecture.
    /// 
    /// Design Philosophy:
    /// - Pixel-perfect sprite management (32 PPU)
    /// - Animation state coordination with AI states  
    /// - Visual feedback for telegraphs and state changes
    /// </summary>
    [AddComponentMenu("Greenlight/AI/Enemy Visuals")]
    public class EnemyVisuals : MonoBehaviour
    {
        [Header("Sprite Components")]
        [SerializeField, Tooltip("Main sprite renderer for the enemy body.")]
        private SpriteRenderer _mainSpriteRenderer;

        [SerializeField, Tooltip("Optional weapon/accessory sprite renderer.")]
        private SpriteRenderer _weaponSpriteRenderer;

        [SerializeField, Tooltip("Transform to apply visual effects to (usually the main sprite).")]
        private Transform _visualTransform;

        [Header("Animation")]
        [SerializeField, Tooltip("Animator component for sprite animations.")]
        private Animator _animator;

        [Header("Facing Direction")]
        [SerializeField, Tooltip("Should the sprite flip horizontally when facing direction changes?")]
        private bool _flipSpriteForDirection = true;

        [SerializeField, Tooltip("Direction the sprite naturally faces (right = 1, left = -1).")]
        private int _naturalFacingDirection = 1;

        // State
        private Vector2 _currentFacingDirection = Vector2.right;
        private FacingCardinal _currentCardinal = FacingCardinal.Down;
        private Color _originalColor;
        private Color _originalWeaponColor;
        private bool _hasTemporaryTint;
        private bool _isFlickering;
        private System.Collections.Generic.HashSet<int> _availableStateHashes;

        // Animation state hashes (for performance)
        // Directional Idle states
        private static readonly int IdleDownHash = Animator.StringToHash("IdleDown");
        private static readonly int IdleUpHash = Animator.StringToHash("IdleUp");
        private static readonly int IdleSideHash = Animator.StringToHash("IdleSide");
        
        // Directional Chase states
        private static readonly int ChaseDownHash = Animator.StringToHash("ChaseDown");
        private static readonly int ChaseUpHash = Animator.StringToHash("ChaseUp");
        private static readonly int ChaseSideHash = Animator.StringToHash("ChaseSide");
        
        // Legacy single-direction states (preserved for backward compatibility)
        private static readonly int IdleHash = Animator.StringToHash("Idle");
        private static readonly int ChaseHash = Animator.StringToHash("Chase");
        
        // Non-directional states (preserved as single-direction)
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int TelegraphHash = Animator.StringToHash("Telegraph");
        private static readonly int StunnedHash = Animator.StringToHash("Stunned");
        private static readonly int DeathHash = Animator.StringToHash("Death");
        private static readonly int RecoveryHash = Animator.StringToHash("Recovery");
        private static readonly int AlertHash = Animator.StringToHash("Alert");

        /// <summary>
        /// Current facing direction (normalized vector).
        /// </summary>
        public Vector2 CurrentFacingDirection => _currentFacingDirection;

        /// <summary>
        /// Current cardinal facing direction for animation system.
        /// </summary>
        public FacingCardinal CurrentCardinal => _currentCardinal;

        /// <summary>
        /// Main sprite renderer.
        /// </summary>
        public SpriteRenderer MainSpriteRenderer => _mainSpriteRenderer;

        private void Awake()
        {
            // Auto-assign components if not set
            if (_mainSpriteRenderer == null)
                _mainSpriteRenderer = GetComponent<SpriteRenderer>();

            // Look for Animator in children first (preferred for 2D workflow), then on this object
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
                if (_animator == null)
                    _animator = GetComponent<Animator>();
            }

            if (_visualTransform == null)
                _visualTransform = transform;

            // Store original colors
            if (_mainSpriteRenderer != null)
            {
                _originalColor = _mainSpriteRenderer.color;
            }

            if (_weaponSpriteRenderer != null)
            {
                _originalWeaponColor = _weaponSpriteRenderer.color;
            }

            // Initialize cardinal direction to default (Down)
            var (defaultCardinal, _) = TopDownFacing.GetDefault();
            _currentCardinal = defaultCardinal;

            // Cache available animator states for performance
            CacheAvailableStates();
        }

        #region Animation Control

        /// <summary>
        /// Play idle animation based on current facing direction.
        /// </summary>
        public void PlayIdleAnimation()
        {
            int stateHash = GetDirectionalIdleHash(_currentCardinal);
            PlayAnimationWithFallback(stateHash, IdleHash, "Idle");
        }

        /// <summary>
        /// Play chase/movement animation based on current facing direction.
        /// </summary>
        public void PlayChaseAnimation()
        {
            int stateHash = GetDirectionalChaseHash(_currentCardinal);
            PlayAnimationWithFallback(stateHash, ChaseHash, "Chase");
        }

        /// <summary>
        /// Play attack animation.
        /// </summary>
        public void PlayAttackAnimation()
        {
            PlayAnimation(AttackHash);
        }

        /// <summary>
        /// Play telegraph animation (warning before attack).
        /// </summary>
        public void PlayTelegraphAnimation()
        {
            PlayAnimation(TelegraphHash);
        }

        /// <summary>
        /// Play stunned animation.
        /// </summary>
        public void PlayStunnedAnimation()
        {
            PlayAnimation(StunnedHash);
        }

        /// <summary>
        /// Play death animation.
        /// </summary>
        public void PlayDeathAnimation()
        {
            PlayAnimation(DeathHash);
        }

        /// <summary>
        /// Play recovery animation (after attack or stun).
        /// </summary>
        public void PlayRecoveryAnimation()
        {
            PlayAnimation(RecoveryHash);
        }

        /// <summary>
        /// Play alert animation (when detecting player).
        /// </summary>
        public void PlayAlertAnimation()
        {
            PlayAnimation(AlertHash);
        }

        /// <summary>
        /// Play a specific animation by hash.
        /// </summary>
        /// <param name="animationHash">Animation state hash</param>
        private void PlayAnimation(int animationHash)
        {
            if (_animator != null)
            {
                _animator.Play(animationHash);
            }
        }

        /// <summary>
        /// Play animation with fallback to legacy state if directional state doesn't exist.
        /// </summary>
        /// <param name="preferredHash">Preferred directional state hash</param>
        /// <param name="fallbackHash">Fallback legacy state hash</param>
        /// <param name="stateName">State name for debugging</param>
        private void PlayAnimationWithFallback(int preferredHash, int fallbackHash, string stateName)
        {
            if (_animator == null)
                return;

            // Try directional state first
            if (HasAnimatorState(preferredHash))
            {
                _animator.Play(preferredHash);
            }
            // Fall back to legacy single-direction state
            else if (HasAnimatorState(fallbackHash))
            {
                _animator.Play(fallbackHash);
            }
            else
            {
                Debug.LogWarning($"[EnemyVisuals] {name}: Neither directional nor legacy '{stateName}' " +
                               "animation state found in Animator Controller.", this);
            }
        }

        /// <summary>
        /// Gets the appropriate directional idle state hash based on cardinal direction.
        /// </summary>
        /// <param name="cardinal">Cardinal facing direction</param>
        /// <returns>Hash for the directional idle state</returns>
        private int GetDirectionalIdleHash(FacingCardinal cardinal)
        {
            return cardinal switch
            {
                FacingCardinal.Down => IdleDownHash,
                FacingCardinal.Up => IdleUpHash,
                FacingCardinal.Side => IdleSideHash,
                _ => IdleDownHash // Default fallback
            };
        }

        /// <summary>
        /// Gets the appropriate directional chase state hash based on cardinal direction.
        /// </summary>
        /// <param name="cardinal">Cardinal facing direction</param>
        /// <returns>Hash for the directional chase state</returns>
        private int GetDirectionalChaseHash(FacingCardinal cardinal)
        {
            return cardinal switch
            {
                FacingCardinal.Down => ChaseDownHash,
                FacingCardinal.Up => ChaseUpHash,
                FacingCardinal.Side => ChaseSideHash,
                _ => ChaseDownHash // Default fallback
            };
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

            // Cache all state hashes we care about (directional + legacy + non-directional)
            int[] statesToCheck = { 
                IdleDownHash, IdleUpHash, IdleSideHash, 
                ChaseDownHash, ChaseUpHash, ChaseSideHash,
                IdleHash, ChaseHash,  // Legacy states
                AttackHash, TelegraphHash, StunnedHash, DeathHash, RecoveryHash, AlertHash  // Non-directional
            };
            
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
        /// Check if currently playing any idle animation (directional or legacy).
        /// </summary>
        /// <returns>True if any idle animation is active</returns>
        public bool IsPlayingIdleAnimation()
        {
            if (_animator == null)
                return false;

            int currentStateHash = _animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            
            // Check directional idle states
            if (currentStateHash == IdleDownHash || 
                currentStateHash == IdleUpHash || 
                currentStateHash == IdleSideHash)
            {
                return true;
            }

            // Check legacy idle state
            return currentStateHash == IdleHash;
        }

        /// <summary>
        /// Get the duration of the death animation.
        /// </summary>
        /// <returns>Death animation duration in seconds</returns>
        public float GetDeathAnimationDuration()
        {
            if (_animator == null)
                return 1f; // Default duration

            // Get the death animation clip length
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.shortNameHash == DeathHash)
            {
                return stateInfo.length;
            }

            // Fallback: try to get the clip directly
            AnimationClip[] clips = _animator.runtimeAnimatorController.animationClips;
            foreach (var clip in clips)
            {
                if (clip.name.ToLower().Contains("death"))
                {
                    return clip.length;
                }
            }

            return 1f; // Default fallback
        }

        #endregion

        #region Facing Direction

        /// <summary>
        /// Set the facing direction and update sprite orientation.
        /// </summary>
        /// <param name="direction">Direction to face (normalized)</param>
        public void SetFacingDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.01f)
                return; // Ignore zero/tiny directions

            _currentFacingDirection = direction.normalized;

            // Get cardinal direction and flip decision from TopDownFacing helper
            var (cardinal, flipX) = TopDownFacing.FromVector(_currentFacingDirection);
            _currentCardinal = cardinal;

            // Update sprite flip based on cardinal direction and helper decision
            if (_flipSpriteForDirection && _mainSpriteRenderer != null)
            {
                bool shouldFlipX;
                
                if (_currentCardinal == FacingCardinal.Side)
                {
                    // Use TopDownFacing decision for consistent behavior
                    shouldFlipX = flipX != (_naturalFacingDirection < 0);
                }
                else
                {
                    // Up/Down: never flip for clean vertical sprites
                    shouldFlipX = false;
                }

                _mainSpriteRenderer.flipX = shouldFlipX;

                // Also flip weapon sprite if present
                if (_weaponSpriteRenderer != null)
                {
                    _weaponSpriteRenderer.flipX = shouldFlipX;
                }
            }
        }

        /// <summary>
        /// Get facing direction as a simple left (-1) or right (1) value.
        /// </summary>
        /// <returns>-1 for left, 1 for right</returns>
        public int GetFacingSign()
        {
            return _currentFacingDirection.x < 0f ? -1 : 1;
        }

        #endregion

        #region Visual Effects

        /// <summary>
        /// Set a temporary color tint on the sprite.
        /// </summary>
        /// <param name="tintColor">Color to tint with</param>
        /// <param name="intensity">Tint intensity (0-1)</param>
        public void SetTemporaryTint(Color tintColor, float intensity = 0.5f)
        {
            if (_mainSpriteRenderer == null)
                return;

            Color targetColor = Color.Lerp(_originalColor, tintColor, intensity);
            _mainSpriteRenderer.color = targetColor;

            if (_weaponSpriteRenderer != null)
            {
                Color weaponTargetColor = Color.Lerp(_originalWeaponColor, tintColor, intensity);
                _weaponSpriteRenderer.color = weaponTargetColor;
            }

            _hasTemporaryTint = true;
        }

        /// <summary>
        /// Clear any temporary color tint.
        /// </summary>
        public void ClearTemporaryTint()
        {
            if (!_hasTemporaryTint || _mainSpriteRenderer == null)
                return;

            _mainSpriteRenderer.color = _originalColor;

            if (_weaponSpriteRenderer != null)
            {
                _weaponSpriteRenderer.color = _originalWeaponColor;
            }

            _hasTemporaryTint = false;
        }

        /// <summary>
        /// Start a flicker effect (for damage indication, etc.).
        /// </summary>
        /// <param name="duration">Flicker duration in seconds</param>
        /// <param name="flickerRate">Flickers per second</param>
        public async void StartFlicker(float duration = 0.5f, float flickerRate = 10f)
        {
            if (_isFlickering || _mainSpriteRenderer == null)
                return;

            _isFlickering = true;
            float flickerInterval = 1f / flickerRate;
            float endTime = Time.time + duration;
            bool isVisible = true;

            while (Time.time < endTime && _isFlickering)
            {
                // Toggle visibility
                isVisible = !isVisible;
                float alpha = isVisible ? 1f : 0.3f;

                Color color = _mainSpriteRenderer.color;
                color.a = alpha;
                _mainSpriteRenderer.color = color;

                if (_weaponSpriteRenderer != null)
                {
                    Color weaponColor = _weaponSpriteRenderer.color;
                    weaponColor.a = alpha;
                    _weaponSpriteRenderer.color = weaponColor;
                }

                try
                {
                    await Awaitable.WaitForSecondsAsync(flickerInterval, destroyCancellationToken);
                }
                catch (System.OperationCanceledException)
                {
                    break;
                }
            }

            // Ensure we end visible
            if (_mainSpriteRenderer != null)
            {
                Color finalColor = _mainSpriteRenderer.color;
                finalColor.a = 1f;
                _mainSpriteRenderer.color = finalColor;
            }

            if (_weaponSpriteRenderer != null)
            {
                Color finalWeaponColor = _weaponSpriteRenderer.color;
                finalWeaponColor.a = 1f;
                _weaponSpriteRenderer.color = finalWeaponColor;
            }

            _isFlickering = false;
        }

        /// <summary>
        /// Stop any ongoing flicker effect.
        /// </summary>
        public void StopFlicker()
        {
            _isFlickering = false;
            
            if (_mainSpriteRenderer != null)
            {
                Color color = _mainSpriteRenderer.color;
                color.a = 1f;
                _mainSpriteRenderer.color = color;
            }

            if (_weaponSpriteRenderer != null)
            {
                Color weaponColor = _weaponSpriteRenderer.color;
                weaponColor.a = 1f;
                _weaponSpriteRenderer.color = weaponColor;
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Reset all visual effects to default state.
        /// </summary>
        public void ResetToDefault()
        {
            ClearTemporaryTint();
            StopFlicker();
            SetFacingDirection(Vector2.right);
            PlayIdleAnimation();
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-assign components
            if (_mainSpriteRenderer == null)
                _mainSpriteRenderer = GetComponent<SpriteRenderer>();

            // Look for Animator in children first (preferred for 2D workflow), then on this object
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
                if (_animator == null)
                    _animator = GetComponent<Animator>();
            }

            if (_visualTransform == null)
                _visualTransform = transform;
        }
#endif
    }
}