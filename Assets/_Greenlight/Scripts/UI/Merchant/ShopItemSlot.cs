using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Greenlight.Economy;

namespace Greenlight.UI
{
    /// <summary>
    /// MVC View component for individual shop item slots.
    /// Handles ONLY display and event firing - NO business logic.
    /// All logic is handled by MerchantPanel (Controller).
    /// 
    /// Architecture:
    /// - View responsibility: Display data, fire events
    /// - Controller responsibility: All logic, data processing
    /// - Model responsibility: Data storage only
    /// </summary>
    [AddComponentMenu("Greenlight/UI/Shop Item Slot")]
    public class ShopItemSlot : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField, Tooltip("Item icon image.")]
        private Image _itemIcon;

        [SerializeField, Tooltip("Item name text.")]
        private TextMeshProUGUI _itemNameText;

        [SerializeField, Tooltip("Item description text.")]
        private TextMeshProUGUI _itemDescriptionText;

        [SerializeField, Tooltip("Item cost text.")]
        private TextMeshProUGUI _itemCostText;

        [SerializeField, Tooltip("Main button for clicking this slot.")]
        private Button _itemButton;

        [Header("Visual States")]
        [SerializeField, Tooltip("Color when item is affordable.")]
        private Color _affordableColor = Color.white;

        [SerializeField, Tooltip("Color when item is not affordable.")]
        private Color _notAffordableColor = Color.gray;

        [SerializeField, Tooltip("Color when item is already purchased.")]
        private Color _purchasedColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        [SerializeField, Tooltip("Text to show when item is purchased.")]
        private string _purchasedText = "SOLD OUT";

        [Header("Rarity Display")]
        [SerializeField, Tooltip("Background image for rarity color.")]
        private Image _rarityBackground;

        [SerializeField, Tooltip("Border image for rarity emphasis.")]
        private Image _rarityBorder;

        // State
        private ItemDefinitionSO _item;
        private MerchantPanel _merchantPanel;
        private bool _isAffordable = true;
        private bool _isPurchased = false;

        /// <summary>
        /// Item this slot represents.
        /// </summary>
        public ItemDefinitionSO Item => _item;

        /// <summary>
        /// Is this item currently affordable?
        /// </summary>
        public bool IsAffordable => _isAffordable;

        /// <summary>
        /// Has this item been purchased?
        /// </summary>
        public bool IsPurchased => _isPurchased;

        private void Awake()
        {
            // Auto-assign button if not set
            if (_itemButton == null)
                _itemButton = GetComponent<Button>();

            // Set up button click handler
            if (_itemButton != null)
            {
                _itemButton.onClick.AddListener(OnButtonClicked);
            }
        }

        /// <summary>
        /// Initialize this slot with item data and controller reference.
        /// </summary>
        /// <param name="item">Item to display</param>
        /// <param name="merchantPanel">Controller to notify on clicks</param>
        public void Initialize(ItemDefinitionSO item, MerchantPanel merchantPanel)
        {
            _item = item;
            _merchantPanel = merchantPanel;

            if (_item == null)
            {
                Debug.LogError("[ShopItemSlot] Cannot initialize with null item!");
                return;
            }

            // Update display
            UpdateDisplay();
        }

        /// <summary>
        /// Update all visual elements to reflect current item data.
        /// Pure display logic - no business rules.
        /// </summary>
        private void UpdateDisplay()
        {
            if (_item == null)
                return;

            // Update icon
            if (_itemIcon != null)
            {
                if (_item.Icon != null)
                {
                    _itemIcon.sprite = _item.Icon;
                    _itemIcon.gameObject.SetActive(true);
                }
                else
                {
                    _itemIcon.gameObject.SetActive(false);
                }
            }

            // Update name
            if (_itemNameText != null)
            {
                _itemNameText.text = _item.ItemName;
            }

            // Update description
            if (_itemDescriptionText != null)
            {
                _itemDescriptionText.text = _item.Description;
            }

            // Update cost
            if (_itemCostText != null)
            {
                _itemCostText.text = _item.GetCostString();
            }

            // Update rarity display
            UpdateRarityDisplay();

            // Update visual state
            UpdateVisualState();
        }

        /// <summary>
        /// Update rarity background/border colors.
        /// </summary>
        private void UpdateRarityDisplay()
        {
            if (_item == null)
                return;

            Color rarityColor = _item.GetRarityColor();

            // Update rarity background
            if (_rarityBackground != null)
            {
                Color bgColor = rarityColor;
                bgColor.a = 0.2f; // Semi-transparent background
                _rarityBackground.color = bgColor;
            }

            // Update rarity border
            if (_rarityBorder != null)
            {
                _rarityBorder.color = rarityColor;
            }
        }

        /// <summary>
        /// Update visual state based on affordability and purchase status.
        /// </summary>
        private void UpdateVisualState()
        {
            Color targetColor;
            bool buttonInteractable;

            if (_isPurchased)
            {
                targetColor = _purchasedColor;
                buttonInteractable = false;

                // Update cost text to show "SOLD OUT"
                if (_itemCostText != null)
                {
                    _itemCostText.text = _purchasedText;
                }
            }
            else if (_isAffordable)
            {
                targetColor = _affordableColor;
                buttonInteractable = true;
            }
            else
            {
                targetColor = _notAffordableColor;
                buttonInteractable = false;
            }

            // Apply color to main elements
            if (_itemIcon != null)
                _itemIcon.color = targetColor;

            if (_itemNameText != null)
                _itemNameText.color = targetColor;

            if (_itemDescriptionText != null)
                _itemDescriptionText.color = targetColor;

            // Update button interactability
            if (_itemButton != null)
            {
                _itemButton.interactable = buttonInteractable;
            }
        }

        /// <summary>
        /// Set whether this item is affordable (called by Controller).
        /// </summary>
        /// <param name="affordable">True if player can afford this item</param>
        public void SetAffordableState(bool affordable)
        {
            if (_isAffordable != affordable)
            {
                _isAffordable = affordable;
                UpdateVisualState();
            }
        }

        /// <summary>
        /// Set whether this item has been purchased (called by Controller).
        /// </summary>
        /// <param name="purchased">True if item has been purchased</param>
        public void SetPurchasedState(bool purchased)
        {
            if (_isPurchased != purchased)
            {
                _isPurchased = purchased;
                UpdateDisplay(); // Full refresh for purchased state
            }
        }

        /// <summary>
        /// Called when the item button is clicked.
        /// Fires event to Controller - NO business logic here.
        /// </summary>
        private void OnButtonClicked()
        {
            if (_merchantPanel != null && _item != null)
            {
                // Delegate ALL logic to the Controller
                _merchantPanel.OnItemClicked(_item);
            }
        }

        /// <summary>
        /// Refresh this slot's display (for external updates).
        /// </summary>
        public void RefreshDisplay()
        {
            UpdateDisplay();
        }

        /// <summary>
        /// Set highlight state for this slot (for hover effects, etc.).
        /// </summary>
        /// <param name="highlighted">True to highlight this slot</param>
        public void SetHighlighted(bool highlighted)
        {
            // Simple scale effect for highlight
            float targetScale = highlighted ? 1.05f : 1f;
            transform.localScale = Vector3.one * targetScale;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-assign button if not set
            if (_itemButton == null)
                _itemButton = GetComponent<Button>();

            // Validate that required UI components are assigned
            if (_itemNameText == null)
                Debug.LogWarning($"[ShopItemSlot] {name}: Item name text not assigned!");

            if (_itemIcon == null)
                Debug.LogWarning($"[ShopItemSlot] {name}: Item icon image not assigned!");

            if (_itemButton == null)
                Debug.LogWarning($"[ShopItemSlot] {name}: Item button not assigned!");
        }

        /// <summary>
        /// Test this slot with sample data (editor only).
        /// </summary>
        [ContextMenu("Test With Sample Data")]
        private void TestWithSampleData()
        {
            // Create temporary test item for preview
            var testItem = ScriptableObject.CreateInstance<ItemDefinitionSO>();
            // Note: This would need reflection or a public constructor to work properly
            // It's mainly for demonstrating the testing concept
            
            Initialize(testItem, null);
            SetAffordableState(true);
            SetPurchasedState(false);
        }
#endif
    }
}