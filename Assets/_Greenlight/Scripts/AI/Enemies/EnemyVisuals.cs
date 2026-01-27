using UnityEngine;

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

        [Header("Visual Effects")]
        [SerializeField, Tooltip("Default tint color for temporary effects.")]
        private Color _defaultTintColor = Color.white;

        // State
        private Vector2 _currentFacingDirection = Vector2.right;
        private Color _originalColor;
        private Color _originalWeaponColor;
        private bool _hasTemporaryTint;
        private bool _isFlickering;

        // Animation state hashes (for performance)
        private static readonly int IdleHash = Animator.StringToHash("Idle");
        private static readonly int ChaseHash = Animator.StringToHash("Chase");
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
        /// Main sprite renderer.
        /// </summary>
        public SpriteRenderer MainSpriteRenderer => _mainSpriteRenderer;

        private void Awake()
        {
            // Auto-assign components if not set
            if (_mainSpriteRenderer == null)
                _mainSpriteRenderer = GetComponent<SpriteRenderer>();

            if (_animator == null)
                _animator = GetComponent<Animator>();

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
        }

        #region Animation Control

        /// <summary>
        /// Play idle animation.
        /// </summary>
        public void PlayIdleAnimation()
        {
            PlayAnimation(IdleHash);
        }

        /// <summary>
        /// Play chase/movement animation.
        /// </summary>
        public void PlayChaseAnimation()
        {
            PlayAnimation(ChaseHash);
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
        /// Check if currently playing idle animation.
        /// </summary>
        /// <returns>True if idle animation is active</returns>
        public bool IsPlayingIdleAnimation()
        {
            return _animator != null && _animator.GetCurrentAnimatorStateInfo(0).shortNameHash == IdleHash;
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

            // Update sprite flip based on horizontal direction
            if (_flipSpriteForDirection && _mainSpriteRenderer != null)
            {
                // Determine if we should flip the sprite
                bool shouldFlipX = (_currentFacingDirection.x < 0f) != (_naturalFacingDirection < 0);
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

            if (_animator == null)
                _animator = GetComponent<Animator>();

            if (_visualTransform == null)
                _visualTransform = transform;
        }
#endif
    }
}