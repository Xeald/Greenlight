using UnityEngine;

namespace Greenlight.AI
{
    /// <summary>
    /// ScriptableObject containing AI-specific behavior parameters.
    /// Used by enemy controllers for consistent behavior settings.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyBehaviorSettings", menuName = "Greenlight/AI/Enemy Behavior Settings")]
    public class EnemyBehaviorSettingsSO : ScriptableObject
    {
        [Header("Detection & Awareness")]
        [SerializeField, Range(0.1f, 10f), Tooltip("Base detection range in Unity units.")]
        private float _baseDetectionRange = 3f;

        [SerializeField, Range(0f, 180f), Tooltip("Field of view angle in degrees (0 = omnidirectional).")]
        private float _fieldOfViewAngle = 120f;

        [SerializeField, Tooltip("Layers that block line of sight to the player.")]
        private LayerMask _sightBlockingLayers = 1; // Default layer

        [SerializeField, Range(0.1f, 2f), Tooltip("How often to check for player detection (seconds).")]
        private float _detectionUpdateInterval = 0.2f;

        [Header("State Timing")]
        [SerializeField, Range(0.1f, 3f), Tooltip("Minimum time to spend in Idle state (seconds).")]
        private float _minIdleTime = 1f;

        [SerializeField, Range(0.1f, 5f), Tooltip("Maximum time to spend in Idle state (seconds).")]
        private float _maxIdleTime = 3f;

        [SerializeField, Range(0.5f, 10f), Tooltip("Time to lose player and return to Idle (seconds).")]
        private float _playerLostTimeout = 3f;

        [SerializeField, Range(0.1f, 2f), Tooltip("Recovery time after completing an attack (seconds).")]
        private float _attackRecoveryTime = 0.8f;

        [Header("Telegraph System")]
        [SerializeField, Range(0.2f, 2f), Tooltip("Default telegraph duration before attacks (seconds).")]
        private float _telegraphDuration = 0.8f;

        [SerializeField, Tooltip("Telegraph visual effect prefab (optional).")]
        private GameObject _telegraphVFXPrefab;

        [SerializeField, Tooltip("Audio clip to play during telegraph (optional).")]
        private AudioClip _telegraphAudioClip;

        [Header("Movement Behavior")]
        [SerializeField, Range(0.1f, 1f), Tooltip("Minimum distance to maintain from player while attacking.")]
        private float _attackDistanceBuffer = 0.2f;

        [SerializeField, Range(0.1f, 2f), Tooltip("Distance to overshoot when moving to attack position.")]
        private float _attackApproachOvershoot = 0.3f;

        [SerializeField, Range(1f, 5f), Tooltip("Speed multiplier when fleeing or retreating.")]
        private float _fleeSpeedMultiplier = 1.8f;

        [Header("Pathfinding")]
        [SerializeField, Range(0.1f, 1f), Tooltip("How close to get to waypoints before considering them reached.")]
        private float _waypointReachThreshold = 0.2f;

        [SerializeField, Tooltip("Layers to treat as obstacles for pathfinding.")]
        private LayerMask _obstacleLayers = 1;

        /// <summary>
        /// Base detection range in Unity units.
        /// </summary>
        public float BaseDetectionRange => _baseDetectionRange;

        /// <summary>
        /// Field of view angle in degrees (0 = omnidirectional).
        /// </summary>
        public float FieldOfViewAngle => _fieldOfViewAngle;

        /// <summary>
        /// Layers that block line of sight to the player.
        /// </summary>
        public LayerMask SightBlockingLayers => _sightBlockingLayers;

        /// <summary>
        /// How often to check for player detection (seconds).
        /// </summary>
        public float DetectionUpdateInterval => _detectionUpdateInterval;

        /// <summary>
        /// Minimum time to spend in Idle state (seconds).
        /// </summary>
        public float MinIdleTime => _minIdleTime;

