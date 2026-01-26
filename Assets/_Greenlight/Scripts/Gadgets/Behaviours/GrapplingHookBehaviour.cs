using UnityEngine;
using Greenlight.Core.Events;
using Greenlight.Environment;

namespace Greenlight.Gadgets
{
    /// <summary>
    /// The Grappling Hook - first verb implementation.
    /// Traversal: Pull player to GrapplePoints
    /// Combat: Pull IPullable enemies toward player
    /// 
    /// CRITICAL TIMING: Hook must visually extend BEFORE player/enemy moves!
    /// Phase 1: ExtendHookToTarget (visual only)
    /// Phase 2: Action (PullPlayer or PullEnemy)
    /// Phase 3: Retract (on whiff)
    /// </summary>
    [AddComponentMenu("Greenlight/Gadgets/Grappling Hook Behaviour")]
    public class GrapplingHookBehaviour : GadgetBehaviour
    {
        public override bool LocksMovement => true;

        [Header("Hook Settings (32 PPU)")]
        [SerializeField, Tooltip("Extension speed in pixels per second (12 tiles/sec).")]
        private float _hookSpeedPixels = 384f;

        [SerializeField, Tooltip("Maximum grapple distance in pixels (5 tiles).")]
        private float _maxDistancePixels = 160f;

        [SerializeField, Tooltip("Player/enemy pull speed in pixels per second (8 tiles/sec).")]
        private float _pullSpeedPixels = 256f;

        [Header("Physics")]
        [SerializeField, Tooltip("Layers that can be grappled to.")]
        private LayerMask _grappleableLayers;

        [Header("Visuals")]
        [SerializeField, Tooltip("Line renderer for the grappling chain.")]
        private LineRenderer _lineRenderer;

        [SerializeField, Tooltip("Transform representing the hook head.")]
        private Transform _hookHead;

        [Header("Events")]
        [SerializeField, Tooltip("Event raised when player traverses to a grapple point.")]
        private GameEventSO _onTraversal;

        [SerializeField, Tooltip("Event raised when enemy is pulled.")]
        private GameEventSO _onPull;

        [SerializeField, Tooltip("Event raised when hook whiffs (hits nothing).")]
        private GameEventSO _onWhiff;

        // Convert to units
        private float HookSpeedUnits => _hookSpeedPixels / 32f;
        private float MaxDistanceUnits => _maxDistancePixels / 32f;
        private float PullSpeedUnits => _pullSpeedPixels / 32f;

        public override async void Execute(GadgetExecutionContext context)
        {
            if (IsExecuting) return;
            IsExecuting = true;

            try
            {
                // ========== PHASE 0: RAYCAST (Instant - Frame 0) ==========
                // Determine outcome immediately, but DON'T act on it yet
                RaycastHit2D hit = Physics2D.Raycast(
                    context.UserPosition,
                    context.FacingDirection,
                    MaxDistanceUnits,
                    _grappleableLayers
                );

                // Calculate visual target point
                Vector2 visualTarget = hit.collider != null
                    ? hit.point
                    : context.UserPosition + context.FacingDirection * MaxDistanceUnits;

                // ========== PHASE 1: EXTENSION (Visual - Must complete first!) ==========
                // Player sees the hook fly out BEFORE anything else happens
                await ExtendHookToTarget(context, visualTarget);

                // ========== PHASE 2: ACTION (After hook reaches target) ==========
                if (hit.collider != null)
                {
                    if (hit.collider.TryGetComponent<GrapplePoint>(out var grapplePoint))
                    {
                        // Hook latched! Now pull the player
                        await PullPlayerToTarget(context, grapplePoint);
                        _onTraversal?.Raise();
                    }
                    else if (hit.collider.TryGetComponent<IPullable>(out var pullable))
                    {
                        // Hook latched to enemy! Pull them toward us
                        await PullTargetToPlayer(context, pullable);
                        _onPull?.Raise();
                    }
                    else
                    {
                        // Hit a wall or non-grappleable - retract
                        await RetractHook(context.UserPosition);
                        _onWhiff?.Raise();
                    }
                }
                else
                {
                    // ========== PHASE 3: RETRACTION (Whiff) ==========
                    // Nothing hit - hook returns
                    await RetractHook(context.UserPosition);
                    _onWhiff?.Raise();
                }
            }
            finally
            {
                IsExecuting = false;
                if (_lineRenderer != null)
                {
                    _lineRenderer.enabled = false;
                }
            }
        }

