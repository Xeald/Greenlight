using UnityEngine;
using Greenlight.Core;
using Greenlight.Core.Events;

namespace Greenlight.Economy
{
    /// <summary>
    /// Component responsible for dropping Luni when enemies die.
    /// Implements "No Grinding" philosophy with guaranteed drops.
    /// 
    /// Design Philosophy:
    /// - 100% drop rate - every enemy ALWAYS drops Luni
    /// - Satisfying visual spawning with physics
    /// - Integration with GameState for immediate currency tracking
    /// - Event-driven for UI feedback
    /// </summary>
    [AddComponentMenu("Greenlight/Economy/Luni Dropper")]
    public class LuniDropper : MonoBehaviour
    {
        [Header("Drop Configuration")]
        [SerializeField, Tooltip("Drop table defining Luni amounts and behavior.")]
        private LuniDropTableSO _dropTable;

        [SerializeField, Tooltip("Override drop amount (0 = use drop table).")]
        private int _overrideDropAmount = 0;

        [Header("Game State Integration")]
        [SerializeField, Tooltip("Game state to add Luni to.")]
        private GameStateSO _gameState;

        [SerializeField, Tooltip("Flag key for player's Luni currency.")]
        private string _luniCurrencyKey = "Player_Luni";

        [Header("Events")]
        [SerializeField, Tooltip("Event fired when Luni is collected.")]
        private GameEventSO _onLuniCollected;

        [Header("Spawn Behavior")]
        [SerializeField, Range(0f, 2f), Tooltip("Delay before spawning Luni (for death animation timing).")]
        private float _spawnDelay = 0.3f;

        [SerializeField, Tooltip("Should spawn physical pickups or add directly to GameState?")]
        private bool _spawnPhysicalPickups = true;

        [Header("Debug")]
        [SerializeField, Tooltip("Enable debug logging for drop events.")]
        private bool _debugLog = false;

        /// <summary>
        /// Drop table reference.
        /// </summary>
        public LuniDropTableSO DropTable => _dropTable;

        /// <summary>
        /// Current Luni currency amount in GameState.
        /// </summary>
        public int CurrentLuni => _gameState?.GetInt(_luniCurrencyKey, 0) ?? 0;

        private void Awake()
        {
            // Auto-assign GameState if not set
            if (_gameState == null)
            {
                _gameState = FindFirstObjectByType<GameStateSO>();
            }
        }

        /// <summary>
        /// Drop Luni at this entity's position.
        /// </summary>
        public void DropLuni()
        {
            DropLuniAtPosition(transform.position);
        }

        /// <summary>
        /// Drop Luni at a specific position.
        /// </summary>
        /// <param name="position">World position to drop Luni</param>
        public async void DropLuniAtPosition(Vector2 position)
        {
            if (_dropTable == null)
            {
                Debug.LogError($"[LuniDropper] {name}: No LuniDropTableSO assigned!");
                return;
            }

            // Apply spawn delay if specified
            if (_spawnDelay > 0f)
            {
                try
                {
                    await Awaitable.WaitForSecondsAsync(_spawnDelay, destroyCancellationToken);
                }
                catch (System.OperationCanceledException)
                {
                    return; // Object destroyed
                }
            }

            // Calculate drop amount
            int dropAmount = _overrideDropAmount > 0 ? _overrideDropAmount : _dropTable.GenerateDropAmount();

            if (_debugLog)
            {
                Debug.Log($"[LuniDropper] {name}: Dropping {dropAmount} Luni at {position}");
            }

            // Handle drop based on mode
            if (_spawnPhysicalPickups && _dropTable.LuniPickupPrefab != null)
            {
                SpawnPhysicalPickups(position, dropAmount);
            }
            else
            {
                AddLuniDirectly(dropAmount);
            }
        }

        /// <summary>
        /// Spawn physical Luni pickup objects.
        /// </summary>
        /// <param name="position">Spawn position</param>
        /// <param name="totalAmount">Total Luni amount to spawn</param>
        private void SpawnPhysicalPickups(Vector2 position, int totalAmount)
        {
            // Calculate how many pickups to spawn
            int pickupCount = _dropTable.CalculatePickupCount(totalAmount);
            int[] luniAmounts = _dropTable.DistributeLuniAmounts(totalAmount, pickupCount);
            Vector2[] spawnPositions = _dropTable.GenerateSpawnPositions(position, pickupCount);

            // Spawn each pickup
            for (int i = 0; i < pickupCount; i++)
            {
                SpawnSinglePickup(spawnPositions[i], luniAmounts[i]);
            }
        }

