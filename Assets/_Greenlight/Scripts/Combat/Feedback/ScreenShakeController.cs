using UnityEngine;

namespace Greenlight.Combat
{
    /// <summary>
    /// Controls screen shake effects for combat feedback.
    /// Provides manual camera shake with customizable intensity and duration.
    /// 
    /// Design Philosophy:
    /// - Self-contained implementation (no Cinemachine dependency)
    /// - Pixel-perfect shake that respects 32 PPU grid
    /// - Smooth easing with proper restore functionality
    /// </summary>
    [AddComponentMenu("Greenlight/Combat/Screen Shake Controller")]
    public class ScreenShakeController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField, Tooltip("Combat feedback settings for shake parameters.")]
        private CombatFeedbackSettingsSO _feedbackSettings;

        [SerializeField, Tooltip("Camera to shake (auto-assigned if null).")]
        private Camera _targetCamera;

        [Header("Shake Parameters")]
        [SerializeField, Range(0.1f, 5f), Tooltip("Global shake intensity multiplier.")]
        private float _globalIntensityMultiplier = 1f;

        [SerializeField, Tooltip("Shake frequency (shakes per second).")]
        private float _shakeFrequency = 20f;

        [Header("Debug")]
        [SerializeField, Tooltip("Enable debug logging for shake events.")]
        private bool _debugLog = false;

        // State
        private bool _isShaking;
        private float _shakeTimeRemaining;
        private float _currentIntensity;
        private Vector3 _originalCameraPosition;
        private Vector3 _shakeOffset;

        /// <summary>
        /// Is screen shake currently active?
        /// </summary>
        public bool IsShaking => _isShaking;

        /// <summary>
        /// Current shake intensity.
        /// </summary>
        public float CurrentIntensity => _currentIntensity;

        private void Awake()
        {
            // Auto-assign main camera if not set
            if (_targetCamera == null)
                _targetCamera = Camera.main;

            if (_targetCamera != null)
                _originalCameraPosition = _targetCamera.transform.localPosition;
        }

        private void Update()
        {
            if (_isShaking)
            {
                UpdateShake();
            }
        }

        /// <summary>
        /// Apply screen shake with the specified intensity and duration.
        /// </summary>
        /// <param name="intensity">Shake intensity (0-1 range typically)</param>
        /// <param name="duration">Shake duration in seconds</param>
        public void Shake(float intensity, float duration)
        {
            if (_targetCamera == null)
            {
                Debug.LogError($"[ScreenShakeController] {name}: No camera assigned for screen shake!");
                return;
            }

            _currentIntensity = intensity * _globalIntensityMultiplier;
            _shakeTimeRemaining = duration;
            _isShaking = true;

            // Store original position if this is a new shake
            if (_shakeOffset == Vector3.zero)
                _originalCameraPosition = _targetCamera.transform.localPosition;

            if (_debugLog)
            {
                Debug.Log($"[ScreenShakeController] Started shake - Intensity: {_currentIntensity:F2}, Duration: {duration:F2}s");
            }
        }

        /// <summary>
        /// Apply screen shake based on damage amount using feedback settings.
        /// </summary>
        /// <param name="damage">Damage dealt (determines shake intensity)</param>
        public void ShakeForDamage(int damage)
        {
            if (_feedbackSettings == null)
            {
                Debug.LogError($"[ScreenShakeController] {name}: No CombatFeedbackSettingsSO assigned!");
                return;
            }

            float intensity = _feedbackSettings.GetShakeIntensity(damage);
            float duration = _feedbackSettings.ShakeDuration;
            Shake(intensity, duration);
        }

        /// <summary>
        /// Apply screen shake with settings-based duration.
        /// </summary>
        /// <param name="intensity">Shake intensity</param>
        public void Shake(float intensity)
        {
            float duration = _feedbackSettings?.ShakeDuration ?? 0.3f;
            Shake(intensity, duration);
        }

        /// <summary>
        /// Stop screen shake immediately and restore camera position.
        /// </summary>
        public void StopShake()
        {
            if (!_isShaking)
                return;

            _isShaking = false;
            _shakeTimeRemaining = 0f;
            _currentIntensity = 0f;
            _shakeOffset = Vector3.zero;

            // Restore original camera position
            if (_targetCamera != null)
                _targetCamera.transform.localPosition = _originalCameraPosition;

            if (_debugLog)
            {
                Debug.Log($"[ScreenShakeController] Stopped shake");
            }
        }

        /// <summary>
        /// Update shake effect each frame.
        /// </summary>
        private void UpdateShake()
        {
            if (_targetCamera == null)
            {
                StopShake();
                return;
            }

            // Decrease shake time
            _shakeTimeRemaining -= Time.deltaTime;

            if (_shakeTimeRemaining <= 0f)
            {
                StopShake();
                return;
            }

            // Calculate shake with easing
            float fadeOut = _shakeTimeRemaining / (_feedbackSettings?.ShakeDuration ?? 0.3f);
            float effectiveIntensity = _currentIntensity * fadeOut;

            // Generate shake offset using Perlin noise for smooth shake
            float time = Time.time * _shakeFrequency;
            float shakeX = (Mathf.PerlinNoise(time, 0f) - 0.5f) * 2f * effectiveIntensity;
            float shakeY = (Mathf.PerlinNoise(0f, time) - 0.5f) * 2f * effectiveIntensity;

            _shakeOffset = new Vector3(shakeX, shakeY, 0f);

            // Snap to pixel grid (32 PPU)
            _shakeOffset.x = Mathf.Round(_shakeOffset.x * 32f) / 32f;
            _shakeOffset.y = Mathf.Round(_shakeOffset.y * 32f) / 32f;

            // Apply shake to camera
            _targetCamera.transform.localPosition = _originalCameraPosition + _shakeOffset;
        }

        /// <summary>
        /// Set the target camera for shake effects.
        /// </summary>
        /// <param name="camera">Camera to shake</param>
        public void SetTargetCamera(Camera camera)
        {
            // Stop current shake if changing cameras
            if (_isShaking)
                StopShake();

            _targetCamera = camera;

            if (_targetCamera != null)
                _originalCameraPosition = _targetCamera.transform.localPosition;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-assign main camera if not set
            if (_targetCamera == null)
                _targetCamera = Camera.main;

            // Clamp values
            _globalIntensityMultiplier = Mathf.Max(0f, _globalIntensityMultiplier);
            _shakeFrequency = Mathf.Max(1f, _shakeFrequency);
        }

        private void OnDrawGizmosSelected()
        {
            if (_isShaking && Application.isPlaying && _targetCamera != null)
            {
                // Draw shake indicator
                Gizmos.color = Color.yellow;
                Vector3 cameraPos = _targetCamera.transform.position;
                Gizmos.DrawWireCube(cameraPos, Vector3.one * (_currentIntensity * 2f));

                // Draw shake offset vector
                Gizmos.color = Color.red;
                Gizmos.DrawLine(cameraPos, cameraPos + _shakeOffset * 5f);
            }
        }
#endif
    }
}