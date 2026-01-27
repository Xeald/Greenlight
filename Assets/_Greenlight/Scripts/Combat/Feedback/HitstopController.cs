using UnityEngine;
using System;

namespace Greenlight.Combat
{
    /// <summary>
    /// Controls hitstop effects by pausing Time.timeScale for precise frame counts.
    /// Uses Unity 6 Awaitable for frame-precise timing control.
    /// 
    /// Critical Design Notes:
    /// - Time.timeScale = 0 affects physics, so knockback must apply AFTER hitstop
    /// - Uses destroyCancellationToken to prevent memory leaks
    /// - Provides callback for sequencing other effects
    /// </summary>
    [AddComponentMenu("Greenlight/Combat/Hitstop Controller")]
    public class HitstopController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField, Tooltip("Combat feedback settings for hitstop parameters.")]
        private CombatFeedbackSettingsSO _feedbackSettings;

        [Header("Debug")]
        [SerializeField, Tooltip("Enable debug logging for hitstop events.")]
        private bool _debugLog = false;

        // State
        private bool _isHitstopActive;
        private int _framesRemaining;
        private float _originalTimeScale;

        /// <summary>
        /// Is hitstop currently active?
        /// </summary>
        public bool IsHitstopActive => _isHitstopActive;

        /// <summary>
        /// Frames remaining in current hitstop.
        /// </summary>
        public int FramesRemaining => _framesRemaining;

        /// <summary>
        /// Event fired when hitstop completes.
        /// </summary>
        public event Action OnHitstopComplete;

        private void Awake()
        {
            _originalTimeScale = Time.timeScale;
        }

        /// <summary>
        /// Apply hitstop for the specified number of frames.
        /// </summary>
        /// <param name="frames">Number of frames to freeze</param>
        public void ApplyHitstop(int frames)
        {
            if (frames <= 0)
                return;

            // If already in hitstop, extend it
            if (_isHitstopActive)
            {
                _framesRemaining = Mathf.Max(_framesRemaining, frames);
                
                if (_debugLog)
                {
                    Debug.Log($"[HitstopController] Extended hitstop to {_framesRemaining} frames");
                }
                return;
            }

            // Start new hitstop
            _framesRemaining = frames;
            _isHitstopActive = true;
            _originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            if (_debugLog)
            {
                Debug.Log($"[HitstopController] Started hitstop for {frames} frames");
            }

            // Start the hitstop coroutine
            ApplyHitstopAsync();
        }

        /// <summary>
        /// Apply hitstop asynchronously and return an awaitable task.
        /// This version allows for proper sequencing with other effects.
        /// </summary>
        /// <param name="frames">Number of frames to freeze</param>
        /// <returns>Awaitable that completes when hitstop ends</returns>
        public async Awaitable ApplyHitstopAsync(int frames)
        {
            if (frames <= 0)
                return;

            // Apply hitstop synchronously first
            ApplyHitstop(frames);

            // Wait for hitstop to complete
            while (_isHitstopActive)
            {
                try
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Object destroyed, clean up
                    if (_isHitstopActive)
                        StopHitstop();
                    break;
                }
            }
        }

        /// <summary>
        /// Apply hitstop based on damage amount using feedback settings.
        /// </summary>
        /// <param name="damage">Damage dealt (determines hitstop duration)</param>
        public void ApplyHitstopForDamage(int damage)
        {
            if (_feedbackSettings == null)
            {
                Debug.LogError($"[HitstopController] {name}: No CombatFeedbackSettingsSO assigned!");
                return;
            }

            int frames = _feedbackSettings.GetHitstopFrames(damage);
            ApplyHitstop(frames);
        }

        /// <summary>
        /// Stop hitstop immediately and restore normal time scale.
        /// </summary>
        public void StopHitstop()
        {
            if (!_isHitstopActive)
                return;

            _isHitstopActive = false;
            _framesRemaining = 0;
            Time.timeScale = _originalTimeScale;

            if (_debugLog)
            {
                Debug.Log($"[HitstopController] Stopped hitstop");
            }

            // Fire completion event
            OnHitstopComplete?.Invoke();
        }

        /// <summary>
        /// Internal async method to handle frame counting.
        /// </summary>
        private async void ApplyHitstopAsync()
        {
            while (_framesRemaining > 0 && _isHitstopActive)
            {
                _framesRemaining--;

                try
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Object destroyed, clean up
                    if (_isHitstopActive)
                        StopHitstop();
                    return;
                }
            }

            // Hitstop complete
            if (_isHitstopActive)
                StopHitstop();
        }

        private void OnDestroy()
        {
            // Ensure time scale is restored if object is destroyed during hitstop
            if (_isHitstopActive)
            {
                Time.timeScale = _originalTimeScale;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure we have valid settings
            if (_feedbackSettings == null && _debugLog)
            {
                Debug.LogWarning($"[HitstopController] {name}: No CombatFeedbackSettingsSO assigned!");
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_isHitstopActive && Application.isPlaying)
            {
                // Draw hitstop indicator
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);

                // Draw frame countdown
                Vector3 textPos = transform.position + Vector3.up * 2.5f;
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(textPos, $"Hitstop: {_framesRemaining}f");
                #endif
            }
        }
#endif
    }
}