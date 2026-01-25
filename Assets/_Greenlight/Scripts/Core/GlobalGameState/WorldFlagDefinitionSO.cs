using UnityEngine;
using Greenlight.Core.Events;

namespace Greenlight.Core
{
    /// <summary>
    /// Designer-friendly metadata for a world flag.
    /// Allows designers to define flags with documentation, categories, and defaults
    /// without touching code. Used by editor tooling for flag discovery and validation.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFlag", menuName = "Greenlight/Flags/Flag Definition")]
    public class WorldFlagDefinitionSO : ScriptableObject
    {
        [Header("Flag Identity")]
        [SerializeField, Tooltip("Unique key for this flag (e.g., 'Village_House1_Burnt'). Use PascalCase_Underscore convention.")]
        private string _flagKey;

        [SerializeField, Tooltip("Category for organization (e.g., 'World', 'Story', 'Gadgets').")]
        private FlagCategory _category = FlagCategory.World;

        [SerializeField, Tooltip("Type of value this flag stores.")]
        private FlagType _flagType = FlagType.Bool;

        [Header("Default Values")]
        [SerializeField, Tooltip("Default boolean value (if FlagType is Bool).")]
        private bool _defaultBool;

        [SerializeField, Tooltip("Default integer value (if FlagType is Int).")]
        private int _defaultInt;

        [SerializeField, Tooltip("Default string value (if FlagType is String).")]
        private string _defaultString = "";

        [Header("Documentation")]
        [SerializeField, TextArea(2, 5), Tooltip("Describe what this flag represents and what triggers it.")]
        private string _description;

        [SerializeField, TextArea(2, 5), Tooltip("Notes about gameplay impact when this flag changes.")]
        private string _gameplayNotes;

        /// <summary>
        /// The unique key used to store/retrieve this flag in GameStateSO.
        /// </summary>
        public string FlagKey => string.IsNullOrEmpty(_flagKey) ? name : _flagKey;

        /// <summary>
        /// Organizational category for this flag.
        /// </summary>
        public FlagCategory Category => _category;

        /// <summary>
        /// The data type of this flag's value.
        /// </summary>
        public FlagType FlagType => _flagType;

        /// <summary>
        /// The default value for boolean flags.
        /// </summary>
        public bool DefaultBool => _defaultBool;

        /// <summary>
        /// The default value for integer flags.
        /// </summary>
        public int DefaultInt => _defaultInt;

        /// <summary>
        /// The default value for string flags.
        /// </summary>
        public string DefaultString => _defaultString;

        /// <summary>
        /// Human-readable description of this flag.
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// Applies this flag's default value to the given GameStateSO.
        /// Useful for initializing a new game or resetting to defaults.
        /// </summary>
        /// <param name="state">The game state to apply the default to.</param>
        /// <param name="silent">If true, suppresses the change event.</param>
        public void ApplyDefault(GameStateSO state, bool silent = true)
        {
            switch (_flagType)
            {
                case FlagType.Bool:
                    state.SetBool(FlagKey, _defaultBool, silent);
                    break;
                case FlagType.Int:
                    state.SetInt(FlagKey, _defaultInt, silent);
                    break;
                case FlagType.String:
                    state.SetString(FlagKey, _defaultString, silent);
                    break;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-generate flag key from asset name if not set
            if (string.IsNullOrEmpty(_flagKey))
            {
                _flagKey = name.Replace(" ", "_");
            }
        }
#endif
    }

    /// <summary>
    /// Categories for organizing flags in the editor.
    /// </summary>
    public enum FlagCategory
    {
        World,      // Environmental state (burnt houses, cleansed shrines)
        Story,      // Narrative progression (quest stages, NPC states)
        Gadgets,    // Tool acquisition (Gadget_Grapple_Acquired)
        Player,     // Player state (current health, checkpoints)
        System      // Meta/system flags (tutorial completed, settings)
    }
}
