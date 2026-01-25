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
    /// 3. Assign a Flag asset or set a FlagKey manually
    /// 4. Choose ActiveWhenTrue or ActiveWhenFalse
    /// </summary>
    [AddComponentMenu("Greenlight/Scene Management/Conditional Activator")]
    public class ConditionalActivator : StateResponder
    {
        [Header("Activation Configuration")]
        [SerializeField, Tooltip("Optional: Assign a flag asset to automatically set the key.")]
        private WorldFlagDefinitionSO _flag;

        [SerializeField, Tooltip("Manual flag key if no asset is assigned.")]
        private string _manualFlagKey;

        [SerializeField, Tooltip("The GameObject to activate/deactivate. If null, uses this GameObject.")]
        private GameObject _target;

        [SerializeField, Tooltip("If true, target is active when flag is TRUE. If false, target is active when flag is FALSE.")]
        private bool _activeWhenTrue = true;

        /// <summary>
        /// The effective flag key this activator responds to.
        /// </summary>
        public string EffectiveFlagKey => _flag != null ? _flag.FlagKey : _manualFlagKey;

        protected override void Awake()
        {
            SyncWatchedFlags();
            base.Awake();
        }

        private void SyncWatchedFlags()
        {
            if (_flag != null)
            {
                _watchedFlags = new[] { _flag };
            }
            else if (!string.IsNullOrEmpty(_manualFlagKey))
            {
                _watchedFlagKeys = new[] { _manualFlagKey };
            }
        }

        public override void OnStateQueried(GameStateSO state)
        {
            base.OnStateQueried(state);
            ApplyActivation();
        }

        protected override void OnFlagChanged(FlagChangePayload payload)
        {
            base.OnFlagChanged(payload);

            if (payload.FlagKey == EffectiveFlagKey && payload.Type == FlagType.Bool)
            {
                ApplyActivation(payload.BoolValue);
            }
        }

        /// <summary>
        /// Applies activation state based on current flag value.
        /// </summary>
        private void ApplyActivation()
        {
            string key = EffectiveFlagKey;
            if (_gameState == null || string.IsNullOrEmpty(key))
                return;

            bool flagValue = _gameState.GetBool(key);
            ApplyActivation(flagValue);
        }

        /// <summary>
        /// Applies activation state based on the given flag value.
        /// </summary>
        private void ApplyActivation(bool flagValue)
        {
            bool shouldBeActive = _activeWhenTrue ? flagValue : !flagValue;
            GameObject target = _target != null ? _target : gameObject;

            if (target != null)
            {
                target.SetActive(shouldBeActive);

                if (_debugLog)
                {
                    Debug.Log($"[ConditionalActivator] '{gameObject.name}' set target '{target.name}' " +
                             $"active={shouldBeActive} (flag '{EffectiveFlagKey}' = {flagValue})");
                }
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            SyncWatchedFlags();
            base.OnValidate();
        }
#endif
    }
}
