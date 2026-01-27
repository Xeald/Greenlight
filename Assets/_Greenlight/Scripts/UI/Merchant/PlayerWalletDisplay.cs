using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Greenlight.UI
{
    /// <summary>
    /// MVC View component for displaying player's Luni balance.
    /// Handles ONLY display - NO business logic or direct GameState access.
    /// 
    /// Architecture:
    /// - View responsibility: Display currency amount with animations/effects
    /// - Controller responsibility: Provide data, handle logic
    /// - Model responsibility: Store actual currency value
    /// </summary>
    [AddComponentMenu("Greenlight/UI/Player Wallet Display")]
    public class PlayerWalletDisplay : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField, Tooltip("Text component for displaying Luni amount.")]
        private TextMeshProUGUI _luniAmountText;

        [SerializeField, Tooltip("Icon image for Luni currency.")]
        private Image _luniIcon;

        [SerializeField, Tooltip("Optional background image.")]
        private Image _backgroundImage;

        [Header("Animation Settings")]
        [SerializeField, Tooltip("Should animate when Luni amount changes?")]
        private bool _animateOnChange = true;

        [SerializeField, Range(0.1f, 1f), Tooltip("Duration of change animation (seconds).")]
        private float _animationDuration = 0.3f;

        [SerializeField, Tooltip("Color to flash when Luni increases.")]
        private Color _increaseFlashColor = Color.green;

        [SerializeField, Tooltip("Color to flash when Luni decreases.")]
        private Color _decreaseFlashColor = Color.red;

        [Header("Formatting")]
        [SerializeField, Tooltip("Text format for displaying Luni (use {0} for amount).")]
        private string _displayFormat = "{0}";

        [SerializeField, Tooltip("Text to show when Luni amount is zero.")]
        private string _zeroAmountText = "0";

        [SerializeField, Tooltip("Should use number formatting (commas for thousands)?")]
        private bool _useNumberFormatting = true;

        // State
        private int _currentDisplayedAmount;
        private int _targetAmount;
        private bool _isAnimating;
        private Color _originalTextColor;

        /// <summary>
        /// Currently displayed Luni amount.
        /// </summary>
        public int CurrentDisplayedAmount => _currentDisplayedAmount;

        /// <summary>
        /// Is the display currently animating?
        /// </summary>
        public bool IsAnimating => _isAnimating;

        private void Awake()
        {
            // Store original text color for animations
            if (_luniAmountText != null)
            {
                _originalTextColor = _luniAmountText.color;
            }

            // Initialize display
            UpdateDisplayImmediate(0);
        }

        /// <summary>
        /// Update the Luni display to show a new amount.
        /// Called by the Controller when currency changes.
        /// </summary>
        /// <param name="newAmount">New Luni amount to display</param>
        public void UpdateLuniDisplay(int newAmount)
        {
            newAmount = Mathf.Max(0, newAmount); // Ensure non-negative

            if (newAmount == _currentDisplayedAmount)
                return; // No change needed

            _targetAmount = newAmount;

            if (_animateOnChange && gameObject.activeInHierarchy)
            {
                // Animate to new amount
                AnimateToNewAmount();
            }
            else
            {
                // Update immediately
                UpdateDisplayImmediate(newAmount);
            }
        }

        /// <summary>
        /// Immediately update display without animation.
        /// </summary>
        /// <param name="amount">Amount to display</param>
        public void UpdateDisplayImmediate(int amount)
        {
            _currentDisplayedAmount = amount;
            _targetAmount = amount;

            // Update text
            UpdateText(amount);

            // Ensure original color
            if (_luniAmountText != null)
            {
                _luniAmountText.color = _originalTextColor;
            }
        }

        /// <summary>
        /// Update the text display with formatted amount.
        /// </summary>
        /// <param name="amount">Amount to display</param>
        private void UpdateText(int amount)
        {
            if (_luniAmountText == null)
                return;

            string displayText;

            if (amount == 0 && !string.IsNullOrEmpty(_zeroAmountText))
            {
                displayText = _zeroAmountText;
            }
            else
            {
                // Format the number
                string formattedNumber;
                if (_useNumberFormatting)
                {
                    formattedNumber = amount.ToString("N0"); // Adds commas for thousands
                }
                else
                {
                    formattedNumber = amount.ToString();
                }

                // Apply display format
                displayText = string.Format(_displayFormat, formattedNumber);
            }

            _luniAmountText.text = displayText;
        }

        /// <summary>
        /// Animate from current amount to target amount.
        /// </summary>
        private async void AnimateToNewAmount()
        {
            if (_isAnimating)
                return; // Already animating

            _isAnimating = true;
            int startAmount = _currentDisplayedAmount;
            bool isIncrease = _targetAmount > startAmount;

            // Flash color to indicate increase/decrease
            Color flashColor = isIncrease ? _increaseFlashColor : _decreaseFlashColor;
            FlashColor(flashColor);

            // Animate the number counting
            float elapsedTime = 0f;
            
            while (elapsedTime < _animationDuration)
            {
                elapsedTime += Time.unscaledDeltaTime; // Use unscaled time for UI
                float progress = elapsedTime / _animationDuration;
                
                // Smooth step for nicer animation curve
                progress = Mathf.SmoothStep(0f, 1f, progress);
                
                // Interpolate amount
                int displayAmount = Mathf.RoundToInt(Mathf.Lerp(startAmount, _targetAmount, progress));
                _currentDisplayedAmount = displayAmount;
                
                // Update display
                UpdateText(displayAmount);

                try
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                }
                catch (System.OperationCanceledException)
                {
                    // Object destroyed during animation
                    return;
                }
            }

            // Ensure final amount is exact
            _currentDisplayedAmount = _targetAmount;
            UpdateText(_targetAmount);
            
            _isAnimating = false;
        }

        /// <summary>
        /// Flash the text color briefly.
        /// </summary>
        /// <param name="flashColor">Color to flash</param>
        private async void FlashColor(Color flashColor)
        {
            if (_luniAmountText == null)
                return;

            // Flash to color
            _luniAmountText.color = flashColor;

            // Wait briefly
            try
            {
                await Awaitable.WaitForSecondsAsync(0.1f, destroyCancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                return;
            }

            // Fade back to original color
            float fadeTime = 0.2f;
            float elapsedTime = 0f;

            while (elapsedTime < fadeTime && _luniAmountText != null)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / fadeTime;
                
                Color currentColor = Color.Lerp(flashColor, _originalTextColor, progress);
                _luniAmountText.color = currentColor;

                try
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                }
                catch (System.OperationCanceledException)
                {
                    return;
                }
            }

            // Ensure final color
            if (_luniAmountText != null)
            {
                _luniAmountText.color = _originalTextColor;
            }
        }

        /// <summary>
        /// Set whether animations are enabled.
        /// </summary>
        /// <param name="enabled">True to enable animations</param>
        public void SetAnimationsEnabled(bool enabled)
        {
            _animateOnChange = enabled;
        }

        /// <summary>
        /// Set the display format string.
        /// </summary>
        /// <param name="format">Format string (use {0} for amount)</param>
        public void SetDisplayFormat(string format)
        {
            if (!string.IsNullOrEmpty(format))
            {
                _displayFormat = format;
                UpdateText(_currentDisplayedAmount); // Refresh with new format
            }
        }

        /// <summary>
        /// Pulse the wallet display for attention.
        /// </summary>
        public async void PulseDisplay()
        {
            if (!gameObject.activeInHierarchy)
                return;

            Vector3 originalScale = transform.localScale;
            Vector3 pulseScale = originalScale * 1.1f;
            
            float pulseDuration = 0.2f;
            float elapsedTime = 0f;

            // Scale up
            while (elapsedTime < pulseDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / pulseDuration;
                
                transform.localScale = Vector3.Lerp(originalScale, pulseScale, progress);

                try
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                }
                catch (System.OperationCanceledException)
                {
                    return;
                }
            }

            elapsedTime = 0f;

            // Scale back down
            while (elapsedTime < pulseDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                float progress = elapsedTime / pulseDuration;
                
                transform.localScale = Vector3.Lerp(pulseScale, originalScale, progress);

                try
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                }
                catch (System.OperationCanceledException)
                {
                    return;
                }
            }

            // Ensure final scale
            transform.localScale = originalScale;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _animationDuration = Mathf.Max(0.1f, _animationDuration);

            // Validate display format
            if (string.IsNullOrEmpty(_displayFormat) || !_displayFormat.Contains("{0}"))
            {
                Debug.LogWarning($"[PlayerWalletDisplay] {name}: Display format should contain {{0}} placeholder!");
                _displayFormat = "{0}";
            }

            // Update display if in play mode
            if (Application.isPlaying && _luniAmountText != null)
            {
                UpdateDisplayImmediate(_currentDisplayedAmount);
            }
        }

        /// <summary>
        /// Test the wallet display with sample values (editor only).
        /// </summary>
        [ContextMenu("Test Animation - Increase")]
        private void TestAnimationIncrease()
        {
            UpdateLuniDisplay(_currentDisplayedAmount + 100);
        }

        [ContextMenu("Test Animation - Decrease")]
        private void TestAnimationDecrease()
        {
            UpdateLuniDisplay(Mathf.Max(0, _currentDisplayedAmount - 50));
        }

        [ContextMenu("Test Pulse")]
        private void TestPulse()
        {
            PulseDisplay();
        }
#endif
    }
}