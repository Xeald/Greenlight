using UnityEngine;

namespace Greenlight.Combat
{
    /// <summary>
    /// Manages invincibility frames (iframes) after taking damage.
    /// Provides visual feedback through sprite flickering.
    /// 
    /// Design Philosophy:
    /// - Integrates with HealthComponent for damage immunity
    /// - Visual flicker using alpha modulation
    /// - Pixel-perfect timing using Awaitable for frame precision
    /// </summary>
    [AddComponentMenu("Greenlight/Combat/Invincibility Controller")]
    public class InvincibilityController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField, Tooltip("Combat settings for invincibility parameters.")]
        private CombatSettingsSO _combatSettings;

        [Header("Visual Components")]
        [SerializeField, Tooltip("Sprite renderer to flicker during invincibility.")]
        private SpriteRenderer _spriteRenderer;

        [Header("Debug")]
        [SerializeField, Tooltip("Enable debug logging for invincibility events.")]
        private bool _debugLog = false;

        // State
        private bool _isInvulnerable;
        private float _invulnerabilityTimeRemaining;
        private bool _isFlickering;
        private float _originalAlpha;

        /// <summary>
        /// Is this entity currently invulnerable to damage?
        /// </summary>
        public bool IsInvulnerable => _isInvulnerable;

        /// <summary>
        /// Time remaining for invulnerability (seconds).
        /// </summary>
        public float InvulnerabilityTimeRemaining => _invulnerabilityTimeRemaining;

        private void Awake()
        {
            // Auto-assign sprite renderer if not set
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            // Store original alpha
            if (_spriteRenderer != null)
                _originalAlpha = _spriteRenderer.color.a;
        }

        private void Update()
        {
            if (_isInvulnerable)
            {
                _invulnerabilityTimeRemaining -= Time.deltaTime;

                if (_invulnerabilityTimeRemaining <= 0f)
                {
                    StopInvincibility();
                }
            }
        }

        /// <summary>
        /// Start invincibility frames with visual flicker.
        /// </summary>
        /// <param name="durationOverride">Optional duration override (uses settings if null)</param>
        public void StartInvincibility(float? durationOverride = null)
        {
            if (_combatSettings == null)
            {
                Debug.LogError($"[InvincibilityController] {name}: No CombatSettingsSO assigned!");
                return;
            }

            float duration = durationOverride ?? _combatSettings.InvulnerabilityDuration;
            _invulnerabilityTimeRemaining = duration;
            _isInvulnerable = true;

            // Start visual flicker
            if (_spriteRenderer != null && !_isFlickering)
            {
                StartFlickerAsync();
            }

            if (_debugLog)
            {
                Debug.Log($"[InvincibilityController] {name}: Started invincibility for {duration:F2}s");
            }
        }

        /// <summary>
        /// Stop invincibility immediately.
        /// </summary>
        public void StopInvincibility()
        {
            _isInvulnerable = false;
            _invulnerabilityTimeRemaining = 0f;

            // Stop visual flicker and restore original appearance
            StopFlicker();

            if (_debugLog)
            {
                Debug.Log($"[InvincibilityController] {name}: Stopped invincibility");
            }
        }

        /// <summary>
        /// Extend current invincibility duration (if already invulnerable).
        /// </summary>
        /// <param name="additionalTime">Additional time to add (seconds)</param>
        public void ExtendInvincibility(float additionalTime)
        {
            if (_isInvulnerable)
            {
                _invulnerabilityTimeRemaining += additionalTime;

                if (_debugLog)
                {
                    Debug.Log($"[InvincibilityController] {name}: Extended invincibility by {additionalTime:F2}s");
                }
            }
        }

        /// <summary>
        /// Start the flicker effect asynchronously.
        /// </summary>
        private async void StartFlickerAsync()
        {
            if (_spriteRenderer == null || _combatSettings == null)
                return;

            _isFlickering = true;
            float flickerInterval = _combatSettings.FlickerInterval;
            float flickerAlpha = _combatSettings.FlickerAlpha;

            bool isVisible = true;

            while (_isInvulnerable && _isFlickering)
            {
                // Toggle visibility
                isVisible = !isVisible;
                Color color = _spriteRenderer.color;
                color.a = isVisible ? _originalAlpha : flickerAlpha;
                _spriteRenderer.color = color;

                // Wait for flicker interval
                try
                {
                    await Awaitable.WaitForSecondsAsync(flickerInterval, destroyCancellationToken);
                }
                catch (System.OperationCanceledException)
                {
                    // Object was destroyed or operation cancelled
                    break;
                }
            }

            // Ensure we end visible if the loop exits
            if (_spriteRenderer != null)
            {
                Color finalColor = _spriteRenderer.color;
                finalColor.a = _originalAlpha;
                _spriteRenderer.color = finalColor;
            }

            _isFlickering = false;
        }

        /// <summary>
        /// Stop the flicker effect and restore original appearance.
        /// </summary>
        private void StopFlicker()
        {
            _isFlickering = false;

            if (_spriteRenderer != null)
            {
                Color color = _spriteRenderer.color;
                color.a = _originalAlpha;
                _spriteRenderer.color = color;
            }
        }

        /// <summary>
        /// Force refresh the original alpha value (call after sprite changes).
        /// </summary>
        public void RefreshOriginalAlpha()
        {
            if (_spriteRenderer != null)
                _originalAlpha = _spriteRenderer.color.a;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-assign sprite renderer
            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            // Update original alpha if sprite renderer is available
            if (_spriteRenderer != null && Application.isPlaying == false)
                _originalAlpha = _spriteRenderer.color.a;
        }

        private void OnDrawGizmosSelected()
        {
            if (_isInvulnerable && Application.isPlaying)
            {
                // Draw invincibility indicator
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, 0.5f);

                // Draw timer arc
                float progress = 1f - (_invulnerabilityTimeRemaining / (_combatSettings?.InvulnerabilityDuration ?? 1f));
                float angle = progress * 360f;
                
                Vector3 start = transform.position + Vector3.up * 0.8f;
                for (int i = 0; i < angle; i += 10)
                {
                    float rad = i * Mathf.Deg2Rad;
                    Vector3 point = start + new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0) * 0.3f;
                    Gizmos.DrawSphere(point, 0.05f);
                }
            }
        }
#endif
    }
}