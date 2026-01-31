# The Nervous System: Technical Documentation

**Version 1.0** | *Phase 1 Architecture*

The "Nervous System" is the foundational architecture of Greenlight. It handles **Game State persistence**, **Event propagation**, and **Scene reactivity**. It is designed to fulfill the core pillar: *"A World That Remembers."*

---

## 🧩 Core Concepts

### 1. Global Game State (The Brain)
The **Game State** is the single source of truth for the entire world. It stores facts about what the player has done (e.g., "Village_Burnt", "Grapple_Unlocked").

- **Type**: `ScriptableObject` (`GameStateSO`)
- **Location**: `Assets/_Greenlight/Data/GameState/MasterGameState.asset`
- **Function**: Stores `bool`, `int`, and `string` flags. Serializes to JSON for saves.

### 2. Flags (The Memories)
Individual facts are called **Flags**. To prevent "Magic String" typos (e.g., typing "Burnt" vs "Burned"), we use **Flag Definition Assets**.

- **Type**: `ScriptableObject` (`WorldFlagDefinitionSO`)
- **Location**: `Assets/_Greenlight/Data/Flags/`
- **Usage**: Drag these assets into inspectors to tell objects what to watch.

### 3. Event Bus (The Nerves)
When a flag changes, the Game State fires an event. Systems listen to this event to react instantly without checking every frame.

- **Type**: `ScriptableObject` (`WorldFlagChangedEventSO`)
- **Location**: `Assets/_Greenlight/Data/Events/OnWorldFlagChanged.asset`

### 4. Scene Initializer (The Wake-Up Call)
When a scene loads, this component wakes up all reactive objects and tells them to check the Game State.

- **Component**: `SceneInitializer`
- **Requirement**: Must exist in every scene (typically on a `_Systems` object).

---

## 🛠️ How to Use (For Designers & Developers)

### Scenario A: Creating a New World State (e.g., "Bridge Repaired")

1. **Create the Flag Asset**:
   - Go to `Assets/_Greenlight/Data/Flags/World/`.
   - Right-click → **Create** → **Greenlight** → **Flags** → **Flag Definition**.
   - Name it `Bridge_Repaired`.

2. **Make an Object React**:
   - Select the Bridge GameObject in the scene.
   - Add a **Variant Swapper** component.
   - **Flag**: Drag the `Bridge_Repaired` asset into the slot.
   - **True Variant**: Drag the "Repaired Bridge" child object here.
   - **False Variant**: Drag the "Broken Bridge" child object here.

3. **Test It**:
   - Enter Play Mode.
   - Select `MasterGameState` in the Project window.
   - In the Inspector, find `Bridge_Repaired` and toggle the checkbox. The bridge should swap instantly.

### Scenario B: Simple Object Toggling

Use the **Conditional Activator** component if you just want to hide/show something based on a flag.

- **Example**: An NPC that only appears after you defeat a boss.
- **Setup**: Add `Conditional Activator` to the NPC. Assign the `Boss_Defeated` flag. Set `Active When True` to `true`.

---

## 💻 Coding Standards

### 1. Querying State in Code
Don't use `GameObject.Find`. Instead, reference the `GameStateSO` asset.

```csharp
[SerializeField] private GameStateSO _gameState;
[SerializeField] private WorldFlagDefinitionSO _myFlag;

public void CheckSomething()
{
    if (_gameState.GetBool(_myFlag.FlagKey))
    {
        // Do something
    }
}
```

### 2. Reacting to Changes
Inherit from `StateResponder` to get automatic event wiring.

```csharp
public class MyReactiveComponent : StateResponder
{
    protected override void OnFlagChanged(FlagChangePayload payload)
    {
        base.OnFlagChanged(payload);
        
        // Check if the changed flag matches what we care about
        if (IsWatching(payload.FlagKey))
        {
            UpdateMyVisuals();
        }
    }
}
```

---

## ⚙️ Administrator Configuration

### Initial Setup Checklist
If setting up a new project or scene, ensure these assets exist and are wired correctly:

1. **MasterGameState** (`GameStateSO`) exists in `Data/GameState`.
2. **OnWorldFlagChanged** (`WorldFlagChangedEventSO`) exists in `Data/Events`.
3. **MasterGameState** has `OnWorldFlagChanged` assigned to its "Event Channel" slot.
4. **SceneInitializer** is present in the scene and references `MasterGameState`.

### Debugging Tools
- **GameState Editor**: Select `MasterGameState` during Play Mode to see real-time flag values and toggle them manually.
- **Console Logging**: Enable `Debug Log` on `SceneInitializer` to see exactly which objects are registering themselves.

---

## 📂 File Structure

### 🏗️ Code & Data
```
Assets/_Greenlight/
├── Scripts/
│   ├── Greenlight.Core.asmdef  # Core assembly definition
│   ├── Core/
│   │   ├── GlobalGameState/   # Logic for the "Brain"
│   │   ├── Events/            # Logic for the "Nerves"
│   │   └── SceneManagement/   # Logic for Scene Reactivity
│   └── Editor/                # Custom Inspectors & Debug Tools
└── Data/
    ├── GameState/             # The MasterGameState asset lives here
    ├── Flags/                 # Flag definitions live here
    └── Events/                # Event channels live here
```

### 🎨 Art Assets
```
Assets/Art/Sprites/
└── 🌍 Environment/          (Tiles & World-building)
    ├── 🏰 Tilesets/        (Primary grid-based sprites)
    │   ├── Overworld/
    │   └── Dungeons/
    └── 🪵 Decor/            (Non-grid props like grass tufts, rocks)
```

---

## 🔗 Related Documentation

- **[Assembly Architecture](assembly-architecture.md)**: Understanding code organization and module dependencies
- **[Phase 2 Vocabulary](phase2-vocabulary.md)**: Player controller and gadget systems
- **[Phase 3 Conversation](phase3-conversation.md)**: Combat, AI, and economy systems
