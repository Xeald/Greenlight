using UnityEngine;
using Greenlight.Core;
using Greenlight.Core.Events;

namespace Greenlight.Combat
{
    /// <summary>
    /// Heart-based health system that integrates with GameState events.
    /// Implements IDamageable for damage handling.
    /// 
    /// Design Philosophy:
    /// - Health is measured in discrete "hearts" (like Zelda)
    /// - Integrates with Global Game State for persistent health
    /// - Fires events for UI/audio to react to health changes
    /// </summary>
    [AddComponentMenu("Greenlight/Combat/Health Component")]
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField, Tooltip("Starting and maximum health in heart units.")]
        private int _maxHealth = 3;

        [SerializeField, Tooltip("Current health. Set at runtime or for testing.")]
        private int _currentHealth = 3;

        [Header("Game State Integration")]
        [SerializeField, Tooltip("Game state for persistent health storage.")]
        private GameStateSO _gameState;

        [SerializeField, Tooltip("Flag key for storing current health (e.g., 'Player_Health').")]
        private string _healthFlagKey = "Player_Health";

        [SerializeField, Tooltip("Flag key for storing max health (e.g., 'Player_MaxHealth').")]
        private string _maxHealthFlagKey = "Player_MaxHealth";

        [Header("Events")]
        [SerializeField, Tooltip("Event fired when this entity takes damage.")]
        private GameEventSO _onDamageReceived;

        [SerializeField, Tooltip("Event fired when health changes (for UI updates).")]
        private GameEventSO _onHealthChanged;

        [SerializeField, Tooltip("Event fired when this entity dies.")]
        private GameEventSO _onEntityDied;

        [Header("Dependencies")]
        [SerializeField, Tooltip("Invincibility controller for iframe management.")]
        private InvincibilityController _invincibilityController;

        /// <summary>
        /// Current health of this entity.
        /// </summary>
        public int CurrentHealth => _currentHealth;

        /// <summary>
        /// Maximum health of this entity.
        /// </summary>
        public int MaxHealth => _maxHealth;

        /// <summary>
        /// Is this entity currently invulnerable to damage?
        /// </summary>
        public bool IsInvulnerable => _invincibilityController != null && _invincibilityController.IsInvulnerable;

        /// <summary>
        /// Is this entity dead (health <= 0)?
        /// </summary>
        public bool IsDead => _currentHealth <= 0;

        private void Awake()
        {
            // Auto-assign invincibility controller if not set
            if (_invincibilityController == null)
                _invincibilityController = GetComponent<InvincibilityController>();
        }

        private void Start()
        {
            // Initialize health from GameState if available
            if (_gameState != null)
            {
                _currentHealth = _gameState.GetInt(_healthFlagKey, _maxHealth);
                _maxHealth = _gameState.GetInt(_maxHealthFlagKey, _maxHealth);

                // Clamp current health to max in case max was reduced
                _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
            }

            // Fire initial health event for UI setup
            _onHealthChanged?.Raise();
        }

        /// <summary>
        /// Apply damage to this entity.
        /// </summary>
        /// <param name="amount">Damage amount in heart units</param>
        /// <param name="source">Transform of the damage source (for knockback direction)</param>
        /// <param name="damageType">Type of damage being dealt</param>
        /// <returns>True if damage was applied, false if blocked/avoided</returns>
        public bool TakeDamage(int amount, Transform source, DamageType damageType = DamageType.Physical)
        {
            // Check if damage can be applied
            if (amount <= 0 || IsDead || IsInvulnerable)
                return false;

            // Apply damage
            _currentHealth = Mathf.Max(0, _currentHealth - amount);

            // Update GameState
            if (_gameState != null)
                _gameState.SetInt(_healthFlagKey, _currentHealth);

            // Fire events
            _onDamageReceived?.Raise();
            _onHealthChanged?.Raise();

            // Start invincibility frames
            if (_invincibilityController != null && !IsDead)
                _invincibilityController.StartInvincibility();

            // Check for death
            if (IsDead)
            {
                OnDeath();
            }

            return true;
        }

        /// <summary>
        /// Heal this entity by the specified amount.
        /// </summary>
        /// <param name="amount">Heal amount in heart units</param>
        public void Heal(int amount)
        {
            if (amount <= 0 || IsDead)
                return;

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);

            // Update GameState
            if (_gameState != null)
                _gameState.SetInt(_healthFlagKey, _currentHealth);

            // Fire health changed event
            _onHealthChanged?.Raise();
        }

        /// <summary>
        /// Increase maximum health by the specified amount (e.g., Heart Container pickup).
        /// </summary>
        /// <param name="amount">Amount to increase max health</param>
        /// <param name="healToFull">If true, also heals to full health</param>
        public void IncreaseMaxHealth(int amount, bool healToFull = true)
        {
            if (amount <= 0)
                return;

            _maxHealth += amount;

            if (healToFull)
                _currentHealth = _maxHealth;

            // Update GameState
            if (_gameState != null)
            {
                _gameState.SetInt(_maxHealthFlagKey, _maxHealth);
                _gameState.SetInt(_healthFlagKey, _currentHealth);
            }

            // Fire health changed event
            _onHealthChanged?.Raise();
        }

        /// <summary>
        /// Called when this entity dies.
        /// </summary>
        private void OnDeath()
        {
            _onEntityDied?.Raise();
        }

        /// <summary>
        /// Set health to full (for testing or respawn).
        /// </summary>
        public void SetToFullHealth()
        {
            _currentHealth = _maxHealth;

            // Update GameState
            if (_gameState != null)
                _gameState.SetInt(_healthFlagKey, _currentHealth);

            // Fire health changed event
            _onHealthChanged?.Raise();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure health values are valid
            _maxHealth = Mathf.Max(1, _maxHealth);
            _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);

            // Auto-assign components during edit time
            if (_invincibilityController == null)
                _invincibilityController = GetComponent<InvincibilityController>();
        }
#endif
    }
}