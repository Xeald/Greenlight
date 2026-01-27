using UnityEngine;
using Greenlight.Gadgets;

namespace Greenlight.AI
{
    /// <summary>
    /// Shielded Guardian enemy - "The Grammar Lesson" enemy type.
    /// Teaches that gadgets solve combat problems through IPullable integration.
    /// 
    /// Design Philosophy:
    /// - Shield blocks frontal damage completely
    /// - Grapple hook can "rip" shield away with satisfying impact
    /// - Creates vulnerable window after shield is removed
    /// - Demonstrates "Gadgets as Grammar" pillar
    /// </summary>
    [AddComponentMenu("Greenlight/AI/Shielded Enemy Controller")]
    public class ShieldedEnemyController : EnemyController, IPullable
    {
        [Header("Shield System")]
        [SerializeField, Tooltip("Shield sprite renderer.")]
        private SpriteRenderer _shieldSpriteRenderer;

        [SerializeField, Tooltip("Transform representing the shield's position.")]
        private Transform _shieldTransform;

        [SerializeField, Tooltip("Position offset for shield when active.")]
        private Vector3 _shieldActivePosition = Vector3.zero;

        [SerializeField, Tooltip("Position offset for shield when dropped.")]
        private Vector3 _shieldDroppedPosition = new Vector3(0.5f, -0.3f, 0f);

        [Header("Shield Mechanics")]
        [SerializeField, Range(0.5f, 3f), Tooltip("Duration shield stays down after being pulled (seconds).")]
        private float _shieldDownDuration = 2f;

        [SerializeField, Range(0.2f, 1f), Tooltip("Time to recover shield after stun ends (seconds).")]
        private float _shieldRecoveryTime = 0.8f;

        [SerializeField, Range(45f, 180f), Tooltip("Shield blocking arc in degrees (front protection).")]
        private float _shieldBlockingArc = 120f;

        [Header("Pull Mechanics")]
        [SerializeField, Range(1f, 5f), Tooltip("Force applied when pulled by grappling hook.")]
        private float _pullForce = 3f;

        [SerializeField, Range(0.2f, 1f), Tooltip("Duration of pull effect (seconds).")]
        private float _pullDuration = 0.5f;

        [Header("Audio")]
        [SerializeField, Tooltip("Sound when shield blocks damage.")]
        private AudioClip _shieldBlockSFX;

        [SerializeField, Tooltip("Sound when shield is ripped away.")]
        private AudioClip _shieldRipSFX;

        [SerializeField, Tooltip("Sound when picking up shield.")]
        private AudioClip _shieldRecoverSFX;

        [SerializeField, Tooltip("Audio source for shield sounds.")]
        private AudioSource _audioSource;

        // Shield state
        private bool _hasShield = true;
        private bool _isBeingPulled;
        private bool _isPullComplete;
        private Vector3 _originalShieldPosition;
        private Quaternion _originalShieldRotation;

        /// <summary>
        /// Does this enemy currently have their shield raised?
        /// </summary>
        public bool HasShield => _hasShield;

        /// <summary>
        /// Is the enemy currently being pulled by a grapple hook?
        /// </summary>
        public bool IsBeingPulled => _isBeingPulled;

        /// <summary>
        /// Current shield blocking direction (normalized).
        /// </summary>
        public Vector2 ShieldDirection => EnemyVisuals != null ? EnemyVisuals.CurrentFacingDirection : Vector2.right;

        private void Awake()
        {
            // Auto-assign shield components
            if (_shieldSpriteRenderer == null)
                _shieldSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (_shieldTransform == null && _shieldSpriteRenderer != null)
                _shieldTransform = _shieldSpriteRenderer.transform;

            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();

            // Store original shield position
            if (_shieldTransform != null)
            {
                _originalShieldPosition = _shieldTransform.localPosition;
                _originalShieldRotation = _shieldTransform.localRotation;
            }
        }

        private void Start()
        {
            // Initialize shield in active position
            SetShieldActive(true);

            // Override state machine with shielded-specific states
            InitializeShieldedStates();
        }

        #region IPullable Implementation

