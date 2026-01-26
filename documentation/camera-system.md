# The Camera System: Technical Documentation

**Version 1.0** | *Phase 2 Architecture*

The Camera System in Greenlight is built on **Cinemachine 3** and the **URP 2D Pixel Perfect** pipeline. It is responsible for maintaining the "Retro-Modern" aesthetic (32 PPU, 640x360) while providing smooth, responsive tracking.

---

## 🧩 Core Concepts

### 1. The Eye (Main Camera)
The physical Unity Camera that renders the scene. It handles the **Pixel Perfect** logic, ensuring sprites snap to the grid and resolution is maintained.

- **Component**: `Pixel Perfect Camera` (URP)
- **Role**: Resolution handling, upscaling, and grid snapping.

### 2. The Brain (Cinemachine Brain)
Attached to the Main Camera, it delegates control to the active Virtual Camera.

### 3. The Tracker (Virtual Camera)
A "ghost" camera that defines movement rules. We use a single **CM_PlayerCamera** for standard gameplay.

- **Type**: `CinemachineCamera` (v3)
- **Role**: Smooth player following, damping, and Z-depth management.

---

## 🛠️ How to Use (Configuration Guide)

### Scenario A: Setting up a New Scene Camera

If creating a new scene from scratch, follow these exact settings to ensure 32 PPU compliance.

#### 1. Main Camera Setup
Select the **Main Camera** GameObject:
- **Projection**: `Orthographic`
- **Size**: `5.625` (Calculated: 360px / 32 PPU / 2)
- **Z Position**: `-10`
- **Rotation**: `(0, 0, 0)`

**Pixel Perfect Camera Component**:
- **Assets Pixels Per Unit**: `32`
- **Reference Resolution**: `640` x `360`
- **Crop Frame**: `Window` (Handles upscaling)
- **Grid Snapping**: `Enabled` (Prevents sub-pixel jitter)

#### 2. Virtual Camera Setup
Create/Select **CM_PlayerCamera**:
- **Position Control**: `Follow`
- **Rotation Control**: `None`
- **Tracking Target**: Assign the `Player` GameObject.

**Extensions**:
- Add **CinemachinePixelPerfect** extension. (Crucial for linking to Main Camera settings).

**Cinemachine Follow Component**:
- **Binding Mode**: `Lock to Target With World Up`
- **Follow Offset**: `(0, 0, -10)` (Maintains Z-depth visibility)
- **Damping**:
  - X: `0.1` (Snappy)
  - Y: `0.1` (Snappy)
  - Z: `0` (Instant/Locked)

### Scenario B: Renderer Configuration (URP)

To support Pixel Perfect and 2D Lights, the **PC_2D_Renderer** asset must be configured correctly.

1. Locate `Assets/Settings/PC_2D_Renderer.asset`.
2. **Transparency Sort Axis**: `(0, 1, 0)` (Sorts by Y for top-down depth).
3. **Light Render Texture Scale**: `1.0` (Prevents blurry lights).
4. **Post-Processing**: `Enabled`.

---

## 💻 Coding Standards

### Camera Shake
Do not move the camera transform directly. Use the **Game Event** system to trigger Cinemachine Impulse or Noise.

```csharp
// BAD
Camera.main.transform.position += Random.insideUnitCircle;

// GOOD (Conceptual)
GameEvents.RequestCameraShake(intensity: 0.5f, duration: 0.2f);
```

### Room Transitions
For room-based movement (e.g., Zelda dungeons), use the **Cinemachine Confiner 2D** extension.
1. Create a PolygonCollider2D trigger for the room bounds.
2. Assign it to the `Bounding Shape 2D` slot on the Confiner extension.

---

## ⚙️ Administrator Configuration

### Debugging Checklist
If the game looks "wrong" (blurry, jittery, or distorted):

1. **Check PPU**: Are all Sprites imported with PPU = `32`?
2. **Check Filter**: Is Sprite Filter Mode set to `Point (No Filter)`?
3. **Check Renderer**: Is the active URP Asset using the **2D Renderer**?
4. **Check Z-Depth**: Is the Camera at Z = `-10` and Player at Z = `0`?
5. **Check Snapping**: Is `Grid Snapping` enabled on the Pixel Perfect Camera?
