using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Greenlight.Core;
using Greenlight.Core.Events;
using Greenlight.Economy;
using System.Collections.Generic;

namespace Greenlight.UI
{
    /// <summary>
    /// MVC Controller for the merchant shop UI.
    /// Handles ALL logic: affordability, transactions, flag updates, UI refresh.
    /// Views only display data and fire events - no business logic in views.
    /// 
    /// Architecture:
    /// - Controller: This class (all logic)
    /// - Model: MerchantInventorySO + GameStateSO (data only)
    /// - View: ShopItemSlot, PlayerWalletDisplay (display + events only)
    /// </summary>
    [AddComponentMenu("Greenlight/UI/Merchant Panel")]
    public class MerchantPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField, Tooltip("Container for shop item slots.")]
        private Transform _itemSlotContainer;

        [SerializeField, Tooltip("Prefab for individual shop item slots.")]
        private GameObject _shopItemSlotPrefab;

        [SerializeField, Tooltip("Player wallet display component.")]
        private PlayerWalletDisplay _walletDisplay;

        [Header("Merchant Info Display")]
        [SerializeField, Tooltip("Merchant name text.")]
        private TextMeshProUGUI _merchantNameText;

        [SerializeField, Tooltip("Merchant description text.")]
        private TextMeshProUGUI _merchantDescriptionText;

        [SerializeField, Tooltip("Merchant portrait image.")]
        private Image _merchantPortraitImage;

        [Header("Feedback Messages")]
        [SerializeField, Tooltip("Text component for displaying transaction messages.")]
        private TextMeshProUGUI _feedbackMessageText;

        [SerializeField, Range(1f, 5f), Tooltip("Duration to show feedback messages (seconds).")]
        private float _feedbackMessageDuration = 2f;

        [Header("Events")]
        [SerializeField, Tooltip("Event fired when purchase succeeds.")]
        private GameEventSO _onPurchaseSuccess;

        [SerializeField, Tooltip("Event fired when purchase fails.")]
        private GameEventSO _onPurchaseFailed;

        [SerializeField, Tooltip("Event fired when Luni amount changes.")]
        private GameEventSO _onLuniChanged;

        [Header("Audio")]
        [SerializeField, Tooltip("Sound when purchase succeeds.")]
        private AudioClip _purchaseSuccessSFX;

        [SerializeField, Tooltip("Sound when purchase fails.")]
        private AudioClip _purchaseFailSFX;

        [SerializeField, Tooltip("Audio source for merchant sounds.")]
        private AudioSource _audioSource;

        // State
        private MerchantInventorySO _currentMerchant;
        private GameStateSO _gameState;
        private List<ShopItemSlot> _activeItemSlots = new List<ShopItemSlot>();
        private string _luniCurrencyKey = "Player_Luni";

        /// <summary>
        /// Current merchant being displayed.
        /// </summary>
        public MerchantInventorySO CurrentMerchant => _currentMerchant;

        /// <summary>
        /// Current player Luni amount.
        /// </summary>
        public int CurrentLuni => _gameState?.GetInt(_luniCurrencyKey, 0) ?? 0;

        private void Awake()
        {
            // Auto-assign audio source
            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();

            // Hide feedback message initially
            if (_feedbackMessageText != null)
                _feedbackMessageText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Initialize the merchant panel with inventory and game state.
        /// </summary>
        /// <param name="merchant">Merchant inventory to display</param>
        /// <param name="gameState">Game state for currency and flags</param>
        public void InitializeMerchant(MerchantInventorySO merchant, GameStateSO gameState)
        {
            _currentMerchant = merchant;
            _gameState = gameState;

            if (_currentMerchant == null || _gameState == null)
            {
                Debug.LogError("[MerchantPanel] Cannot initialize: missing merchant or game state!");
                return;
            }

            // Update merchant info display
            UpdateMerchantInfo();

            // Generate shop items
            RefreshShopDisplay();

            // Update wallet display
            RefreshWalletDisplay();
        }

        /// <summary>
        /// Update merchant info display (name, description, portrait).
        /// </summary>
        private void UpdateMerchantInfo()
        {
            if (_currentMerchant == null)
                return;

            // Update text displays
            if (_merchantNameText != null)
                _merchantNameText.text = _currentMerchant.MerchantName;

            if (_merchantDescriptionText != null)
                _merchantDescriptionText.text = _currentMerchant.MerchantDescription;

            // Update portrait
            if (_merchantPortraitImage != null && _currentMerchant.MerchantPortrait != null)
            {
                _merchantPortraitImage.sprite = _currentMerchant.MerchantPortrait;
                _merchantPortraitImage.gameObject.SetActive(true);
            }
            else if (_merchantPortraitImage != null)
            {
                _merchantPortraitImage.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Refresh the shop display with current available items.
        /// </summary>
        public void RefreshShopDisplay()
        {
            if (_currentMerchant == null || _gameState == null || _itemSlotContainer == null)
                return;

            // Clear existing slots
            ClearItemSlots();

            // Get available items
            var availableItems = _currentMerchant.GetAvailableItems(_gameState);

            // Create slots for each item
            foreach (var item in availableItems)
            {
                CreateItemSlot(item);
            }

            // Update affordability for all slots
            UpdateItemAffordability();
        }

        /// <summary>
        /// Clear all existing item slots.
        /// </summary>
        private void ClearItemSlots()
        {
            foreach (var slot in _activeItemSlots)
            {
                if (slot != null)
                    DestroyImmediate(slot.gameObject);
            }
            _activeItemSlots.Clear();
        }

        /// <summary>
        /// Create a shop item slot for the given item.
        /// </summary>
        /// <param name="item">Item to create slot for</param>
        private void CreateItemSlot(ItemDefinitionSO item)
        {
            if (_shopItemSlotPrefab == null || item == null)
                return;

            // Instantiate slot
            GameObject slotObject = Instantiate(_shopItemSlotPrefab, _itemSlotContainer);
            ShopItemSlot slot = slotObject.GetComponent<ShopItemSlot>();

            if (slot == null)
            {
                Debug.LogError("[MerchantPanel] ShopItemSlot component not found on prefab!");
                DestroyImmediate(slotObject);
                return;
            }

            // Initialize slot
            slot.Initialize(item, this);
            _activeItemSlots.Add(slot);

            // Check if already purchased (for unique items)
            bool isPurchased = item.IsUnique && item.IsAlreadyPurchased(_gameState);
            slot.SetPurchasedState(isPurchased);
        }

        /// <summary>
        /// Update affordability display for all item slots.
        /// </summary>
        private void UpdateItemAffordability()
        {
            int playerLuni = CurrentLuni;

            foreach (var slot in _activeItemSlots)
            {
                if (slot != null && slot.Item != null)
                {
                    bool canAfford = playerLuni >= slot.Item.Cost;
                    bool isPurchased = slot.Item.IsUnique && slot.Item.IsAlreadyPurchased(_gameState);
                    
                    slot.SetAffordableState(canAfford && !isPurchased);
                }
            }
        }

        /// <summary>
        /// Refresh wallet display.
        /// </summary>
        public void RefreshWalletDisplay()
        {
            if (_walletDisplay != null)
            {
                _walletDisplay.UpdateLuniDisplay(CurrentLuni);
            }
        }

        /// <summary>
        /// Called when a shop item is clicked (from ShopItemSlot).
        /// This is the main transaction logic - ALL purchase logic happens here.
        /// </summary>
        /// <param name="item">Item to attempt to purchase</param>
        public void OnItemClicked(ItemDefinitionSO item)
        {
            if (item == null || _gameState == null)
                return;

            // Check if already purchased (unique items)
            if (item.IsUnique && item.IsAlreadyPurchased(_gameState))
            {
                ShowFeedbackMessage(_currentMerchant.AlreadyPurchasedMessage);
                PlaySound(_purchaseFailSFX);
                _onPurchaseFailed?.Raise();
                return;
            }

            // Check affordability
            int playerLuni = CurrentLuni;
            if (playerLuni < item.Cost)
            {
                ShowFeedbackMessage(_currentMerchant.InsufficientFundsMessage);
                PlaySound(_purchaseFailSFX);
                _onPurchaseFailed?.Raise();
                return;
            }

            // Execute purchase
            ExecutePurchase(item);
        }

        /// <summary>
        /// Execute the purchase transaction.
        /// </summary>
        /// <param name="item">Item to purchase</param>
        private void ExecutePurchase(ItemDefinitionSO item)
        {
            // Deduct Luni
            int newLuniAmount = CurrentLuni - item.Cost;
            _gameState.SetInt(_luniCurrencyKey, newLuniAmount);

            // Apply item effect
            item.ApplyEffect(_gameState);

            // Show success message
            ShowFeedbackMessage(_currentMerchant.PurchaseSuccessMessage);

            // Play success sound
            if (item.PurchaseSFX != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(item.PurchaseSFX);
            }
            else
            {
                PlaySound(_purchaseSuccessSFX);
            }

            // Fire success event
            _onPurchaseSuccess?.Raise();

            // Fire Luni changed event
            _onLuniChanged?.Raise();

            // Spawn purchase VFX if available
            if (item.PurchaseVFXPrefab != null)
            {
                GameObject vfx = Instantiate(item.PurchaseVFXPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, 3f); // Auto-cleanup
            }

            // Refresh displays
            RefreshWalletDisplay();
            RefreshShopDisplay(); // This will update affordability and hide purchased unique items

            Debug.Log($"[MerchantPanel] Purchased '{item.ItemName}' for {item.Cost} Luni. Remaining: {newLuniAmount}");
        }

        /// <summary>
        /// Show a feedback message to the player.
        /// </summary>
        /// <param name="message">Message to display</param>
        private async void ShowFeedbackMessage(string message)
        {
            if (_feedbackMessageText == null || string.IsNullOrEmpty(message))
                return;

            // Show message
            _feedbackMessageText.text = message;
            _feedbackMessageText.gameObject.SetActive(true);

            // Wait for duration
            try
            {
                await Awaitable.WaitForSecondsAsync(_feedbackMessageDuration, destroyCancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                return;
            }

            // Hide message
            if (_feedbackMessageText != null)
                _feedbackMessageText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Play a sound effect.
        /// </summary>
        /// <param name="clip">Audio clip to play</param>
        private void PlaySound(AudioClip clip)
        {
            if (_audioSource != null && clip != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Close the merchant panel (called from UI buttons or external systems).
        /// </summary>
        public void CloseMerchantPanel()
        {
            // This would typically be handled by the MerchantInteractable
            // or a UI manager system
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Get debug information about current merchant state.
        /// </summary>
        /// <returns>Debug info string</returns>
        public string GetDebugInfo()
        {
            if (_currentMerchant == null || _gameState == null)
                return "No merchant initialized";

            var availableItems = _currentMerchant.GetAvailableItems(_gameState);
            var soldOutItems = _currentMerchant.GetSoldOutItems(_gameState);

            return $"Merchant: {_currentMerchant.MerchantName}\n" +
                   $"Available Items: {availableItems.Length}\n" +
                   $"Sold Out: {soldOutItems.Length}\n" +
                   $"Player Luni: {CurrentLuni}";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_shopItemSlotPrefab != null)
            {
                var slotComponent = _shopItemSlotPrefab.GetComponent<ShopItemSlot>();
                if (slotComponent == null)
                {
                    Debug.LogWarning("[MerchantPanel] Shop item slot prefab missing ShopItemSlot component!");
                }
            }
        }
#endif
    }
}