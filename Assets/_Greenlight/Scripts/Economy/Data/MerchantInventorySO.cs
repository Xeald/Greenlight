using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Greenlight.Economy
{
    /// <summary>
    /// ScriptableObject defining a merchant's inventory and behavior.
    /// Integrates with "A World That Remembers" through unique item tracking.
    /// 
    /// Design Philosophy:
    /// - Each merchant has unique identity and inventory
    /// - World remembers what has been purchased
    /// - Dynamic inventory based on player progress/flags
    /// - Clear value progression for meaningful choices
    /// </summary>
    [CreateAssetMenu(fileName = "MerchantInventory", menuName = "Greenlight/Economy/Merchant Inventory")]
    public class MerchantInventorySO : ScriptableObject
    {
        [Header("Merchant Identity")]
        [SerializeField, Tooltip("Merchant's display name.")]
        private string _merchantName = "Mysterious Merchant";

        [SerializeField, TextArea(2, 4), Tooltip("Merchant's greeting/description.")]
        private string _merchantDescription = "A traveler with rare wares to sell.";

        [SerializeField, Tooltip("Merchant portrait sprite for UI.")]
        private Sprite _merchantPortrait;

        [Header("Inventory")]
        [SerializeField, Tooltip("Items available for purchase.")]
        private ItemDefinitionSO[] _availableItems = new ItemDefinitionSO[0];

        [SerializeField, Tooltip("Should items be sorted by cost automatically?")]
        private bool _autoSortByCost = true;

        [Header("Merchant Behavior")]
        [SerializeField, Tooltip("Merchant's dialogue when player has insufficient Luni.")]
        private string _insufficientFundsMessage = "You don't have enough Luni for that, friend.";

        [SerializeField, Tooltip("Merchant's dialogue after successful purchase.")]
        private string _purchaseSuccessMessage = "Pleasure doing business!";

        [SerializeField, Tooltip("Merchant's dialogue when item is already purchased.")]
        private string _alreadyPurchasedMessage = "You've already got that one!";

        [Header("Shop Configuration")]
        [SerializeField, Range(1, 12), Tooltip("Maximum number of items to display at once.")]
        private int _maxDisplayedItems = 8;

        [SerializeField, Tooltip("Show sold out slots for unique items?")]
        private bool _showSoldOutSlots = false;

        /// <summary>
        /// Merchant's display name.
        /// </summary>
        public string MerchantName => _merchantName;

        /// <summary>
        /// Merchant's description/greeting.
        /// </summary>
        public string MerchantDescription => _merchantDescription;

        /// <summary>
        /// Merchant portrait sprite.
        /// </summary>
        public Sprite MerchantPortrait => _merchantPortrait;

        /// <summary>
        /// All available items (before filtering).
        /// </summary>
        public ItemDefinitionSO[] AllAvailableItems => _availableItems;

        /// <summary>
        /// Maximum items to display at once.
        /// </summary>
        public int MaxDisplayedItems => _maxDisplayedItems;

        /// <summary>
        /// Show sold out slots?
        /// </summary>
        public bool ShowSoldOutSlots => _showSoldOutSlots;

        /// <summary>
        /// Insufficient funds message.
        /// </summary>
        public string InsufficientFundsMessage => _insufficientFundsMessage;

        /// <summary>
        /// Purchase success message.
        /// </summary>
        public string PurchaseSuccessMessage => _purchaseSuccessMessage;

        /// <summary>
        /// Already purchased message.
        /// </summary>
        public string AlreadyPurchasedMessage => _alreadyPurchasedMessage;

        /// <summary>
        /// Get items currently available for purchase (filters out already purchased unique items).
        /// </summary>
        /// <param name="gameState">GameState to check purchase history against</param>
        /// <returns>Array of available items</returns>
        public ItemDefinitionSO[] GetAvailableItems(GameStateSO gameState)
        {
            if (_availableItems == null || _availableItems.Length == 0)
                return new ItemDefinitionSO[0];

            List<ItemDefinitionSO> availableItems = new List<ItemDefinitionSO>();

            foreach (var item in _availableItems)
            {
                if (item == null)
                    continue;

                // Check if unique item is already purchased
                if (item.IsUnique && item.IsAlreadyPurchased(gameState))
                {
                    // Skip this item unless we're showing sold out slots
                    if (!_showSoldOutSlots)
                        continue;
                }

                availableItems.Add(item);
            }

            // Sort by cost if enabled
            if (_autoSortByCost)
            {
                availableItems.Sort((a, b) => a.Cost.CompareTo(b.Cost));
            }

            // Limit to max displayed items
            if (availableItems.Count > _maxDisplayedItems)
            {
                availableItems = availableItems.Take(_maxDisplayedItems).ToList();
            }

            return availableItems.ToArray();
        }

        /// <summary>
        /// Get items that have been sold out (purchased unique items).
        /// </summary>
        /// <param name="gameState">GameState to check purchase history</param>
        /// <returns>Array of sold out items</returns>
        public ItemDefinitionSO[] GetSoldOutItems(GameStateSO gameState)
        {
            if (_availableItems == null || gameState == null)
                return new ItemDefinitionSO[0];

            return _availableItems
                .Where(item => item != null && item.IsUnique && item.IsAlreadyPurchased(gameState))
                .ToArray();
        }

        /// <summary>
        /// Check if player can afford a specific item.
        /// </summary>
        /// <param name="item">Item to check</param>
        /// <param name="playerLuni">Player's current Luni amount</param>
        /// <returns>True if player can afford the item</returns>
        public bool CanAfford(ItemDefinitionSO item, int playerLuni)
        {
            return item != null && playerLuni >= item.Cost;
        }

        /// <summary>
        /// Get the total value of all items in this merchant's inventory.
        /// </summary>
        /// <returns>Total Luni value of all items</returns>
        public int GetTotalInventoryValue()
        {
            if (_availableItems == null)
                return 0;

            return _availableItems
                .Where(item => item != null)
                .Sum(item => item.Cost);
        }

        /// <summary>
        /// Get items within a specific cost range.
        /// </summary>
        /// <param name="minCost">Minimum cost (inclusive)</param>
        /// <param name="maxCost">Maximum cost (inclusive)</param>
        /// <param name="gameState">GameState for filtering purchased items</param>
        /// <returns>Items within the cost range</returns>
        public ItemDefinitionSO[] GetItemsInCostRange(int minCost, int maxCost, GameStateSO gameState)
        {
            return GetAvailableItems(gameState)
                .Where(item => item.Cost >= minCost && item.Cost <= maxCost)
                .ToArray();
        }

        /// <summary>
        /// Get items of a specific type.
        /// </summary>
        /// <param name="itemType">Type of items to get</param>
        /// <param name="gameState">GameState for filtering purchased items</param>
        /// <returns>Items of the specified type</returns>
        public ItemDefinitionSO[] GetItemsOfType(ItemDefinitionSO.ItemType itemType, GameStateSO gameState)
        {
            return GetAvailableItems(gameState)
                .Where(item => item.Type == itemType)
                .ToArray();
        }

        /// <summary>
        /// Get a random selection of items from the inventory.
        /// </summary>
        /// <param name="count">Number of items to select</param>
        /// <param name="gameState">GameState for filtering</param>
        /// <returns>Random selection of available items</returns>
        public ItemDefinitionSO[] GetRandomItems(int count, GameStateSO gameState)
        {
            var availableItems = GetAvailableItems(gameState);
            
            if (availableItems.Length <= count)
                return availableItems;

            // Shuffle and take requested count
            var shuffled = availableItems.OrderBy(x => Random.value).Take(count);
            return shuffled.ToArray();
        }

        /// <summary>
        /// Validate that all items in inventory are properly configured.
        /// </summary>
        /// <returns>List of validation errors</returns>
        public List<string> ValidateInventory()
        {
            List<string> errors = new List<string>();

            if (_availableItems == null || _availableItems.Length == 0)
            {
                errors.Add("No items in inventory");
                return errors;
            }

            for (int i = 0; i < _availableItems.Length; i++)
            {
                var item = _availableItems[i];
                
                if (item == null)
                {
                    errors.Add($"Item at index {i} is null");
                    continue;
                }

                // Check for duplicate items
                for (int j = i + 1; j < _availableItems.Length; j++)
                {
                    if (_availableItems[j] == item)
                    {
                        errors.Add($"Duplicate item '{item.ItemName}' found at indices {i} and {j}");
                    }
                }

                // Check item configuration
                if (item.Cost <= 0)
                {
                    errors.Add($"Item '{item.ItemName}' has invalid cost: {item.Cost}");
                }

                if (item.IsUnique && item.PurchasedFlagRef == null)
                {
                    errors.Add($"Unique item '{item.ItemName}' missing PurchasedFlagRef");
                }
            }

            return errors;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _maxDisplayedItems = Mathf.Max(1, _maxDisplayedItems);

            // Validate inventory and show warnings
            var errors = ValidateInventory();
            foreach (var error in errors)
            {
                Debug.LogWarning($"[MerchantInventory] {name}: {error}");
            }
        }

        [UnityEditor.MenuItem("Tools/Greenlight/Economy/Validate All Merchant Inventories")]
        private static void ValidateAllMerchantInventories()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:MerchantInventorySO");
            
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var inventory = UnityEditor.AssetDatabase.LoadAssetAtPath<MerchantInventorySO>(path);
                
                var errors = inventory.ValidateInventory();
                if (errors.Count > 0)
                {
                    Debug.LogWarning($"[MerchantInventory] {inventory.name} has {errors.Count} validation errors:");
                    foreach (var error in errors)
                    {
                        Debug.LogWarning($"  - {error}");
                    }
                }
            }
        }
#endif
    }
}