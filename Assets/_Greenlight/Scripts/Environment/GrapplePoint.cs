using UnityEngine;

namespace Greenlight.Environment
{
    /// <summary>
    /// Marker component for environment points the grappling hook can attach to.
    /// Place these on platforms, walls, or objects the player can grapple to.
    /// 
    /// Design Philosophy:
    /// - Simple marker with landing offset
    /// - Gizmos for designer visibility
    /// - No logic - just data for GrapplingHookBehaviour
    /// </summary>
    [AddComponentMenu("Greenlight/Environment/Grapple Point")]
    public class GrapplePoint : MonoBehaviour
    {
        [Tooltip("Offset from transform position where player lands after grappling.")]
        [SerializeField] private Vector2 _landingOffset;

        /// <summary>
        /// Offset from this point where the player should land.
        /// </summary>
        public Vector2 LandingOffset => _landingOffset;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Draw subtle indicator when not selected
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }

        private void OnDrawGizmosSelected()
        {
            // Draw prominent indicator when selected
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // Draw landing position
            Vector3 landingPos = transform.position + (Vector3)_landingOffset;
            Gizmos.DrawLine(transform.position, landingPos);
            Gizmos.DrawWireSphere(landingPos, 0.15f);

            // Draw label
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, "Grapple Point");
        }
#endif
    }
}
