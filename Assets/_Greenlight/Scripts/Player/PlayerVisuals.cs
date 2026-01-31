using UnityEngine;
using Greenlight.Core.Animation;

namespace Greenlight.Player
{
    /// <summary>
    /// Handles visual representation and pixel-perfect snapping for the player.
    /// Runs in LateUpdate to snap sprite position to pixel grid after all physics/logic.
    /// 
    /// Design Philosophy:
    /// - Separation: Physics position can be sub-pixel, visual snaps to grid
    /// - 32 PPU: 1 pixel = 1/32 unit, so we round to nearest 1/32
    /// - LateUpdate ensures snapping happens after all movement
    /// </summary>
    [AddComponentMenu("Greenlight/Player/Player Visuals")]
    public class PlayerVisuals : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Transform of the sprite/visual representation to snap.")]
        private Transform _spriteTransform;

        [Header("Pixel Snapping")]
        [SerializeField, Tooltip("Enable pixel-perfect snapping (32 PPU).")]
        private bool _enablePixelSnapping = true;

        [Header("Facing Direction")]
        [SerializeField, Tooltip("Should sprite flip based on movement direction?")]
        private bool _flipSpriteOnDirection = true;

        [SerializeField, Tooltip("Sprite renderer to flip (optional).")]
        private SpriteRenderer _spriteRenderer;

        private const float PIXELS_PER_UNIT = 32f;
        private const float UNIT_PER_PIXEL = 1f / PIXELS_PER_UNIT;

        private Vector2 _lastFacingDirection = Vector2.down;
        private FacingCardinal _currentCardinal = FacingCardinal.Down;

        /// <summary>
        /// Last non-zero facing direction (for gadget aiming).
        /// </summary>
        public Vector2 FacingDirection => _lastFacingDirection;

        /// <summary>
        /// Current cardinal facing direction for animation system.
        /// </summary>
        public FacingCardinal CurrentCardinal => _currentCardinal;

        private void Awake()
        {
            // Auto-assign sprite transform if not set
            if (_spriteTransform == null)
            {
                _spriteTransform = transform;
            }

            // Auto-find SpriteRenderer if not assigned
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            // Initialize cardinal direction to default (Down)
            var (defaultCardinal, _) = TopDownFacing.GetDefault();
            _currentCardinal = defaultCardinal;
        }

        /// <summary>
        /// Update facing direction based on movement.
        /// Call this from PlayerController when movement input changes.
        /// </summary>
        /// <param name="moveDirection">Current movement direction.</param>
        public void UpdateFacingDirection(Vector2 moveDirection)
        {
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                _lastFacingDirection = moveDirection.normalized;

                // Get cardinal direction and flip decision from TopDownFacing helper
                var (cardinal, flipX) = TopDownFacing.FromVector(moveDirection);
                _currentCardinal = cardinal;

                // Apply sprite flip - only flip for Side directions when facing left
                if (_flipSpriteOnDirection && _spriteRenderer != null)
                {
                    // Only flip horizontally for Side direction when facing left
                    // Up/Down directions never flip (clean vertical sprites)
                    if (_currentCardinal == FacingCardinal.Side)
                    {
                        _spriteRenderer.flipX = flipX;
                    }
                    else
                    {
                        // Up/Down: always face right (no flip)
                        _spriteRenderer.flipX = false;
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (!_enablePixelSnapping || _spriteTransform == null) return;

            SnapToPixelGrid();
        }

        /// <summary>
        /// Snaps the sprite transform to the nearest pixel position.
        /// Rounds to 1/32 unit increments (32 PPU standard).
        /// </summary>
        private void SnapToPixelGrid()
        {
            Vector3 pos = _spriteTransform.position;

            // Round to nearest pixel (1/32 unit)
            pos.x = Mathf.Round(pos.x * PIXELS_PER_UNIT) * UNIT_PER_PIXEL;
            pos.y = Mathf.Round(pos.y * PIXELS_PER_UNIT) * UNIT_PER_PIXEL;

            _spriteTransform.position = pos;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_spriteTransform == null)
            {
                _spriteTransform = transform;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw facing direction
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, _lastFacingDirection * 0.5f);
            }
        }
#endif
    }
}
