using UnityEngine;
using Greenlight.Core;
using Greenlight.Core.Events;

namespace Greenlight.Economy
{
    /// <summary>
    /// Physical Luni pickup object that magnets toward the player and adds currency.
    /// Provides satisfying collection feedback and integrates with GameState.
    /// 
    /// Design Philosophy:
    /// - Magnetic collection for smooth gameplay flow
    /// - Clear visual/audio feedback on collection
    /// - Pixel-perfect movement (32 PPU)
    /// - Automatic GameState integration
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [AddComponentMenu("Greenlight/Economy/Luni Pickup")]
    public class LuniPickup : MonoBehaviour
    {
        [Header("Pickup Value")]
        [SerializeField, Range(1, 50), Tooltip("Luni value of this pickup.")]
        private int _luniValue = 1;

        [Header("Magnet Behavior")]
        [SerializeField, Range(1f, 10f), Tooltip("Distance at which pickup starts moving toward player.")]
        private float _magnetRange = 3f;

        [SerializeField, Range(2f, 20f), Tooltip("Speed when moving toward player (pixels per second).")]
        private float _magnetSpeedPixels = 320f;

        [SerializeField, Range(0.5f, 5f), Tooltip("Acceleration when magnetized.")]
        private float _magnetAcceleration = 2f;

        [Header("Collection")]
        [SerializeField, Range(0.1f, 1f), Tooltip("Collection radius (how close player needs to be).")]
        private float _collectionRadius = 0.5f;

        [SerializeField, Tooltip("Auto-destroy after this many seconds if not collected.")]
        private float _lifespan = 30f;

        [Header("Visual Effects")]
        [SerializeField, Tooltip("Sprite renderer for the pickup visual.")]
        private SpriteRenderer _spriteRenderer;

        [SerializeField, Tooltip("Particle effect when collected.")]
        private GameObject _collectionVFXPrefab;

        [SerializeField, Range(0.5f, 2f), Tooltip("Scale multiplier when magnetized.")]
        private float _magnetizedScale = 1.2f;

        [Header("Audio")]
        [SerializeField, Tooltip("Sound effect when collected.")]
        private AudioClip _collectionSFX;

        [SerializeField, Range(0.1f, 2f), Tooltip("Pitch variation for collection sound.")]
        private float _pitchVariation = 0.2f;

        [Header("Physics")]
        [SerializeField, Tooltip("Should this pickup bounce on spawn?")]
        private bool _enableBounce = true;

        [SerializeField, Range(0.3f, 0.8f), Tooltip("Bounce damping factor.")]
        private float _bounceDamping = 0.6f;

        // State
        private bool _isInitialized;
        private bool _isMagnetized;
        private bool _isCollected;
        private Vector2 _magnetVelocity;
        private Transform _playerTransform;
        private GameStateSO _gameState;
        private string _currencyKey;
        private GameEventSO _onCollectedEvent;
        private Rigidbody2D _rigidbody;
        private Collider2D _collider;
        private Vector3 _originalScale;
        private float _spawnTime;

        /// <summary>
        /// Luni value of this pickup.
        /// </summary>
        public int LuniValue => _luniValue;

        /// <summary>
        /// Is this pickup currently magnetized toward the player?
        /// </summary>
        public bool IsMagnetized => _isMagnetized;

        /// <summary>
        /// Has this pickup been collected?
        /// </summary>
        public bool IsCollected => _isCollected;

        private void Awake()
        {
            // Get components
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();

            if (_spriteRenderer == null)
                _spriteRenderer = GetComponent<SpriteRenderer>();

            // Store original scale
            _originalScale = transform.localScale;

            // Set up collider as trigger
            if (_collider != null)
                _collider.isTrigger = true;

            // Configure rigidbody
            if (_rigidbody != null)
            {
                _rigidbody.bodyType = RigidbodyType2D.Dynamic;
                _rigidbody.gravityScale = 1f;
            }

            _spawnTime = Time.time;
        }

        private void Start()
        {
            // Find player
            FindPlayer();

            // Start lifespan countdown
            if (_lifespan > 0f)
            {
                Invoke(nameof(ExpirePickup), _lifespan);
            }
        }

        private void Update()
        {
            if (_isCollected || !_isInitialized)
                return;

            // Check for magnetization
            UpdateMagnetization();

            // Handle magnetized movement
            if (_isMagnetized)
            {
                UpdateMagnetMovement();
            }

            // Check for collection
            CheckForCollection();
        }

        /// <summary>
        /// Initialize this pickup with external parameters.
        /// </summary>
        /// <param name="value">Luni value</param>
        /// <param name="gameState">GameState to add currency to</param>
        /// <param name="currencyKey">GameState key for currency</param>
        /// <param name="onCollectedEvent">Event to fire when collected</param>
        public void Initialize(int value, GameStateSO gameState, string currencyKey, GameEventSO onCollectedEvent)
        {
            _luniValue = value;
            _gameState = gameState;
            _currencyKey = currencyKey;
            _onCollectedEvent = onCollectedEvent;
            _isInitialized = true;
        }

        /// <summary>
        /// Find and cache player transform reference.
        /// </summary>
        private void FindPlayer()
        {
            GameObject player = GameObject.FindWithTag("Player");
            _playerTransform = player?.transform;
        }

        /// <summary>
        /// Update magnetization state based on player distance.
        /// </summary>
        private void UpdateMagnetization()
        {
            if (_playerTransform == null)
                return;

            float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);
            bool shouldMagnetize = distanceToPlayer <= _magnetRange;

            if (shouldMagnetize && !_isMagnetized)
            {
                StartMagnetization();
            }
            else if (!shouldMagnetize && _isMagnetized)
            {
                StopMagnetization();
            }
        }

