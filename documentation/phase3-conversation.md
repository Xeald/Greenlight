# Phase 3: The Conversation (Combat & AI) - Technical Documentation

**Version 1.0** | *Phase 3 Architecture*

The "Conversation" is the third foundational phase of Greenlight. It implements **Tactical AI**, **Combat Feedback**, and the **Luni Economy Loop**. It fulfills the core pillars: *"Combat as Conversation"* and reinforces *"Gadgets as Grammar"* through IPullable enemy integration.

---

## 🧩 Core Concepts

### 1. Combat as Conversation Philosophy

Every enemy attack is a **statement** that requires a **response** from the player. Combat is not about reflexes or stats—it's about reading, understanding, and responding appropriately.

**Key Design Principles:**
- **Telegraph First**: Every attack has clear visual + audio warning (0.3-1.2s telegraph time)
- **Grammar Lessons**: Enemies teach specific tools/techniques (Shield = grapple hook required)  
- **No Ambush**: Player should never feel "cheated" by an attack they couldn't see coming
- **Mechanical Feedback**: Hitstop, screen shake, and audio make every hit feel impactful

### 2. Four-Layer Combat Architecture

| Layer | Components | Responsibility |
|-------|------------|----------------|
| **Combat Foundation** | `IDamageable`, `HealthComponent`, `KnockbackReceiver` | Core damage/health system |
| **Combat Feedback** | `HitstopController`, `ScreenShakeController`, `CombatFeedbackManager` | Impact "juice" and timing |
| **Enemy AI** | `EnemyStateMachine`, `EnemyController`, `EnemyTelegraph` | Behavioral logic and telegraphs |
| **Economy Integration** | `LuniDropper`, `LuniPickup`, `MerchantSystem` | Reward loop and progression |

**Critical Sequencing Rule:**
1. **Hitstop FIRST** (freezes Time.timeScale = 0)
2. **Knockback AFTER** (applied when timeScale returns to 1)
3. **Screen Shake CONCURRENT** (with knockback for maximum impact)

### 3. Enemy Types (Teaching Design)

#### Slime - "The Introduction"
- **Teaches**: Basic timing and dodge fundamentals
- **Pattern**: Telegraph (0.8s) → Charge (0.5s) → Recovery (1.2s vulnerable)
- **Visual Language**: Red flash = dodge NOW
- **Audio Cues**: Rising "boing" charge-up → whoosh → deflate "pffft"

#### Shielded Guardian - "The Grammar Lesson"  
- **Teaches**: Gadgets solve combat problems (not just traversal)
- **Pattern**: Shield blocks frontal damage → Grapple rips shield → Stunned vulnerability
- **Critical Moment**: "Shield Rip" - hook doesn't politely ask, it **RIPS** the shield away
- **IPullable Integration**: Demonstrates gadget-gated combat solutions

### 4. No Grinding Economy (100% Drop Rate)

**Core Principle**: Every enemy kill should feel rewarding, every room clear should provide progression.

| Enemy Type | Luni Drop | Design Intent |
|------------|-----------|---------------|
| Slime | 5-8 (guaranteed) | Common, always rewarding |
| Shielded Guardian | 15-20 (guaranteed) | Skill-gated, higher payout |
| Mini-boss | 50-75 (guaranteed) | Room clear bonus |

**Merchant Integration ("A World That Remembers"):**
- Unique items disappear after purchase
- World remembers every transaction
- NPCs can reference previous purchases
- Example: "Enjoying that Heart Container?" after purchase

---

## 🛠️ How to Use (Designer Workflows)

### Setting Up a New Enemy

1. **Create Enemy Definition (Brain)**
   - Right-click in Project → **Create** → **Greenlight** → **AI** → **Enemy Definition**
   - Configure: Health, damage, speeds (in pixels/second), detection ranges
   - Set Luni drop amounts (min/max, always drops)

2. **Configure Behavior Settings**
   - Right-click in Project → **Create** → **Greenlight** → **AI** → **Enemy Behavior Settings**  
   - Set: Detection range, telegraph duration, attack cooldowns
   - Configure line-of-sight blocking layers