        /// <summary>
        /// Phase 1: Animate hook head traveling from player to target.
        /// LineRenderer extends visually. Player does NOT move yet.
        /// </summary>
        private async Awaitable ExtendHookToTarget(GadgetExecutionContext context, Vector2 target)
        {
            Vector2 origin = context.UserPosition;
            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = true;
                _lineRenderer.positionCount = 2;
            }

            float distance = Vector2.Distance(origin, target);
            float duration = distance / HookSpeedUnits;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Lerp hook head position
                Vector2 currentHookPos = Vector2.Lerp(origin, target, t);
                if (_hookHead != null)
                {
                    _hookHead.position = currentHookPos;
                }

                // Update line renderer
                if (_lineRenderer != null)
                {
                    Vector2 currentOrigin = context.UserTransform != null ? (Vector2)context.UserTransform.position : origin;
                    _lineRenderer.SetPosition(0, currentOrigin);
                    _lineRenderer.SetPosition(1, currentHookPos);
                }

                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }

            // Snap to final position
            if (_hookHead != null)
            {
                _hookHead.position = target;
            }
            if (_lineRenderer != null)
            {
                _lineRenderer.SetPosition(1, target);
            }
        }

        /// <summary>
        /// Phase 2a: Pull player toward the grapple point.
        /// Called AFTER ExtendHookToTarget completes.
        /// </summary>
        private async Awaitable PullPlayerToTarget(GadgetExecutionContext context, GrapplePoint target)
        {
            if (context.UserRigidbody == null)
            {
                Debug.LogError("[Grapple] UserRigidbody is NULL! Cannot pull player. Check that Rigidbody2D is on the same GameObject as GadgetUser.");
                return;
            }

            Vector2 targetPos = (Vector2)target.transform.position + target.LandingOffset;
            Vector2 startPos = context.UserRigidbody.position;

            float distance = Vector2.Distance(startPos, targetPos);
            
            Debug.Log($"[Grapple] Starting pull. StartPos: {startPos}, TargetPos: {targetPos}, Distance: {distance:F2} units, RB: {context.UserRigidbody.name}");

            // Move at constant speed toward target
            while (Vector2.Distance(context.UserRigidbody.position, targetPos) > 0.05f)
            {
                Vector2 currentPos = context.UserRigidbody.position;
                Vector2 direction = (targetPos - currentPos).normalized;
                float step = PullSpeedUnits * Time.deltaTime;
                
                Vector2 newPos = currentPos + direction * step;
                
                // Don't overshoot
                if (Vector2.Distance(currentPos, targetPos) < step)
                {
                    newPos = targetPos;
                }

                Debug.Log($"[Grapple] Moving from {currentPos} to {newPos} (step: {step:F3})");
                context.UserRigidbody.MovePosition(newPos);

                // Keep line attached
                if (_lineRenderer != null)
                {
                    _lineRenderer.SetPosition(0, newPos);
                }

                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }

            // Snap to landing position
            context.UserRigidbody.MovePosition(targetPos);
            Debug.Log($"[Grapple] Pull complete! Final position: {context.UserRigidbody.position}");
        }

        /// <summary>
        /// Phase 2b: Pull enemy toward the player.
        /// Called AFTER ExtendHookToTarget completes.
        /// </summary>
        private async Awaitable PullTargetToPlayer(GadgetExecutionContext context, IPullable target)
        {
            target.PullToward(context.UserPosition, PullSpeedUnits);

            // Wait for pull to complete
            // In a full implementation, you'd listen for OnPullComplete callback
            await Awaitable.WaitForSecondsAsync(0.5f, destroyCancellationToken);

            target.OnPullComplete();
        }

        /// <summary>
        /// Phase 3: Retract hook back to player (whiff case).
        /// </summary>
        private async Awaitable RetractHook(Vector2 playerPos)
        {
            if (_hookHead == null) return;

            Vector2 hookPos = _hookHead.position;
            float distance = Vector2.Distance(hookPos, playerPos);
            float duration = distance / HookSpeedUnits;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Vector2 currentPos = Vector2.Lerp(hookPos, playerPos, t);
                _hookHead.position = currentPos;

                if (_lineRenderer != null)
                {
                    _lineRenderer.SetPosition(1, currentPos);
                }

                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }
        }

        public override void Terminate()
        {
            base.Terminate();
            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = false;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure speeds are positive
            _hookSpeedPixels = Mathf.Max(1f, _hookSpeedPixels);
            _maxDistancePixels = Mathf.Max(1f, _maxDistancePixels);
            _pullSpeedPixels = Mathf.Max(1f, _pullSpeedPixels);

            // Auto-find LineRenderer if not assigned
            if (_lineRenderer == null)
            {
                _lineRenderer = GetComponentInChildren<LineRenderer>();
            }
        }
#endif
    }
}
