# Greenlight 🌿

> *A world that remembers.*

Greenlight is a top-down action-RPG built in Unity 6.3, fusing the exploration of *A Link to the Past* with the mechanical ingenuity of *Sparklite*. It features a living world that reacts to player choices through a persistent global state.

## 📚 Documentation

### Core Systems
- **[The Nervous System](documentation/nervous-system.md)**: The architectural backbone of Greenlight. Explains the Global Game State, Event Bus, and reactive scene composition.
- **[The Vocabulary](documentation/phase2-vocabulary.md)**: Details the Player Controller, Modular Gadgets, and the Grappling Hook implementation.
- **[The Camera System](documentation/camera-system.md)**: Configuration guide for the 32 PPU Pixel Perfect Camera, Cinemachine 3, and 2D Renderer settings.

### Project Standards
- **[Project Vision](PROJECT_VISION.md)**: The creative pillars and non-negotiable design principles.
- **[Roadmap](Dev-plans/ROADMAP.md)**: Development phases and milestones.

---

## 🚀 Getting Started for Developers

1. **Unity Version**: Ensure you are using **Unity 6.3 (URP 2D)**.
2. **Setup**:
   - Open `Assets/_Greenlight/Data` and verify the `MasterGameState` asset exists.
   - Open `Assets/Scenes/SampleScene` to see the `SceneInitializer` in action.

## 🏗️ Architecture Overview

Greenlight avoids "God Objects" and tight coupling by using a **ScriptableObject-based architecture**:
- **State** is stored in data assets, not scene objects.
- **Communication** happens via event channels, ensuring systems don't reference each other directly.
- **Logic** is reactive; objects query the state or listen for changes.

---

*Documentation maintained by the Greenlight Dev Team & AI Collaborators.*