3. **Set Up Telegraph Effects**
   - Right-click in Project → **Create** → **Greenlight** → **AI** → **Telegraph Settings**
   - Define: Visual flash color/intensity, scale effects, audio cues
   - Set duration and animation curves

4. **Build Enemy Prefab**
   - Add `EnemyController` (or specific type like `SlimeController`)
   - Add `HealthComponent`, `KnockbackReceiver`, `InvincibilityController`
   - Add `EnemyVisuals`, `EnemyTelegraph` components
   - Configure Rigidbody2D as **Kinematic** for **snappy‑but‑smooth** movement (smooth physics + pixel-snapped visuals)
   - Assign all ScriptableObject references in Inspector

### Setting Up Combat Feedback

1. **Create Combat Settings**
   - Right-click → **Create** → **Greenlight** → **Combat** → **Combat Settings**
   - Configure: Base damage, invincibility duration, knockback force (pixels/second)
   - Set sprite flicker parameters

2. **Create Feedback Settings**
   - Right-click → **Create** → **Greenlight** → **Combat** → **Combat Feedback Settings**  
   - Set hitstop frames for light/medium/heavy hits (1-20 frames)
   - Configure screen shake intensity per damage tier
   - Set feedback delay timing

3. **Set Up Scene Feedback Manager**
   - Add `CombatFeedbackManager` to scene (auto-finds controllers)
   - Add `HitstopController` and `ScreenShakeController` components  
   - Assign feedback settings ScriptableObject
   - Wire up combat events (OnDamageDealt, OnEntityDefeated, etc.)

### Configuring the Economy

1. **Create Luni Drop Table**
   - Right-click → **Create** → **Greenlight** → **Economy** → **Luni Drop Table**
   - Set min/max amounts (remember: 100% drop rate philosophy)
   - Assign LuniPickup prefab, configure spread radius and physics

2. **Set Up LuniPickup Prefab**
   - Add `LuniPickup` component with Collider2D (trigger)
   - Configure: Magnet range, movement speed (pixels/second), collection radius
   - Add visual effects: particle systems, audio clips
   - Set up physics: Rigidbody2D with bounce damping

3. **Create Merchant Inventory**
   - Right-click → **Create** → **Greenlight** → **Economy** → **Merchant Inventory**
   - Add ItemDefinitionSO references, configure merchant name/description
   - Set max displayed items, sort by cost options

4. **Create Shop Items**
   - Right-click → **Create** → **Greenlight** → **Economy** → **Item Definition**  
   - Configure: Name, icon, cost, effect type, magnitude
   - For unique items: Check IsUnique, assign WorldFlagDefinitionSO for tracking
   - Set purchase VFX/audio, define effect on GameState

### Testing Combat Feel

1. **Hitstop Timing**
   - Light hits: 2-4 frames feels snappy
   - Medium hits: 4-8 frames feels impactful  
   - Heavy hits: 8-15 frames feels devastating
   - **Warning**: >15 frames feels frozen, not impactful

2. **Screen Shake Intensity**
   - Light: 0.1-0.4 (subtle feedback)
   - Medium: 0.4-0.8 (noticeable impact)
   - Heavy: 0.8-2.0 (screen-rattling)
   - **Duration**: 0.2-0.4s is the sweet spot

3. **Telegraph Timing**
   - **Too Fast** (<0.3s): Feels unfair, can't react
   - **Sweet Spot** (0.5-1.2s): Readable, skill-based timing  
   - **Too Slow** (>1.5s): Feels tedious, breaks pacing

---

## 💻 Coding Standards

### Combat System Integration

```csharp
// ✅ Correct: Apply damage with proper source reference
public void DealDamage(Transform target, int amount)
{
    var health = target.GetComponent<HealthComponent>();
    if (health != null)
    {
        bool damageDealt = health.TakeDamage(amount, transform, DamageType.Physical);
        
        if (damageDealt)
        {
            // Trigger feedback through static API
            CombatFeedbackManager.FeedbackAPI.ApplyDamageFeedback(
                amount, target, transform
            );
        }
    }
}
```

