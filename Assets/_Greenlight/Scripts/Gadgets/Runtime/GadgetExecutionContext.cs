using UnityEngine;

namespace Greenlight.Gadgets
{
    /// <summary>
    /// Context passed to GadgetBehaviour.Execute() containing
    /// all information needed to perform the gadget action.
    /// </summary>
    public struct GadgetExecutionContext
    {
        /// <summary>Position of the user when gadget was activated.</summary>
        public Vector2 UserPosition;

        /// <summary>Direction the user is facing (for aiming).</summary>
        public Vector2 FacingDirection;

        /// <summary>Reference to the user's Transform for movement gadgets.</summary>
        public Transform UserTransform;

        /// <summary>Reference to the user's Rigidbody2D if physics needed.</summary>
        public Rigidbody2D UserRigidbody;

        /// <summary>
        /// Creates a new execution context from a user's state.
        /// </summary>
        public static GadgetExecutionContext FromUser(Transform userTransform, Vector2 facingDirection)
        {
            Rigidbody2D rb = userTransform.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = userTransform.GetComponentInParent<Rigidbody2D>();
                Debug.Log($"[GadgetContext] Rigidbody2D not found on '{userTransform.name}', searching parent: {(rb != null ? rb.name : "NOT FOUND")}");
            }
            else
            {
                Debug.Log($"[GadgetContext] Found Rigidbody2D on '{userTransform.name}'");
            }
            
            return new GadgetExecutionContext
            {
                UserPosition = userTransform.position,
                FacingDirection = facingDirection.normalized,
                UserTransform = userTransform,
                UserRigidbody = rb
            };
        }
    }
}
