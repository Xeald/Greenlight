using UnityEngine;
using Greenlight.Core.Events;

namespace Greenlight.Combat
{
    /// <summary>
    /// Orchestrates combat feedback effects (hitstop, screen shake, etc.).
    /// Listens to combat events and sequences effects properly.
    /// 
    /// Critical Sequencing (from plan):
    /// 1. Apply hitstop FIRST (freezes everything)
    /// 2. Apply knockback AFTER hitstop completes
    /// 3. Trigger screenshake concurrent with knockback
    /// 
    /// This ensures knockback doesn't get lost during Time.timeScale = 0
    /// </summary>
    [AddComponentMenu("Greenlight/Combat/Combat Feedback Manager")]
    public class CombatFeedbackManager : MonoBehaviour, IGameEventListener
    {
        [Header("Settings")]
        [SerializeField, Tooltip("Combat feedback settings for effect parameters.")]
        private CombatFeedbackSettingsSO _feedbackSettings;

        [Header("Controllers")]
        [SerializeField, Tooltip("Hitstop controller (auto-assigned if null).")]
        private HitstopController _hitstopController;

        [SerializeField, Tooltip("Screen shake controller (auto-assigned if null).")]
        private ScreenShakeController _screenShakeController;

        [Header("Event Listeners")]
        [SerializeField, Tooltip("Event fired when damage is dealt (triggers feedback).")]
        private GameEventSO _onDamageDealt;

        [SerializeField, Tooltip("Event fired when entity is defeated (special feedback).")]
        private GameEventSO _onEntityDefeated;

        [Header("Debug")]
        [SerializeField, Tooltip("Enable debug logging for feedback events.")]
        private bool _debugLog = false;

        // Temporary storage for damage context
        private struct DamageContext
        {
            public Transform Target;
            public Transform Attacker;
            public int Damage;
            public DamageType DamageType;
        }

        private DamageContext _currentDamageContext;

        private void Awake()
        {
            // Auto-assign controllers
            if (_hitstopController == null)
                _hitstopController = FindFirstObjectByType<HitstopController>();

            if (_screenShakeController == null)
                _screenShakeController = FindFirstObjectByType<ScreenShakeController>();
        }

        private void OnEnable()
        {
            // Register for events
            _onDamageDealt?.RegisterListener(this);
            _onEntityDefeated?.RegisterListener(this);

            // Subscribe to hitstop completion for sequencing
            if (_hitstopController != null)
                _hitstopController.OnHitstopComplete += OnHitstopComplete;
        }

        private void OnDisable()
        {
            // Unregister from events
            _onDamageDealt?.UnregisterListener(this);
            _onEntityDefeated?.UnregisterListener(this);

            // Unsubscribe from hitstop completion
            if (_hitstopController != null)
                _hitstopController.OnHitstopComplete -= OnHitstopComplete;
        }

        /// <summary>
        /// Called when any game event we're listening to is raised.
        /// </summary>
        public void OnEventRaised()
        {
            // This is a simplified version - in a full implementation,
            // we'd need payload data from the events to know damage amounts, targets, etc.
            // For now, we'll trigger default feedback
            
            if (_debugLog)
            {
                Debug.Log("[CombatFeedbackManager] Event raised - triggering feedback");
            }

            // Apply default feedback (1 damage worth)
            ApplyFeedbackForDamage(1, null, null);
        }

        /// <summary>
        /// Apply combat feedback for a damage event.
        /// This is the main entry point that sequences all effects properly.
        /// </summary>
        /// <param name="damage">Damage amount dealt</param>
        /// <param name="target">Target that took damage</param>
        /// <param name="attacker">Source of the damage</param>
        public async void ApplyFeedbackForDamage(int damage, Transform target, Transform attacker)
        {
            if (_feedbackSettings == null)
            {
                Debug.LogError($"[CombatFeedbackManager] {name}: No CombatFeedbackSettingsSO assigned!");
                return;
            }

            // Store damage context for use in sequencing
            _currentDamageContext = new DamageContext
            {
                Target = target,
                Attacker = attacker,
                Damage = damage,
                DamageType = DamageType.Physical
            };

            if (_debugLog)
            {
                Debug.Log($"[CombatFeedbackManager] Applying feedback for {damage} damage");
            }

            // Apply optional feedback delay
            if (_feedbackSettings.FeedbackDelay > 0f)
            {
                try
                {
                    await Awaitable.WaitForSecondsAsync(_feedbackSettings.FeedbackDelay, destroyCancellationToken);
                }
                catch (System.OperationCanceledException)
                {
                    return; // Object destroyed
                }
            }

            // Step 1: Apply hitstop FIRST (this will freeze Time.timeScale)
            if (_hitstopController != null)
            {
                try
                {
                    await _hitstopController.ApplyHitstopAsync(_feedbackSettings.GetHitstopFrames(damage));
                }
                catch (System.OperationCanceledException)
                {
                    return; // Object destroyed
                }
            }

            // Step 2 & 3: Apply knockback and screenshake AFTER hitstop completes
            ApplyPostHitstopEffects();
        }

