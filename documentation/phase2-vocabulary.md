# Phase 2: The Vocabulary (Core Mechanics) - Technical Documentation

**Version 1.0** | *Phase 2 Architecture*

The "Vocabulary" is the second foundational phase of Greenlight. It implements the **Responsive Character Controller**, **Modular Gadget Framework**, and the **Grappling Hook** as the first verb. It fulfills the core pillar: *"Gadgets as Grammar."*

---

## 🧩 Core Concepts

### 1. Responsive Character Controller

The player controller is built with three distinct components following Separation of Concerns:

- **PlayerController**: Routes input to subsystems, respects input lock flags
- **PlayerMotor**: Handles physics-based movement with kinematic Rigidbody2D
- **PlayerVisuals**: Manages sprite rendering and pixel-perfect snapping (32 PPU)

**Key Design Decisions:**
- Kinematic Rigidbody2D prevents "slippery" modern physics feel
- Movement expressed in pixels/second, converted to units automatically
- Pixel snapping happens in `LateUpdate` after all physics
- Input locking integrated with Nervous System via `StateResponder`

### 2. Modular Gadget Framework (Data + Behavior Pairs)

Gadgets follow a strict **Brain + Body** separation:

| Component | Type | Role |
|-----------|------|------|
| **Brain** (GadgetDefinitionSO) | ScriptableObject | Holds metadata: name, icon, mana cost, prefab reference |
| **Body** (GadgetBehaviour) | MonoBehaviour on Prefab | Handles physics, VFX, scene interactions |

**Why This Architecture?**
- **Designer Workflow**: Create gadget variants by duplicating ScriptableObjects
- **Engine Workflow**: Physics/VFX handled by instantiated prefabs with direct scene access
- **No Bloat**: Data assets are cheap; complex prefabs only instantiated when needed

### 3. The Grappling Hook (First Verb)

The grappling hook demonstrates the "Lock and Key" philosophy:
- **Traversal**: Pull player to GrapplePoints (environment markers)
- **Combat**: Pull IPullable enemies toward player
- **Three-Phase Timing**: Hook extends visually BEFORE player moves

---

## 🛠️ How to Use (For Designers & Developers)

### Scenario A: Setting Up the Player

1. **Create Player Settings Asset**:
   - Navigate to `Assets/_Greenlight/Data/Player/`
   - Right-click → **Create** → **Greenlight** → **Player** → **Player Settings**
   - Name it `DefaultPlayerSettings`
   - Set **Move Speed Pixels Per Second** to `96` (3 tiles/sec at 32 PPU)

2. **Add Components to Player GameObject**:
   - Create a parent GameObject named **Player**
   - Create a child GameObject named **Visuals** (Add **Sprite Renderer** here)
   - On **Player** (Parent):
     - Add **PlayerMotor** component
     - Add **PlayerVisuals** component
     - Add **PlayerController** component
     - Add **GadgetUser** component
     - Add **Rigidbody2D** (Set **Body Type** to `Kinematic`, **Interpolation** to `None` for retro feel)
     - Add **PlayerInput** component

3. **Configure Components**:
   - **Player Visuals**: Drag the **Visuals** child into **Sprite Transform** and **Sprite Renderer** slots.
   - **Player Controller**:
     - Assign dependencies (Motor, Visuals, Gadget User)
     - Assign **Game State** (MasterGameState) & **On Flag Changed** (OnWorldFlagChanged)
   - **Gadget User**: Drag `PlayerInventory` into **Inventory** slot.

4. **Configure Input System**:
   - On **PlayerInput**:
     - Set **Actions** to `InputSystem_Actions`
     - Set **Behavior** to `Invoke Unity Events`
     - Expand **Events** → **Player** (you may need to expand the foldout)
     - Wire up callbacks:
       - `OnMove` → `PlayerController.OnMove`
       - `OnSprint` → `PlayerController.OnSprint`
       - `OnUseGadget` → `PlayerController.OnUseGadget`
       - `OnNext` → `PlayerController.OnNext`
       - `OnPrevious` → `PlayerController.OnPrevious`

### Scenario B: Creating a New Gadget

1. **Create Gadget Definition Asset**:
   - Navigate to `Assets/_Greenlight/Data/Gadgets/Definitions/`
   - Right-click → **Create** → **Greenlight** → **Gadgets** → **Gadget Definition**
   - Name it (e.g., `GrapplingHook`)
   - Fill in Name, Icon, and Mana Cost

2. **Create Behaviour Prefab**:
   - Create empty GameObject `GrapplingHookBehaviour`
   - Add **Grappling Hook Behaviour** component
   - Add **Line Renderer** (Set positions size to 2, width to 0.05)
   - Create child `HookHead` with a small Sprite Renderer
   - **Important**: Set **Grappleable Layers** mask to include the `Grappleable` layer (see Scenario C)
   - Drag to `Assets/_Greenlight/Prefabs/Gadgets/` to create prefab

3. **Link Definition to Behaviour**:
   - Select the Definition asset
   - Drag the Behaviour prefab into **Behaviour Prefab** slot

