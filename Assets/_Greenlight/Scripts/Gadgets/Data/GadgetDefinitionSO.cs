using UnityEngine;
using Greenlight.Core;

namespace Greenlight.Gadgets
{
    /// <summary>
    /// ScriptableObject definition for a gadget - the "Brain" that holds data.
    /// Designers create these assets to define new gadgets without code.
    /// 
    /// Design Philosophy (Data + Behavior Pairs):
    /// - Brain (this): ScriptableObject with metadata (name, icon, cost)
    /// - Body: MonoBehaviour prefab with GadgetBehaviour component
    /// - Separation enables data-driven gadget creation
    /// </summary>
    [CreateAssetMenu(fileName = "NewGadget", menuName = "Greenlight/Gadgets/Gadget Definition")]
    public class GadgetDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Tooltip("Display name of the gadget.")]
        private string _gadgetName;

        [SerializeField, Tooltip("Icon shown in UI/inventory.")]
        private Sprite _icon;

        [SerializeField, TextArea(2, 4), Tooltip("Description shown to player.")]
        private string _description;

        [Header("Cost")]
        [SerializeField, Tooltip("Mana cost per use (0 = free).")]
        private int _manaCost;

        [Header("Behaviour")]
        [SerializeField, Tooltip("Prefab containing the GadgetBehaviour component.")]
        private GadgetBehaviour _behaviourPrefab;

        [Header("State Integration")]
        [SerializeField, Tooltip("Flag set when player acquires this gadget.")]
        private WorldFlagDefinitionSO _acquisitionFlag;

        /// <summary>
        /// Display name of the gadget.
        /// </summary>
        public string GadgetName => _gadgetName;

        /// <summary>
        /// Icon for UI display.
        /// </summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// Description text.
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// Mana cost per use.
        /// </summary>
        public int ManaCost => _manaCost;

        /// <summary>
        /// Prefab containing the GadgetBehaviour component.
        /// </summary>
        public GadgetBehaviour BehaviourPrefab => _behaviourPrefab;

        /// <summary>
        /// Flag definition for acquisition tracking.
        /// </summary>
        public WorldFlagDefinitionSO AcquisitionFlag => _acquisitionFlag;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-generate name from asset name if empty
            if (string.IsNullOrEmpty(_gadgetName))
            {
                _gadgetName = name.Replace("_", " ");
            }

            // Ensure mana cost is non-negative
            _manaCost = Mathf.Max(0, _manaCost);

            // Validate behaviour prefab has GadgetBehaviour component
            if (_behaviourPrefab != null)
            {
                var behaviour = _behaviourPrefab.GetComponent<GadgetBehaviour>();
                if (behaviour == null)
                {
                    Debug.LogError($"[GadgetDefinitionSO] Behaviour prefab '{_behaviourPrefab.name}' " +
                                 $"must have a GadgetBehaviour component!", this);
                }
            }
        }
#endif
    }
}