```csharp
// ✅ Correct: Implement IDamageable with resistance/weakness logic
public bool TakeDamage(int amount, Transform source, DamageType damageType = DamageType.Physical)
{
    if (amount <= 0 || IsDead || IsInvulnerable) return false;
    
    // Apply enemy-specific damage modifiers
    if (_enemyDefinition != null)
        amount = _enemyDefinition.CalculateEffectiveDamage(amount, damageType);
    
    _currentHealth = Mathf.Max(0, _currentHealth - amount);
    
    // Update GameState and fire events
    if (_gameState != null)
        _gameState.SetInt(_healthFlagKey, _currentHealth);
    
    _onHealthChanged?.Raise();
    return true;
}
```

### Enemy AI State Management

```csharp
// ✅ Correct: Clean state transitions with proper cleanup
public override EnemyState CheckTransitions()
{
    // Check highest priority transitions first
    if (Owner.HealthComponent.IsDead)
        return StateMachine.GetState<DeathState>();
        
    if (IsPlayerInRange(Owner.Definition.AttackRange))
        return StateMachine.GetState<AttackState>();
        
    if (IsPlayerInRange(Owner.Definition.DetectionRange) && 
        HasLineOfSightToPlayer(Owner.Definition.DetectionRange))
        return StateMachine.GetState<ChaseState>();
        
    // Stay in current state if no transition conditions met
    return null;
}
```

### IPullable Integration (Critical for Gadgets as Grammar)

```csharp
// ✅ Correct: Implement IPullable with satisfying feedback
public void PullToward(Vector2 targetPosition, float speed)
{
    if (_isBeingPulled) return;
    
    _isBeingPulled = true;
    
    // The "Shield Rip" moment - disable shield immediately
    SetShieldActive(false);
    
    // Apply visual/audio feedback for impact
    PlayShieldRipEffects();
    
    // Apply knockback toward pull source
    var knockback = GetComponent<KnockbackReceiver>();
    if (knockback != null)
    {
        Vector2 pullDirection = (targetPosition - (Vector2)transform.position).normalized;
        knockback.ApplyKnockback(pullDirection, speed, 0.5f);
    }
}

public void OnPullComplete()
{
    // Force transition to vulnerable state
    if (StateMachine != null)
        StateMachine.TransitionTo<StunnedState>();
        
    // Heavy feedback for satisfying impact
    CombatFeedbackManager.FeedbackAPI.ApplyDamageFeedback(2, transform, null);
}
```

### Economy Integration Patterns

```csharp
// ✅ Correct: 100% drop rate with GameState integration
public void DropLuni()
{
    int amount = _dropTable.GenerateDropAmount(); // Always > 0
    
    if (_spawnPhysicalPickups)
    {
        SpawnPhysicalPickups(transform.position, amount);
    }
    else
    {
        // Direct GameState integration for immediate feedback
        int current = _gameState.GetInt("Player_Luni", 0);
        _gameState.SetInt("Player_Luni", current + amount);
        _onLuniCollected?.Raise();
    }
}
```

### MVC Pattern for UI (Merchant System)

```csharp
// ✅ Correct: Controller handles ALL logic, Views only display + fire events
// MerchantPanel (Controller)
public void OnItemClicked(ItemDefinitionSO item)
{
    // ALL business logic here
    int playerLuni = _gameState.GetInt("Player_Luni", 0);
    
    if (playerLuni < item.Cost)
    {
        ShowFeedbackMessage(_merchant.InsufficientFundsMessage);
        _onPurchaseFailed?.Raise();
        return;
    }
    
    // Execute transaction
    _gameState.SetInt("Player_Luni", playerLuni - item.Cost);
    item.ApplyEffect(_gameState);
    
    RefreshShopDisplay(); // Update all Views
}

// ShopItemSlot (View) - NO business logic
private void OnButtonClicked()
{
    // Delegate everything to Controller
    _merchantPanel?.OnItemClicked(_item);
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
```

### Modern Unity 6 Patterns

```csharp
// ✅ Use FindFirstObjectByType (not FindObjectOfType)
_gameState = FindFirstObjectByType<GameStateSO>();

// ✅ Use Awaitable for async operations (not Coroutines)
await Awaitable.WaitForSecondsAsync(duration, destroyCancellationToken);

// ✅ Proper cancellation token handling for UI
_feedbackCTS?.Cancel();
_feedbackCTS = new CancellationTokenSource();
await Awaitable.WaitForSecondsAsync(duration, _feedbackCTS.Token);
```

