using UnityEngine;
using Greenlight.Combat;

namespace Greenlight.AI
{
    /// <summary>
    /// ScriptableObject defining an enemy type with all its data-driven parameters.
    /// Separates data from behavior for flexible enemy design.
    /// 
    /// Design Philosophy:
    /// - Brain (this SO) + Body (behavior prefab) architecture
    /// - All tunable values exposed for designers
    /// - Luni drop ranges for economy integration
    /// - References to behavior prefabs for instantiation
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Greenlight/AI/Enemy Definition")]
    public class EnemyDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField, Tooltip("Display name of this enemy type.")]
        private string _enemyName = "New Enemy";

        [SerializeField, Tooltip("Description for designer reference.")]
        [TextArea(2, 4)]
        private string _description = "Describe this enemy's role and behavior.";

        [Header("Combat Stats")]
        [SerializeField, Range(1, 10), Tooltip("Maximum health in heart units.")]
        private int _maxHealth = 1;

        [SerializeField, Range(1, 5), Tooltip("Damage dealt per attack.")]
        private int _attackDamage = 1;

        [SerializeField, Tooltip("Types of damage this enemy is resistant to.")]
        private DamageType[] _resistances = new DamageType[0];

        [SerializeField, Tooltip("Types of damage this enemy is weak to (takes double damage).")]
        private DamageType[] _weaknesses = new DamageType[0];

        [Header("Movement (32 PPU)")]
        [SerializeField, Tooltip("Movement speed in pixels per second.")]
        private float _moveSpeedPixels = 64f;

        [SerializeField, Range(0.1f, 3f), Tooltip("Speed multiplier when chasing player.")]
        private float _chaseSpeedMultiplier = 1.5f;

        [SerializeField, Range(0.1f, 2f), Tooltip("Rotation speed when turning (radians per second).")]
        private float _rotationSpeed = 5f;

        [Header("AI Behavior")]
        [SerializeField, Tooltip("Detection range for finding the player (Unity units).")]
        private float _detectionRange = 4f;

        [SerializeField, Tooltip("Attack range - how close to get before attacking.")]
        private float _attackRange = 1.5f;

        [SerializeField, Tooltip("Time between attacks (seconds).")]
        private float _attackCooldown = 2f;

        [SerializeField, Tooltip("Duration of stun after taking damage (seconds).")]
        private float _stunDuration = 0.5f;

        [Header("Economy")]
        [SerializeField, Tooltip("Minimum Luni dropped on death.")]
        private int _minLuniDrop = 3;

        [SerializeField, Tooltip("Maximum Luni dropped on death.")]
        private int _maxLuniDrop = 5;

        [Header("Prefab References")]
        [SerializeField, Tooltip("Prefab containing the enemy's behavior components and visuals.")]
        private GameObject _behaviorPrefab;

        [SerializeField, Tooltip("Optional death VFX prefab.")]
        private GameObject _deathVFXPrefab;

        /// <summary>
        /// Display name of this enemy type.
        /// </summary>
        public string EnemyName => _enemyName;

        /// <summary>
        /// Description for designer reference.
        /// </summary>
        public string Description => _description;

        /// <summary>
        /// Maximum health in heart units.
        /// </summary>
        public int MaxHealth => _maxHealth;

        /// <summary>
        /// Damage dealt per attack.
        /// </summary>
        public int AttackDamage => _attackDamage;

        /// <summary>
        /// Types of damage this enemy resists.
        /// </summary>
        public DamageType[] Resistances => _resistances;

        /// <summary>
        /// Types of damage this enemy is weak to.
        /// </summary>
        public DamageType[] Weaknesses => _weaknesses;

        /// <summary>
        /// Movement speed in Unity units per second.
        /// </summary>
        public float MoveSpeedUnits => _moveSpeedPixels / 32f;

        /// <summary>
        /// Raw movement speed in pixels per second for designer reference.
        /// </summary>
        public float MoveSpeedPixels => _moveSpeedPixels;

        /// <summary>
        /// Chase speed in Unity units per second.
        /// </summary>
        public float ChaseSpeedUnits => (_moveSpeedPixels * _chaseSpeedMultiplier) / 32f;

        /// <summary>
        /// Chase speed multiplier.
        /// </summary>
        public float ChaseSpeedMultiplier => _chaseSpeedMultiplier;

        /// <summary>
        /// Rotation speed when turning (radians per second).
        /// </summary>
        public float RotationSpeed => _rotationSpeed;

        /// <summary>
        /// Detection range for finding the player.
        /// </summary>
        public float DetectionRange => _detectionRange;

        /// <summary>
        /// Attack range - how close to get before attacking.
        /// </summary>
        public float AttackRange => _attackRange;

        /// <summary>
        /// Time between attacks (seconds).
        /// </summary>
        public float AttackCooldown => _attackCooldown;

        /// <summary>
        /// Duration of stun after taking damage (seconds).
        /// </summary>
        public float StunDuration => _stunDuration;

        /// <summary>
        /// Minimum Luni dropped on death.
        /// </summary>
        public int MinLuniDrop => _minLuniDrop;

        /// <summary>
        /// Maximum Luni dropped on death.
        /// </summary>
        public int MaxLuniDrop => _maxLuniDrop;

        /// <summary>
        /// Random Luni drop amount within the defined range.
        /// </summary>
        public int RandomLuniDrop => Random.Range(_minLuniDrop, _maxLuniDrop + 1);

        /// <summary>
        /// Prefab containing the enemy's behavior components and visuals.
        /// </summary>
        public GameObject BehaviorPrefab => _behaviorPrefab;

        /// <summary>
        /// Optional death VFX prefab.
        /// </summary>
        public GameObject DeathVFXPrefab => _deathVFXPrefab;

        /// <summary>
        /// Check if this enemy is resistant to a damage type.
        /// </summary>
        /// <param name="damageType">Damage type to check</param>
        /// <returns>True if resistant (takes half damage)</returns>
        public bool IsResistantTo(DamageType damageType)
        {
            return System.Array.IndexOf(_resistances, damageType) >= 0;
        }

        /// <summary>
        /// Check if this enemy is weak to a damage type.
        /// </summary>
        /// <param name="damageType">Damage type to check</param>
        /// <returns>True if weak (takes double damage)</returns>
        public bool IsWeakTo(DamageType damageType)
        {
            return System.Array.IndexOf(_weaknesses, damageType) >= 0;
        }

        /// <summary>
        /// Calculate effective damage after resistances and weaknesses.
        /// </summary>
        /// <param name="baseDamage">Base damage amount</param>
        /// <param name="damageType">Type of damage</param>
        /// <returns>Final damage after modifiers</returns>
        public int CalculateEffectiveDamage(int baseDamage, DamageType damageType)
        {
            float multiplier = 1f;

            if (IsResistantTo(damageType))
                multiplier *= 0.5f;
            
            if (IsWeakTo(damageType))
                multiplier *= 2f;

            return Mathf.RoundToInt(baseDamage * multiplier);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure values stay positive and reasonable
            _maxHealth = Mathf.Max(1, _maxHealth);
            _attackDamage = Mathf.Max(1, _attackDamage);
            _moveSpeedPixels = Mathf.Max(1f, _moveSpeedPixels);
            _detectionRange = Mathf.Max(0.5f, _detectionRange);
            _attackRange = Mathf.Max(0.1f, _attackRange);
            _attackCooldown = Mathf.Max(0.1f, _attackCooldown);
            
            // Ensure min <= max for Luni drops
            if (_minLuniDrop > _maxLuniDrop)
                _maxLuniDrop = _minLuniDrop;
        }
#endif
    }
}