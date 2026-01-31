# Character Animation Workflow: 4-Direction System

**Version 1.0** | *Phase 2 - The Vocabulary*

This document defines the **4-direction animation system** for Player, NPC, and Enemy characters in Greenlight. The system uses cardinal directions (Down, Up, Side) with directional state names, providing a consistent workflow from sprite creation to runtime animation control.

---

## 🧩 Core Concepts

### 1. TopDownFacing System

**Type**: Static Helper Class  
**Location**: `Assets/_Greenlight/Scripts/Core/Animation/TopDownFacing.cs`  
**Function**: Converts Vector2 movement directions into cardinal directions and sprite flip decisions.

The system uses three cardinal directions:

| Cardinal | When Used | Flip Logic |
|----------|-----------|------------|
| **Down** | abs(y) > abs(x) AND y < 0 | Never flip |
| **Up** | abs(y) > abs(x) AND y > 0 | Never flip |
| **Side** | abs(x) >= abs(y) | Flip when x < 0 (facing left) |

### 2. State Naming Contract

All character types use consistent animation state names:

| Character Type | Idle States | Movement States |
|----------------|-------------|-----------------|
| **Player** | `IdleDown`, `IdleUp`, `IdleSide` | `MoveDown`, `MoveUp`, `MoveSide` |
| **NPC** | `IdleDown`, `IdleUp`, `IdleSide` | `MoveDown`, `MoveUp`, `MoveSide` |
| **Enemy** | `IdleDown`, `IdleUp`, `IdleSide` | `ChaseDown`, `ChaseUp`, `ChaseSide` |

**Legacy Support**: Enemies preserve existing single-direction states (`Attack`, `Telegraph`, `Stunned`, `Death`, `Recovery`, `Alert`) for backward compatibility.

### 3. Component Architecture

| Component | Purpose | Location |
|-----------|---------|----------|
| **PlayerAnimationDriver** | Drives Player animations based on PlayerMotor + PlayerVisuals | Player Assembly |
| **EnemyVisuals** | Enhanced with directional support; driven by AI state machine | AI Assembly |
| **NpcAnimationDriver** | Reusable driver for any NPC type | Core Assembly |

### 4. Animator Placement

**Standard 2D Workflow**: Place `Animator` component on the **`Visuals` child object**, not the physics root. This keeps animation separate from physics and collision components.

```
Player (Root)
├── Rigidbody2D, Colliders, Scripts
└── Visuals (Child)
    ├── Animator ← Place here
    └── SpriteRenderer
```

---

## 🛠️ How to Use

### Scenario A: Setting Up Player Animation

#### Step 1: Prepare Sprite Sheets (32 PPU Standard)

1. **Project Window**: Select your Player sprite sheet texture
2. **Inspector → Texture Import Settings**:
   - **Texture Type**: `Sprite (2D and UI)`
   - **Sprite Mode**: `Multiple`
   - **Pixels Per Unit**: `32`
   - **Filter Mode**: `Point (no filter)`
   - **Compression**: `None`
3. **Sprite Editor**: Slice frames → confirm consistent pivots → **Apply**

#### Step 2: Create Animation Clips

**Folder Structure**: `Assets/_Greenlight/Art/Animations/Player/`

Create these clips with sprites:

| Clip Name | Loop | Sprite Direction | Notes |
|-----------|------|------------------|-------|
| `IdleDown.anim` | ✓ | Down-facing | Default state |
| `IdleUp.anim` | ✓ | Up-facing | |
| `IdleSide.anim` | ✓ | Right-facing | Code handles left flip |
| `MoveDown.anim` | ✓ | Down-facing walk | |
| `MoveUp.anim` | ✓ | Up-facing walk | |
| `MoveSide.anim` | ✓ | Right-facing walk | Code handles left flip |

#### Step 3: Create Animator Controller

1. **Right-click** → **Create → Animator Controller** → name `Player.controller`
2. **Animator Window**: Create states with exact names:
   - `IdleDown` (set as **default state**)
   - `IdleUp`, `IdleSide`
   - `MoveDown`, `MoveUp`, `MoveSide`
