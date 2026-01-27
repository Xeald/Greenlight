using UnityEngine;
using Greenlight.Core;

namespace Greenlight.Economy
{
    /// <summary>
    /// ScriptableObject defining purchasable items for the merchant system.
    /// Integrates with "A World That Remembers" pillar through unique item tracking.
    /// 
    /// Design Philosophy:
    /// - Meaningful purchases that change gameplay permanently
    /// - Unique items disappear after purchase (world remembers)
    /// - Clear value proposition for each item
    /// - Integration with GameState for persistence
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "Greenlight/Economy/Item Definition")]
    public class ItemDefinitionSO : ScriptableObject
    {
        [Header("Item Identity")]
        [SerializeField, Tooltip("Display name shown in shop.")]
        private string _itemName = "New Item";

        [SerializeField, TextArea(2, 4), Tooltip("Description shown to player in shop.")]
        private string _description = "A mysterious item with unknown properties.";

        [SerializeField, Tooltip("Icon sprite for UI display.")]
        private Sprite _icon;

        [Header("Economy")]
        [SerializeField, Range(1, 999), Tooltip("Cost in Luni currency.")]
        private int _cost = 10;

        [SerializeField, Tooltip("Is this a unique item that can only be purchased once?")]
        private bool _isUnique = true;

        [SerializeField, Tooltip("WorldFlagDefinitionSO reference for tracking if this unique item was purchased.")]
        private WorldFlagDefinitionSO _purchasedFlagRef;

        [Header("Item Effects")]
        [SerializeField, Tooltip("Type of effect this item provides.")]
        private ItemType _itemType = ItemType.Consumable;

        [SerializeField, Range(1, 10), Tooltip("Effect magnitude (e.g., hearts gained, damage bonus).")]
        private int _effectMagnitude = 1;

        [SerializeField, Tooltip("GameState flag key to modify when purchased (optional).")]
        private string _gameStateFlagKey = "";

        [Header("Visual Feedback")]
        [SerializeField, Tooltip("Particle effect when purchased (optional).")]
        private GameObject _purchaseVFXPrefab;

        [SerializeField, Tooltip("Sound effect when purchased.")]
        private AudioClip _purchaseSFX;

        /// <summary>
        /// Types of items available for purchase.
        /// </summary>
        public enum ItemType
        {
            HealthUpgrade,     // Increase max hearts
            Consumable,        // Single-use item
            Upgrade,           // Permanent stat boost
            KeyItem,           // Quest/progression item
            Gadget             // New tool/ability
        }

        /// <summary>
        /// Display name of the item.
        /// </summary>
        public string ItemName => _itemName;

        /// <summary>
        /// Item description for the player.
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// Icon sprite for UI display.
        /// </summary>
        public Sprite Icon => _icon;

        /// <summary>
        /// Cost in Luni currency.
        /// </summary>
        public int Cost => _cost;

        /// <summary>
        /// Is this a unique item?
        /// </summary>
        public bool IsUnique => _isUnique;

        /// <summary>
        /// Flag reference for tracking purchase state.
        /// </summary>
        public WorldFlagDefinitionSO PurchasedFlagRef => _purchasedFlagRef;

        /// <summary>
        /// Type of item effect.
        /// </summary>
        public ItemType Type => _itemType;

        /// <summary>
        /// Effect magnitude.
        /// </summary>
        public int EffectMagnitude => _effectMagnitude;

        /// <summary>
        /// GameState flag key to modify.
        /// </summary>
        public string GameStateFlagKey => _gameStateFlagKey;

        /// <summary>
        /// Purchase VFX prefab.
        /// </summary>
        public GameObject PurchaseVFXPrefab => _purchaseVFXPrefab;

        /// <summary>
        /// Purchase sound effect.
        /// </summary>
        public AudioClip PurchaseSFX => _purchaseSFX;

        /// <summary>
        /// Check if this item has already been purchased (for unique items).
        /// </summary>
        /// <param name="gameState">GameState to check against</param>
        /// <returns>True if already purchased</returns>
        public bool IsAlreadyPurchased(GameStateSO gameState)
        {
            if (!_isUnique || _purchasedFlagRef == null || gameState == null)
                return false;

            return gameState.GetBool(_purchasedFlagRef.FlagKey, false);
        }

        /// <summary>
        /// Apply the item's effect when purchased.
        /// </summary>
        /// <param name="gameState">GameState to modify</param>
        public void ApplyEffect(GameStateSO gameState)
        {
            if (gameState == null)
                return;

            switch (_itemType)
            {
                case ItemType.HealthUpgrade:
                    ApplyHealthUpgrade(gameState);
                    break;

                case ItemType.Consumable:
                    ApplyConsumableEffect(gameState);
                    break;

                case ItemType.Upgrade:
                    ApplyUpgradeEffect(gameState);
                    break;

                case ItemType.KeyItem:
                    ApplyKeyItemEffect(gameState);
                    break;

                case ItemType.Gadget:
                    ApplyGadgetEffect(gameState);
                    break;
            }

            // Mark as purchased if unique
            if (_isUnique && _purchasedFlagRef != null)
            {
                gameState.SetBool(_purchasedFlagRef.FlagKey, true);
            }

            // Set custom game state flag if specified
            if (!string.IsNullOrEmpty(_gameStateFlagKey))
            {
                gameState.SetBool(_gameStateFlagKey, true);
            }
        }

        /// <summary>
        /// Apply health upgrade effect.
        /// </summary>
        /// <param name="gameState">GameState to modify</param>
        private void ApplyHealthUpgrade(GameStateSO gameState)
        {
            // Increase max hearts
            int currentMaxHearts = gameState.GetInt("Player_MaxHealth", 3);
            int newMaxHearts = currentMaxHearts + _effectMagnitude;
            gameState.SetInt("Player_MaxHealth", newMaxHearts);

            // Also heal to full
            gameState.SetInt("Player_Health", newMaxHearts);

            Debug.Log($"[ItemDefinition] {_itemName}: Increased max hearts to {newMaxHearts}");
        }

        /// <summary>
        /// Apply consumable item effect.
        /// </summary>
        /// <param name="gameState">GameState to modify</param>
        private void ApplyConsumableEffect(GameStateSO gameState)
        {
            // For consumables, typically heal or provide temporary buffs
            int currentHealth = gameState.GetInt("Player_Health", 3);
            int maxHealth = gameState.GetInt("Player_MaxHealth", 3);
            int newHealth = Mathf.Min(maxHealth, currentHealth + _effectMagnitude);
            gameState.SetInt("Player_Health", newHealth);

            Debug.Log($"[ItemDefinition] {_itemName}: Healed {_effectMagnitude} hearts");
        }

        /// <summary>
        /// Apply permanent upgrade effect.
        /// </summary>
        /// <param name="gameState">GameState to modify</param>
        private void ApplyUpgradeEffect(GameStateSO gameState)
        {
            // Apply upgrade based on custom flag key
            if (!string.IsNullOrEmpty(_gameStateFlagKey))
            {
                gameState.SetInt(_gameStateFlagKey, _effectMagnitude);
            }

            Debug.Log($"[ItemDefinition] {_itemName}: Applied upgrade effect");
        }

        /// <summary>
        /// Apply key item effect.
        /// </summary>
        /// <param name="gameState">GameState to modify</param>
        private void ApplyKeyItemEffect(GameStateSO gameState)
        {
            // Key items typically just set flags for quest progression
            Debug.Log($"[ItemDefinition] {_itemName}: Obtained key item");
        }

        /// <summary>
        /// Apply gadget effect.
        /// </summary>
        /// <param name="gameState">GameState to modify</param>
        private void ApplyGadgetEffect(GameStateSO gameState)
        {
            // Gadgets require more complex integration with the gadget system
            // For now, just set a flag that the gadget system can check
            Debug.Log($"[ItemDefinition] {_itemName}: Obtained new gadget");
        }

        /// <summary>
        /// Get a formatted cost string for UI display.
        /// </summary>
        /// <returns>Cost string (e.g., "25 Luni")</returns>
        public string GetCostString()
        {
            return $"{_cost} Luni";
        }

        /// <summary>
        /// Get item rarity color based on cost.
        /// </summary>
        /// <returns>Color representing rarity</returns>
        public Color GetRarityColor()
        {
            return _cost switch
            {
                <= 10 => Color.white,      // Common
                <= 25 => Color.green,      // Uncommon
                <= 50 => Color.blue,       // Rare
                <= 100 => Color.magenta,   // Epic
                _ => Color.yellow          // Legendary
            };
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure values are reasonable
            _cost = Mathf.Max(1, _cost);
            _effectMagnitude = Mathf.Max(1, _effectMagnitude);

            // Validate unique item configuration
            if (_isUnique && _purchasedFlagRef == null)
            {
                Debug.LogWarning($"[ItemDefinition] {name}: Unique item should have a PurchasedFlagRef assigned!");
            }

            // Ensure icon is assigned
            if (_icon == null)
            {
                Debug.LogWarning($"[ItemDefinition] {name}: No icon sprite assigned!");
            }
        }
#endif
    }
}