using UnityEngine;
using Greenlight.Core.Events;

namespace Greenlight.Core.SceneManagement
{
    /// <summary>
    /// Simple enable/disable component based on a boolean flag.
    /// For straightforward cases where you just need to show/hide an object.
    /// 
    /// Usage:
    /// 1. Add to a parent object or the object you want to control
    /// 2. Assign the Target (can be self or a child)
    /// 3. Set FlagKey and choose ActiveWhenTrue or ActiveWhenFalse
    /// </summary>
    [AddComponentMenu("Greenlight/Scene Management/Conditional Activator")]
    public class ConditionalActivator : StateResponder
    {
        [Header("Activation Configuration")]
        [SerializeField, Tooltip("The specific flag key that controls activation.")]
        private string _flagKey;

        [SerializeField, Tooltip("The GameObject to activate/deactivate. If null, uses this GameObject.")]
        private GameObject _target;

        [SerializeField, Tooltip("If true, target is active when flag is TRUE. If false, target is active when flag is FALSE.")]
        private bool _activeWhenTrue = true;

        /// <summary>
        /// The flag key this activator responds to.
        /// </summary>
        public string FlagKey
        {
            get => _flagKey;
            set
            {
                _flagKey = value;
                _watchedFlagKeys = string.IsNullOrEmpty(value) ? null : new[] { value };
            }
        }

        /// <summary>
        /// The target GameObject to control. Defaults to this GameObject if null.
        /// </summary>
        public GameObject Target
        {
            get => _target != null ? _target : gameObject;
            set => _target = value;
        }

        /// <summary>
        /// If true, target activates when flag is true. If false, target activates when flag is false.
        /// </summary>
        public bool ActiveWhenTrue
        {
            get => _activeWhenTrue;
            set => _activeWhenTrue = value;
        }

        protected override void OnEnable()
        {
            if (!string.IsNullOrEmpty(_flagKey))
            {
                _watchedFlagKeys = new[] { _flagKey };
            }

            base.OnEnable();
        }

        public override void OnStateQueried(GameStateSO state)
        {
            base.OnStateQueried(state);
            ApplyActivation();
        }

        protected override void OnFlagChanged(FlagChangePayload payload)
        {
            base.OnFlagChanged(payload);

            if (payload.FlagKey == _flagKey && payload.Type == FlagType.Bool)
            {
                ApplyActivation(payload.BoolValue);
            }
        }

        /// <summary>
        /// Applies activation state based on current flag value.
        /// </summary>
        private void ApplyActivation()
        {
            if (_gameState == null || string.IsNullOrEmpty(_flagKey))
                return;

            bool flagValue = _gameState.GetBool(_flagKey);
            ApplyActivation(flagValue);
        }

        /// <summary>
        /// Applies activation state based on the given flag value.
        /// </summary>
        private void ApplyActivation(bool flagValue)
        {
            bool shouldBeActive = _activeWhenTrue ? flagValue : !flagValue;
            GameObject target = Target;

            if (target != null)
            {
                target.SetActive(shouldBeActive);

                if (_debugLog)
                {
                    Debug.Log($"[ConditionalActivator] '{gameObject.name}' set target '{target.name}' " +
                             $"active={shouldBeActive} (flag '{_flagKey}' = {flagValue})");
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(_flagKey))
            {
                _watchedFlagKeys = new[] { _flagKey };
            }
        }
#endif
    }
}