4. **Add to Player Inventory** (for testing):
   - Open `Assets/_Greenlight/Data/Gadgets/PlayerInventory.asset`
   - Add the new Definition to **Acquired Gadgets** list so you start with it

### Scenario C: Placing Grapple Points

1. **Configure Layers**:
   - Go to **Layers** → **Edit Layers...**
   - Add a User Layer named `Grappleable`

2. **Create Grapple Point**:
   - Create empty GameObject `TestHookPoint`
   - Add **GrapplePoint** component
   - Add **BoxCollider2D** (Required for raycast detection!)
   - **Set Layer** to `Grappleable`
   - Position in world

3. **Visual Feedback**:
   - Grapple points show cyan gizmos in editor

---

## 💻 Coding Standards

### 1. Creating a Custom Gadget Behaviour

```csharp
using UnityEngine;
using Greenlight.Gadgets;

namespace Greenlight.Gadgets
{
    public class LanternBehaviour : GadgetBehaviour
    {
        [SerializeField] private Light2D _light;
        [SerializeField] private float _radius = 5f;

        public override void Initialize(GadgetDefinitionSO definition, GadgetUser user)
        {
            base.Initialize(definition, user);
            // Custom initialization
            if (_light != null)
            {
                _light.enabled = false;
            }
        }

        public override bool CanExecute(GadgetExecutionContext context)
        {
            // Check mana, cooldowns, etc.
            return base.CanExecute(context);
        }

        public override void Execute(GadgetExecutionContext context)
        {
            if (IsExecuting) return;
            IsExecuting = true;

            // Toggle light
            if (_light != null)
            {
                _light.enabled = !_light.enabled;
            }

            IsExecuting = false;
        }

        public override void Terminate()
        {
            base.Terminate();
            if (_light != null)
            {
                _light.enabled = false;
            }
        }
    }
}
```

### 2. Implementing IPullable for Enemies

```csharp
using UnityEngine;
using Greenlight.Gadgets;

public class PullableEnemy : MonoBehaviour, IPullable
{
    [SerializeField] private Rigidbody2D _rb;
    private Vector2 _pullTarget;
    private float _pullSpeed;
    private bool _isPulling;

    public void PullToward(Vector2 targetPosition, float speed)
    {
        _pullTarget = targetPosition;
        _pullSpeed = speed;
        _isPulling = true;
    }

    public void OnPullComplete()
    {
        _isPulling = false;
        // Trigger stun, damage, or other effects
    }

    private void FixedUpdate()
    {
        if (_isPulling && _rb != null)
        {
            Vector2 direction = (_pullTarget - _rb.position).normalized;
            Vector2 newPos = _rb.position + direction * _pullSpeed * Time.fixedDeltaTime;
            _rb.MovePosition(newPos);

            // Check if reached target
            if (Vector2.Distance(_rb.position, _pullTarget) < 0.1f)
            {
                OnPullComplete();
            }
        }
    }
}
```

### 3. Pixel-Perfect Movement Calculations

```csharp
// Always express speeds in pixels per second
public float MoveSpeedPixels = 96f; // 3 tiles/sec at 32 PPU

// Convert to units for physics
public float MoveSpeedUnits => MoveSpeedPixels / 32f; // 3.0 units/sec

// Snap positions to pixel grid
Vector3 pos = transform.position;
pos.x = Mathf.Round(pos.x * 32f) / 32f;
pos.y = Mathf.Round(pos.y * 32f) / 32f;
transform.position = pos;
```

---

## ⚙️ Administrator Configuration

### Initial Setup Checklist

If setting up Phase 2 in a new scene, ensure:

1. **Player GameObject** exists with all components:
   - PlayerMotor (references PlayerSettingsSO)
   - PlayerVisuals (references sprite transform)
   - PlayerController (references Motor, Visuals, GadgetUser, GameState)
   - GadgetUser (references InventorySO)
   - PlayerInput (wired to PlayerController callbacks)
   - Rigidbody2D (Kinematic, configured by PlayerMotor)

2. **Data Assets** exist:
   - `DefaultPlayerSettings.asset` in `Data/Player/`
   - `PlayerInventory.asset` in `Data/Gadgets/`
   - At least one GadgetDefinitionSO in `Data/Gadgets/Definitions/`

3. **Event Assets** exist in `Data/Events/Gadgets/`:
   - `OnGadgetUsed.asset` (GameEventSO)
   - `OnGadgetSwapped.asset` (GameEventSO)
   - `OnGadgetAcquired.asset` (GameEventSO)
   - `OnGrappleTraversal.asset` (GameEventSO)
   - `OnGrapplePull.asset` (GameEventSO)
   - `OnGrappleWhiff.asset` (GameEventSO)

4. **Input Actions** configured:
   - `UseGadget` action exists in InputSystem_Actions
   - Bindings: Gamepad West (X), Mouse Left, Touch Tap, Keyboard Space

5. **Layers** configured:
   - Create "Grappleable" layer for GrapplePoints
   - Configure GrapplingHookBehaviour's **Grappeable Layers** mask

