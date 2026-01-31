using UnityEngine;

namespace Greenlight.Player
{
    /// <summary>
    /// ScriptableObject containing tunable movement constants for the player.
    /// All speeds are expressed in pixels per second (32 PPU standard) then converted to units.
    /// 
    /// Design Philosophy:
    /// - Designers tweak values in pixel space (intuitive for retro games)
    /// - Runtime converts to Unity units automatically
    /// - No magic numbers in code
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerSettings", menuName = "Greenlight/Player/Player Settings")]
    public class PlayerSettingsSO : ScriptableObject
    {
        [Header("Movement (32 PPU)")]
        [SerializeField, Tooltip("Base movement speed in pixels per second. 96 = 3 tiles/sec at 32 PPU.")]
        private float _moveSpeedPixelsPerSecond = 96f;

        [SerializeField, Tooltip("Multiplier applied when sprinting (if sprint is enabled).")]
        private float _sprintMultiplier = 1.5f;

        [Header("Physics")]
        [SerializeField, Tooltip("Collision layers the player can walk on.")]
        private LayerMask _groundLayers;

        [SerializeField, Tooltip("Collision layers that block player movement.")]
        private LayerMask _obstacleLayers;

        [Header("Input Response")]
        [SerializeField, Range(0f, 1f), Tooltip("How quickly player accelerates to target speed (0=instant, 1=gradual).")]
        private float _acceleration = 0.1f;

        [SerializeField, Range(0f, 1f), Tooltip("How quickly player decelerates when input stops (0=instant, 1=gradual).")]
        private float _deceleration = 0.2f;

        [Header("Combat (Heart System)")]
        [SerializeField, Tooltip("Starting maximum hearts for the player.")]
        private int _maxHearts = 3;

        [SerializeField, Range(0.1f, 3f), Tooltip("Invincibility frame duration after taking damage (seconds).")]
        private float _iframeDuration = 1.0f;

        [SerializeField, Tooltip("Knockback force when player is hit (pixels per second).")]
        private float _knockbackForcePixels = 128f;

        /// <summary>
        /// Base movement speed in Unity units per second.
        /// Automatically converted from pixel speed using 32 PPU.
        /// </summary>
        public float MoveSpeedUnits => _moveSpeedPixelsPerSecond / 32f;

        /// <summary>
        /// Sprint speed in Unity units per second.
        /// </summary>
        public float SprintSpeedUnits => (_moveSpeedPixelsPerSecond * _sprintMultiplier) / 32f;

        /// <summary>
        /// Raw pixel speed for designer reference.
        /// </summary>
        public float MoveSpeedPixels => _moveSpeedPixelsPerSecond;

        /// <summary>
        /// Sprint multiplier.
        /// </summary>
        public float SprintMultiplier => _sprintMultiplier;

        /// <summary>
        /// Layers the player can walk on.
        /// </summary>
        public LayerMask GroundLayers => _groundLayers;

        /// <summary>
        /// Layers that block player movement.
        /// </summary>
        public LayerMask ObstacleLayers => _obstacleLayers;

        /// <summary>
        /// Acceleration factor (0-1).
        /// </summary>
        public float Acceleration => _acceleration;

        /// <summary>
        /// Deceleration factor (0-1).
        /// </summary>
        public float Deceleration => _deceleration;

        /// <summary>
        /// Starting maximum hearts for the player.
        /// </summary>
        public int MaxHearts => _maxHearts;

        /// <summary>
        /// Invincibility frame duration after taking damage (seconds).
        /// </summary>
        public float IframeDuration => _iframeDuration;

        /// <summary>
        /// Knockback force when player is hit (Unity units per second).
        /// </summary>
        public float KnockbackForceUnits => _knockbackForcePixels / 32f;

        /// <summary>
        /// Raw knockback force in pixels per second for designer reference.
        /// </summary>
        public float KnockbackForcePixels => _knockbackForcePixels;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure speeds stay positive
            _moveSpeedPixelsPerSecond = Mathf.Max(1f, _moveSpeedPixelsPerSecond);
            _sprintMultiplier = Mathf.Max(1f, _sprintMultiplier);

            // Ensure combat values stay positive
            _maxHearts = Mathf.Max(1, _maxHearts);
            _knockbackForcePixels = Mathf.Max(1f, _knockbackForcePixels);
        }
#endif
    }
}
