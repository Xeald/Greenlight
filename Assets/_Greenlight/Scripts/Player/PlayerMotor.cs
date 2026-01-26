using UnityEngine;

namespace Greenlight.Player
{
    /// <summary>
    /// Handles physics-based movement for the player character.
    /// Uses kinematic Rigidbody2D with MovePosition to avoid velocity-based drift.
    /// 
    /// Design Philosophy:
    /// - Kinematic mode prevents "slippery" modern physics feel
    /// - MovePosition ensures pixel-perfect positioning (32 PPU)
    /// - Separation of Concerns: Motor handles physics, not input
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [AddComponentMenu("Greenlight/Player/Player Motor")]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField, Tooltip("Movement settings asset.")]
        private PlayerSettingsSO _settings;

        [Header("Debug")]
        [SerializeField] private bool _debugLog;

        private Rigidbody2D _rb;
        private Vector2 _currentVelocity;
        private Vector2 _targetVelocity;

        /// <summary>
        /// Current movement velocity in units per second.
        /// </summary>
        public Vector2 CurrentVelocity => _currentVelocity;

        /// <summary>
        /// Is the motor currently moving?
        /// </summary>
        public bool IsMoving => _currentVelocity.sqrMagnitude > 0.01f;

        /// <summary>
        /// When true, the motor will not process movement logic or call MovePosition.
        /// Useful for external movement control like grappling hooks or knockback.
        /// </summary>
        public bool Paused { get; set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            
            // Configure Rigidbody2D for retro-style movement
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.useFullKinematicContacts = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        /// <summary>
        /// Sets the desired movement direction and speed.
        /// Call this from PlayerController based on input.
        /// </summary>
        /// <param name="direction">Normalized movement direction.</param>
        /// <param name="isSprinting">Whether sprint modifier should be applied.</param>
        public void SetMovementDirection(Vector2 direction, bool isSprinting = false)
        {
            if (_settings == null)
            {
                Debug.LogError("[PlayerMotor] No PlayerSettingsSO assigned!");
                return;
            }

            // Calculate target velocity
            float targetSpeed = isSprinting ? _settings.SprintSpeedUnits : _settings.MoveSpeedUnits;
            _targetVelocity = direction.normalized * targetSpeed;
        }

        /// <summary>
        /// Stops all movement immediately.
        /// </summary>
        public void Stop()
        {
            _targetVelocity = Vector2.zero;
            _currentVelocity = Vector2.zero;
        }

        private void FixedUpdate()
        {
            if (_settings == null || Paused) return;

            // Smoothly interpolate current velocity toward target
            float smoothing = _targetVelocity.sqrMagnitude > 0.01f
                ? _settings.Acceleration
                : _settings.Deceleration;

            _currentVelocity = Vector2.Lerp(_currentVelocity, _targetVelocity, 1f - smoothing);

            // Stop completely if velocity is negligible
            if (_currentVelocity.sqrMagnitude < 0.001f)
            {
                _currentVelocity = Vector2.zero;
            }

            // Calculate new position
            Vector2 newPosition = _rb.position + _currentVelocity * Time.fixedDeltaTime;

            // Move using MovePosition (kinematic, no velocity drift)
            _rb.MovePosition(newPosition);

            if (_debugLog && IsMoving)
            {
                Debug.Log($"[PlayerMotor] Moving at {_currentVelocity.magnitude:F2} units/sec " +
                         $"({_currentVelocity.magnitude * 32f:F0} pixels/sec)");
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-assign Rigidbody2D if missing
            if (_rb == null)
            {
                _rb = GetComponent<Rigidbody2D>();
            }
        }
#endif
    }
}
