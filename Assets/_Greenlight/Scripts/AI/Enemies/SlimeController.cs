using UnityEngine;

namespace Greenlight.AI
{
    /// <summary>
    /// Slime enemy controller - "The Introduction" enemy type.
    /// Teaches player timing and dodge fundamentals through clear telegraphs.
    /// 
    /// Design Philosophy:
    /// - Simple, readable attack pattern: Telegraph -> Charge -> Recovery
    /// - Clear visual and audio cues (red flash = dodge NOW)
    /// - Vulnerable recovery phase rewards good timing
    /// - Teaches fundamental combat vocabulary
    /// </summary>
    [AddComponentMenu("Greenlight/AI/Slime Controller")]
    public class SlimeController : EnemyController
    {
        [Header("Slime-Specific Settings")]
        [SerializeField, Tooltip("Telegraph settings for charge attack.")]
        private TelegraphSettingsSO _chargeTelegraphSettings;

        [SerializeField, Range(0.3f, 1.5f), Tooltip("Telegraph duration before charge (seconds).")]
        private float _telegraphDuration = 0.8f;

        [SerializeField, Range(0.2f, 0.8f), Tooltip("Charge execution duration (seconds).")]
        private float _chargeDuration = 0.5f;

        [SerializeField, Range(0.5f, 2f), Tooltip("Recovery duration after charge (seconds).")]
        private float _recoveryDuration = 1.2f;

        [SerializeField, Range(1f, 5f), Tooltip("Charge speed multiplier.")]
        private float _chargeSpeedMultiplier = 3f;

        [Header("Slime Visuals")]
        [SerializeField, Tooltip("Scale multiplier during telegraph (squash effect).")]
        private Vector2 _telegraphScale = new Vector2(1.2f, 0.6f);

        [SerializeField, Tooltip("Scale multiplier during charge (stretch effect).")]
        private Vector2 _chargeScale = new Vector2(0.7f, 1.4f);

        [Header("Audio")]
        [SerializeField, Tooltip("Audio clip for telegraph charge-up sound.")]
        private AudioClip _telegraphSFX;

        [SerializeField, Tooltip("Audio clip for charge whoosh sound.")]
        private AudioClip _chargeSFX;

        [SerializeField, Tooltip("Audio clip for recovery deflate sound.")]
        private AudioClip _recoverySFX;

        [SerializeField, Tooltip("Audio source for slime sounds.")]
        private AudioSource _audioSource;

        // State tracking for slime-specific behavior
        private Vector2 _chargeDirection;
        private Vector3 _originalScale;
        private bool _isCharging;
        private float _chargeStartTime;

        /// <summary>
        /// Is the slime currently executing a charge attack?
        /// </summary>
        public bool IsCharging => _isCharging;

        /// <summary>
        /// Direction the slime is charging in.
        /// </summary>
        public Vector2 ChargeDirection => _chargeDirection;

        private void Awake()
        {
            base.Awake();

            // Store original scale
            _originalScale = transform.localScale;

            // Auto-assign audio source
            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            base.Start();

            // Override state machine with slime-specific states
            InitializeSlimeStates();
        }

        private void Update()
        {
            // Handle charge movement if actively charging
            if (_isCharging)
            {
                UpdateChargeMovement();
            }
        }

        /// <summary>
        /// Initialize slime-specific state machine states.
        /// </summary>
        private void InitializeSlimeStates()
        {
            // Replace the generic AttackState with SlimeAttackState
            StateMachine.AddState(new SlimeAttackState());
        }

        #region Slime Attack Behavior

        /// <summary>
        /// Start the charge attack sequence.
        /// </summary>
        /// <param name="targetDirection">Direction to charge in</param>
        public void StartChargeAttack(Vector2 targetDirection)
        {
            _chargeDirection = targetDirection.normalized;
            
            // Start with telegraph phase
            StartChargeTelegraph();
        }

