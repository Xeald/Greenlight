# Physics & Collision Architecture

**Version 1.0** | *Project Architecture Standard*

This document defines the rigid standards for handling 2D physics, collisions, and movement in Greenlight. Our goal is to achieve a **"High-Fidelity Retro"** feel—smooth sub-pixel simulation (High-Fidelity) with crisp, readable pixel art output (Retro).

---

## 🧩 Core Concepts

### 1. The "Kinematic Control" Philosophy
Unlike standard Unity games that use `Dynamic` Rigidbodies for physics simulation, Greenlight uses **Kinematic Rigidbodies** for the Player and Enemies.
- **Why?** Dynamic bodies "slide," "bounce," and "drift." We want **snappy-but-smooth** movement: responsive stops without chaotic physics.
- **How?** We use `Rigidbody2D.MovePosition` to move, and we **manually check for collisions** before moving.
- **Critical Detail**: Physics movement is allowed to be **smooth (sub-pixel)**. Pixel clarity is enforced by **snapping SpriteRenderers in `LateUpdate`** (visual child only).

### 2. The Collision Layers
The world is divided into strict layers to prevent logic errors (e.g., enemies blocking the player, or projectiles hitting the UI).

| Layer Name | ID (Approx) | Purpose |
| :--- | :--- | :--- |
| **Default** | 0 | Non-colliding visuals, floor tiles. |
| **Player** | (Custom) | The Player object only. |
| **Enemies** | (Custom) | All Enemy objects. |
| **Environment** | (Custom) | Walls, Cliffs, Solid objects. |
| **Grappleable** | (Custom) | Specific anchor points for the Grappling Hook. |

### 3. The Tilemap Hierarchy
Level geometry is split into two distinct Tilemaps to separate visuals from physics.

- **Grid** (Parent)
  - **Base** (Layer: `Default`): Floor tiles, decorations. **NO COLLIDERS.**
  - **Ground_Tilemap** (Layer: `Environment`): Walls, cliffs, obstacles. **HAS COLLIDERS.**
    - Components: `Tilemap Collider 2D`, `Composite Collider 2D`, `Rigidbody 2D (Static)`.

---

## 🛠️ How to Use

### 1. Setting Up the Player Physics
The Player requires a specific configuration to move correctly without passing through walls.

**Components:**
- `Rigidbody 2D`: Body Type = **Kinematic**, Use Full Kinematic Contacts = **True**, Interpolate = **Interpolate**.
- `Box Collider 2D`: Size = Slightly smaller than sprite (0.8x0.8). Offset = Centered on feet.

**Scripting (`PlayerMotor.cs`):**
Movement is handled via `BoxCast` to predict collisions *before* they happen.
```csharp
// 1. Calculate desired movement
Vector2 moveDelta = velocity * Time.fixedDeltaTime;

// 2. BoxCast check (X-axis)
// We use a box 5% smaller than the collider to avoid "snagging" on parallel walls
if (!Physics2D.BoxCast(pos, size * 0.95f, 0, new Vector2(moveDelta.x, 0), ...))
{
    // Move X
}

// 3. BoxCast check (Y-axis) - Separated for wall sliding
if (!Physics2D.BoxCast(pos, size * 0.95f, 0, new Vector2(0, moveDelta.y), ...))
{
    // Move Y
}

// 4. Move (smooth sub-pixel physics)
// IMPORTANT: Do NOT snap the Rigidbody position. Snap visuals in LateUpdate instead.
_rb.MovePosition(finalPos);
```

### 2. Setting Up Environment Tiles
To prevent the "Invisible Wall" bug where players get stuck on floor tiles:

1. **Floor Tiles:** Paint these on the **Base** Tilemap. Ensure this Tilemap has **NO Collider**.
2. **Wall Tiles:** Paint these on the **Ground_Tilemap**. Ensure this Tilemap has a **Composite Collider**.
3. **Special Decorations (Rugs/Flowers):**
   - If they are on the `Ground_Tilemap` but should be walkable:
   - Select the **Tile Asset** in the Project window.
   - Set **Collider Type** to **None**.

### 3. Debugging Collisions
If a character is stuck or walking through walls:

1. **Check the Physics Matrix**: `Edit > Project Settings > Physics 2D`. Ensure `Player` intersects with `Environment`.
2. **Check the Z-Axis**: Ensure Player and Tilemaps are at **Z = 0**.
3. **Check the "Ghost Tile"**: Use the **Physics Debugger** (`Window > Analysis > Physics Debugger`) to see if there is an invisible green box where you are standing.

---

## 💻 Coding Standards

### 1. No `OnCollisionEnter` for Movement
Do not rely on `OnCollisionEnter2D` to stop the player. Since we are Kinematic, we must **pre-emptively** stop using Raycasts/BoxCasts.

### 2. Pixel Snapping
All **SpriteRenderer visuals** must be rounded to the nearest 1/32 unit to prevent shimmer, while physics remains smooth.
```csharp
// Standard Snapping Formula
float pixelUnit = 1f / 32f;
float snappedX = Mathf.Round(rawX / pixelUnit) * pixelUnit;
```

**Standard Implementation Pattern (Visual Child Only):**
```csharp
// In PlayerVisuals / EnemyVisuals (LateUpdate)
var pos = _spriteTransform.position;
pos.x = Mathf.Round(pos.x * 32f) / 32f;
pos.y = Mathf.Round(pos.y * 32f) / 32f;
_spriteTransform.position = pos;
```

### 3. LayerMask usage
Never hardcode Layer IDs. Use `LayerMask` fields in `SettingsSO` assets.
```csharp
// ✅ Correct
[SerializeField] private LayerMask _obstacleLayers;

// ❌ Wrong
int layerMask = 1 << 8;
```

---

## ⚙️ Administrator Configuration

### 1. Player Settings Asset
The `PlayerSettingsSO` controls which layers stop the player.
- **Obstacle Layers**: Must include `Environment` (and `Enemies` if you want body blocking).
- **Ground Layers**: Used for "IsGrounded" checks (if jumping is ever added).

### 2. Physics 2D Settings
- **Simulation Mode**: Fixed Update
- **Default Contact Offset**: 0.01 (Low value prevents "hovering" near walls)
- **Layer Collision Matrix**:
  - `Player` <-> `Environment` = ✅
  - `Player` <-> `Enemies` = ✅
  - `Enemies` <-> `Environment` = ✅