---

## ⚙️ Administrator Configuration

### Scene Setup Checklist

**Combat Scene Requirements:**
- [ ] `CombatFeedbackManager` in scene
- [ ] `HitstopController` and `ScreenShakeController` components 
- [ ] Main camera has Cinemachine or manual shake support
- [ ] Combat events created in `Data/Events/Combat/`
- [ ] Default combat settings ScriptableObject configured

**Enemy Setup Checklist:**
- [ ] Enemy prefab has kinematic Rigidbody2D
- [ ] All combat components: `HealthComponent`, `KnockbackReceiver`, `InvincibilityController`
- [ ] Enemy definition, behavior settings, telegraph settings assigned
- [ ] `LuniDropper` component with drop table reference
- [ ] Proper collision layers set for detection/line-of-sight

**Economy Setup Checklist:**  
- [ ] GameState configured with `Player_Luni` flag
- [ ] LuniPickup prefab with magnet/collection behavior
- [ ] Merchant inventory ScriptableObjects created
- [ ] Shop item definitions with proper unique flags
- [ ] Merchant UI prefab with MVC components properly wired

### Debug Tools

**Combat Debug Commands:**
```csharp
// In Inspector or Debug console
CombatFeedbackManager.FeedbackAPI.ApplyDamageFeedback(3, target, source);
hitstopController.ApplyHitstop(10); // 10 frames
screenShakeController.Shake(1.5f, 0.5f); // intensity, duration
```

**Enemy AI Debug:**
- Enemy gizmos show detection/attack ranges when selected
- State machine displays current state in Scene view labels
- Telegraph progress indicators show timing in editor

**Economy Debug:**
- Merchant inventories have validation menu: **Tools → Greenlight → Economy → Validate All Merchant Inventories**
- LuniDropper shows estimated drops in Scene view
- GameState inspector shows current Luni amount

### Performance Considerations

**Enemy Count Guidelines:**
- **Max Active Enemies**: 8-12 per scene (state machine overhead)
- **Telegraph Effects**: Pool particle systems for reuse
- **Luni Pickups**: Auto-cleanup after 30 seconds to prevent accumulation

**Combat Feedback Optimization:**
- Hitstop controller uses `Time.timeScale` - limit concurrent hitstops
- Screen shake uses manual camera manipulation (lighter than Cinemachine impulse)
- Combat events use ScriptableObject channels (lighter than UnityEvents)

**UI Performance:**
- Merchant panels refresh only on currency change, not every frame
- Shop item slots cache affordability state to prevent redundant calculations
- Wallet display uses object pooling for number animation

---

## 📂 File Structure

