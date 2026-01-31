---
name: Character Idle+Move Animations
overview: Add a shared 4-direction (Up/Down/Side) Idle+Move animation approach for Player/NPCs and wire Enemies to play directional idle/chase animations. Provide Unity Editor step-by-step for creating clips/controllers and assigning them to existing prefabs.
todos:
  - id: core-facing-helper
    content: Add shared TopDownFacing helper in Core/Animation to map Vector2 to Up/Down/Side + flipX.
    status: completed
  - id: player-anim-driver
    content: Add PlayerAnimationDriver that chooses Idle/Move directional animator states based on PlayerMotor + PlayerVisuals facing.
    status: completed
    dependencies:
      - core-facing-helper
  - id: player-visuals-4dir
    content: Update PlayerVisuals to expose FacingCardinal and flip only for Side directions.
    status: completed
    dependencies:
      - core-facing-helper
  - id: enemy-visuals-4dir
    content: Update EnemyVisuals to find Animator in children and play directional Idle*/Chase* states (preserve existing non-directional states).
    status: completed
    dependencies:
      - core-facing-helper
  - id: npc-driver
    content: Add reusable NpcAnimationDriver in Core/Animation that future NPC controllers can drive.
    status: completed
    dependencies:
      - core-facing-helper
  - id: doc-animation-workflow
    content: Create full Gemmy-style documentation (animation-workflow.md) with Core Concepts, How to Use, Coding Standards, and Configuration sections.
    status: completed
    dependencies:
      - core-facing-helper
  - id: doc-registry-update
    content: Update documentation-guidelines.md registry to include animation-workflow.md.
    status: completed
    dependencies:
      - doc-animation-workflow
---

# Add Idle + Movement Animations (Player/NPC/Enemy)

## Current State Assessment

| Entity | Code Ready? | Prefab Ready? | Notes |

|--------|-------------|---------------|-------|

| **Enemy** | Partial | No | AI state machine calls `EnemyVisuals.PlayIdleAnimation()` / `PlayChaseAnimation()`, but prefabs lack `Animator` component |

| **Player** | No | No | `PlayerVisuals` only handles pixel snapping + flip; no `Animator` on prefab |

| **NPC** | N/A | N/A | No NPC system exists yet; we'll provide reusable driver for future use |

## Code Changes (What I Will Implement)

### 1) Shared Top-Down Facing Helper (Core Assembly)

- **New file**: `Assets/_Greenlight/Scripts/Core/Animation/TopDownFacing.cs`
- **Namespace**: `Greenlight.Core.Animation`
- **Purpose**: Convert `Vector2` direction to cardinal enum + flipX decision
```csharp
public enum FacingCardinal { Down, Up, Side }

public static class TopDownFacing
{
    public static (FacingCardinal cardinal, bool flipX) FromVector(Vector2 direction);
    // Side if abs(x) >= abs(y), else Up/Down by sign of y
    // flipX = true when cardinal is Side AND x < 0
}
```


### 2) PlayerAnimationDriver (Player Assembly)

- **New file**: `Assets/_Greenlight/Scripts/Player/PlayerAnimationDriver.cs`
- **Namespace**: `Greenlight.Player`
- **Design**:
  - Cache `Animator` reference in `Awake()` via `GetComponentInChildren<Animator>()`
  - Read `PlayerMotor.IsMoving` and `PlayerVisuals.FacingDirection` in `Update()`
  - Only call `Animator.Play(hash)` when state actually changes (prevents clip restart)
  - Graceful fallback: log warning if state doesn't exist, don't crash

**State Name Contract (Player/NPC)**:

| State | Idle | Moving |

|-------|------|--------|

| Down | `IdleDown` | `MoveDown` |

| Up | `IdleUp` | `MoveUp` |

| Side | `IdleSide` | `MoveSide` |

### 3) PlayerVisuals 4-Direction Update

- **Edit**: `Assets/_Greenlight/Scripts/Player/PlayerVisuals.cs`
- **Changes**:
  - Expose `FacingCardinal CurrentCardinal { get; }` for animation driver
  - Flip logic: `flipX = true` only when cardinal is `Side` AND moving left
  - Up/Down cardinals: `flipX = false` (clean vertical sprites)