        /// <summary>
        /// Called by grappling hook to pull this enemy.
        /// Critical moment: the "Shield Rip" effect.
        /// </summary>
        /// <param name="targetPosition">Position to pull toward</param>
        /// <param name="speed">Pull speed</param>
        public void PullToward(Vector2 targetPosition, float speed)
        {
            if (_isBeingPulled)
                return; // Already being pulled

            _isBeingPulled = true;
            _isPullComplete = false;

            // Execute the "Shield Rip" sequence
            ExecuteShieldRip(targetPosition, speed);
        }

        /// <summary>
        /// Called when pull effect completes.
        /// </summary>
        public void OnPullComplete()
        {
            _isPullComplete = true;

            // Force transition to stunned state
            if (StateMachine != null)
            {
                StateMachine.TransitionTo<StunnedState>();
            }

            // Play impact effect
            PlayShieldRipImpact();

            // Start shield recovery timer
            StartShieldRecoveryTimer();
        }

        #endregion

        #region Shield Mechanics

        /// <summary>
        /// Check if incoming damage should be blocked by the shield.
        /// </summary>
        /// <param name="damageSource">Source of the damage</param>
        /// <returns>True if damage should be blocked</returns>
        public bool ShouldBlockDamage(Transform damageSource)
        {
            if (!_hasShield || damageSource == null)
                return false;

            // Calculate angle from enemy to damage source
            Vector2 toSource = (damageSource.position - transform.position).normalized;
            Vector2 shieldDirection = ShieldDirection;

            float angle = Vector2.Angle(shieldDirection, toSource);

            // Block if within shield arc
            bool shouldBlock = angle <= _shieldBlockingArc * 0.5f;

            if (shouldBlock)
            {
                // Play shield block effects
                PlayShieldBlockEffects();
            }

            return shouldBlock;
        }

        /// <summary>
        /// Set shield active or inactive state.
        /// </summary>
        /// <param name="active">True to raise shield, false to lower it</param>
        public void SetShieldActive(bool active)
        {
            _hasShield = active;

            if (_shieldTransform != null)
            {
                if (active)
                {
                    // Raise shield
                    _shieldTransform.localPosition = _originalShieldPosition + _shieldActivePosition;
                    _shieldTransform.localRotation = _originalShieldRotation;
                }
                else
                {
                    // Drop shield
                    _shieldTransform.localPosition = _originalShieldPosition + _shieldDroppedPosition;
                    
                    // Rotate shield to show it's dropped
                    _shieldTransform.localRotation = _originalShieldRotation * Quaternion.Euler(0, 0, 45f);
                }
            }

            // Update sprite visibility
            if (_shieldSpriteRenderer != null)
            {
                _shieldSpriteRenderer.enabled = active || !active; // Always show shield, just positioned differently
                
                if (!active)
                {
                    // Dim the shield when dropped
                    Color shieldColor = _shieldSpriteRenderer.color;
                    shieldColor.a = 0.6f;
                    _shieldSpriteRenderer.color = shieldColor;
                }
                else
                {
                    // Restore full alpha
                    Color shieldColor = _shieldSpriteRenderer.color;
                    shieldColor.a = 1f;
                    _shieldSpriteRenderer.color = shieldColor;
                }
            }
        }

        #endregion

        #region Shield Rip Sequence

