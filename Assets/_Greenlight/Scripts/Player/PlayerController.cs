using UnityEngine;
using UnityEngine.InputSystem;
using Greenlight.Core;
using Greenlight.Core.SceneManagement;
using Greenlight.Gadgets;
using Greenlight.Combat;

namespace Greenlight.Player
{
    /// <summary>
    /// Main player controller that routes input to appropriate subsystems.
    /// Extends StateResponder to react to input lock flags from GameStateSO.
    /// 
    /// Design Philosophy:
    /// - Input routing only - delegates execution to Motor, GadgetUser, etc.
    /// - Respects System_Input_Locked flag from Nervous System
    /// - Separation of Concerns: Controller doesn't handle physics or visuals
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerVisuals))]
    [AddComponentMenu("Greenlight/Player/Player Controller")]
    public class PlayerController : StateResponder
    {
        [Header("Components")]
        [SerializeField, Tooltip("Player motor for movement.")]
        private PlayerMotor _motor;

        [SerializeField, Tooltip("Player visuals for sprite control.")]
        private PlayerVisuals _visuals;

        [SerializeField, Tooltip("Gadget user for tool execution.")]
        private GadgetUser _gadgetUser;

        [Header("Combat Components")]
        [SerializeField, Tooltip("Health component for damage handling.")]
        private HealthComponent _healthComponent;

        [SerializeField, Tooltip("Knockback receiver for impact physics.")]
        private KnockbackReceiver _knockbackReceiver;

        [SerializeField, Tooltip("Invincibility controller for iframes.")]
        private InvincibilityController _invincibilityController;

        [Header("Input Lock")]
        [SerializeField, Tooltip("Flag key that locks all player input (e.g., during cutscenes).")]
        private string _inputLockFlagKey = "System_Input_Locked";

        // Input state
        private Vector2 _moveInput;
        private bool _sprintInput;
        private bool _isInputLocked;

        /// <summary>
        /// Is player input currently locked?
        /// </summary>
        public bool IsInputLocked => _isInputLocked;

        /// <summary>
        /// Current movement input vector.
        /// </summary>
        public Vector2 MoveInput => _moveInput;

        protected override void Awake()
        {
            base.Awake();

            // Auto-assign components
            if (_motor == null) _motor = GetComponent<PlayerMotor>();
            if (_visuals == null) _visuals = GetComponent<PlayerVisuals>();
            if (_gadgetUser == null) _gadgetUser = GetComponent<GadgetUser>();

            // Auto-assign combat components
            if (_healthComponent == null) _healthComponent = GetComponent<HealthComponent>();
            if (_knockbackReceiver == null) _knockbackReceiver = GetComponent<KnockbackReceiver>();
            if (_invincibilityController == null) _invincibilityController = GetComponent<InvincibilityController>();
        }

        private void Update()
        {
            // Determine if movement is locked (system, gadget, or combat states)
            bool gadgetLocking = _gadgetUser != null && _gadgetUser.IsMovementLocked;
            bool combatLocking = (_knockbackReceiver != null && _knockbackReceiver.IsKnockedBack) ||
                                 (_healthComponent != null && _healthComponent.IsDead);
            bool movementLocked = _isInputLocked || gadgetLocking || combatLocking;

            // Process movement input
            if (!movementLocked && _motor != null)
            {
                _motor.Paused = false;
                _motor.SetMovementDirection(_moveInput, _sprintInput);

                // Update facing direction for visuals
                if (_visuals != null && _moveInput.sqrMagnitude > 0.01f)
                {
                    _visuals.UpdateFacingDirection(_moveInput);
                }
            }
            else if (_motor != null)
            {
                // Movement locked - stop motor and pause appropriately
                bool shouldPause = gadgetLocking || combatLocking;
                
                if (_debugLog && shouldPause != _motor.Paused)
                {
                    string reason = gadgetLocking ? "gadget" : combatLocking ? "combat" : "system";
                    Debug.Log($"[PlayerController] Movement locked by {reason}");
                }
                
                _motor.Stop();
                _motor.Paused = shouldPause;
            }
        }

        #region Input System Callbacks

        /// <summary>
        /// Called by Input System when Move action is performed.
        /// </summary>
        public void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();

            if (_debugLog)
            {
                Debug.Log($"[PlayerController] Move input: {_moveInput}");
            }
        }

        /// <summary>
        /// Called by Input System when Sprint action is performed.
        /// </summary>
        public void OnSprint(InputAction.CallbackContext context)
        {
            _sprintInput = context.ReadValueAsButton();

            if (_debugLog && context.performed)
            {
                Debug.Log($"[PlayerController] Sprint: {_sprintInput}");
            }
        }

        /// <summary>
        /// Called by Input System when UseGadget action is performed.
        /// </summary>
        public void OnUseGadget(InputAction.CallbackContext context)
        {
            if (_isInputLocked || !context.performed) return;

            if (_gadgetUser != null && _visuals != null)
            {
                _gadgetUser.UseEquippedGadget(_visuals.FacingDirection);

                if (_debugLog)
                {
                    Debug.Log("[PlayerController] Use gadget requested");
                }
            }
        }

        /// <summary>
        /// Called by Input System when Next action is performed (cycle gadget forward).
        /// </summary>
        public void OnNext(InputAction.CallbackContext context)
        {
            if (_isInputLocked || !context.performed) return;

            if (_gadgetUser != null)
            {
                _gadgetUser.CycleGadget(1);

                if (_debugLog)
                {
                    Debug.Log("[PlayerController] Cycle gadget next");
                }
            }
        }

        /// <summary>
        /// Called by Input System when Previous action is performed (cycle gadget backward).
        /// </summary>
        public void OnPrevious(InputAction.CallbackContext context)
        {
            if (_isInputLocked || !context.performed) return;

            if (_gadgetUser != null)
            {
                _gadgetUser.CycleGadget(-1);

                if (_debugLog)
                {
                    Debug.Log("[PlayerController] Cycle gadget previous");
                }
            }
        }

        #endregion

        #region State Observer Implementation

        public override void OnStateQueried(GameStateSO state)
        {
            base.OnStateQueried(state);

            // Check if input is locked on scene load
            _isInputLocked = state.GetBool(_inputLockFlagKey, false);

            if (_debugLog)
            {
                Debug.Log($"[PlayerController] Input lock state: {_isInputLocked}");
            }
        }

        protected override void OnFlagChanged(Core.Events.FlagChangePayload payload)
        {
            base.OnFlagChanged(payload);

            // React to input lock changes at runtime
            if (payload.FlagKey == _inputLockFlagKey && payload.Type == Core.Events.FlagType.Bool)
            {
                _isInputLocked = payload.BoolValue;

                if (_isInputLocked)
                {
                    // Clear input when locked
                    _moveInput = Vector2.zero;
                    _sprintInput = false;

                    if (_motor != null)
                    {
                        _motor.Stop();
                    }
                }

                if (_debugLog)
                {
                    Debug.Log($"[PlayerController] Input lock changed: {_isInputLocked}");
                }
            }
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_motor == null) _motor = GetComponent<PlayerMotor>();
            if (_visuals == null) _visuals = GetComponent<PlayerVisuals>();
            if (_gadgetUser == null) _gadgetUser = GetComponent<GadgetUser>();

            // Auto-assign combat components
            if (_healthComponent == null) _healthComponent = GetComponent<HealthComponent>();
            if (_knockbackReceiver == null) _knockbackReceiver = GetComponent<KnockbackReceiver>();
            if (_invincibilityController == null) _invincibilityController = GetComponent<InvincibilityController>();
        }
#endif
    }
}