### 4) EnemyVisuals Directional States

- **Edit**: `Assets/_Greenlight/Scripts/AI/Enemies/EnemyVisuals.cs`
- **Changes**:
  - Animator lookup: `GetComponentInChildren<Animator>()` (supports Animator on `Visuals` child)
  - Add directional state hashes for Idle/Chase:
    - `IdleDown`, `IdleUp`, `IdleSide`
    - `ChaseDown`, `ChaseUp`, `ChaseSide`
  - **Preserve existing non-directional states**: `Attack`, `Telegraph`, `Stunned`, `Death`, `Recovery`, `Alert` remain single-direction (can be made directional later)
  - `PlayIdleAnimation()` / `PlayChaseAnimation()` select state based on `_currentFacingDirection`
  - Add `HasAnimatorState(int hash)` helper to avoid runtime errors when states don't exist

### 5) NpcAnimationDriver (Core Assembly - Reusable)

- **New file**: `Assets/_Greenlight/Scripts/Core/Animation/NpcAnimationDriver.cs`
- **Namespace**: `Greenlight.Core.Animation`
- **Design**:
  - `[RequireComponent(typeof(SpriteRenderer))]`
  - Public API: `SetMovement(Vector2 velocity)` - call from any NPC controller
  - Uses same state name contract as Player
  - Handles flip logic internally

### 6) Full Gemmy-Style Documentation

- **New file**: `documentation/animation-workflow.md`
- **Structure** (per documentation-guidelines.md):

  1. **Header**: Title, Version 1.0, Abstract
  2. **🧩 Core Concepts**: TopDownFacing enum, state naming contract, Animator placement
  3. **🛠️ How to Use**: Step-by-step for creating clips, controllers, prefab setup
  4. **💻 Coding Standards**: API usage, hash caching, graceful fallbacks
  5. **⚙️ Administrator Configuration**: Prefab checklist, debugging tips
  6. **📂 File Structure**: Animation folder organization

- **Update**: `documentation/documentation-guidelines.md` registry to include new doc

## Unity Editor Steps (Manual, Step-by-Step)

> These steps will be documented in detail in `documentation/animation-workflow.md` (🛠️ How to Use section)

### A) Prepare Sprites (32 PPU Standard)

1. **Project Window**: Select sprite sheet texture (Player or Enemy)
2. **Inspector → Texture Import Settings**:

   - **Texture Type**: `Sprite (2D and UI)`
   - **Sprite Mode**: `Multiple`
   - **Pixels Per Unit**: `32` (project standard)
   - **Filter Mode**: `Point (no filter)`
   - **Compression**: `None`

3. Click **Sprite Editor…** → Slice frames (Grid or Automatic) → Confirm consistent pivots
4. Click **Apply**

### B) Create Animation Clips

**Recommended folder structure**: `Assets/_Greenlight/Art/Animations/<CharacterName>/`

Per character, create these clips:

| Clip Name | Loop Time | Notes |

|-----------|-----------|-------|

| `IdleDown.anim` | Yes | Default idle state |

| `IdleUp.anim` | Yes | |

| `IdleSide.anim` | Yes | Right-facing; code flips for left |

| `MoveDown.anim` | Yes | |

| `MoveUp.anim` | Yes | |

| `MoveSide.anim` | Yes | Right-facing; code flips for left |

**For Enemies only** (additional states):

| Clip Name | Loop Time | Notes |

|-----------|-----------|-------|

| `ChaseDown.anim` | Yes | Replace Move* for enemies |

| `ChaseUp.anim` | Yes | |

| `ChaseSide.anim` | Yes | |

| `Attack.anim` | No | Single-direction for now |

| `Telegraph.anim` | No | Attack warning |

| `Stunned.anim` | Yes | |

| `Death.anim` | No | |

| `Recovery.anim` | No | |

| `Alert.anim` | No | Detection reaction |

### C) Create Animator Controllers

#### Player Controller

