---
name: Phase 1 Nervous System
overview: "Architect the foundational \"Nervous System\" for Greenlight: a Global Game State (ScriptableObject persistence), Event Bus (decoupled communication), and Scene Initializer (reactive scene composition) that enables \"A World That Remembers\" without bloat."
todos:
  - id: ggs-core
    content: Implement GameStateSO with flag dictionaries and change notification
    status: completed
  - id: flag-definition
    content: Create WorldFlagDefinitionSO for designer-friendly flag metadata
    status: completed
  - id: event-base
    content: Build GameEventSO and GameEventSO<T> base event channel classes
    status: completed
  - id: flag-event
    content: Implement WorldFlagChangedEventSO with FlagChangePayload struct
    status: completed
  - id: event-listener
    content: Create GameEventListener MonoBehaviour for scene objects
    status: completed
  - id: serializer
    content: Build StateSerializer with JSON export/import for cross-platform saves
    status: completed
  - id: scene-init
    content: Implement SceneInitializer that queries GGS on Awake
    status: completed
  - id: state-responder
    content: Create StateResponder base class and IStateObserver interface
    status: completed
  - id: variant-swapper
    content: Build VariantSwapper component for prefab/object swapping
    status: completed
  - id: conditional-activator
    content: Build ConditionalActivator for simple enable/disable logic
    status: completed
  - id: editor-debug
    content: Create GameStateEditor custom inspector for hot-reload testing
    status: completed
---

# Phase 1: The Nervous System - Technical Architecture

This plan defines the foundational systems for Greenlight's reactive world. All systems follow the `RetroAdventure.[Module]` namespace convention and Unity 6.3 URP 2D standards.

---

## File Structure

```
Assets/
├── _Greenlight/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── GlobalGameState/
│   │   │   │   ├── GameStateSO.cs              # Main state container
│   │   │   │   ├── WorldFlagDefinitionSO.cs    # Flag metadata (name, category, default)
│   │   │   │   ├── WorldFlagRegistry.cs        # Runtime flag lookup dictionary
│   │   │   │   └── StateSerializer.cs          # JSON serialization for saves
│   │   │   │
│   │   │   ├── Events/
│   │   │   │   ├── GameEventSO.cs              # Base parameterless event
│   │   │   │   ├── GameEventSO_T.cs            # Generic typed event (bool, int, string)
│   │   │   │   ├── WorldFlagChangedEventSO.cs  # Specific event for flag changes
│   │   │   │   ├── GameEventListener.cs        # MonoBehaviour listener component
│   │   │   │   └── GameEventListener_T.cs      # Generic typed listener
│   │   │   │
│   │   │   └── SceneManagement/
│   │   │       ├── SceneInitializer.cs         # Queries GGS on scene load
│   │   │       ├── StateResponder.cs           # Base class for reactive objects
│   │   │       ├── VariantSwapper.cs           # Enables/disables child variants
│   │   │       └── ConditionalActivator.cs     # Simple enable/disable based on flag
│   │   │
│   │   └── Interfaces/
│   │       ├── IStateObserver.cs               # Interface for state-reactive objects
│   │       └── ISerializableState.cs           # Interface for saveable components
│   │
│   ├── Data/
│   │   ├── GameState/
│   │   │   ├── MasterGameState.asset           # The singleton GameStateSO instance
│   │   │   └── Flags/
│   │   │       ├── World/                      # WorldFlagDefinitionSO assets
│   │   │       ├── Story/
│   │   │       └── Gadgets/
│   │   │
│   │   └── Events/
│   │       ├── OnWorldFlagChanged.asset        # WorldFlagChangedEventSO instance
│   │       ├── OnSceneInitialized.asset        # Fired after scene setup complete
│   │       └── OnGameLoaded.asset              # Fired after save load
│   │
│   └── Editor/
│       ├── GameStateEditor.cs                  # Custom inspector for debugging
│       └── WorldFlagDefinitionEditor.cs        # Flag creation wizard
```

---

## Class Relationships