        /// <summary>
        /// Spawn a single Luni pickup object.
        /// </summary>
        /// <param name="position">Spawn position</param>
        /// <param name="luniValue">Luni value for this pickup</param>
        private void SpawnSinglePickup(Vector2 position, int luniValue)
        {
            // Instantiate pickup
            GameObject pickup = Instantiate(_dropTable.LuniPickupPrefab, position, Quaternion.identity);

            // Configure pickup value
            LuniPickup luniPickup = pickup.GetComponent<LuniPickup>();
            if (luniPickup != null)
            {
                luniPickup.Initialize(luniValue, _gameState, _luniCurrencyKey, _onLuniCollected);
            }

            // Apply initial physics impulse
            Rigidbody2D rb = pickup.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                float impulseForce = _dropTable.InitialDropVelocity;
                rb.AddForce(randomDirection * impulseForce, ForceMode2D.Impulse);

                // Add slight upward bias for more natural physics
                rb.AddForce(Vector2.up * impulseForce * 0.5f, ForceMode2D.Impulse);
            }

            if (_debugLog)
            {
                Debug.Log($"[LuniDropper] Spawned pickup worth {luniValue} Luni at {position}");
            }
        }

        /// <summary>
        /// Add Luni directly to GameState without spawning pickups.
        /// </summary>
        /// <param name="amount">Amount to add</param>
        private void AddLuniDirectly(int amount)
        {
            if (_gameState == null)
                return;

            int currentLuni = _gameState.GetInt(_luniCurrencyKey, 0);
            int newTotal = currentLuni + amount;
            _gameState.SetInt(_luniCurrencyKey, newTotal);

            // Fire collection event for UI feedback
            _onLuniCollected?.Raise();

            if (_debugLog)
            {
                Debug.Log($"[LuniDropper] Added {amount} Luni directly. Total: {newTotal}");
            }
        }

        /// <summary>
        /// Force drop a specific amount of Luni (for testing/special cases).
        /// </summary>
        /// <param name="amount">Specific amount to drop</param>
        public void ForceDropAmount(int amount)
        {
            int originalOverride = _overrideDropAmount;
            _overrideDropAmount = amount;
            DropLuni();
            _overrideDropAmount = originalOverride;
        }

        /// <summary>
        /// Set up automatic dropping on entity death.
        /// Call this from enemy death events or similar.
        /// </summary>
        public void SetupAutoDrop()
        {
            // In a full implementation, this would subscribe to health depletion events
            // For now, it's called manually from enemy death states
        }

        /// <summary>
        /// Get estimated drop value for UI display.
        /// </summary>
        /// <returns>String describing expected drop (e.g., "3-5 Luni")</returns>
        public string GetEstimatedDropString()
        {
            if (_dropTable == null)
                return "Unknown";

            if (_overrideDropAmount > 0)
                return $"{_overrideDropAmount} Luni";

            return $"{_dropTable.MinLuniAmount}-{_dropTable.MaxLuniAmount} Luni";
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _overrideDropAmount = Mathf.Max(0, _overrideDropAmount);

            if (_dropTable == null)
            {
                Debug.LogWarning($"[LuniDropper] {name}: No LuniDropTableSO assigned!");
            }

            if (_spawnPhysicalPickups && _dropTable?.LuniPickupPrefab == null)
            {
                Debug.LogWarning($"[LuniDropper] {name}: Physical pickup spawning enabled but no pickup prefab assigned!");
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_dropTable == null)
                return;

            // Draw drop radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCircle(transform.position, _dropTable.DropSpreadRadius);

            // Draw drop info
            Vector3 labelPos = transform.position + Vector3.up * 1f;
            
            #if UNITY_EDITOR
            string dropInfo = GetEstimatedDropString();
            UnityEditor.Handles.Label(labelPos, dropInfo);
            #endif
        }
#endif
    }
}