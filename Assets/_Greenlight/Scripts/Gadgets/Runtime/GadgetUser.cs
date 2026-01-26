using UnityEngine;

namespace Greenlight.Gadgets
{
    /// <summary>
    /// MonoBehaviour that equips and executes gadgets for the player.
    /// Instantiates behaviour prefabs and manages their lifecycle.
    /// 
    /// Design Philosophy:
    /// - Equip: Instantiate behaviour prefab from definition
    /// - Execute: Call behaviour's Execute() with context
    /// - Swap: Terminate old behaviour, equip new one
    /// </summary>
    [AddComponentMenu("Greenlight/Gadgets/Gadget User")]
    public class GadgetUser : MonoBehaviour
    {
        [Header("Inventory")]
        [SerializeField, Tooltip("Player's gadget inventory.")]
        private InventorySO _inventory;

        [Header("Anchor")]
        [SerializeField, Tooltip("Transform where equipped gadget behaviour is parented.")]
        private Transform _gadgetAnchor;

        [Header("Debug")]
        [SerializeField] private bool _debugLog;

        private GadgetBehaviour _equippedBehaviour;
        private GadgetDefinitionSO _currentDefinition;

        /// <summary>
        /// Is the currently equipped gadget executing and locking movement?
        /// </summary>
        public bool IsMovementLocked
        {
            get
            {
                bool result = _equippedBehaviour != null && _equippedBehaviour.IsExecuting && _equippedBehaviour.LocksMovement;
                if (_debugLog && result)
                {
                    Debug.Log($"[GadgetUser] Movement locked! Behaviour: {_equippedBehaviour?.name}, IsExecuting: {_equippedBehaviour?.IsExecuting}, LocksMovement: {_equippedBehaviour?.LocksMovement}");
                }
                return result;
            }
        }

        private void Awake()
        {
            // Create gadget anchor if not assigned
            if (_gadgetAnchor == null)
            {
                GameObject anchorObj = new GameObject("GadgetAnchor");
                _gadgetAnchor = anchorObj.transform;
                _gadgetAnchor.SetParent(transform);
                _gadgetAnchor.localPosition = Vector3.zero;
            }
        }

        private void Start()
        {
            // Equip the active gadget on start
            if (_inventory != null && _inventory.ActiveGadget != null)
            {
                EquipGadget(_inventory.ActiveGadget);
            }

            // Subscribe to inventory swap events
            if (_inventory != null && _inventory.OnGadgetSwapped != null)
            {
                _inventory.OnGadgetSwapped.RegisterListener(new GadgetSwapListener(this));
            }
        }

        private void OnDestroy()
        {
            // Clean up equipped behaviour
            if (_equippedBehaviour != null)
            {
                _equippedBehaviour.Terminate();
                Destroy(_equippedBehaviour.gameObject);
            }
        }

        /// <summary>
        /// Equips a gadget by instantiating its behaviour prefab.
        /// </summary>
        /// <param name="definition">Gadget definition to equip.</param>
        public void EquipGadget(GadgetDefinitionSO definition)
        {
            if (definition == null)
            {
                Debug.LogWarning("[GadgetUser] Cannot equip null gadget definition.");
                return;
            }

            if (definition.BehaviourPrefab == null)
            {
                Debug.LogError($"[GadgetUser] Gadget '{definition.GadgetName}' has no behaviour prefab!", this);
                return;
            }

            // Terminate and destroy old behaviour
            if (_equippedBehaviour != null)
            {
                _equippedBehaviour.Terminate();
                Destroy(_equippedBehaviour.gameObject);
                _equippedBehaviour = null;
            }

            // Instantiate new behaviour
            _equippedBehaviour = Instantiate(definition.BehaviourPrefab, _gadgetAnchor);
            _equippedBehaviour.transform.localPosition = Vector3.zero;
            _equippedBehaviour.Initialize(definition, this);
            _currentDefinition = definition;

            if (_debugLog)
            {
                Debug.Log($"[GadgetUser] Equipped gadget: {definition.GadgetName}");
            }
        }

        /// <summary>
        /// Uses the currently equipped gadget.
        /// </summary>
        /// <param name="facingDirection">Direction the user is facing.</param>
        public void UseEquippedGadget(Vector2 facingDirection)
        {
            if (_equippedBehaviour == null)
            {
                if (_debugLog)
                {
                    Debug.LogWarning("[GadgetUser] No gadget equipped.");
                }
                return;
            }

            // Create execution context
            GadgetExecutionContext context = GadgetExecutionContext.FromUser(transform, facingDirection);

            // Check if gadget can execute
            if (!_equippedBehaviour.CanExecute(context))
            {
                if (_debugLog)
                {
                    Debug.Log($"[GadgetUser] Gadget '{_currentDefinition.GadgetName}' cannot execute right now.");
                }
                return;
            }

            // Execute gadget
            _equippedBehaviour.Execute(context);

            if (_debugLog)
            {
                Debug.Log($"[GadgetUser] Executed gadget: {_currentDefinition.GadgetName}");
            }
        }

        /// <summary>
        /// Cycles to a different gadget in the inventory.
        /// </summary>
        /// <param name="delta">Direction to cycle (1 = next, -1 = previous).</param>
        public void CycleGadget(int delta)
        {
            if (_inventory == null) return;

            _inventory.Cycle(delta);

            // Equip the new active gadget
            if (_inventory.ActiveGadget != null)
            {
                EquipGadget(_inventory.ActiveGadget);
            }
        }

        /// <summary>
        /// Handles inventory swap events.
        /// </summary>
        private void OnInventorySwapped()
        {
            if (_inventory != null && _inventory.ActiveGadget != null)
            {
                EquipGadget(_inventory.ActiveGadget);
            }
        }

        /// <summary>
        /// Helper listener for inventory swap events.
        /// </summary>
        private class GadgetSwapListener : Core.Events.IGameEventListener
        {
            private readonly GadgetUser _user;

            public GadgetSwapListener(GadgetUser user)
            {
                _user = user;
            }

            public void OnEventRaised()
            {
                _user.OnInventorySwapped();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_gadgetAnchor == null)
            {
                _gadgetAnchor = transform;
            }
        }
#endif
    }
}
