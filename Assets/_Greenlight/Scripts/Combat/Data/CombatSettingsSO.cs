using UnityEngine;

namespace Greenlight.Combat
{
    /// <summary>
    /// ScriptableObject containing tunable combat parameters.
    /// Follows the 32 PPU standard with pixel-based values converted to units.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatSettings", menuName = "Greenlight/Combat/Combat Settings")]
    public class CombatSettingsSO : ScriptableObject
    {
        [Header("Damage & Health")]
        [SerializeField, Tooltip("Base damage dealt by player attacks.")]
        private int _baseDamage = 1;

        [SerializeField, Range(0.1f, 3f), Tooltip("Invincibility duration after taking damage (seconds).")]
        private float _invulnerabilityDuration = 1.0f;

        [Header("Knockback (32 PPU)")]
        [SerializeField, Tooltip("Knockback force in pixels per second.")]
        private float _knockbackForcePixels = 160f;

        [SerializeField, Range(0.1f, 2f), Tooltip("Duration of knockback effect (seconds).")]
        private float _knockbackDuration = 0.3f;

        [Header("Visual Feedback")]
        [SerializeField, Range(0f, 1f), Tooltip("Sprite alpha during invincibility flicker.")]
        private float _flickerAlpha = 0.3f;

        [SerializeField, Range(0.05f, 0.2f), Tooltip("Flicker interval (seconds between visibility toggles).")]
        private float _flickerInterval = 0.1f;

        /// <summary>
        /// Base damage dealt by player attacks.
        /// </summary>
        public int BaseDamage => _baseDamage;

        /// <summary>
        /// Invincibility duration after taking damage (seconds).
        /// </summary>
        public float InvulnerabilityDuration => _invulnerabilityDuration;

        /// <summary>
        /// Knockback force in Unity units per second (converted from pixels).
        /// </summary>
        public float KnockbackForceUnits => _knockbackForcePixels / 32f;

        /// <summary>
        /// Raw knockback force in pixels per second for designer reference.
        /// </summary>
        public float KnockbackForcePixels => _knockbackForcePixels;

        /// <summary>
        /// Duration of knockback effect (seconds).
        /// </summary>
        public float KnockbackDuration => _knockbackDuration;

        /// <summary>
        /// Sprite alpha during invincibility flicker.
        /// </summary>
        public float FlickerAlpha => _flickerAlpha;

        /// <summary>
        /// Flicker interval (seconds between visibility toggles).
        /// </summary>
        public float FlickerInterval => _flickerInterval;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure values stay positive
            _baseDamage = Mathf.Max(1, _baseDamage);
            _knockbackForcePixels = Mathf.Max(1f, _knockbackForcePixels);
        }
#endif
    }
}