        /// <summary>
        /// Maximum time to spend in Idle state (seconds).
        /// </summary>
        public float MaxIdleTime => _maxIdleTime;

        /// <summary>
        /// Random idle time within the defined range.
        /// </summary>
        public float RandomIdleTime => Random.Range(_minIdleTime, _maxIdleTime);

        /// <summary>
        /// Time to lose player and return to Idle (seconds).
        /// </summary>
        public float PlayerLostTimeout => _playerLostTimeout;

        /// <summary>
        /// Recovery time after completing an attack (seconds).
        /// </summary>
        public float AttackRecoveryTime => _attackRecoveryTime;

        /// <summary>
        /// Default telegraph duration before attacks (seconds).
        /// </summary>
        public float TelegraphDuration => _telegraphDuration;

        /// <summary>
        /// Telegraph visual effect prefab (optional).
        /// </summary>
        public GameObject TelegraphVFXPrefab => _telegraphVFXPrefab;

        /// <summary>
        /// Audio clip to play during telegraph (optional).
        /// </summary>
        public AudioClip TelegraphAudioClip => _telegraphAudioClip;

        /// <summary>
        /// Minimum distance to maintain from player while attacking.
        /// </summary>
        public float AttackDistanceBuffer => _attackDistanceBuffer;

        /// <summary>
        /// Distance to overshoot when moving to attack position.
        /// </summary>
        public float AttackApproachOvershoot => _attackApproachOvershoot;

        /// <summary>
        /// Speed multiplier when fleeing or retreating.
        /// </summary>
        public float FleeSpeedMultiplier => _fleeSpeedMultiplier;

        /// <summary>
        /// How close to get to waypoints before considering them reached.
        /// </summary>
        public float WaypointReachThreshold => _waypointReachThreshold;

        /// <summary>
        /// Layers to treat as obstacles for pathfinding.
        /// </summary>
        public LayerMask ObstacleLayers => _obstacleLayers;

        /// <summary>
        /// Check if the enemy has line of sight to a target position.
        /// </summary>
        /// <param name="fromPosition">Starting position</param>
        /// <param name="toPosition">Target position</param>
        /// <param name="maxDistance">Maximum sight distance</param>
        /// <returns>True if line of sight is clear</returns>
        public bool HasLineOfSight(Vector2 fromPosition, Vector2 toPosition, float maxDistance)
        {
            Vector2 direction = toPosition - fromPosition;
            float distance = direction.magnitude;

            if (distance > maxDistance)
                return false;

            // Check if within field of view (if specified)
            if (_fieldOfViewAngle > 0f && _fieldOfViewAngle < 360f)
            {
                // For this implementation, we'll assume the enemy is always facing the right direction
                // In a full implementation, you'd compare against the enemy's facing direction
                // float angleToTarget = Vector2.SignedAngle(enemy.forward, direction);
                // if (Mathf.Abs(angleToTarget) > _fieldOfViewAngle * 0.5f) return false;
            }

            // Raycast to check for obstacles
            RaycastHit2D hit = Physics2D.Raycast(fromPosition, direction.normalized, distance, _sightBlockingLayers);
            return hit.collider == null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure values stay within reasonable bounds
            _baseDetectionRange = Mathf.Max(0.1f, _baseDetectionRange);
            _fieldOfViewAngle = Mathf.Clamp(_fieldOfViewAngle, 0f, 360f);
            _detectionUpdateInterval = Mathf.Max(0.05f, _detectionUpdateInterval);
            
            // Ensure min <= max for idle times
            if (_minIdleTime > _maxIdleTime)
                _maxIdleTime = _minIdleTime;
            
            _playerLostTimeout = Mathf.Max(0.1f, _playerLostTimeout);
            _attackRecoveryTime = Mathf.Max(0.1f, _attackRecoveryTime);
            _telegraphDuration = Mathf.Max(0.1f, _telegraphDuration);
        }
#endif
    }
}