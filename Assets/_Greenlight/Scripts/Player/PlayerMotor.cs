using UnityEngine;
using Greenlight.Core.Physics;

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
    public class PlayerMotor : MonoBehaviour, ICollisionChecker
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

            // Move using MovePosition (kinematic, no velocity drift)
            // Note: Pixel snapping is handled by PlayerVisuals in LateUpdate for smooth physics
            _rb.MovePosition(finalPosition);

            if (_debugLog && IsMoving)
            {
                Debug.Log($"[PlayerMotor] Moving at {_currentVelocity.magnitude:F2} units/sec " +
                         $"({_currentVelocity.magnitude * 32f:F0} pixels/sec)");
            }
        }

        /// <summary>
        /// Casts a box in the desired movement direction to check for obstacles.
        /// Uses a skin width and normal check to allow escape from overlaps.
        /// Public so other systems (knockback, gadgets) can respect player collisions.
        /// </summary>
        /// <param name="delta">The movement vector to check.</param>
        /// <returns>True if a collision is detected that blocks movement.</returns>
        public bool CheckCollision(Vector2 delta)
        {
            if (_collider == null) return false;

            // Use a slightly smaller box than the actual collider to prevent "snagging" 
            // on parallel walls or floating point errors.
            Vector2 size = _collider.size * 0.95f;
            Vector2 origin = _rb.position + _collider.offset;
            float distance = delta.magnitude;
            Vector2 direction = delta.normalized;

            // Early exit for negligible movement
            if (distance < 0.0001f) return false;

            // Robust check: Cast from slightly behind (Skin Width) to detect surfaces 
            // even if we are already touching or slightly overlapping them.
            // This allows us to distinguish between moving INTO a wall vs moving AWAY from it.
            float skinWidth = 0.015f; // ~0.5 pixels at 32 PPU

            RaycastHit2D hit = Physics2D.BoxCast(
                origin - direction * skinWidth,
                size,
                0f,
                direction,
                distance + skinWidth,
                _settings.ObstacleLayers
            );

            if (hit.collider != null)
            {
                // Only block if we are moving TOWARDS the surface (dot product < 0).
                // If dot product > 0, we are moving away from the wall, so allow movement.
                float dot = Vector2.Dot(direction, hit.normal);
                if (dot < -0.01f)
                {
                    return true; // Blocked by wall
                }
            }

            return false; // No collision or moving away from wall
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