1. **Project Window**: Right-click → **Create → Animator Controller** → name `Player.controller`
2. **Animator Window**: Create states with **exact names**:

   - `IdleDown` (set as **default state** - orange)
   - `IdleUp`, `IdleSide`
   - `MoveDown`, `MoveUp`, `MoveSide`

3. Assign each state's **Motion** to matching `.anim` clip
4. **No transitions needed** - code switches states directly via `Animator.Play()`

#### Enemy Controllers (Slime, ShieldedGuardian)

1. Create `Slime.controller`, `ShieldedGuardian.controller`
2. Create states with **exact names**:

   - `IdleDown` (default), `IdleUp`, `IdleSide`
   - `ChaseDown`, `ChaseUp`, `ChaseSide`
   - `Attack`, `Telegraph`, `Stunned`, `Death`, `Recovery`, `Alert`

3. Assign motions to clips

### D) Prefab Configuration

#### Player Prefab

1. **Open**: `Assets/_Greenlight/Prefabs/Player/Player.prefab` (Prefab Mode)
2. **Select child**: `Visuals`
3. **Add Component**: `Animator`
4. **Assign Controller**: `Player.controller`
5. **Select root**: `Player`
6. **Add Component**: `PlayerAnimationDriver` (new script)
7. **Save Prefab**

#### Enemy Prefabs

1. **Open**: `Assets/_Greenlight/Prefabs/Enemies/Slime.prefab`
2. **Select child**: `Visuals`
3. **Add Component**: `Animator`
4. **Assign Controller**: `Slime.controller`
5. **Verify**: `EnemyVisuals` component on root already exists (no changes needed)
6. **Save Prefab**
7. **Repeat** for `ShieldedGuardian.prefab`

### E) Verification Checklist

| Test | Expected Result |

|------|-----------------|

| Move Player (WASD) | Switches between `Move*` and `Idle*` states; direction changes appropriately |

| Player faces left | Sprite flips horizontally (only for Side direction) |

| Player faces up/down | Sprite does NOT flip horizontally |

| Enemy detects player | Switches from `Idle*` to `Chase*` while pursuing |

| Enemy loses player | Returns to `Idle*` state |

| Console | No "state not found" warnings |

---

## Technical Constraints

| Constraint | Rationale |

|------------|-----------|

| **State-name driven** (no Blend Trees) | Per user preference; simpler setup |

| **`Animator.Play(hash)` only on change** | Prevents clip restart every frame |

| **Animator on `Visuals` child** | Standard 2D workflow; keeps physics root clean |

| **Cache animator in `Awake()`** | Performance: no `GetComponent()` in `Update()` |

| **Graceful state fallback** | Log warning but don't crash if state missing |

| **Preserve non-directional enemy states** | Attack/Death/etc. remain single-direction for now |

---

## File Structure (After Implementation)

```
Assets/_Greenlight/
├── Scripts/
│   ├── Core/
│   │   └── Animation/           ← NEW FOLDER
│   │       ├── TopDownFacing.cs
│   │       └── NpcAnimationDriver.cs
│   ├── Player/
│   │   ├── PlayerAnimationDriver.cs  ← NEW
│   │   └── PlayerVisuals.cs          ← MODIFIED
│   └── AI/
│       └── Enemies/
│           └── EnemyVisuals.cs       ← MODIFIED
├── Art/
│   └── Animations/              ← RECOMMENDED
│       ├── Player/
│       │   ├── IdleDown.anim
│       │   ├── IdleUp.anim
│       │   ├── IdleSide.anim
│       │   ├── MoveDown.anim
│       │   ├── MoveUp.anim
│       │   ├── MoveSide.anim
│       │   └── Player.controller
│       ├── Slime/
│       │   └── (clips + controller)
│       └── ShieldedGuardian/
│           └── (clips + controller)
└── Prefabs/
    ├── Player/
    │   └── Player.prefab         ← MODIFIED (add Animator, PlayerAnimationDriver)
    └── Enemies/
        ├── Slime.prefab          ← MODIFIED (add Animator to Visuals child)
        └── ShieldedGuardian.prefab

documentation/
├── animation-workflow.md         ← NEW (full Gemmy-style doc)
└── documentation-guidelines.md   ← MODIFIED (add to registry)
```