3. **Assign Motions**: Drag each `.anim` clip to matching state
4. **No transitions needed** - code switches states directly

#### Step 4: Configure Player Prefab

1. **Open**: `Assets/_Greenlight/Prefabs/Player/Player.prefab` (Prefab Mode)
2. **Select**: `Visuals` child object
3. **Add Component**: `Animator`
4. **Assign**: Controller = `Player.controller`
5. **Select**: `Player` root
6. **Add Component**: `PlayerAnimationDriver`
7. **Save Prefab**

### Scenario B: Setting Up Enemy Animation

#### Follow Steps 1-2 from Player setup, but create:

| Clip Name | Loop | Notes |
|-----------|------|-------|
| `IdleDown.anim` | ✓ | Enemy idle states |
| `IdleUp.anim`, `IdleSide.anim` | ✓ | |
| `ChaseDown.anim` | ✓ | Enemy uses "Chase" not "Move" |
| `ChaseUp.anim`, `ChaseSide.anim` | ✓ | |
| `Attack.anim` | ✗ | Single-direction (for now) |
| `Telegraph.anim` | ✗ | Attack warning |
| `Stunned.anim` | ✓ | |
| `Death.anim` | ✗ | |
| `Recovery.anim` | ✗ | |
| `Alert.anim` | ✗ | Detection reaction |

#### Enemy Prefab Configuration

1. **Open**: Enemy prefab (`Slime.prefab`, `ShieldedGuardian.prefab`)
2. **Select**: `Visuals` child → **Add**: `Animator` → **Assign**: controller
3. **Root**: `EnemyVisuals` component already exists (no changes needed)
4. **Save Prefab**

### Scenario C: Setting Up NPC Animation

**NPCs use same clips as Player** (Idle/Move pattern).

1. **Create**: NPC prefab with `Visuals` child structure
2. **Add**: `NpcAnimationDriver` to root
3. **Controller**: Use Player controller or create NPC-specific one
4. **Drive from script**:

```csharp
// In your NPC controller
var animDriver = GetComponent<NpcAnimationDriver>();
animDriver.SetMovement(movementVelocity); // Updates animation automatically
```

---

## 💻 Coding Standards

### Hash-Based Animation Performance

**✅ Correct - Cache state hashes**:

```csharp
private static readonly int IdleDownHash = Animator.StringToHash("IdleDown");

// In animation method
_animator.Play(IdleDownHash); // Fast hash lookup
```

**❌ Wrong - String-based calls**:

```csharp
_animator.Play("IdleDown"); // Slow string comparison every call
```

### Graceful State Fallback

**✅ Correct - Check state exists before playing**:

```csharp
private bool HasAnimatorState(int stateHash)
{
    if (_animator?.runtimeAnimatorController == null)
        return false;
        
    for (int i = 0; i < _animator.layerCount; i++)
    {
        if (_animator.HasState(i, stateHash))
            return true;
    }
    return false;
}

private void PlayAnimationState(int stateHash, string stateName)
{
    if (!HasAnimatorState(stateHash))
    {
        Debug.LogWarning($"State '{stateName}' not found - add to controller");
        return; // Don't crash
    }
    _animator.Play(stateHash);
}
```

### State Change Optimization

**✅ Correct - Only play when state changes**:

```csharp
private int _currentStateHash = -1;

private void Update()
{
    int desiredState = GetDesiredStateHash();
    if (desiredState != _currentStateHash)  // Only when changed
    {
        _animator.Play(desiredState);
        _currentStateHash = desiredState;
    }
}
```

**❌ Wrong - Playing every frame**:

```csharp
private void Update()
{
    _animator.Play(GetDesiredStateHash()); // Restarts clip every frame!
}
```

### Component Reference Caching

**✅ Correct - Cache in Awake()**:

```csharp
private Animator _animator;

private void Awake()
{
    _animator = GetComponentInChildren<Animator>(); // Cache once
}

private void Update()
{
    if (_animator != null) // Use cached reference
        _animator.Play(stateHash);
}
```

---

## ⚙️ Administrator Configuration

### Prefab Setup Checklist