        /// <summary>
        /// Called when hitstop completes - applies effects that need normal time scale.
        /// </summary>
        private void OnHitstopComplete()
        {
            ApplyPostHitstopEffects();
        }

        /// <summary>
        /// Apply effects that should happen after hitstop (knockback, screenshake).
        /// </summary>
        private void ApplyPostHitstopEffects()
        {
            if (_feedbackSettings == null)
                return;

            var context = _currentDamageContext;

            // Apply knockback to target (if it has a KnockbackReceiver)
            if (context.Target != null && context.Attacker != null)
            {
                var knockbackReceiver = context.Target.GetComponent<KnockbackReceiver>();
                if (knockbackReceiver != null && knockbackReceiver.CanApplyKnockback())
                {
                    knockbackReceiver.ApplyKnockbackFromSource(context.Attacker);

                    if (_debugLog)
                    {
                        Debug.Log($"[CombatFeedbackManager] Applied knockback to {context.Target.name}");
                    }
                }
            }

            // Apply screen shake (concurrent with knockback)
            if (_screenShakeController != null)
            {
                _screenShakeController.ShakeForDamage(context.Damage);

                if (_debugLog)
                {
                    Debug.Log($"[CombatFeedbackManager] Applied screen shake for {context.Damage} damage");
                }
            }
        }

        /// <summary>
        /// Apply special feedback for entity defeat.
        /// </summary>
        /// <param name="defeatedEntity">The entity that was defeated</param>
        public void ApplyDefeatFeedback(Transform defeatedEntity)
        {
            if (_feedbackSettings == null)
                return;

            // Apply enhanced feedback for defeats (heavier shake, longer hitstop)
            int heavyDamage = 3; // Treat defeats as heavy hits
            ApplyFeedbackForDamage(heavyDamage, defeatedEntity, null);

            if (_debugLog)
            {
                Debug.Log($"[CombatFeedbackManager] Applied defeat feedback for {defeatedEntity?.name}");
            }
        }

        /// <summary>
        /// Manually trigger feedback with custom parameters.
        /// </summary>
        /// <param name="hitstopFrames">Hitstop duration in frames</param>
        /// <param name="shakeIntensity">Screen shake intensity</param>
        /// <param name="shakeDuration">Screen shake duration</param>
        public void ApplyCustomFeedback(int hitstopFrames, float shakeIntensity, float shakeDuration)
        {
            // Apply hitstop
            if (_hitstopController != null && hitstopFrames > 0)
            {
                _hitstopController.ApplyHitstop(hitstopFrames);
            }

            // Apply screen shake
            if (_screenShakeController != null && shakeIntensity > 0f)
            {
                _screenShakeController.Shake(shakeIntensity, shakeDuration);
            }

            if (_debugLog)
            {
                Debug.Log($"[CombatFeedbackManager] Applied custom feedback - Hitstop: {hitstopFrames}f, Shake: {shakeIntensity:F2}");
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-assign controllers if not set
            if (_hitstopController == null)
                _hitstopController = FindFirstObjectByType<HitstopController>();

            if (_screenShakeController == null)
                _screenShakeController = FindFirstObjectByType<ScreenShakeController>();
        }
#endif

        /// <summary>
        /// Public API for external systems to trigger feedback.
        /// </summary>
        public static class FeedbackAPI
        {
            private static CombatFeedbackManager _instance;

            /// <summary>
            /// Get or find the active CombatFeedbackManager instance.
            /// </summary>
            public static CombatFeedbackManager Instance
            {
                get
                {
                    if (_instance == null)
                        _instance = FindFirstObjectByType<CombatFeedbackManager>();
                    return _instance;
                }
            }

            /// <summary>
            /// Apply feedback for a damage event (static convenience method).
            /// </summary>
            public static void ApplyDamageFeedback(int damage, Transform target, Transform attacker)
            {
                Instance?.ApplyFeedbackForDamage(damage, target, attacker);
            }

            /// <summary>
            /// Apply feedback for an entity defeat (static convenience method).
            /// </summary>
            public static void ApplyDefeatFeedback(Transform defeatedEntity)
            {
                Instance?.ApplyDefeatFeedback(defeatedEntity);
            }
        }
    }
}