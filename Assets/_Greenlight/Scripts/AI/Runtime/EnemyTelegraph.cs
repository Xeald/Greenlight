using UnityEngine;

namespace Greenlight.AI
{
    /// <summary>
    /// Manages telegraph effects for enemy attacks.
    /// Implements "Combat as Conversation" pillar with clear visual and audio cues.
    /// 
    /// Design Philosophy:
    /// - Every attack must "speak" before striking
    /// - Visual and audio feedback combined for clarity
    /// - Pixel-perfect effects that respect 32 PPU standard
    /// </summary>
    [AddComponentMenu("Greenlight/AI/Enemy Telegraph")]
    public class EnemyTelegraph : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField, Tooltip("Telegraph settings for this enemy.")]
        private TelegraphSettingsSO _telegraphSettings;

        [Header("Visual Components")]
        [SerializeField, Tooltip("Sprite renderer to apply telegraph effects to.")]
        private SpriteRenderer _targetSpriteRenderer;

        [SerializeField, Tooltip("Optional additional sprite renderer (e.g., weapon, shield).")]
        private SpriteRenderer _secondarySpriteRenderer;

        [Header("Audio")]
        [SerializeField, Tooltip("Audio source for telegraph sound effects.")]
        private AudioSource _audioSource;

        [Header("Debug")]
        [SerializeField, Tooltip("Enable debug logging for telegraph events.")]
        private bool _debugLog = false;

        // State
        private bool _isTelegraphing;
        private float _telegraphStartTime;
        private Color _originalColor;
        private Color _originalSecondaryColor;
        private Vector3 _originalScale;
        private GameObject _spawnedParticleEffect;

        /// <summary>
        /// Is telegraph currently active?
        /// </summary>
        public bool IsTelegraphing => _isTelegraphing;

        /// <summary>
        /// Normalized progress through telegraph (0-1).
        /// </summary>
        public float TelegraphProgress
        {
            get
            {
                if (!_isTelegraphing || _telegraphSettings == null)
                    return 0f;

                float elapsed = Time.time - _telegraphStartTime;
                return Mathf.Clamp01(elapsed / _telegraphSettings.Duration);
            }
        }

        private void Awake()
        {
            // Auto-assign components if not set
            if (_targetSpriteRenderer == null)
                _targetSpriteRenderer = GetComponent<SpriteRenderer>();

            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();

            // Store original values
            if (_targetSpriteRenderer != null)
            {
                _originalColor = _targetSpriteRenderer.color;
                _originalScale = transform.localScale;
            }

            if (_secondarySpriteRenderer != null)
            {
                _originalSecondaryColor = _secondarySpriteRenderer.color;
            }
        }

        private void Update()
        {
            if (_isTelegraphing)
            {
                UpdateTelegraphEffects();
            }
        }

        /// <summary>
        /// Start the telegraph effect.
        /// </summary>
        public async void StartTelegraph()
        {
            if (_telegraphSettings == null)
            {
                Debug.LogError($"[EnemyTelegraph] {name}: No TelegraphSettingsSO assigned!");
                return;
            }

            if (_isTelegraphing)
            {
                Debug.LogWarning($"[EnemyTelegraph] {name}: Telegraph already in progress!");
                return;
            }

            _isTelegraphing = true;
            _telegraphStartTime = Time.time;

            if (_debugLog)
            {
                Debug.Log($"[EnemyTelegraph] {name}: Started telegraph for {_telegraphSettings.Duration:F2}s");
            }

            // Apply start delay if specified
            if (_telegraphSettings.StartDelay > 0f)
            {
                try
                {
                    await Awaitable.WaitForSecondsAsync(_telegraphSettings.StartDelay, destroyCancellationToken);
                }
                catch (System.OperationCanceledException)
                {
                    return; // Object destroyed
                }

                if (!_isTelegraphing) return; // Telegraph was stopped during delay
            }

            // Start visual and audio effects
            StartTelegraphEffects();

            // Auto-stop after duration
            try
            {
                await Awaitable.WaitForSecondsAsync(_telegraphSettings.Duration, destroyCancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                return; // Object destroyed
            }

            if (_isTelegraphing)
            {
                StopTelegraph();
            }
        }

        /// <summary>
        /// Stop the telegraph effect immediately.
        /// </summary>
        public void StopTelegraph()
        {
            if (!_isTelegraphing)
                return;

            _isTelegraphing = false;

            // Restore original appearance
            RestoreOriginalAppearance();

            // Clean up particle effects
            CleanupParticleEffects();

            if (_debugLog)
            {
                Debug.Log($"[EnemyTelegraph] {name}: Stopped telegraph");
            }
        }

        /// <summary>
        /// Start telegraph visual and audio effects.
        /// </summary>
        private void StartTelegraphEffects()
        {
            // Play telegraph sound effect
            if (_audioSource != null && _telegraphSettings.TelegraphSFX != null)
            {
                _audioSource.clip = _telegraphSettings.TelegraphSFX;
                _audioSource.volume = _telegraphSettings.SFXVolume;
                _audioSource.pitch = _telegraphSettings.SFXPitch;
                _audioSource.Play();
            }

            // Spawn particle effect if available
            if (_telegraphSettings.ParticleEffectPrefab != null)
            {
                Vector3 spawnPosition = transform.position + _telegraphSettings.ParticleOffset;
                _spawnedParticleEffect = Instantiate(
                    _telegraphSettings.ParticleEffectPrefab,
                    spawnPosition,
                    Quaternion.identity,
                    transform // Parent to enemy so it follows
                );
            }
        }

        /// <summary>
        /// Update telegraph effects each frame.
        /// </summary>
        private void UpdateTelegraphEffects()
        {
            if (_telegraphSettings == null || _targetSpriteRenderer == null)
                return;

            float progress = TelegraphProgress;

            // Update color/flash effect
            Color currentColor = _telegraphSettings.GetColorAtTime(_originalColor, progress);
            _targetSpriteRenderer.color = currentColor;

            // Update secondary sprite if available
            if (_secondarySpriteRenderer != null)
            {
                Color secondaryColor = _telegraphSettings.GetColorAtTime(_originalSecondaryColor, progress);
                _secondarySpriteRenderer.color = secondaryColor;
            }

            // Update scale effect
            if (_telegraphSettings.EnableScaleEffect)
            {
                float scaleMultiplier = _telegraphSettings.GetScaleAtTime(progress);
                transform.localScale = _originalScale * scaleMultiplier;
            }
        }

        /// <summary>
        /// Restore the enemy's original appearance.
        /// </summary>
        private void RestoreOriginalAppearance()
        {
            if (_targetSpriteRenderer != null)
            {
                _targetSpriteRenderer.color = _originalColor;
            }

            if (_secondarySpriteRenderer != null)
            {
                _secondarySpriteRenderer.color = _originalSecondaryColor;
            }

            transform.localScale = _originalScale;

            // Stop audio
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
        }

        /// <summary>
        /// Clean up spawned particle effects.
        /// </summary>
        private void CleanupParticleEffects()
        {
            if (_spawnedParticleEffect != null)
            {
                // Stop emission and let existing particles fade
                var particleSystem = _spawnedParticleEffect.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                {
                    particleSystem.Stop();
                }

                // Destroy after particles finish
                Destroy(_spawnedParticleEffect, 2f);
                _spawnedParticleEffect = null;
            }
        }

        /// <summary>
        /// Force an immediate telegraph with custom duration.
        /// </summary>
        /// <param name="duration">Custom telegraph duration</param>
        public void StartCustomTelegraph(float duration)
        {
            if (_telegraphSettings == null)
                return;

            // Create temporary settings with custom duration
            var originalDuration = _telegraphSettings.Duration;
            
            // Note: This is a simplified approach. In a full implementation,
            // you might want to create a runtime copy of the settings
            StartTelegraph();
        }

        private void OnDestroy()
        {
            // Clean up any ongoing effects
            if (_isTelegraphing)
            {
                CleanupParticleEffects();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-assign components
            if (_targetSpriteRenderer == null)
                _targetSpriteRenderer = GetComponent<SpriteRenderer>();

            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();
        }

        private void OnDrawGizmosSelected()
        {
            if (_isTelegraphing && Application.isPlaying)
            {
                // Draw telegraph progress indicator
                Gizmos.color = Color.red;
                float progress = TelegraphProgress;
                float radius = 1f + (progress * 0.5f);
                Gizmos.DrawWireSphere(transform.position, radius);

                // Draw progress arc
                Vector3 center = transform.position + Vector3.up * 1.5f;
                float angle = progress * 360f;
                
                for (int i = 0; i < angle; i += 10)
                {
                    float rad = i * Mathf.Deg2Rad;
                    Vector3 point = center + new Vector3(Mathf.Sin(rad), Mathf.Cos(rad), 0) * 0.3f;
                    Gizmos.DrawSphere(point, 0.05f);
                }
            }

            // Draw telegraph settings info
            if (_telegraphSettings != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, 0.8f);
            }
        }
#endif
    }
}