### 🏗️ Code & Data
```
Assets/_Greenlight/
├── Scripts/
│   ├── Combat/                 # Combat Foundation Assembly
│   │   ├── Greenlight.Combat.asmdef
│   │   ├── Interfaces/
│   │   │   └── IDamageable.cs
│   │   ├── Data/
│   │   │   ├── CombatSettingsSO.cs
│   │   │   └── CombatFeedbackSettingsSO.cs
│   │   ├── Runtime/
│   │   │   ├── HealthComponent.cs
│   │   │   ├── KnockbackReceiver.cs
│   │   │   └── InvincibilityController.cs
│   │   └── Feedback/
│   │       ├── CombatFeedbackManager.cs
│   │       ├── HitstopController.cs
│   │       └── ScreenShakeController.cs
│   ├── AI/                     # Enemy AI Assembly
│   │   ├── Greenlight.AI.asmdef
│   │   ├── Data/
│   │   │   ├── EnemyDefinitionSO.cs
│   │   │   ├── EnemyBehaviorSettingsSO.cs
│   │   │   └── TelegraphSettingsSO.cs
│   │   ├── Runtime/
│   │   │   └── EnemyTelegraph.cs
│   │   ├── StateMachine/
│   │   │   ├── EnemyState.cs
│   │   │   ├── EnemyStateMachine.cs
│   │   │   └── States/
│   │   │       ├── IdleState.cs
│   │   │       ├── ChaseState.cs
│   │   │       ├── AttackState.cs
│   │   │       ├── StunnedState.cs
│   │   │       └── DeathState.cs
│   │   └── Enemies/
│   │       ├── EnemyController.cs
│   │       ├── EnemyVisuals.cs
│   │       ├── SlimeController.cs
│   │       └── ShieldedEnemyController.cs
│   ├── Economy/               # Economy System Assembly
│   │   ├── Greenlight.Economy.asmdef
│   │   ├── Data/
│   │   │   ├── LuniDropTableSO.cs
│   │   │   ├── ItemDefinitionSO.cs
│   │   │   └── MerchantInventorySO.cs
│   │   └── Runtime/
│   │       ├── LuniDropper.cs
│   │       ├── LuniPickup.cs
│   │       └── MerchantInteractable.cs
│   └── UI/                    # UI System Assembly
│       ├── Greenlight.UI.asmdef
│       └── Merchant/
│           ├── MerchantPanel.cs      # MVC Controller
│           ├── ShopItemSlot.cs       # MVC View
│           └── PlayerWalletDisplay.cs # MVC View
├── Data/
│   ├── Combat/
│   │   └── CombatSettings.asset
│   ├── Enemies/
│   │   ├── Definitions/
│   │   │   ├── Slime.asset
│   │   │   └── ShieldedGuardian.asset
│   │   ├── Behaviours/
│   │   │   └── DefaultEnemyBehaviour.asset
│   │   └── Telegraphs/
│   │       └── SlimeTelegraph.asset
│   └── Events/
│       └── Combat/
│           ├── OnEntityDefeated.asset
│           ├── OnEntityHurt.asset
│           ├── OnHealthChanged.asset
│           └── OnPlayerDefeated.asset
└── Prefabs/
    └── Enemies/
        ├── Slime.prefab
        └── ShieldedGuardian.prefab
```

### 🎨 Art Assets
```
Assets/Art/Sprites/
├── 🤺 Actors/
│   └── 👹 Enemies/
├── 🏺 Interactives/         (Objects linked to Global Game State)
│   ├── Destructibles/
│   └── QuestItems/
└── 🖥️ UI/                   (HUD, Icons, and Menus)
    ├── 📋 Icons/
    └── 🖼️ HUD/
```

---

## 🔗 Integration with Previous Phases

**Phase 1 (Nervous System) Dependencies:**
- `GameStateSO` for health, Luni currency, purchase flags
- `WorldFlagChangedEventSO` for merchant item tracking
- `StateResponder` pattern for input locking during combat/shopping

**Phase 2 (Vocabulary) Dependencies:**
- `IPullable` interface for grapple hook combat integration
- `GadgetUser` movement locking for combat state management  
- Player prefab extension with combat components

**Phase 3 Provides for Phase 4:**
- Complete combat foundation for environmental puzzles
- Economy system for tool-gated progression
- Telegraph/feedback patterns for environmental interaction cues

---

## 🚨 Common Gotchas

### Combat Timing Issues
**Problem**: Knockback feels "slippery" or doesn't apply correctly
**Solution**: Ensure hitstop completes BEFORE knockback. Check `CombatFeedbackManager` sequencing.

### Enemy Detection Problems  
**Problem**: Enemies see through walls or don't detect player
**Solution**: Verify line-of-sight layers in `EnemyBehaviorSettingsSO`. Use Debug.DrawRay for raycasts.

### Economy Sync Issues
**Problem**: Shop shows wrong prices or purchased items reappear
**Solution**: Ensure `RefreshShopDisplay()` called after every GameState change. Check unique item flag references.

### Performance Drops in Combat
**Problem**: Framerate drops during intense combat
**Solution**: Limit concurrent hitstop effects. Pool particle systems. Check enemy count per scene.

### Telegraph Timing Feels Wrong
**Problem**: Attacks feel too fast/slow or unfair
**Solution**: Sweet spot is 0.5-1.2s for most attacks. Test with multiple players to validate timing.

---

*This documentation represents the current state of Phase 3 implementation. Update this file when making architectural changes that affect designer workflows or system integration.*