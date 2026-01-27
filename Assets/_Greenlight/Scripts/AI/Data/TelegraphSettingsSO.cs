using UnityEngine;

namespace Greenlight.AI
{
    /// <summary>
    /// ScriptableObject containing telegraph effect parameters.
    /// Defines how enemies telegraph their attacks for "Combat as Conversation".
    /// </summary>
    [CreateAssetMenu(fileName = "TelegraphSettings", menuName = "Greenlight/AI/Telegraph Settings")]
    public class TelegraphSettingsSO : ScriptableObject
    {
        [Header("Timing")]
        [SerializeField, Range(0.2f, 3f), Tooltip("Duration of the telegraph effect (seconds).")]
        private float _duration = 0.8f;

        [SerializeField, Range(0f, 0.5f), Tooltip("Delay before telegraph starts (seconds).")]
        private float _startDelay = 0.1f;

        [Header("Visual Effects")]
        [SerializeField, Tooltip("Color tint during telegraph.")]
        private Color _telegraphColor = Color.red;

        [SerializeField, Range(0f, 1f), Tooltip("Flash intensity (0 = no flash, 1 = full color).")]
        private float _flashIntensity = 0.7f;

        [SerializeField, Tooltip("Animation curve for flash intensity over time.")]
        private AnimationCurve _flashCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Scale/Squash Effects")]
        [SerializeField, Tooltip("Enable scale/squash animation during telegraph.")]
        private bool _enableScaleEffect = true;

        [SerializeField, Range(0.5f, 2f), Tooltip("Scale multiplier at peak of telegraph.")]
        private float _scaleMultiplier = 1.2f;

        [SerializeField, Tooltip("Animation curve for scale effect over time.")]
        private AnimationCurve _scaleCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.7f, 1.2f),
            new Keyframe(1f, 1f)
        );

        [Header("Audio")]
        [SerializeField, Tooltip("Audio clip to play during telegraph.")]
        private AudioClip _telegraphSFX;

        [SerializeField, Range(0f, 1f), Tooltip("Volume of telegraph sound effect.")]
        private float _sfxVolume = 0.8f;

        [SerializeField, Range(0.5f, 2f), Tooltip("Pitch of telegraph sound effect.")]
        private float _sfxPitch = 1f;

        [Header("Particle Effects")]
        [SerializeField, Tooltip("Particle effect prefab to spawn during telegraph.")]
        private GameObject _particleEffectPrefab;

        [SerializeField, Tooltip("Offset for particle effect relative to enemy position.")]
        private Vector3 _particleOffset = Vector3.zero;

        /// <summary>
        /// Duration of the telegraph effect (seconds).
        /// </summary>
        public float Duration => _duration;

        /// <summary>
        /// Delay before telegraph starts (seconds).
        /// </summary>
        public float StartDelay => _startDelay;

        /// <summary>
        /// Color tint during telegraph.
        /// </summary>
        public Color TelegraphColor => _telegraphColor;

        /// <summary>
        /// Flash intensity (0-1).
        /// </summary>
        public float FlashIntensity => _flashIntensity;

        /// <summary>
        /// Animation curve for flash intensity over time.
        /// </summary>
        public AnimationCurve FlashCurve => _flashCurve;

        /// <summary>
        /// Enable scale/squash animation during telegraph.
        /// </summary>
        public bool EnableScaleEffect => _enableScaleEffect;

        /// <summary>
        /// Scale multiplier at peak of telegraph.
        /// </summary>
        public float ScaleMultiplier => _scaleMultiplier;

        /// <summary>
        /// Animation curve for scale effect over time.
        /// </summary>
        public AnimationCurve ScaleCurve => _scaleCurve;

        /// <summary>
        /// Audio clip to play during telegraph.
        /// </summary>
        public AudioClip TelegraphSFX => _telegraphSFX;

        /// <summary>
        /// Volume of telegraph sound effect.
        /// </summary>
        public float SFXVolume => _sfxVolume;

        /// <summary>
        /// Pitch of telegraph sound effect.
        /// </summary>
        public float SFXPitch => _sfxPitch;

        /// <summary>
        /// Particle effect prefab to spawn during telegraph.
        /// </summary>
        public GameObject ParticleEffectPrefab => _particleEffectPrefab;

        /// <summary>
        /// Offset for particle effect relative to enemy position.
        /// </summary>
        public Vector3 ParticleOffset => _particleOffset;

        /// <summary>
        /// Get flash intensity at a specific time in the telegraph.
        /// </summary>
        /// <param name="normalizedTime">Time from 0 to 1 within the telegraph duration</param>
        /// <returns>Flash intensity at that time</returns>
        public float GetFlashIntensityAtTime(float normalizedTime)
        {
            normalizedTime = Mathf.Clamp01(normalizedTime);
            return _flashCurve.Evaluate(normalizedTime) * _flashIntensity;
        }

        /// <summary>
        /// Get scale multiplier at a specific time in the telegraph.
        /// </summary>
        /// <param name="normalizedTime">Time from 0 to 1 within the telegraph duration</param>
        /// <returns>Scale multiplier at that time</returns>
        public float GetScaleAtTime(float normalizedTime)
        {
            if (!_enableScaleEffect)
                return 1f;

            normalizedTime = Mathf.Clamp01(normalizedTime);
            return _scaleCurve.Evaluate(normalizedTime);
        }

        /// <summary>
        /// Get color with flash applied at a specific time.
        /// </summary>
        /// <param name="baseColor">Original sprite color</param>
        /// <param name="normalizedTime">Time from 0 to 1 within the telegraph duration</param>
        /// <returns>Color with telegraph flash applied</returns>
        public Color GetColorAtTime(Color baseColor, float normalizedTime)
        {
            float flashAmount = GetFlashIntensityAtTime(normalizedTime);
            return Color.Lerp(baseColor, _telegraphColor, flashAmount);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure values stay within reasonable bounds
            _duration = Mathf.Max(0.1f, _duration);
            _startDelay = Mathf.Max(0f, _startDelay);
            _flashIntensity = Mathf.Clamp01(_flashIntensity);
            _scaleMultiplier = Mathf.Max(0.1f, _scaleMultiplier);
            _sfxVolume = Mathf.Clamp01(_sfxVolume);
            _sfxPitch = Mathf.Max(0.1f, _sfxPitch);

            // Ensure animation curves are valid
            if (_flashCurve == null || _flashCurve.length == 0)
                _flashCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            if (_scaleCurve == null || _scaleCurve.length == 0)
                _scaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 1f);
        }
#endif
    }
}