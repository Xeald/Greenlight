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
    /// 3. Set the FlagKey to watch (e.g., "Village_House1_Burnt")
    /// 4. On scene load, the correct variant activates based on current state
    /// 5. If the flag changes at runtime, variants swap automatically
    /// </summary>
    [AddComponentMenu("Greenlight/Scene Management/Variant Swapper")]
    public class VariantSwapper : StateResponder
    {
        [Header("Variant Configuration")]
        [SerializeField, Tooltip("The specific flag key that controls this swapper.")]
        private string _flagKey;

        [SerializeField, Tooltip("GameObject to show when flag is TRUE.")]
        private GameObject _trueVariant;

        [SerializeField, Tooltip("GameObject to show when flag is FALSE.")]
        private GameObject _falseVariant;

        [Header("Behavior")]
        [SerializeField, Tooltip("If true, inverts the logic (TRUE shows falseVariant, FALSE shows trueVariant).")]
        private bool _invertLogic;

        /// <summary>
        /// The flag key this swapper responds to.
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
        /// The variant shown when the flag is true.
        /// </summary>
        public GameObject TrueVariant
        {
            get => _trueVariant;
            set => _trueVariant = value;
        }

        /// <summary>
        /// The variant shown when the flag is false.
        /// </summary>
        public GameObject FalseVariant
        {
            get => _falseVariant;
            set => _falseVariant = value;
        }

        protected override void OnEnable()
        {
            // Ensure watched keys match the flag key
            if (!string.IsNullOrEmpty(_flagKey))
            {
                _watchedFlagKeys = new[] { _flagKey };
            }

            base.OnEnable();
        }

        public override void OnStateQueried(GameStateSO state)
        {
            base.OnStateQueried(state);
            ApplyVariant();
        }

        protected override void OnFlagChanged(FlagChangePayload payload)
        {
            base.OnFlagChanged(payload);

            if (payload.FlagKey == _flagKey && payload.Type == FlagType.Bool)
            {
                ApplyVariant(payload.BoolValue);
            }
        }

        /// <summary>
        /// Applies the correct variant based on current flag state.
        /// </summary>
        private void ApplyVariant()
        {
            if (_gameState == null || string.IsNullOrEmpty(_flagKey))
                return;

            bool flagValue = _gameState.GetBool(_flagKey);
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
                Debug.Log($"[VariantSwapper] '{gameObject.name}' showing {activeVariant} (flag '{_flagKey}' = {flagValue})");
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Sync watched keys with flag key in editor
            if (!string.IsNullOrEmpty(_flagKey))
            {
                _watchedFlagKeys = new[] { _flagKey };
            }
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
