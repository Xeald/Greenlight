using System.Collections.Generic;
using UnityEngine;
using Greenlight.Core.Events;

namespace Greenlight.Gadgets
{
    /// <summary>
    /// Tracks which gadgets the player has acquired and which is active.
    /// ScriptableObject persists across scenes but resets in editor to prevent state pollution.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerInventory", menuName = "Greenlight/Player/Inventory")]
    public class InventorySO : ScriptableObject
    {
        [Header("Gadgets")]
        [SerializeField, Tooltip("List of gadgets the player has acquired.")]
        private List<GadgetDefinitionSO> _acquiredGadgets = new();

        [SerializeField, Tooltip("Index of the currently active gadget.")]
        private int _activeIndex;

        [Header("Events")]
        [SerializeField, Tooltip("Event raised when player swaps to a different gadget.")]
        private GameEventSO _onGadgetSwapped;

        [SerializeField, Tooltip("Event raised when player acquires a new gadget.")]
        private GameEventSO _onGadgetAcquired;

        /// <summary>
        /// Read-only list of acquired gadgets.
        /// </summary>
        public IReadOnlyList<GadgetDefinitionSO> AcquiredGadgets => _acquiredGadgets;

        /// <summary>
        /// Index of the currently active gadget.
        /// </summary>
        public int ActiveIndex => _activeIndex;

        /// <summary>
        /// Event raised when gadget is swapped.
        /// </summary>
        public GameEventSO OnGadgetSwapped => _onGadgetSwapped;

        /// <summary>
        /// Event raised when new gadget is acquired.
        /// </summary>
        public GameEventSO OnGadgetAcquired => _onGadgetAcquired;

        /// <summary>
        /// Currently active gadget, or null if no gadgets acquired.
        /// </summary>
        public GadgetDefinitionSO ActiveGadget =>
            _acquiredGadgets.Count > 0 && _activeIndex >= 0 && _activeIndex < _acquiredGadgets.Count
                ? _acquiredGadgets[_activeIndex]
                : null;

        /// <summary>
        /// Cycles to the next gadget in the list.
        /// </summary>
        public void CycleNext()
        {
            if (_acquiredGadgets.Count <= 1) return;

            _activeIndex = (_activeIndex + 1) % _acquiredGadgets.Count;
            _onGadgetSwapped?.Raise();
        }

        /// <summary>
        /// Cycles to the previous gadget in the list.
        /// </summary>
        public void CyclePrevious()
        {
            if (_acquiredGadgets.Count <= 1) return;

            _activeIndex = (_activeIndex - 1 + _acquiredGadgets.Count) % _acquiredGadgets.Count;
            _onGadgetSwapped?.Raise();
        }

        /// <summary>
        /// Cycles gadget by a delta amount (positive = forward, negative = backward).
        /// </summary>
        /// <param name="delta">Direction to cycle (1 = next, -1 = previous).</param>
        public void Cycle(int delta)
        {
            if (delta > 0)
            {
                CycleNext();
            }
            else if (delta < 0)
            {
                CyclePrevious();
            }
        }

        /// <summary>
        /// Adds a gadget to the inventory if not already acquired.
        /// </summary>
        /// <param name="gadget">Gadget definition to acquire.</param>
        /// <returns>True if gadget was newly acquired, false if already owned.</returns>
        public bool AcquireGadget(GadgetDefinitionSO gadget)
        {
            if (gadget == null)
            {
                Debug.LogWarning("[InventorySO] Attempted to acquire null gadget.");
                return false;
            }

            if (_acquiredGadgets.Contains(gadget))
            {
                Debug.Log($"[InventorySO] Already have gadget: {gadget.GadgetName}");
                return false;
            }

            _acquiredGadgets.Add(gadget);
            _onGadgetAcquired?.Raise();

            Debug.Log($"[InventorySO] Acquired gadget: {gadget.GadgetName}");
            return true;
        }

        /// <summary>
        /// Checks if a specific gadget has been acquired.
        /// </summary>
        public bool HasGadget(GadgetDefinitionSO gadget)
        {
            return _acquiredGadgets.Contains(gadget);
        }

        /// <summary>
        /// Clears all acquired gadgets (for new game).
        /// </summary>
        public void ClearAll()
        {
            _acquiredGadgets.Clear();
            _activeIndex = 0;
        }

#if UNITY_EDITOR
        private void OnEnable()
        {
            // Prevent play mode state pollution
            if (!Application.isPlaying)
            {
                _activeIndex = 0;
                // Don't clear gadgets in editor - designers may want to test with pre-populated inventory
            }
        }

        private void OnValidate()
        {
            // Clamp active index to valid range
            if (_acquiredGadgets.Count > 0)
            {
                _activeIndex = Mathf.Clamp(_activeIndex, 0, _acquiredGadgets.Count - 1);
            }
            else
            {
                _activeIndex = 0;
            }
        }
#endif
    }
}
