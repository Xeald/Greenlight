# Phase 3: Quick Reference Guide

**Version 1.0** | *Developer Cheat Sheet*

Quick API references and common code patterns for Phase 3 combat, AI, and economy systems. For full documentation, see **[Phase 3: The Conversation](phase3-conversation.md)**.

---

## 🚀 Getting Started

### Required Scene Components
```csharp
// Add to scene for combat
CombatFeedbackManager    // Auto-finds controllers
HitstopController       // Handles Time.timeScale manipulation  
ScreenShakeController   // Manual camera shake
```

### Required Prefab Components
```csharp
// Player additions
HealthComponent         // Integrates with GameState
KnockbackReceiver      // Pixel-perfect physics
InvincibilityController // iframes with flicker

// Enemy requirements  
EnemyController        // Base controller
HealthComponent        // Damage handling
KnockbackReceiver      // Impact physics
EnemyVisuals           // Animation/sprite management
EnemyTelegraph        // Attack warning system
LuniDropper           // 100% drop economy
```

---

## 💻 Common Code Patterns

### Deal Damage (Combat Foundation)
```csharp
// ✅ Standard damage dealing
public void DealDamage(Transform target, int amount)
{
    var health = target.GetComponent<HealthComponent>();
    if (health != null)
    {
        bool damageDealt = health.TakeDamage(amount, transform, DamageType.Physical);
        
        if (damageDealt)
        {
            // Trigger feedback
            CombatFeedbackManager.FeedbackAPI.ApplyDamageFeedback(
                amount, target, transform
            );
        }
    }
}
```

### Apply Combat Feedback
```csharp
// ✅ Manual feedback triggering
CombatFeedbackManager.FeedbackAPI.ApplyDamageFeedback(
    damageAmount,    // int: damage dealt
    targetTransform, // Transform: who took damage  
    sourceTransform  // Transform: who dealt damage
);

// ✅ Custom feedback
var manager = FindFirstObjectByType<CombatFeedbackManager>();
manager.ApplyCustomFeedback(
    hitstopFrames: 6,      // int: frames to freeze
    shakeIntensity: 0.8f,  // float: screen shake power
    shakeDuration: 0.3f    // float: shake time
);
```

### Implement IDamageable
```csharp
public class MyDamageableObject : MonoBehaviour, IDamageable
{
    [SerializeField] private int _maxHealth = 3;
    private int _currentHealth;
    
    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;
    public bool IsInvulnerable { get; private set; }
    
    private void Start() => _currentHealth = _maxHealth;
    
    public bool TakeDamage(int amount, Transform source, DamageType damageType = DamageType.Physical)
    {
        if (amount <= 0 || _currentHealth <= 0 || IsInvulnerable)
            return false;
            
        _currentHealth = Mathf.Max(0, _currentHealth - amount);
        
        // Add your damage response logic here
        return true;
    }
}
```

### Enemy State Transitions
```csharp
public override EnemyState CheckTransitions()
{
    // Priority order matters!
    if (Owner.HealthComponent.IsDead)
        return StateMachine.GetState<DeathState>();
        
    if (IsPlayerInRange(Owner.Definition.AttackRange))
        return StateMachine.GetState<AttackState>();
        
    if (IsPlayerInRange(Owner.Definition.DetectionRange) && 
        HasLineOfSightToPlayer(Owner.Definition.DetectionRange))
        return StateMachine.GetState<ChaseState>();
        
    return null; // Stay in current state
}
```

### IPullable Implementation (Gadgets as Grammar)
```csharp
public class PullableEnemy : MonoBehaviour, IPullable
{
    public void PullToward(Vector2 targetPosition, float speed)
    {
        if (_isBeingPulled) return;
        
        _isBeingPulled = true;
        
        // Immediate effect (shield drop, etc.)
        DisableDefenses();
        
        // Apply knockback toward pull source
        var knockback = GetComponent<KnockbackReceiver>();
        Vector2 pullDirection = (targetPosition - (Vector2)transform.position).normalized;
        knockback?.ApplyKnockback(pullDirection, speed, 0.5f);
    }
    
    public void OnPullComplete()
    {
        // Force vulnerable state
        var enemy = GetComponent<EnemyController>();
        enemy?.StateMachine.TransitionTo<StunnedState>();
        
        // Heavy impact feedback
        CombatFeedbackManager.FeedbackAPI.ApplyDamageFeedback(2, transform, null);
    }
}
```

### Economy Integration
```csharp
// ✅ Drop Luni (100% guaranteed)
public void DropLuni(int amount)
{
    var dropper = GetComponent<LuniDropper>();
    if (dropper != null)
    {
        dropper.ForceDropAmount(amount); // Always drops
    }
}

// ✅ Add Luni directly to player
public void AddLuniToPlayer(int amount)
{
    var gameState = FindFirstObjectByType<GameStateSO>();
    if (gameState != null)
    {
        int current = gameState.GetInt("Player_Luni", 0);
        gameState.SetInt("Player_Luni", current + amount);
    }
}

// ✅ Check if player can afford item
public bool CanPlayerAfford(int cost)
{
    var gameState = FindFirstObjectByType<GameStateSO>();
    if (gameState != null)
    {
        int playerLuni = gameState.GetInt("Player_Luni", 0);
        return playerLuni >= cost;
    }
    return false;
}
```

