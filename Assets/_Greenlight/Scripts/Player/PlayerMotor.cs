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
    [RequireComponent(typeof(BoxCollider2D))]
    [AddComponentMenu("Greenlight/Player/Player Motor")]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField, Tooltip("Movement settings asset.")]
        private PlayerSettingsSO _settings;

        [Header("Debug")]
        [SerializeField] private bool _debugLog;

        private Rigidbody2D _rb;
        private BoxCollider2D _collider;
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
            _collider = GetComponent<BoxCollider2D>();
            
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

            // Calculate movement for this frame
            Vector2 moveDelta = _currentVelocity * Time.fixedDeltaTime;

            // Split movement into X and Y for sliding collision response
            Vector2 finalPosition = _rb.position;

            // --- Horizontal Move ---
            if (Mathf.Abs(moveDelta.x) > 0.0001f)
            {
                if (!CheckCollision(new Vector2(moveDelta.x, 0)))
                {
                    finalPosition.x += moveDelta.x;
                }
            }

            // --- Vertical Move ---
            if (Mathf.Abs(moveDelta.y) > 0.0001f)
            {
                if (!CheckCollision(new Vector2(0, moveDelta.y)))
                {
                    finalPosition.y += moveDelta.y;
                }
            }

            // Snap to pixel grid (32 PPU standard) for retro precision
            finalPosition.x = Mathf.Round(finalPosition.x * 32f) / 32f;
            finalPosition.y = Mathf.Round(finalPosition.y * 32f) / 32f;

            // Move using MovePosition (kinematic, no velocity drift)
            _rb.MovePosition(finalPosition);

            if (_debugLog && IsMoving)
            {
                Debug.Log($"[PlayerMotor] Moving at {_currentVelocity.magnitude:F2} units/sec " +
                         $"({_currentVelocity.magnitude * 32f:F0} pixels/sec)");
            }
        }

        /// <summary>
        /// Casts a box in the desired movement direction to check for obstacles.
        /// </summary>
        /// <param name="delta">The movement vector to check.</param>
        /// <returns>True if a collision is detected.</returns>
        private bool CheckCollision(Vector2 delta)
        {
            if (_collider == null) return false;

            // Use a slightly smaller box than the actual collider to prevent "snagging" 
            // on parallel walls or floating point errors.
            Vector2 size = _collider.size * 0.95f;
            Vector2 origin = _rb.position + _collider.offset;
            float distance = delta.magnitude;
            Vector2 direction = delta.normalized;

            RaycastHit2D hit = Physics2D.BoxCast(
                origin,
                size,
                0f,
                direction,
                distance,
                _settings.ObstacleLayers
            );

            return hit.collider != null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-assign components if missing
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            if (_collider == null) _collider = GetComponent<BoxCollider2D>();
        }
#endif
    }
}
