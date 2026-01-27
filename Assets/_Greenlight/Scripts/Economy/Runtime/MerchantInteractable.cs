using UnityEngine;
using Greenlight.Core;
using Greenlight.Core.Events;

namespace Greenlight.Economy
{
    /// <summary>
    /// Component that handles merchant interaction trigger zone and UI activation.
    /// Integrates with the merchant UI system for trading.
    /// 
    /// Design Philosophy:
    /// - Clear interaction feedback and prompts
    /// - Seamless integration with UI system
    /// - Respects input locking for cutscenes/dialogue
    /// - Pixel-perfect interaction zones
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Greenlight/Economy/Merchant Interactable")]
    public class MerchantInteractable : MonoBehaviour
    {
        [Header("Merchant Configuration")]
        [SerializeField, Tooltip("Merchant inventory and configuration.")]
        private MerchantInventorySO _merchantInventory;

        [SerializeField, Tooltip("Game state for currency and purchase tracking.")]
        private GameStateSO _gameState;

        [Header("Interaction")]
        [SerializeField, Tooltip("Should interaction trigger automatically on contact?")]
        private bool _autoTrigger = false;

        [SerializeField, Tooltip("Key/button prompt for manual interaction.")]
        private string _interactionPrompt = "Press E to shop";

        [Header("UI Integration")]
        [SerializeField, Tooltip("Merchant UI panel to activate.")]
        private GameObject _merchantUIPanel;

        [SerializeField, Tooltip("Canvas group for fading UI in/out.")]
        private CanvasGroup _merchantCanvasGroup;

        [Header("Events")]
        [SerializeField, Tooltip("Event fired when merchant interaction starts.")]
        private GameEventSO _onMerchantInteractionStart;

        [SerializeField, Tooltip("Event fired when merchant interaction ends.")]
        private GameEventSO _onMerchantInteractionEnd;

        [Header("Visual Feedback")]
        [SerializeField, Tooltip("Interaction prompt UI (world space or screen space).")]
        private GameObject _interactionPromptUI;

        [SerializeField, Tooltip("Merchant character sprite renderer.")]
        private SpriteRenderer _merchantSprite;

        [SerializeField, Tooltip("Highlight color when player is in range.")]
        private Color _highlightColor = Color.white;

        [Header("Audio")]
        [SerializeField, Tooltip("Sound when player enters interaction range.")]
        private AudioClip _enterRangeSFX;

        [SerializeField, Tooltip("Sound when shop opens.")]
        private AudioClip _shopOpenSFX;

        [SerializeField, Tooltip("Sound when shop closes.")]
        private AudioClip _shopCloseSFX;

        [SerializeField, Tooltip("Audio source for merchant sounds.")]
        private AudioSource _audioSource;

        // State
        private bool _playerInRange;
        private bool _shopOpen;
        private Transform _playerTransform;
        private Color _originalMerchantColor;
        private Collider2D _triggerCollider;

        /// <summary>
        /// Merchant inventory reference.
        /// </summary>
        public MerchantInventorySO MerchantInventory => _merchantInventory;

        /// <summary>
        /// Is the player currently in interaction range?
        /// </summary>
        public bool PlayerInRange => _playerInRange;

        /// <summary>
        /// Is the shop UI currently open?
        /// </summary>
        public bool ShopOpen => _shopOpen;

        private void Awake()
        {
            // Get components
            _triggerCollider = GetComponent<Collider2D>();
            
            if (_audioSource == null)
                _audioSource = GetComponent<AudioSource>();

            // Ensure collider is a trigger
            if (_triggerCollider != null)
                _triggerCollider.isTrigger = true;

            // Store original merchant color
            if (_merchantSprite != null)
                _originalMerchantColor = _merchantSprite.color;

            // Auto-assign GameState if not set
            if (_gameState == null)
                _gameState = FindFirstObjectByType<GameStateSO>();
        }

        private void Start()
        {
            // Initialize UI state
            SetUIVisible(false);
            
            // Initialize interaction prompt
            SetInteractionPromptVisible(false);
        }

        private void Update()
        {
            // Handle manual interaction
            if (_playerInRange && !_autoTrigger && !_shopOpen)
            {
                // Check for interaction input (this would typically be handled by an input system)
                if (Input.GetKeyDown(KeyCode.E)) // Placeholder - replace with proper input system
                {
                    OpenShop();
                }
            }

            // Handle shop closing
            if (_shopOpen)
            {
                // Check for close input (escape key or same interaction key)
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
                {
                    CloseShop();
                }
            }
        }

        /// <summary>
        /// Open the merchant shop UI.
        /// </summary>
        public void OpenShop()
        {
            if (_shopOpen || _merchantInventory == null)
                return;

            _shopOpen = true;

            // Fire start event
            _onMerchantInteractionStart?.Raise();

            // Play open sound
            PlaySound(_shopOpenSFX);

            // Show UI
            SetUIVisible(true);

            // Hide interaction prompt
            SetInteractionPromptVisible(false);

            // Lock player input during shop interaction
            if (_gameState != null)
            {
                _gameState.SetBool("System_Input_Locked", true);
            }

            // Initialize merchant UI panel if it exists
            // This would be handled by the MerchantPanel component
            if (_merchantUIPanel != null)
            {
                // Use reflection to avoid circular assembly dependency (Economy -> UI)
                var merchantPanelType = System.Type.GetType("Greenlight.UI.MerchantPanel, Greenlight.UI");
                if (merchantPanelType != null)
                {
                    var merchantPanelComponent = _merchantUIPanel.GetComponent(merchantPanelType);
                    if (merchantPanelComponent != null)
                    {
                        var initMethod = merchantPanelType.GetMethod("InitializeMerchant");
                        initMethod?.Invoke(merchantPanelComponent, new object[] { _merchantInventory, _gameState });
                    }
                }
            }
        }

