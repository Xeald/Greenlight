using UnityEngine;

namespace Greenlight.Combat
{
    /// <summary>
    /// Applies knockback effects with pixel-perfect movement (32 PPU).
    /// Uses Rigidbody2D.MovePosition() to respect the retro physics feel.
    /// 
    /// Design Philosophy:
    /// - Knockback force expressed in pixels/second, converted to units
    /// - Fixed duration knockback that decays over time
    /// - Compatible with hitstop (applies after Time.timeScale returns to 1)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Greenlight/Combat/Knockback Receiver")]
    public class KnockbackReceiver : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField, Tooltip("Combat settings for knockback parameters.")]
        private CombatSettingsSO _combatSettings;

        [Header("Debug")]
        [SerializeField, Tooltip("Enable debug logging for knockback events.")]
        private bool _debugLog = false;

        // Components
        private Rigidbody2D _rigidbody;

        // Knockback state
        private Vector2 _knockbackVelocity;
        private float _knockbackTimeRemaining;
        private bool _isKnockedBack;

        /// <summary>
        /// Is this entity currently being knocked back?
        /// </summary>
        public bool IsKnockedBack => _isKnockedBack;

        /// <summary>
        /// Current knockback velocity in units per second.
        /// </summary>
        public Vector2 KnockbackVelocity => _knockbackVelocity;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();

            // Ensure rigidbody is kinematic for manual movement control
            if (_rigidbody.bodyType != RigidbodyType2D.Kinematic)
            {
                Debug.LogWarning($"[KnockbackReceiver] {name}: Rigidbody2D should be Kinematic for pixel-perfect movement. Auto-correcting.");
                _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            }
        }

        private void FixedUpdate()
        {
            if (!_isKnockedBack || _knockbackTimeRemaining <= 0f)
            {
                if (_isKnockedBack)
                    StopKnockback();
                return;
            }

            // Apply knockback movement using MovePosition for pixel-perfect control
            // Note: Uses Time.fixedDeltaTime (scaled with timeScale) for physics consistency.
            // This ensures knockback respects hitstop (timeScale = 0) and other time effects.
            // If you need knockback to ignore timeScale, use Time.fixedUnscaledDeltaTime instead.
            Vector2 currentPos = _rigidbody.position;
            Vector2 movement = _knockbackVelocity * Time.fixedDeltaTime;
            Vector2 newPos = currentPos + movement;

            // Snap to pixel grid (32 PPU)
            newPos.x = Mathf.Round(newPos.x * 32f) / 32f;
            newPos.y = Mathf.Round(newPos.y * 32f) / 32f;

            _rigidbody.MovePosition(newPos);

            // Decay knockback over time
            float decayFactor = _knockbackTimeRemaining / (_combatSettings?.KnockbackDuration ?? 0.3f);
            _knockbackVelocity = _knockbackVelocity.normalized * (_combatSettings?.KnockbackForceUnits ?? 5f) * decayFactor;

            _knockbackTimeRemaining -= Time.fixedDeltaTime;

            if (_debugLog)
            {
                Debug.Log($"[KnockbackReceiver] {name}: Velocity={_knockbackVelocity.magnitude:F2} u/s, Time={_knockbackTimeRemaining:F2}s");
            }
        }

        /// <summary>
        /// Apply knockback in the specified direction.
        /// </summary>
        /// <param name="direction">Normalized knockback direction</param>
        /// <param name="forceOverride">Optional force override (uses settings if null)</param>
        /// <param name="durationOverride">Optional duration override (uses settings if null)</param>
        public void ApplyKnockback(Vector2 direction, float? forceOverride = null, float? durationOverride = null)
        {
            if (_combatSettings == null)
            {
                Debug.LogError($"[KnockbackReceiver] {name}: No CombatSettingsSO assigned!");
                return;
            }

            // Normalize direction
            direction = direction.normalized;

            // Apply knockback
            float force = forceOverride ?? _combatSettings.KnockbackForceUnits;
            float duration = durationOverride ?? _combatSettings.KnockbackDuration;

            _knockbackVelocity = direction * force;
            _knockbackTimeRemaining = duration;
            _isKnockedBack = true;

            if (_debugLog)
            {
                Debug.Log($"[KnockbackReceiver] {name}: Applied knockback - Direction={direction}, Force={force:F2} u/s, Duration={duration:F2}s");
            }
        }

        /// <summary>
        /// Apply knockback away from a source transform.
        /// </summary>
        /// <param name="source">Transform to knock back away from</param>
        /// <param name="forceOverride">Optional force override</param>
        /// <param name="durationOverride">Optional duration override</param>
        public void ApplyKnockbackFromSource(Transform source, float? forceOverride = null, float? durationOverride = null)
        {
            if (source == null)
            {
                Debug.LogWarning($"[KnockbackReceiver] {name}: Knockback source is null!");
                return;
            }

            Vector2 direction = (transform.position - source.position).normalized;
            ApplyKnockback(direction, forceOverride, durationOverride);
        }

        /// <summary>
        /// Stop knockback immediately.
        /// </summary>
        public void StopKnockback()
        {
            _isKnockedBack = false;
            _knockbackVelocity = Vector2.zero;
            _knockbackTimeRemaining = 0f;

            if (_debugLog)
            {
                Debug.Log($"[KnockbackReceiver] {name}: Knockback stopped");
            }
        }

        /// <summary>
        /// Check if knockback can be applied (not already knocked back, unless overriding).
        /// </summary>
        /// <param name="allowOverride">If true, allows overriding existing knockback</param>
        /// <returns>True if knockback can be applied</returns>
        public bool CanApplyKnockback(bool allowOverride = false)
        {
            return !_isKnockedBack || allowOverride;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-assign rigidbody and check kinematic setting
            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody2D>();

            if (_rigidbody != null && _rigidbody.bodyType != RigidbodyType2D.Kinematic)
            {
                Debug.LogWarning($"[KnockbackReceiver] {name}: Rigidbody2D should be Kinematic for pixel-perfect movement.");
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_isKnockedBack && Application.isPlaying)
            {
                // Draw knockback velocity vector
                Gizmos.color = Color.red;
                Vector3 start = transform.position;
                Vector3 end = start + (Vector3)_knockbackVelocity.normalized * 2f;
                Gizmos.DrawLine(start, end);
                Gizmos.DrawSphere(end, 0.1f);
            }
        }
#endif
    }
}