```mermaid
classDiagram
    direction TB

    class GameStateSO {
        -Dictionary~string,bool~ _boolFlags
        -Dictionary~string,int~ _intFlags
        -Dictionary~string,string~ _stringFlags
        +WorldFlagChangedEventSO OnFlagChanged
        +GetFlag(string key) bool
        +SetFlag(string key, bool value)
        +Serialize() string
        +Deserialize(string json)
    }

    class WorldFlagDefinitionSO {
        +string FlagKey
        +string Category
        +bool DefaultValue
        +string Description
    }

    class GameEventSO {
        -List~GameEventListener~ _listeners
        +Raise()
        +RegisterListener(listener)
        +UnregisterListener(listener)
    }

    class GameEventSO_T~T~ {
        -List~GameEventListener_T~ _listeners
        +Raise(T payload)
    }

    class WorldFlagChangedEventSO {
        +Raise(string flagKey, bool newValue)
    }

    class GameEventListener {
        +GameEventSO Event
        +UnityEvent Response
        +OnEnable()
        +OnDisable()
        +OnEventRaised()
    }

    class SceneInitializer {
        +GameStateSO GameState
        +GameEventSO OnSceneInitialized
        -List~IStateObserver~ _observers
        +Awake()
        +InitializeScene()
    }

    class IStateObserver {
        <<interface>>
        +OnStateQueried(GameStateSO state)
    }

    class StateResponder {
        <<abstract>>
        +GameStateSO GameState
        +string[] WatchedFlags
        +OnStateQueried(state)*
        +OnFlagChanged(key, value)
    }

    class VariantSwapper {
        +string FlagKey
        +GameObject TrueVariant
        +GameObject FalseVariant
    }

    class ConditionalActivator {
        +string FlagKey
        +bool ActiveWhenTrue
        +GameObject Target
    }

    GameStateSO --> WorldFlagChangedEventSO : raises
    GameStateSO ..> WorldFlagDefinitionSO : references
    WorldFlagChangedEventSO --|> GameEventSO_T~FlagChangePayload~
    GameEventSO_T --|> GameEventSO
    GameEventListener --> GameEventSO : subscribes
    SceneInitializer --> GameStateSO : queries
    SceneInitializer --> IStateObserver : notifies
    StateResponder ..|> IStateObserver
    VariantSwapper --|> StateResponder
    ConditionalActivator --|> StateResponder
```

---

## Logic Flow: World State Change Propagation

**Scenario**: Player triggers event that burns down a house (`Village_House1_Burnt = true`)

```mermaid
sequenceDiagram
    participant Trigger as InteractionTrigger
    participant GGS as GameStateSO
    participant Event as WorldFlagChangedEventSO
    participant Listener as GameEventListener
    participant Responder as VariantSwapper
    participant Dialogue as DialogueSystem
    participant Music as MusicSystem

    Note over Trigger: Player interacts with brazier

    Trigger->>GGS: SetFlag("Village_House1_Burnt", true)
    GGS->>GGS: _boolFlags["Village_House1_Burnt"] = true

    GGS->>Event: Raise("Village_House1_Burnt", true)

    par Parallel notification to all listeners
        Event->>Listener: OnEventRaised(payload)
        Listener->>Responder: OnFlagChanged("Village_House1_Burnt", true)
        Responder->>Responder: Disable "IntactHouse" variant
        Responder->>Responder: Enable "BurntRuins" variant
    and
        Event->>Dialogue: OnFlagChanged(payload)
        Dialogue->>Dialogue: Update NPC dialogue branches
    and
        Event->>Music: OnFlagChanged(payload)
        Music->>Music: Shift to somber theme variant
    end

    Note over Responder: Scene now shows burnt ruins
```

---

## Data Serialization for Cross-Platform Cloud Saves

The `GameStateSO` uses a flat JSON structure optimized for portability:

```json
{
  "version": 1,
  "timestamp": "2026-01-25T14:30:00Z",
  "boolFlags": {
    "Village_House1_Burnt": true,
    "ForestShrine_Cleansed": false,
    "Gadget_Grapple_Acquired": true
  },
  "intFlags": {
    "Player_MaxHealth": 6,
    "Player_Luni": 245
  },
  "stringFlags": {
    "Player_LastCheckpoint": "Village_Square"
  }
}
```

**Key design decisions for cloud saves:**

- Flat dictionary structure (no nested objects) for easy diff/merge
- Version field for migration compatibility
- String keys allow runtime flag creation without recompilation
- Platform-agnostic: uses `System.Text.Json` or Unity's `JsonUtility`

---

## Hot-Reloading Support (Editor Testing)

The Event Bus supports live testing without recompiles:

1. **ScriptableObject Events persist through Play Mode**: Changes to `GameStateSO` in Play Mode can be inspected in real-time via custom inspector.

2. **Runtime Flag Injection**: The `GameStateEditor.cs` provides a debug panel to:

   - View all current flags and values
   - Toggle any bool flag with a single click
   - Manually fire `WorldFlagChangedEventSO` with custom payloads

3. **Scene Re-initialization**: A debug button calls `SceneInitializer.InitializeScene()` to re-query all `IStateObserver` objects without reloading the scene.

---

## Integration Points for Future Phases

| Future System | Integration Hook |

|---------------|------------------|

| **Gadget System (Phase 2)** | `SetFlag("Gadget_[Name]_Acquired", true)` triggers unlock events |

| **Dialogue System (Phase 2)** | Subscribes to `OnWorldFlagChanged` for conditional branches |

| **Save/Load UI** | Calls `GameStateSO.Serialize()` / `Deserialize()` |

| **Combat (Phase 3)** | Queries `GameStateSO` for enemy spawn variants |

| **Music (Phase 4)** | Subscribes to flag changes for dynamic theme switching |

---

## Implementation Order

The recommended implementation sequence ensures each piece can be tested before building dependent systems.