        /// <summary>
        /// Execute the satisfying "Shield Rip" effect.
        /// </summary>
        /// <param name="pullTarget">Position being pulled toward</param>
        /// <param name="pullSpeed">Speed of the pull</param>
        private async void ExecuteShieldRip(Vector2 pullTarget, float pullSpeed)
        {
            // Play shield rip sound immediately for feedback
            PlaySound(_shieldRipSFX);

            // 1. Shield "detaches" and spins
            AnimateShieldRip();

            // 2. Enemy rotates toward pull source (feels like being yanked)
            Vector2 pullDirection = (pullTarget - (Vector2)transform.position).normalized;
            RotateTowardPull(pullDirection);

            // 3. Apply knockback in pull direction
            if (GetComponent<KnockbackReceiver>() != null)
            {
                GetComponent<KnockbackReceiver>().ApplyKnockback(
                    pullDirection,
                    _pullForce,
                    _pullDuration
                );
            }

            // Wait for pull duration
            try
            {
                await Awaitable.WaitForSecondsAsync(_pullDuration, destroyCancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                return;
            }

            // Complete the pull
            OnPullComplete();
        }

        /// <summary>
        /// Animate the shield being ripped away.
        /// </summary>
        private async void AnimateShieldRip()
        {
            if (_shieldTransform == null)
                return;

            // Disable shield protection immediately
            SetShieldActive(false);

            // Animate shield spinning away
            Vector3 startPos = _shieldTransform.position;
            Quaternion startRot = _shieldTransform.rotation;
            
            float spinSpeed = 720f; // degrees per second
            float moveDistance = 1.5f;
            Vector2 spinDirection = Random.insideUnitCircle.normalized;

            float animTime = 0f;
            float animDuration = 0.8f;

            while (animTime < animDuration)
            {
                animTime += Time.deltaTime;
                float progress = animTime / animDuration;

                // Spin the shield
                float rotationAmount = spinSpeed * animTime;
                _shieldTransform.rotation = startRot * Quaternion.Euler(0, 0, rotationAmount);

                // Move shield in spin direction with falloff
                float falloffCurve = 1f - (progress * progress); // Quadratic falloff
                Vector3 offset = (Vector3)spinDirection * moveDistance * falloffCurve * progress;
                _shieldTransform.position = startPos + offset;

                try
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                }
                catch (System.OperationCanceledException)
                {
                    return;
                }
            }

            // Shield settles in dropped position
            _shieldTransform.localPosition = _originalShieldPosition + _shieldDroppedPosition;
            _shieldTransform.localRotation = _originalShieldRotation * Quaternion.Euler(0, 0, 45f);
        }

        /// <summary>
        /// Rotate enemy toward the pull direction.
        /// </summary>
        /// <param name="pullDirection">Direction of the pull</param>
        private void RotateTowardPull(Vector2 pullDirection)
        {
            if (EnemyVisuals != null)
            {
                EnemyVisuals.SetFacingDirection(pullDirection);
            }
        }

        /// <summary>
        /// Play shield rip impact effects.
        /// </summary>
        private void PlayShieldRipImpact()
        {
            // Visual feedback for impact
            if (EnemyVisuals != null)
            {
                EnemyVisuals.StartFlicker(0.2f, 15f);
            }

            // Combat feedback for satisfying impact
            CombatFeedbackManager.FeedbackAPI.ApplyDamageFeedback(2, transform, null);
        }