        /// <summary>
        /// Start the telegraph phase of the charge attack.
        /// </summary>
        private async void StartChargeTelegraph()
        {
            // Play telegraph sound
            PlaySound(_telegraphSFX);

            // Apply telegraph visual effects
            ApplyTelegraphVisuals();

            // Apply telegraph tint (red flash)
            if (EnemyVisuals != null)
            {
                EnemyVisuals.SetTemporaryTint(Color.red, 0.7f);
            }

            // Wait for telegraph duration
            try
            {
                await Awaitable.WaitForSecondsAsync(_telegraphDuration, destroyCancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                return; // Object destroyed
            }

            // Start charge execution
            ExecuteCharge();
        }

        /// <summary>
        /// Execute the charge attack.
        /// </summary>
        private async void ExecuteCharge()
        {
            _isCharging = true;
            _chargeStartTime = Time.time;

            // Play charge sound
            PlaySound(_chargeSFX);

            // Apply charge visuals
            ApplyChargeVisuals();

            // Clear telegraph tint
            if (EnemyVisuals != null)
            {
                EnemyVisuals.ClearTemporaryTint();
            }

            // Set charge movement
            SetMoveSpeedMultiplier(_chargeSpeedMultiplier);
            MoveInDirection(_chargeDirection);

            // Wait for charge duration
            try
            {
                await Awaitable.WaitForSecondsAsync(_chargeDuration, destroyCancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                return; // Object destroyed
            }

            // End charge
            EndCharge();
        }

        /// <summary>
        /// End the charge and start recovery phase.
        /// </summary>
        private async void EndCharge()
        {
            _isCharging = false;

            // Stop movement
            StopMovement();
            SetMoveSpeedMultiplier(1f);

            // Play recovery sound
            PlaySound(_recoverySFX);

            // Apply recovery visuals
            ApplyRecoveryVisuals();

            // Wait for recovery duration
            try
            {
                await Awaitable.WaitForSecondsAsync(_recoveryDuration, destroyCancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                return; // Object destroyed
            }

            // Return to normal state
            RestoreNormalVisuals();
        }

        /// <summary>
        /// Update charge movement (called during charge execution).
        /// </summary>
        private void UpdateChargeMovement()
        {
            // Continue moving in charge direction
            MoveInDirection(_chargeDirection);

            // Check for collision or obstacles
            // In a full implementation, this would handle bouncing off walls,
            // hitting the player, etc.
        }

        #endregion

        #region Visual Effects

        /// <summary>
        /// Apply telegraph visual effects (squash flat + vibrate + red tint).
        /// </summary>
        private void ApplyTelegraphVisuals()
        {
            // Apply squash effect
            transform.localScale = new Vector3(
                _originalScale.x * _telegraphScale.x,
                _originalScale.y * _telegraphScale.y,
                _originalScale.z
            );

            // Start vibration effect
            StartTelegraphVibration();

            // Play telegraph animation
            EnemyVisuals?.PlayTelegraphAnimation();
        }

        /// <summary>
        /// Apply charge visual effects (stretched horizontally).
        /// </summary>
        private void ApplyChargeVisuals()
        {
            // Apply stretch effect
            transform.localScale = new Vector3(
                _originalScale.x * _chargeScale.x,
                _originalScale.y * _chargeScale.y,
                _originalScale.z
            );

            // Play charge animation
            EnemyVisuals?.PlayAttackAnimation();
        }

        /// <summary>
        /// Apply recovery visual effects (dizzy stars, vulnerable state).
        /// </summary>
        private void ApplyRecoveryVisuals()
        {
            // Restore normal scale
            transform.localScale = _originalScale;

            // Apply vulnerable tint
            if (EnemyVisuals != null)
            {
                EnemyVisuals.SetTemporaryTint(Color.yellow, 0.3f);
            }

            // Play recovery animation (shows dizzy stars)
            EnemyVisuals?.PlayRecoveryAnimation();
        }

        /// <summary>
        /// Restore normal visual state.
        /// </summary>
        private void RestoreNormalVisuals()
        {
            // Restore scale
            transform.localScale = _originalScale;

            // Clear any tints
            if (EnemyVisuals != null)
            {
                EnemyVisuals.ClearTemporaryTint();
            }

            // Return to idle animation
            EnemyVisuals?.PlayIdleAnimation();
        }

        /// <summary>
        /// Start telegraph vibration effect.
        /// </summary>
        private async void StartTelegraphVibration()
        {
            Vector3 originalPosition = transform.position;
            float vibrationIntensity = 0.05f; // Small vibration in Unity units
            float vibrationSpeed = 20f; // Vibrations per second

            float endTime = Time.time + _telegraphDuration;
            
            while (Time.time < endTime && !_isCharging)
            {
                // Calculate vibration offset
                float vibrationX = Mathf.Sin(Time.time * vibrationSpeed) * vibrationIntensity;
                float vibrationY = Mathf.Sin(Time.time * vibrationSpeed * 1.3f) * vibrationIntensity * 0.5f;

                // Apply vibration with pixel snapping
                Vector3 vibrationOffset = new Vector3(vibrationX, vibrationY, 0f);
                vibrationOffset.x = Mathf.Round(vibrationOffset.x * 32f) / 32f;
                vibrationOffset.y = Mathf.Round(vibrationOffset.y * 32f) / 32f;

                transform.position = originalPosition + vibrationOffset;

                try
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                }
                catch (System.OperationCanceledException)
                {
                    break;
                }
            }

            // Restore position
            transform.position = originalPosition;
        }

        #endregion

        #region Audio

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

        #region Slime-Specific State

        /// <summary>
        /// Custom attack state for slime that handles the charge sequence.
        /// </summary>
        private class SlimeAttackState : AttackState
        {
            private SlimeController _slimeController;

            public override void Initialize(EnemyController owner, EnemyStateMachine stateMachine)
            {
                base.Initialize(owner, stateMachine);
                _slimeController = owner as SlimeController;
            }

            public override void Enter()
            {
                // Don't call base.Enter() - we handle our own attack sequence
                TimeInState = 0f;

                // Start slime charge attack
                Vector2 directionToPlayer = GetDirectionToPlayer();
                if (directionToPlayer != Vector2.zero && _slimeController != null)
                {
                    _slimeController.StartChargeAttack(directionToPlayer);
                }
            }

            public override EnemyState CheckTransitions()
            {
                // Wait for charge sequence to complete
                float totalAttackTime = _slimeController._telegraphDuration + 
                                       _slimeController._chargeDuration + 
                                       _slimeController._recoveryDuration;

                if (TimeInState >= totalAttackTime)
                {
                    // Attack complete - decide next state
                    if (IsPlayerInRange(Owner.Definition.AttackRange))
                    {
                        // Player still close - wait for cooldown before next attack
                        if (TimeInState >= totalAttackTime + Owner.Definition.AttackCooldown)
                        {
                            return StateMachine.GetState<SlimeAttackState>();
                        }
                    }
                    else if (IsPlayerInRange(Owner.Definition.DetectionRange))
                    {
                        // Player moved away - chase
                        return StateMachine.GetState<ChaseState>();
                    }
                    else
                    {
                        // Player lost - return to idle
                        return StateMachine.GetState<IdleState>();
                    }
                }

                return null; // Stay in attack state
            }

            public override void OnDamageTaken(int damage, Transform source)
            {
                // Slime is vulnerable during recovery phase
                float recoveryStartTime = _slimeController._telegraphDuration + _slimeController._chargeDuration;
                
                if (TimeInState >= recoveryStartTime)
                {
                    // During recovery - can be interrupted
                    base.OnDamageTaken(damage, source);
                }
                else
                {
                    // During telegraph/charge - harder to interrupt
                    // Still take damage but don't necessarily interrupt attack
                    // This makes the timing more forgiving for players
                }
            }
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (Definition == null)
                return;

            // Draw basic enemy info
            Gizmos.color = Color.yellow;
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, Definition.DetectionRange);
            Gizmos.color = Color.red;
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, Definition.AttackRange);

            // Draw charge direction if charging
            if (Application.isPlaying && _isCharging)
            {
                Gizmos.color = Color.red;
                Vector3 chargeEnd = transform.position + (Vector3)_chargeDirection * 3f;
                Gizmos.DrawLine(transform.position, chargeEnd);
                Gizmos.DrawSphere(chargeEnd, 0.2f);
            }
        }
#endif
    }
}