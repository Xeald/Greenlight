using UnityEngine;

namespace Greenlight.Combat
{
    /// <summary>
    /// ScriptableObject containing tunable parameters for combat feedback effects.
    /// Hitstop, screen shake, and other "juice" parameters to make combat feel impactful.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatFeedbackSettings", menuName = "Greenlight/Combat/Combat Feedback Settings")]
    public class CombatFeedbackSettingsSO : ScriptableObject
    {
        [Header("Hitstop")]
        [SerializeField, Range(1, 10), Tooltip("Number of frames to freeze on light hits.")]
        private int _lightHitstopFrames = 2;

        [SerializeField, Range(1, 15), Tooltip("Number of frames to freeze on medium hits.")]
        private int _mediumHitstopFrames = 4;

        [SerializeField, Range(1, 20), Tooltip("Number of frames to freeze on heavy hits.")]
        private int _heavyHitstopFrames = 8;

        [Header("Screen Shake")]
        [SerializeField, Range(0.1f, 2f), Tooltip("Screen shake intensity for light hits.")]
        private float _lightShakeIntensity = 0.3f;

        [SerializeField, Range(0.1f, 3f), Tooltip("Screen shake intensity for medium hits.")]
        private float _mediumShakeIntensity = 0.6f;

        [SerializeField, Range(0.1f, 5f), Tooltip("Screen shake intensity for heavy hits.")]
        private float _heavyShakeIntensity = 1.2f;

        [SerializeField, Range(0.1f, 1f), Tooltip("Duration of screen shake effect (seconds).")]
        private float _shakeDuration = 0.3f;

        [Header("Feedback Timing")]
        [SerializeField, Range(0f, 0.2f), Tooltip("Delay before applying feedback effects (seconds).")]
        private float _feedbackDelay = 0.02f;

        /// <summary>
        /// Get hitstop frames for the given damage amount.
        /// </summary>
        /// <param name="damage">Damage dealt (heart units)</param>
        /// <returns>Number of frames to freeze</returns>
        public int GetHitstopFrames(int damage)
        {
            return damage switch
            {
                1 => _lightHitstopFrames,
                2 => _mediumHitstopFrames,
                >= 3 => _heavyHitstopFrames,
                _ => _lightHitstopFrames
            };
        }

        /// <summary>
        /// Get screen shake intensity for the given damage amount.
        /// </summary>
        /// <param name="damage">Damage dealt (heart units)</param>
        /// <returns>Shake intensity</returns>
        public float GetShakeIntensity(int damage)
        {
            return damage switch
            {
                1 => _lightShakeIntensity,
                2 => _mediumShakeIntensity,
                >= 3 => _heavyShakeIntensity,
                _ => _lightShakeIntensity
            };
        }

        /// <summary>
        /// Light hit hitstop frames.
        /// </summary>
        public int LightHitstopFrames => _lightHitstopFrames;

        /// <summary>
        /// Medium hit hitstop frames.
        /// </summary>
        public int MediumHitstopFrames => _mediumHitstopFrames;

        /// <summary>
        /// Heavy hit hitstop frames.
        /// </summary>
        public int HeavyHitstopFrames => _heavyHitstopFrames;

        /// <summary>
        /// Light hit shake intensity.
        /// </summary>
        public float LightShakeIntensity => _lightShakeIntensity;

        /// <summary>
        /// Medium hit shake intensity.
        /// </summary>
        public float MediumShakeIntensity => _mediumShakeIntensity;

        /// <summary>
        /// Heavy hit shake intensity.
        /// </summary>
        public float HeavyShakeIntensity => _heavyShakeIntensity;

        /// <summary>
        /// Duration of screen shake effect (seconds).
        /// </summary>
        public float ShakeDuration => _shakeDuration;

        /// <summary>
        /// Delay before applying feedback effects (seconds).
        /// </summary>
        public float FeedbackDelay => _feedbackDelay;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure frame counts are valid
            _lightHitstopFrames = Mathf.Max(1, _lightHitstopFrames);
            _mediumHitstopFrames = Mathf.Max(_lightHitstopFrames, _mediumHitstopFrames);
            _heavyHitstopFrames = Mathf.Max(_mediumHitstopFrames, _heavyHitstopFrames);

            // Ensure shake intensities are valid
            _lightShakeIntensity = Mathf.Max(0.1f, _lightShakeIntensity);
            _mediumShakeIntensity = Mathf.Max(_lightShakeIntensity, _mediumShakeIntensity);
            _heavyShakeIntensity = Mathf.Max(_mediumShakeIntensity, _heavyShakeIntensity);
        }
#endif
    }
}