        /// <summary>
        /// Start timer to eventually recover the shield.
        /// </summary>
        private async void StartShieldRecoveryTimer()
        {
            // Wait for shield down duration
            try
            {
                await Awaitable.WaitForSecondsAsync(_shieldDownDuration, destroyCancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                return;
            }

            // Start shield recovery if not still being controlled
            if (!_isBeingPulled && IsAlive)
            {
                RecoverShield();
            }
        }

        /// <summary>
        /// Recover the shield (pick it back up).
        /// </summary>
        private async void RecoverShield()
        {
            // Play shield recovery sound
            PlaySound(_shieldRecoverSFX);

            // Animate shield recovery
            if (_shieldTransform != null)
            {
                Vector3 startPos = _shieldTransform.localPosition;
                Quaternion startRot = _shieldTransform.localRotation;
                Vector3 targetPos = _originalShieldPosition + _shieldActivePosition;
                Quaternion targetRot = _originalShieldRotation;

                float animTime = 0f;
                
                while (animTime < _shieldRecoveryTime)
                {
                    animTime += Time.deltaTime;
                    float progress = animTime / _shieldRecoveryTime;
                    progress = Mathf.SmoothStep(0f, 1f, progress); // Smooth easing

                    _shieldTransform.localPosition = Vector3.Lerp(startPos, targetPos, progress);
                    _shieldTransform.localRotation = Quaternion.Lerp(startRot, targetRot, progress);

                    try
                    {
                        await Awaitable.NextFrameAsync(destroyCancellationToken);
                    }
                    catch (System.OperationCanceledException)
                    {
                        return;
                    }
                }

                // Ensure final position
                _shieldTransform.localPosition = targetPos;
                _shieldTransform.localRotation = targetRot;
            }

            // Restore shield functionality
            SetShieldActive(true);
            _isBeingPulled = false;
            _isPullComplete = false;
        }

        #endregion

        #region Audio & Effects

        /// <summary>
        /// Play shield block effects.
        /// </summary>
        private void PlayShieldBlockEffects()
        {
            PlaySound(_shieldBlockSFX);

            // Visual spark effect
            if (EnemyVisuals != null && _shieldTransform != null)
            {
                // Flash shield briefly
                StartShieldFlash();
            }
        }

        /// <summary>
        /// Flash shield when blocking.
        /// </summary>
        private async void StartShieldFlash()
        {
            if (_shieldSpriteRenderer == null)
                return;

            Color originalColor = _shieldSpriteRenderer.color;
            Color flashColor = Color.white;

            // Flash to white
            _shieldSpriteRenderer.color = flashColor;

            try
            {
                await Awaitable.WaitForSecondsAsync(0.1f, destroyCancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                return;
            }

            // Return to original color
            _shieldSpriteRenderer.color = originalColor;
        }

        /// <summary>
        /// Play a sound effect.
        /// </summary>
        /// <param name="clip">Audio clip to play</param>
        private void PlaySound(AudioClip clip)
        {
            if (_audioSource != null && clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        #endregion

        #region State Machine Integration

        /// <summary>
        /// Initialize shielded-specific states.
        /// </summary>
        private void InitializeShieldedStates()
        {
            // Add shielded-specific guard state
            StateMachine.AddState(new ShieldedGuardState());
        }

        /// <summary>
        /// Override damage handling to account for shield blocking.
        /// </summary>
        /// <param name="damage">Damage amount</param>
        /// <param name="source">Damage source</param>
        /// <param name="damageType">Type of damage</param>
        public new void OnDamageTaken(int damage, Transform source, DamageType damageType)
        {
            // Check if shield should block this damage
            if (ShouldBlockDamage(source))
            {
                // Damage blocked - no effect
                return;
            }

            // Damage not blocked - apply normally
            base.OnDamageTaken(damage, source, damageType);
        }

        #endregion

        #region Custom State

        /// <summary>
        /// Special guard state for shielded enemies.
        /// </summary>
        private class ShieldedGuardState : IdleState
        {
            private ShieldedEnemyController _shieldedController;

            public override void Initialize(EnemyController owner, EnemyStateMachine stateMachine)
            {
                base.Initialize(owner, stateMachine);
                _shieldedController = owner as ShieldedEnemyController;
            }

            public override void Enter()
            {
                base.Enter();

                // Ensure shield is active when guarding
                _shieldedController?.SetShieldActive(true);
            }

            public override void Execute()
            {
                base.Execute();

                // Always face the player when guarding
                Vector2 directionToPlayer = GetDirectionToPlayer();
                if (directionToPlayer != Vector2.zero && _shieldedController?.EnemyVisuals != null)
                {
                    _shieldedController.EnemyVisuals.SetFacingDirection(directionToPlayer);
                }
            }
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (Definition == null)
                return;

            // Draw basic enemy ranges
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCircle(transform.position, Definition.DetectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCircle(transform.position, Definition.AttackRange);

            // Draw shield blocking arc
            if (_hasShield)
            {
                Gizmos.color = Color.blue;
                Vector3 shieldDir = (Vector3)ShieldDirection;
                float halfArc = _shieldBlockingArc * 0.5f;
                
                // Draw shield arc
                for (int i = -Mathf.RoundToInt(halfArc); i <= Mathf.RoundToInt(halfArc); i += 10)
                {
                    float angle = i * Mathf.Deg2Rad;
                    Vector3 arcDir = Quaternion.Euler(0, 0, i) * shieldDir;
                    Gizmos.DrawLine(transform.position, transform.position + arcDir * 1.5f);
                }
            }

            // Draw pull direction if being pulled
            if (Application.isPlaying && _isBeingPulled)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position, 0.5f);
            }
        }
#endif
    }
}