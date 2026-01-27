using UnityEngine;

namespace Greenlight.Economy
{
    /// <summary>
    /// ScriptableObject defining Luni drop parameters for enemies.
    /// Implements "No Grinding" philosophy with 100% drop rates.
    /// 
    /// Design Philosophy:
    /// - Every enemy ALWAYS drops Luni (100% drop rate)
    /// - Variance is in amount, not existence
    /// - Clearing a room ONCE provides progression currency
    /// - No farming required for meaningful progress
    /// </summary>
    [CreateAssetMenu(fileName = "LuniDropTable", menuName = "Greenlight/Economy/Luni Drop Table")]
    public class LuniDropTableSO : ScriptableObject
    {
        [Header("Drop Amounts (100% Drop Rate)")]
        [SerializeField, Range(1, 50), Tooltip("Minimum Luni dropped (guaranteed).")]
        private int _minLuniAmount = 3;

        [SerializeField, Range(1, 100), Tooltip("Maximum Luni dropped.")]
        private int _maxLuniAmount = 5;

        [Header("Visual Configuration")]
        [SerializeField, Tooltip("Prefab for the Luni pickup object.")]
        private GameObject _luniPickupPrefab;

        [SerializeField, Range(0.1f, 2f), Tooltip("Spread radius for multiple Luni drops.")]
        private float _dropSpreadRadius = 0.5f;

        [SerializeField, Range(0.5f, 3f), Tooltip("Initial velocity for spawned pickups (for bounce effect).")]
        private float _initialDropVelocity = 1.5f;

        [Header("Drop Behavior")]
        [SerializeField, Tooltip("Should large amounts spawn as individual pickups?")]
        private bool _spawnIndividualPickups = true;

        [SerializeField, Range(1, 10), Tooltip("Maximum individual pickups to spawn (excess combined into larger pickups).")]
        private int _maxIndividualPickups = 5;

        [SerializeField, Range(0f, 1f), Tooltip("Chance for bonus Luni (sparkle effect).")]
        private float _bonusChance = 0.1f;

        [SerializeField, Range(1, 5), Tooltip("Bonus Luni amount when bonus triggers.")]
        private int _bonusAmount = 2;

        /// <summary>
        /// Minimum Luni amount dropped.
        /// </summary>
        public int MinLuniAmount => _minLuniAmount;

        /// <summary>
        /// Maximum Luni amount dropped.
        /// </summary>
        public int MaxLuniAmount => _maxLuniAmount;

        /// <summary>
        /// Prefab for Luni pickup objects.
        /// </summary>
        public GameObject LuniPickupPrefab => _luniPickupPrefab;

        /// <summary>
        /// Spread radius for multiple drops.
        /// </summary>
        public float DropSpreadRadius => _dropSpreadRadius;

        /// <summary>
        /// Initial velocity for dropped pickups.
        /// </summary>
        public float InitialDropVelocity => _initialDropVelocity;

        /// <summary>
        /// Should spawn individual pickups for large amounts?
        /// </summary>
        public bool SpawnIndividualPickups => _spawnIndividualPickups;

        /// <summary>
        /// Maximum individual pickups to spawn.
        /// </summary>
        public int MaxIndividualPickups => _maxIndividualPickups;

        /// <summary>
        /// Generate a random Luni drop amount within the defined range.
        /// ALWAYS returns at least MinLuniAmount (100% drop rate).
        /// </summary>
        /// <returns>Luni amount to drop</returns>
        public int GenerateDropAmount()
        {
            // Base drop amount
            int baseAmount = Random.Range(_minLuniAmount, _maxLuniAmount + 1);

            // Check for bonus
            if (Random.value < _bonusChance)
            {
                baseAmount += _bonusAmount;
            }

            return baseAmount;
        }

        /// <summary>
        /// Calculate how many individual pickups to spawn for a given amount.
        /// </summary>
        /// <param name="totalAmount">Total Luni amount to drop</param>
        /// <returns>Number of individual pickups</returns>
        public int CalculatePickupCount(int totalAmount)
        {
            if (!_spawnIndividualPickups)
                return 1;

            // Limit to max individual pickups
            return Mathf.Min(totalAmount, _maxIndividualPickups);
        }

        /// <summary>
        /// Calculate Luni value per pickup for a given total amount.
        /// </summary>
        /// <param name="totalAmount">Total amount to distribute</param>
        /// <param name="pickupCount">Number of pickups to create</param>
        /// <returns>Array of values for each pickup</returns>
        public int[] DistributeLuniAmounts(int totalAmount, int pickupCount)
        {
            if (pickupCount <= 0)
                return new int[0];

            int[] amounts = new int[pickupCount];
            int remaining = totalAmount;

            // Distribute evenly with remainder handling
            int baseAmount = totalAmount / pickupCount;
            int remainder = totalAmount % pickupCount;

            for (int i = 0; i < pickupCount; i++)
            {
                amounts[i] = baseAmount;
                
                // Distribute remainder to first few pickups
                if (i < remainder)
                {
                    amounts[i]++;
                }
            }

            return amounts;
        }

        /// <summary>
        /// Generate spawn positions for multiple pickups around a center point.
        /// </summary>
        /// <param name="centerPosition">Center spawn position</param>
        /// <param name="count">Number of positions needed</param>
        /// <returns>Array of spawn positions</returns>
        public Vector2[] GenerateSpawnPositions(Vector2 centerPosition, int count)
        {
            Vector2[] positions = new Vector2[count];

            if (count == 1)
            {
                positions[0] = centerPosition;
                return positions;
            }

            // Distribute around circle with some randomness
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * 2f * Mathf.PI;
                float distance = Random.Range(_dropSpreadRadius * 0.5f, _dropSpreadRadius);
                
                Vector2 offset = new Vector2(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance
                );

                positions[i] = centerPosition + offset;
            }

            return positions;
        }

        /// <summary>
        /// Get a description of this drop table for debugging.
        /// </summary>
        /// <returns>Description string</returns>
        public string GetDescription()
        {
            string desc = $"Luni Drop: {_minLuniAmount}-{_maxLuniAmount}";
            
            if (_bonusChance > 0f)
            {
                desc += $" (+{_bonusAmount} bonus {_bonusChance * 100f:F1}%)";
            }

            desc += " (100% drop rate)";
            return desc;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure values are reasonable
            _minLuniAmount = Mathf.Max(1, _minLuniAmount);
            _maxLuniAmount = Mathf.Max(_minLuniAmount, _maxLuniAmount);
            _dropSpreadRadius = Mathf.Max(0.1f, _dropSpreadRadius);
            _initialDropVelocity = Mathf.Max(0.1f, _initialDropVelocity);
            _maxIndividualPickups = Mathf.Max(1, _maxIndividualPickups);
            _bonusAmount = Mathf.Max(0, _bonusAmount);
            _bonusChance = Mathf.Clamp01(_bonusChance);

            // Provide helpful warnings
            if (_luniPickupPrefab == null)
            {
                Debug.LogWarning($"[LuniDropTableSO] {name}: No LuniPickupPrefab assigned!");
            }
        }
#endif
    }
}