        /// <summary>
        /// Start magnetizing toward the player.
        /// </summary>
        private void StartMagnetization()
        {
            _isMagnetized = true;

            // Disable physics gravity when magnetized
            if (_rigidbody != null)
            {
                _rigidbody.gravityScale = 0f;
                _rigidbody.linearVelocity = Vector2.zero;
            }

            // Scale up slightly when magnetized
            transform.localScale = _originalScale * _magnetizedScale;
        }

        /// <summary>
        /// Stop magnetizing and return to normal physics.
        /// </summary>
        private void StopMagnetization()
        {
            _isMagnetized = false;
            _magnetVelocity = Vector2.zero;

            // Re-enable physics
            if (_rigidbody != null)
            {
                _rigidbody.gravityScale = 1f;
            }

            // Restore original scale
            transform.localScale = _originalScale;
        }

        /// <summary>
        /// Update movement when magnetized toward player.
        /// </summary>
        private void UpdateMagnetMovement()
        {
            if (_playerTransform == null)
                return;

            // Calculate direction to player
            Vector2 directionToPlayer = (_playerTransform.position - transform.position).normalized;
            
            // Calculate target velocity
            float targetSpeed = _magnetSpeedPixels / 32f; // Convert to Unity units
            Vector2 targetVelocity = directionToPlayer * targetSpeed;

            // Accelerate toward target velocity
            _magnetVelocity = Vector2.MoveTowards(
                _magnetVelocity,
                targetVelocity,
                _magnetAcceleration * Time.deltaTime
            );

            // Apply movement with pixel-perfect snapping
            Vector2 currentPos = transform.position;
            Vector2 newPos = currentPos + _magnetVelocity * Time.deltaTime;

            // Snap to pixel grid
            newPos.x = Mathf.Round(newPos.x * 32f) / 32f;
            newPos.y = Mathf.Round(newPos.y * 32f) / 32f;

            transform.position = newPos;
        }

        /// <summary>
        /// Check if player is close enough for collection.
        /// </summary>
        private void CheckForCollection()
        {
            if (_playerTransform == null)
                return;

            float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);
            
            if (distanceToPlayer <= _collectionRadius)
            {
                CollectPickup();
            }
        }

        /// <summary>
        /// Collect this pickup and add Luni to player.
        /// </summary>
        private void CollectPickup()
        {
            if (_isCollected)
                return;

            _isCollected = true;

            // Add Luni to GameState
            if (_gameState != null && !string.IsNullOrEmpty(_currencyKey))
            {
                int currentLuni = _gameState.GetInt(_currencyKey, 0);
                _gameState.SetInt(_currencyKey, currentLuni + _luniValue);
            }

            // Fire collection event
            _onCollectedEvent?.Raise();

            // Play collection effects
            PlayCollectionEffects();

            // Destroy pickup
            Destroy(gameObject);
        }

        /// <summary>
        /// Play visual and audio collection effects.
        /// </summary>
        private void PlayCollectionEffects()
        {
            // Spawn particle effect
            if (_collectionVFXPrefab != null)
            {
                GameObject vfx = Instantiate(_collectionVFXPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, 2f); // Auto-cleanup
            }

            // Play sound effect
            if (_collectionSFX != null)
            {
                // Use AudioSource.PlayClipAtPoint for fire-and-forget audio
                float randomPitch = 1f + Random.Range(-_pitchVariation, _pitchVariation);
                
                // Create temporary AudioSource for pitch control
                GameObject tempAudio = new GameObject("LuniPickupAudio");
                AudioSource audioSource = tempAudio.AddComponent<AudioSource>();
                audioSource.clip = _collectionSFX;
                audioSource.pitch = randomPitch;
                audioSource.Play();

                // Clean up after audio finishes
                Destroy(tempAudio, _collectionSFX.length + 1f);
            }
        }

        /// <summary>
        /// Called when pickup expires without being collected.
        /// </summary>
        private void ExpirePickup()
        {
            if (!_isCollected)
            {
                // Fade out and destroy
                StartCoroutine(FadeOutAndDestroy());
            }
        }

        /// <summary>
        /// Fade out the pickup before destroying it.
        /// </summary>
        private System.Collections.IEnumerator FadeOutAndDestroy()
        {
            float fadeTime = 1f;
            float elapsed = 0f;

            Color originalColor = _spriteRenderer?.color ?? Color.white;

            while (elapsed < fadeTime && _spriteRenderer != null)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(originalColor.a, 0f, elapsed / fadeTime);
                
                Color newColor = originalColor;
                newColor.a = alpha;
                _spriteRenderer.color = newColor;

                yield return null;
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// Trigger collection when player enters trigger.
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") && !_isCollected)
            {
                CollectPickup();
            }
        }

        /// <summary>
        /// Handle bounce physics on collision.
        /// </summary>
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!_enableBounce || _rigidbody == null)
                return;

            // Apply bounce damping
            _rigidbody.linearVelocity *= _bounceDamping;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _luniValue = Mathf.Max(1, _luniValue);
            _magnetRange = Mathf.Max(0.5f, _magnetRange);
            _magnetSpeedPixels = Mathf.Max(32f, _magnetSpeedPixels);
            _collectionRadius = Mathf.Max(0.1f, _collectionRadius);
            _lifespan = Mathf.Max(1f, _lifespan);
        }

        private void OnDrawGizmosSelected()
        {
            // Draw magnet range
            Gizmos.color = Color.cyan;
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, _magnetRange);

            // Draw collection radius
            Gizmos.color = Color.green;
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, _collectionRadius);

            // Draw value label
            Vector3 labelPos = transform.position + Vector3.up * 0.8f;
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(labelPos, $"{_luniValue} Luni");
            #endif
        }
#endif
    }
}