### High‑Fidelity Retro Movement (Smooth Physics + Pixel‑Perfect Visuals)
```csharp
// ✅ Correct pattern:
// - Physics stays smooth (sub-pixel) via Rigidbody2D.MovePosition
// - Sprite visuals snap to 32 PPU grid in LateUpdate (visual child only)
private void FixedUpdate()
{
    Vector2 currentPos = _rigidbody.position;
    Vector2 newPos = currentPos + _velocity * Time.fixedDeltaTime;
    _rigidbody.MovePosition(newPos); // Do NOT snap physics position
}

private void LateUpdate()
{
    Vector3 pos = _spriteTransform.position;
    pos.x = Mathf.Round(pos.x * 32f) / 32f;
    pos.y = Mathf.Round(pos.y * 32f) / 32f;
    _spriteTransform.position = pos;
}

// ✅ Convert pixels to Unity units
float unitsPerSecond = pixelsPerSecond / 32f;

// ✅ Convert Unity units to pixels  
float pixelsPerSecond = unitsPerSecond * 32f;
```

---

## ⚙️ ScriptableObject Creation Quick Commands

### Combat
```
Right-click → Create → Greenlight → Combat → Combat Settings
Right-click → Create → Greenlight → Combat → Combat Feedback Settings
```

### Enemy AI
```
Right-click → Create → Greenlight → AI → Enemy Definition
Right-click → Create → Greenlight → AI → Enemy Behavior Settings  
Right-click → Create → Greenlight → AI → Telegraph Settings
```

### Economy
```
Right-click → Create → Greenlight → Economy → Luni Drop Table
Right-click → Create → Greenlight → Economy → Item Definition
Right-click → Create → Greenlight → Economy → Merchant Inventory
```

---

## 🚨 Common Gotchas & Fixes

### Hitstop Issues
```csharp
// ❌ Knockback during hitstop feels "slippery"
rigidbody.AddForce(knockbackForce); // Don't do this during hitstop!

// ✅ Proper sequencing
await hitstopController.ApplyHitstopAsync(frames);
knockbackReceiver.ApplyKnockback(direction, force); // AFTER hitstop
```

### Enemy Detection
```csharp
// ❌ Enemies see through walls
bool canSeePlayer = Vector2.Distance(transform.position, player.position) < range;

// ✅ Proper line-of-sight check  
bool canSeePlayer = behaviorSettings.HasLineOfSight(
    transform.position, 
    player.position, 
    detectionRange
);
```

### Economy Sync
```csharp
// ❌ Shop shows wrong state after purchase
// (Forgetting to refresh after GameState change)

// ✅ Always refresh UI after GameState modification
gameState.SetInt("Player_Luni", newAmount);
merchantPanel.RefreshShopDisplay(); // Don't forget this!
merchantPanel.RefreshWalletDisplay();
```

### Performance in Combat
```csharp
// ❌ Multiple concurrent hitstops
hitstopController.ApplyHitstop(10);
hitstopController.ApplyHitstop(8); // Second call extends, doesn't stack

// ✅ Check if hitstop is already active
if (!hitstopController.IsHitstopActive)
    hitstopController.ApplyHitstop(frames);
```

---

## 📊 Recommended Values

### Hitstop Frames (60 FPS)
- **Light hits**: 2-4 frames (snappy)
- **Medium hits**: 4-8 frames (impactful)  
- **Heavy hits**: 8-15 frames (devastating)
- **⚠️ Warning**: >15 frames feels frozen

### Screen Shake Intensity
- **Light**: 0.1-0.4 (subtle feedback)
- **Medium**: 0.4-0.8 (noticeable impact)
- **Heavy**: 0.8-2.0 (screen-rattling)
- **Duration**: 0.2-0.4s optimal

### Telegraph Timing
- **Too Fast**: <0.3s (feels unfair)
- **Sweet Spot**: 0.5-1.2s (skill-based)
- **Too Slow**: >1.5s (breaks pacing)

### Enemy Stats (32 PPU)
```csharp
// Movement speeds in pixels/second
var slimeSpeed = 64f;     // Slow, lumbering
var guardianSpeed = 96f;  // Moderate patrol  
var fastEnemySpeed = 128f; // Quick pursuit

// Detection ranges in Unity units
var closeRange = 2f;      // Melee detection
var mediumRange = 4f;     // Standard enemy  
var longRange = 6f;       // Ranged/alert enemy
```

### Luni Economy
```csharp
// Drop amounts (100% guarantee)
var commonEnemyLuni = 3-5;    // Basic encounters
var eliteEnemyLuni = 8-12;    // Skill-gated enemies  
var miniBossLuni = 20-30;     // Room completion

// Shop item costs
var consumableCost = 5-10;    // Health potions
var upgradeCost = 25-50;      // Stat improvements  
var uniqueItemCost = 75-150;  // Heart containers, gadgets
```

---

## 🔧 Debug Commands

### Inspector Debug
```csharp
// Add [ContextMenu] to test in Inspector
[ContextMenu("Test Damage")]
private void TestDamage() => TakeDamage(1, null);

[ContextMenu("Test Telegraph")]
private void TestTelegraph() => enemyTelegraph.StartTelegraph();

[ContextMenu("Add 100 Luni")]  
private void AddLuni() => AddLuniToPlayer(100);
```

### Console Commands
```csharp
// Use in Debug console or custom debug window
CombatFeedbackManager.FeedbackAPI.ApplyDamageFeedback(3, target, source);
FindFirstObjectByType<HitstopController>().ApplyHitstop(10);
FindFirstObjectByType<ScreenShakeController>().Shake(1.5f, 0.5f);
```

---

*For complete implementation details, see **[Phase 3: The Conversation](phase3-conversation.md)**.*