| Component | Location | Required Fields | Notes |
|-----------|----------|-----------------|-------|
| **Player** | Root | `PlayerAnimationDriver` | Auto-finds Animator in children |
| **Enemy** | Root | `EnemyVisuals` | Enhanced with directional support |
| **NPC** | Root | `NpcAnimationDriver` | Reusable for any NPC type |
| **Animator** | `Visuals` child | Controller assigned | Keep separate from physics root |

### Required Animator Controller States

**Minimum states for each character type**:

| Character | Required States |
|-----------|-----------------|
| **Player** | `IdleDown` (default), `IdleUp`, `IdleSide`, `MoveDown`, `MoveUp`, `MoveSide` |
| **Enemy** | Above + `ChaseDown`, `ChaseUp`, `ChaseSide`, `Attack`, `Telegraph`, `Stunned`, `Death`, `Recovery`, `Alert` |
| **NPC** | Same as Player |

### Debug Tools

#### Animation State Inspector

Add to any animation driver for runtime debugging:

```csharp
[Header("Debug")]
[SerializeField] private bool _debugLog = false;

#if UNITY_EDITOR
public string GetDebugInfo()
{
    return $"State: {_currentStateName} | Cardinal: {_currentCardinal} | Moving: {_isMoving}";
}
#endif
```

#### Gizmo Facing Direction

`PlayerVisuals` includes facing direction gizmos:

```csharp
private void OnDrawGizmosSelected()
{
    if (Application.isPlaying)
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, _lastFacingDirection * 0.5f);
    }
}
```

### Troubleshooting Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| "State not found" warnings | Missing states in controller | Add all required states to Animator Controller |
| Animations don't change | Driver not added to prefab | Add animation driver component to character root |
| Sprite doesn't flip | Flip settings disabled | Check `_flipSpriteOnDirection` in driver component |
| Animations restart constantly | No state change check | Driver handles this - ensure using provided components |

---

## 📂 File Structure

### Recommended Animation Assets Organization

```
Assets/_Greenlight/
├── Scripts/
│   ├── Core/
│   │   └── Animation/
│   │       ├── TopDownFacing.cs           ← Shared helper
│   │       └── NpcAnimationDriver.cs      ← Reusable NPC driver
│   ├── Player/
│   │   ├── PlayerAnimationDriver.cs       ← Player-specific driver
│   │   └── PlayerVisuals.cs               ← Enhanced with cardinal direction
│   └── AI/
│       └── Enemies/
│           └── EnemyVisuals.cs            ← Enhanced with directional support
├── Art/
│   └── Animations/
│       ├── Player/
│       │   ├── IdleDown.anim
│       │   ├── IdleUp.anim
│       │   ├── IdleSide.anim
│       │   ├── MoveDown.anim
│       │   ├── MoveUp.anim
│       │   ├── MoveSide.anim
│       │   └── Player.controller
│       ├── Slime/
│       │   ├── IdleDown.anim, IdleUp.anim, IdleSide.anim
│       │   ├── ChaseDown.anim, ChaseUp.anim, ChaseSide.anim
│       │   ├── Attack.anim, Telegraph.anim, Stunned.anim
│       │   ├── Death.anim, Recovery.anim, Alert.anim
│       │   └── Slime.controller
│       └── ShieldedGuardian/
│           └── (similar structure to Slime)
└── Prefabs/
    ├── Player/
    │   └── Player.prefab                  ← Has Animator on Visuals child + PlayerAnimationDriver
    └── Enemies/
        ├── Slime.prefab                   ← Has Animator on Visuals child
        └── ShieldedGuardian.prefab
```

### Assembly Dependencies

- **Core.Animation** → No dependencies (shared utilities)
- **Player** → Depends on Core (uses TopDownFacing)  
- **AI** → Depends on Core (uses TopDownFacing)

---

## 🚨 Migration Notes

**From Previous Single-Direction System**:

1. **Enemies**: Existing `PlayIdleAnimation()` calls work unchanged - they now automatically choose directional states
2. **Backward Compatibility**: Legacy single states (`Idle`, `Chase`) still work as fallbacks
3. **Graceful Upgrade**: Add directional states to controllers gradually - system warns but doesn't crash if missing