using UnityEngine;
using Greenlight.Core.Events;

namespace Greenlight.Core.SceneManagement
{
    /// <summary>
    /// Swaps between two variant GameObjects based on a boolean flag.
    /// Use for environmental state changes like "IntactHouse" vs "BurntRuins".
    /// 
    /// Usage:
    /// 1. Place both variants as children of this GameObject
    /// 2. Assign them to TrueVariant and FalseVariant
    /// 3. Assign a Flag asset or set a FlagKey manually
    /// 4. On scene load, the correct variant activates based on current state
    /// 5. If the flag changes at runtime, variants swap automatically
    /// </summary>
    [AddComponentMenu("Greenlight/Scene Management/Variant Swapper")]
    public class VariantSwapper : StateResponder
    {
        [Header("Variant Configuration")]
        [SerializeField, Tooltip("Optional: Assign a flag asset to automatically set the key.")]
        private WorldFlagDefinitionSO _flag;

        [SerializeField, Tooltip("Manual flag key if no asset is assigned.")]
        private string _manualFlagKey;

        [SerializeField, Tooltip("GameObject to show when flag is TRUE.")]
        private GameObject _trueVariant;

        [SerializeField, Tooltip("GameObject to show when flag is FALSE.")]
        private GameObject _falseVariant;

        [Header("Behavior")]
        [SerializeField, Tooltip("If true, inverts the logic (TRUE shows falseVariant, FALSE shows trueVariant).")]
        private bool _invertLogic;

        /// <summary>
        /// The effective flag key this swapper responds to.
        /// </summary>
        public string EffectiveFlagKey => _flag != null ? _flag.FlagKey : _manualFlagKey;

        protected override void Awake()
        {
            // Sync watched keys before Awake logic
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
            ApplyVariant();
        }

        protected override void OnFlagChanged(FlagChangePayload payload)
        {
            base.OnFlagChanged(payload);

            if (payload.FlagKey == EffectiveFlagKey && payload.Type == FlagType.Bool)
            {
                ApplyVariant(payload.BoolValue);
            }
        }

        /// <summary>
        /// Applies the correct variant based on current flag state.
        /// </summary>
        private void ApplyVariant()
        {
            string key = EffectiveFlagKey;
            if (_gameState == null || string.IsNullOrEmpty(key))
                return;

            bool flagValue = _gameState.GetBool(key);
            ApplyVariant(flagValue);
        }

        /// <summary>
        /// Applies the correct variant based on the given flag value.
        /// </summary>
        private void ApplyVariant(bool flagValue)
        {
            bool showTrue = _invertLogic ? !flagValue : flagValue;

            if (_trueVariant != null)
            {
                _trueVariant.SetActive(showTrue);
            }

            if (_falseVariant != null)
            {
                _falseVariant.SetActive(!showTrue);
            }

            if (_debugLog)
            {
                string activeVariant = showTrue ? "TrueVariant" : "FalseVariant";
                Debug.Log($"[VariantSwapper] '{gameObject.name}' showing {activeVariant} (flag '{EffectiveFlagKey}' = {flagValue})");
            }
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            SyncWatchedFlags();
            base.OnValidate();
        }

        /// <summary>
        /// Editor utility to preview variants without entering play mode.
        /// </summary>
        [ContextMenu("Preview True Variant")]
        private void PreviewTrue()
        {
            if (_trueVariant != null) _trueVariant.SetActive(true);
            if (_falseVariant != null) _falseVariant.SetActive(false);
        }

        [ContextMenu("Preview False Variant")]
        private void PreviewFalse()
        {
            if (_trueVariant != null) _trueVariant.SetActive(false);
            if (_falseVariant != null) _falseVariant.SetActive(true);
        }
#endif
    }
}