        /// <summary>
        /// Close the merchant shop UI.
        /// </summary>
        public void CloseShop()
        {
            if (!_shopOpen)
                return;

            _shopOpen = false;

            // Fire end event
            _onMerchantInteractionEnd?.Raise();

            // Play close sound
            PlaySound(_shopCloseSFX);

            // Hide UI
            SetUIVisible(false);

            // Show interaction prompt if player still in range
            if (_playerInRange)
            {
                SetInteractionPromptVisible(true);
            }

            // Unlock player input
            if (_gameState != null)
            {
                _gameState.SetBool("System_Input_Locked", false);
            }
        }

        /// <summary>
        /// Set merchant UI visibility.
        /// </summary>
        /// <param name="visible">True to show UI</param>
        private void SetUIVisible(bool visible)
        {
            if (_merchantUIPanel != null)
            {
                _merchantUIPanel.SetActive(visible);
            }

            if (_merchantCanvasGroup != null)
            {
                _merchantCanvasGroup.alpha = visible ? 1f : 0f;
                _merchantCanvasGroup.interactable = visible;
                _merchantCanvasGroup.blocksRaycasts = visible;
            }
        }

        /// <summary>
        /// Set interaction prompt visibility.
        /// </summary>
        /// <param name="visible">True to show prompt</param>
        private void SetInteractionPromptVisible(bool visible)
        {
            if (_interactionPromptUI != null)
            {
                _interactionPromptUI.SetActive(visible && !_autoTrigger);
            }
        }

        /// <summary>
        /// Update merchant highlight based on player proximity.
        /// </summary>
        /// <param name="highlighted">True to highlight merchant</param>
        private void SetMerchantHighlighted(bool highlighted)
        {
            if (_merchantSprite == null)
                return;

            Color targetColor = highlighted ? _highlightColor : _originalMerchantColor;
            _merchantSprite.color = targetColor;
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
        /// Called when player enters interaction trigger.
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;

            _playerInRange = true;
            _playerTransform = other.transform;

            // Play enter range sound
            PlaySound(_enterRangeSFX);

            // Update visual feedback
            SetMerchantHighlighted(true);
            
            if (!_shopOpen)
            {
                SetInteractionPromptVisible(true);

                // Auto-trigger if enabled
                if (_autoTrigger)
                {
                    OpenShop();
                }
            }
        }

        /// <summary>
        /// Called when player exits interaction trigger.
        /// </summary>
        private void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;

            _playerInRange = false;
            _playerTransform = null;

            // Update visual feedback
            SetMerchantHighlighted(false);
            SetInteractionPromptVisible(false);

            // Close shop if it's open and we're not auto-triggering
            if (_shopOpen && !_autoTrigger)
            {
                CloseShop();
            }
        }

        /// <summary>
        /// Manually trigger shop opening (for external systems).
        /// </summary>
        public void TriggerShopOpen()
        {
            if (_playerInRange || _autoTrigger)
            {
                OpenShop();
            }
        }

        /// <summary>
        /// Check if merchant has specific item available.
        /// </summary>
        /// <param name="itemName">Name of item to check</param>
        /// <returns>True if item is available</returns>
        public bool HasItemAvailable(string itemName)
        {
            if (_merchantInventory == null || _gameState == null)
                return false;

            var availableItems = _merchantInventory.GetAvailableItems(_gameState);
            return System.Array.Exists(availableItems, item => item.ItemName == itemName);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure trigger collider
            var collider = GetComponent<Collider2D>();
            if (collider != null && !collider.isTrigger)
            {
                collider.isTrigger = true;
            }

            // Validate merchant inventory
            if (_merchantInventory == null)
            {
                Debug.LogWarning($"[MerchantInteractable] {name}: No MerchantInventorySO assigned!");
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw interaction trigger area
            Collider2D trigger = GetComponent<Collider2D>();
            if (trigger != null)
            {
                Gizmos.color = Color.cyan;
                
                if (trigger is BoxCollider2D box)
                {
                    Gizmos.DrawWireCube(
                        transform.position + (Vector3)box.offset,
                        box.size
                    );
                }
                else if (trigger is CircleCollider2D circle)
                {
                    UnityEditor.Handles.DrawWireDisc(
                        transform.position + (Vector3)circle.offset, Vector3.forward, circle.radius
                    );
                }
            }

            // Draw merchant info
            if (_merchantInventory != null)
            {
                Vector3 labelPos = transform.position + Vector3.up * 2f;
                
                #if UNITY_EDITOR
                string info = $"{_merchantInventory.MerchantName}\n{_merchantInventory.AllAvailableItems.Length} items";
                UnityEditor.Handles.Label(labelPos, info);
                #endif
            }
        }
#endif
    }
}