### Creating Event Assets (Manual Steps)

Since event assets must be created in Unity Editor:

1. Navigate to `Assets/_Greenlight/Data/Events/`
2. Create subfolder `Gadgets/`
3. For each event:
   - Right-click → **Create** → **Greenlight** → **Events** → **Game Event**
   - Name according to list above
   - Add description in Inspector

### Debugging Tools

- **PlayerController Debug Log**: Enable to see input events
- **PlayerMotor Debug Log**: Enable to see movement velocity in pixels/sec
- **GadgetUser Debug Log**: Enable to see gadget equip/use events
- **Gizmos**: GrapplePoints show cyan spheres in Scene view

---

## 📂 File Structure

```
Assets/_Greenlight/
├── Scripts/
│   ├── Player/
│   │   ├── PlayerController.cs           # Input routing + state orchestration
│   │   ├── PlayerMotor.cs                # Kinematic physics movement
│   │   ├── PlayerSettingsSO.cs           # Movement constants (32 PPU)
│   │   ├── PlayerVisuals.cs              # Pixel snapping + facing direction
│   │   └── Greenlight.Player.asmdef
│   ├── Gadgets/
│   │   ├── Data/
│   │   │   ├── GadgetDefinitionSO.cs     # Brain (ScriptableObject)
│   │   │   └── InventorySO.cs            # Gadget collection
│   │   ├── Behaviours/
│   │   │   ├── GadgetBehaviour.cs        # Body (abstract MonoBehaviour)
│   │   │   └── GrapplingHookBehaviour.cs # First verb implementation
│   │   ├── Runtime/
│   │   │   ├── GadgetUser.cs             # Equip/execute gadgets
│   │   │   └── GadgetExecutionContext.cs # Execution payload
│   │   ├── Interfaces/
│   │   │   └── IPullable.cs              # Interface for pullable objects
│   │   └── Greenlight.Gadgets.asmdef
│   └── Environment/
│       ├── GrapplePoint.cs               # Marker for grapple targets
│       └── Greenlight.Environment.asmdef
├── Data/
│   ├── Player/
│   │   └── DefaultPlayerSettings.asset
│   ├── Gadgets/
│   │   ├── Definitions/
│   │   │   └── GrapplingHook.asset       # GadgetDefinitionSO
│   │   └── PlayerInventory.asset         # InventorySO
│   └── Events/
│       └── Gadgets/
│           ├── OnGadgetUsed.asset
│           ├── OnGadgetSwapped.asset
│           ├── OnGadgetAcquired.asset
│           ├── OnGrappleTraversal.asset
│           ├── OnGrapplePull.asset
│           └── OnGrappleWhiff.asset
└── Prefabs/
    ├── Player/
    │   └── Player.prefab
    └── Gadgets/
        └── GrapplingHook.prefab          # Has GrapplingHookBehaviour + LineRenderer
```

### Assembly References

`Greenlight.Player.asmdef` references:
- `Greenlight.Core`
- `Greenlight.Gadgets`
- `Unity.InputSystem`

`Greenlight.Gadgets.asmdef` references:
- `Greenlight.Core`
- `Greenlight.Environment`

`Greenlight.Environment.asmdef` references:
- `Greenlight.Core`

---

## 🎯 32 PPU Compliance

All Phase 2 systems adhere to the 32 Pixels Per Unit standard:

- ✅ `PlayerSettingsSO.MoveSpeedPixelsPerSecond` expressed in pixels (96 = 3 tiles/sec)
- ✅ `PlayerMotor` uses `Rigidbody2D.MovePosition()` not velocity
- ✅ `PlayerVisuals.LateUpdate()` snaps sprite to pixel grid (round to 1/32)
- ✅ `GrapplingHookBehaviour` speeds/distances defined in pixels, converted to units
- ✅ No `Time.deltaTime` drift accumulation in movement
- ✅ All LayerMasks configured to filter appropriately

---

## 🔗 Integration with Phase 1 (Nervous System)

Phase 2 builds on Phase 1's architecture:

1. **PlayerController extends StateResponder**:
   - Registers with StateRegistry on `OnEnable`
   - Watches `System_Input_Locked` flag
   - Respects input lock during cutscenes/dialogue

2. **GadgetDefinitionSO references WorldFlagDefinitionSO**:
   - Acquisition tracked via `Gadget_Grapple_Acquired` flag
   - GameState integration for save/load

3. **Event-Driven Feedback**:
   - Gadget events (OnGadgetUsed, etc.) follow GameEventSO pattern
   - Decoupled listeners for UI, Audio, VFX

---

## 🚀 Next Steps (Phase 3: Combat & AI)

With Phase 2 complete, the foundation is ready for:

- **Tactical AI**: Enemies that react to gadget usage
- **Combat Feedback**: Hitstop, screenshake, audio cues
- **Economy Loop**: Luni drops and merchant interactions
- **IPullable Enemies**: Implement interface on enemy types

---

*This document follows the "Gemmy" documentation style. For questions or clarifications, refer to `documentation/documentation-guidelines.md`.*
