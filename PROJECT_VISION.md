# PROJECT VISION: GREENLIGHT

> *A Context Anchor Document for AI Agents & Collaborators*  
> *Version 1.0 — January 2026*

---

## The Elevator Pitch

**Greenlight** is a handcrafted top-down action-RPG where a world in decay remembers every choice you make—NPCs whisper of your deeds, landscapes scar and heal in response to your actions, and forgotten gadgets become the keys to unlocking a deeply interconnected mystery. It fuses the exploration-driven wonder of *Zelda: A Link to the Past* with the mechanical ingenuity of *Sparklite* and the unflinching emotional depth of *Alundra*, all rendered in breathtaking modern pixel art that honors and transcends the 16-bit golden age.

---

## Core Pillars

These are the **non-negotiable design principles** that define Greenlight. Every feature, system, and artistic choice must serve at least one of these pillars. If a proposed addition conflicts with these tenets, it does not belong in Greenlight.

### 1. 🌱 **A World That Remembers**

Greenlight's universe is not a static backdrop—it is a living organism that evolves in response to player agency. A house you failed to save remains a charred ruin. A village you liberated flourishes with new merchants and hopeful dialogue. NPCs are not amnesiacs isolated in their scenes; they *know* the world's state and speak to it. This is achieved through a **Global Game State** that acts as the single source of truth, driving dynamic scene composition, dialogue branching, and narrative consequence. The player should feel that their journey leaves *fingerprints* on the world.

### 2. 🔧 **Gadgets as Grammar**

Progression in Greenlight is defined by *tools, skills, and story*—not arbitrary stat grinding. Each gadget the player acquires expands their vocabulary for interacting with the world. A grappling hook doesn't just let you cross chasms—it lets you *re-read* every chasm you've ever seen. A lantern doesn't just illuminate darkness—it reveals hidden truths in places you thought you understood. This "Lock and Key" philosophy permeates exploration, combat, and puzzle design. Backtracking is not tedium; it is *revelation*.

**Progression Systems:**
- **Health & Mana**: Players increase maximum health by finding or purchasing **Red Hearts**, and maximum mana via **Blue Hearts**. These are finite, placed rewards—not random drops.
- **No Levels, No XP**: There is no experience point system. The player does not "level up" in the traditional RPG sense.
- **Story-Driven Evolution**: The player begins with base stats. These may evolve at specific narrative beats—a transformative story event might permanently enhance the player's abilities as a meaningful milestone, not a grind reward.
- **Combat Prowess**: Your ability to face harder enemies comes from your equipment, your gadgets, and the *skills you develop as a player*—pattern recognition, timing, tool mastery.
- **Economy**: Enemies drop **Luni**, the in-game currency, used to purchase items, upgrades, and hearts from merchants.

### 3. ⚔️ **Combat as Conversation**

Encounters in Greenlight are tactical dialogues, not button-mashing monologues. Every enemy has tells, patterns, and weaknesses tied to your gadget arsenal. Combat should feel *snappy* and *mechanical*—responsive inputs, clear feedback, and meaningful choices about which tool to deploy. The player should feel clever when they win, not lucky. Inspired by the crisp action of *Sparklite* and the item-integrated encounters of *A Link to the Past*, combat rewards mastery and punishes recklessness.

### 4. 🧩 **Puzzles with Weight**

Greenlight does not shy away from challenge, but it also understands *pacing*. Puzzles exist on a spectrum of complexity:

- **Scene-Local Puzzles**: Many puzzles are self-contained within a single scene—push blocks, pressure plates, switch sequences, timing challenges. These provide rhythm and satisfaction without requiring external knowledge.
- **Multi-Scene Puzzles**: In the tradition of *Alundra*, some puzzles span multiple areas. A lever in one scene may open a gate in another. A symbol discovered in a dungeon may unlock a secret in the overworld. These reward attentive explorers who think beyond immediate boundaries.
- **Tool-Gated Returns**: Some puzzles cannot be solved on first encounter. The player must leave, acquire new gadgets, and return with fresh capabilities. This is intentional—it creates the joy of *revelation* when revisiting familiar spaces.

The satisfaction of solving a Greenlight puzzle should echo in memory. Environmental puzzles demand genuine thought—multi-step logic chains, physics interactions, and "aha!" moments that respect the player's intelligence. Some will be *hard*. The game trusts the player to rise to the challenge.

---

## The Look & Feel

Greenlight's visual identity is a love letter to the 16-bit era, evolved through modern techniques into something that feels both nostalgic and strikingly contemporary.

### Pixel Density & Resolution

- **Target Aesthetic**: 640×360 base resolution scaled to modern displays (1080p/4K), preserving crisp pixel edges.
- **Sprite Fidelity**: Characters and objects are authored at **32×32 pixels** and upscaled to **64×64** for display, providing detailed animation frames (8-12+ frames for key actions) with a chunky, readable silhouette.
- **Tile Density**: Environments use dense, hand-placed tiles with layered parallax elements to create depth without breaking the pixel grid.

### Color Palette

Greenlight's palette is **vibrant but grounded**. Think the lush greens of an ancient forest dappled with golden afternoon light, the deep cerulean of a twilight lake, the warm amber of a village hearth against encroaching shadow.

- **Primary Tones**: Verdant greens (the "Greenlight" motif), rich earth browns, and soft sky blues.
- **Accent Colors**: Warm golds and ambers for life/hope, deep crimsons and purples for danger/corruption, ethereal teals for magic/mystery.
- **Shadow Philosophy**: Shadows are not black—they are desaturated, cooler versions of local colors, creating cohesion and depth.

### Lighting & Atmosphere

This is where Greenlight transcends its 16-bit ancestors. Inspired by *Sea of Stars*, lighting in Greenlight is **dynamic and meaningful**:

- **Time-of-Day Systems**: Scenes transition through dawn, day, dusk, and night, with palette shifts and shadow movement that feel organic.
- **Local Light Sources**: Torches, lanterns, and magical effects cast real-time light that interacts with the environment, creating pools of warmth in darkness.
- **Atmospheric Effects**: Volumetric fog in swamps, dust motes in sunbeams, rain that reflects light—subtle particle work that adds life without obscuring gameplay.
- **Emotive Lighting**: Lighting is a storytelling tool. A corrupted forest is darker, with sickly green undertones. A liberated village glows warmer. The world's emotional state is visible at a glance.

### UI & Readability

In the tradition of *Zelda: ALttP* and *Sparklite*, UI in Greenlight is **clean, unobtrusive, and instantly readable**:

- **Minimal HUD**: Health, equipped gadget, and essential info only. No clutter.
- **High Contrast**: UI elements use distinct outlines and color separation from gameplay layers.
- **Touch-Friendly Scaling**: UI scales appropriately for Android without compromising the PC experience.
- **Iconography**: Gadgets and items use distinct silhouettes recognizable at a glance—no two items should be confusable.

---

## Audio Atmosphere

Sound in Greenlight is not background noise—it is an **emotional current** that carries the player through the world.

### Musical Identity

The score is a **chiptune-orchestral fusion**: the warmth of classic synthesized melodies married to the dynamic range of live instrumentation. Think a solo piano echoing through an abandoned temple, joined by pulsing arpeggiated synths as danger approaches, swelling into full orchestral triumph as the player overcomes.

- **Exploration Themes**: Wistful, melodic, with space for the world to breathe. Instruments like acoustic guitar, flute, and soft pads evoke wonder and melancholy.
- **Combat Themes**: Driving percussion, urgent brass, and tight synth leads. Music responds to combat intensity—dynamic layers that build and release.
- **Emotional Peaks**: Full orchestral arrangements for key story moments. Strings, choir, and brass used sparingly but devastatingly for maximum emotional resonance.
- **Regional Identity**: Each major area has a distinct musical motif that evolves with the area's state. A corrupted village plays its theme in minor key, with dissonant undertones; restored, the same motif returns major, triumphant.

### Sound Design

- **Tactile Feedback**: Every action *sounds* satisfying. Sword swings cut the air. Gadgets click and whir with mechanical precision. Footsteps change based on terrain.
- **Environmental Soundscapes**: Ambient layers tell stories—birdsong in thriving forests, unsettling silence in corrupted zones, distant thunder before a storm.
- **UI Audio**: Subtle, pleasant confirmation sounds. Menu navigation should feel as polished as gameplay.

---

## Player Experience

### The Emotional Arc

A player finishing Greenlight should feel:

1. **Wonder** — "What is this place? What secrets does it hold?"
2. **Competence** — "I understand this gadget now. I can do things I couldn't before."
3. **Connection** — "I care about these characters. This world feels alive."
4. **Triumph** — "I solved it. I overcame. I made a difference."
5. **Reflection** — "The choices I made mattered. The world remembers."

### PC Experience (Controller & Keyboard)

- **Controller-First Design**: Greenlight is designed around a gamepad. Analog movement, responsive face buttons for gadgets and actions, shoulder buttons for quick-swap tools.
- **Keyboard Parity**: Full WASD + mouse support with rebindable keys. No action should feel awkward on keyboard.
- **Precision Input**: Combat and puzzles demand precise input. Input lag is unacceptable. Actions execute on button press, not release.
- **Comfortable Sessions**: Designed for both brief play sessions and extended exploration. Frequent, unobtrusive auto-saves.

### Android Experience (Touch)

Mobile is not an afterthought—it is a **first-class citizen**.

- **Virtual Joystick**: Clean, customizable on-screen joystick for movement. Semi-transparent, repositionable based on player preference.
- **Contextual Touch Actions**: Interact buttons appear only when relevant. Attack and gadget buttons are large, thumb-friendly, and positioned for natural grip.
- **No Precision Penalties**: If a puzzle or combat encounter is frustrating on touch, it needs redesign. Touch players should never feel handicapped.
- **Seamless Cross-Play**: Cloud save support allowing players to continue their journey between PC and mobile.
- **Battery & Performance Conscious**: Optimized rendering and input handling for mobile hardware. No excessive battery drain or thermal throttling.

---

## What Greenlight is NOT

To maintain creative focus and protect the player experience, the following are **explicitly excluded** from Greenlight's design:

### ❌ No Microtransactions

Greenlight is a complete experience at purchase. No premium currency, no loot boxes, no "time-saver" packs, no cosmetic stores. The player buys the game; they own the game.

### ❌ No Procedural Generation

Every screen, puzzle, and enemy placement in Greenlight is **handcrafted**. Procedural generation creates content; hand-crafting creates *meaning*. Each room tells a story. Each encounter is designed. Randomness is reserved for minor loot variance, never for world structure.

### ❌ No Hand-Holding

Greenlight respects player intelligence. There are no intrusive tutorials, no mandatory hint systems, no glowing breadcrumb trails. Guidance is environmental—level design teaches through play. Optional hint NPCs exist for those who seek them, but the game never *forces* help upon the player.

### ❌ No Grinding

Progression is gated by **discovery and skill**, not time investment. There is no XP treadmill. The player grows stronger by finding gadgets, locating hearts, and mastering their skills—not by killing 1000 slimes.

**Enemy Respawn Philosophy**: Enemies *do* respawn, but not infinitely or exploitably. Leaving a scene and immediately returning will not reset enemies. Instead, respawns are tied to **world progression**—solving a puzzle in another area, advancing the story, or sufficient in-game time passing. This keeps the world feeling alive and dangerous on return visits without enabling mindless farming loops.

### ❌ No Bloat

Greenlight is a focused 15-25 hour experience, not a 100-hour checklist. Every quest has narrative purpose. Every collectible has gameplay impact. If content doesn't serve the pillars, it doesn't ship.

### ❌ No "Good/Evil" Binary

Moral choices in Greenlight exist in shades of gray. There is no karma meter, no angel/devil ending split. Choices have consequences, but the game does not judge. The player must sit with their decisions.

---

## Architectural Intent

> *This section provides high-level guidance for technical implementation without prescribing specific code. It exists to inform AI agents and developers about the systemic requirements implied by the design pillars.*

### The Global Game State

Greenlight's reactive world demands a **centralized, persistent state system**. This is the "Source of Truth" that all systems query:

- **State Flags**: Boolean and enum flags representing world events (e.g., `ForestShrine_Cleansed`, `Village_BurntDown`, `Gadget_Grapple_Acquired`).
- **Persistence**: State must serialize to save files and restore perfectly on load.
- **Accessibility**: Any system (dialogue, scene composition, NPC behavior, music) must be able to query state without tight coupling.

**Implication**: Consider **ScriptableObject-based state containers** or a dedicated **GameState singleton** with event-driven change notifications.

### Event-Driven Architecture

Systems in Greenlight should communicate through **loose coupling**:

- **Event Buses**: A publish-subscribe pattern allowing systems to react to state changes without direct references.
- **Example Flow**: Player cleanses a shrine → GameState updates `ForestShrine_Cleansed = true` → Event fires → Dialogue system updates NPC responses → Scene composition system enables "cleansed" prefab variant → Music system shifts regional theme to major key.

**Implication**: Implement a lightweight **Event Bus** or **ScriptableObject-based event channels** to decouple systems.

### Dynamic Scene Composition

Scenes must support **multiple states** without duplicating entire scenes:

- **Prefab Swapping**: Key environmental objects (e.g., a house) have variant prefabs (Intact, Damaged, Burnt, Repaired) activated/deactivated based on state flags.
- **Object Toggles**: Simpler variations handled by enabling/disabling child objects or sprite swaps.
- **Scene Initialization**: On scene load, a composition system reads relevant state flags and configures the scene accordingly.

**Implication**: Design scenes with **variant-aware root objects** and a **Scene Initializer** pattern that queries GameState on `Awake()` or `Start()`.

### Conditional Dialogue System

NPC dialogue must be **state-aware**:

- **Branching Conditions**: Dialogue nodes can specify required state flags. If `ForestShrine_Cleansed == true`, show dialogue branch A; else, show branch B.
- **Priority & Fallbacks**: Multiple conditions may apply; system needs priority ordering and default fallback dialogue.
- **Integration with Events**: Dialogue system listens to state changes to update available responses without requiring scene reload.

**Implication**: Use a **data-driven dialogue format** (ScriptableObjects or external JSON/YAML) with condition fields. Consider integration with existing dialogue middleware or a custom lightweight system.

### Modular Gadget System

Gadgets are central to gameplay and must be **extensible**:

- **ScriptableObject Definitions**: Each gadget defined as a ScriptableObject containing metadata (name, icon, description) and references to behavior components.
- **Behavior Components**: Gadget functionality implemented as modular MonoBehaviours or pure C# classes that can be composed.
- **State Integration**: Gadget acquisition updates GameState, enabling associated world changes and dialogue.

**Implication**: Design gadgets as **data + behavior pairs**, allowing designers to create new gadgets by combining existing behaviors with new data.

### Save System Requirements

Given the reactive world, saving is non-trivial:

- **Full State Serialization**: GameState, player inventory, gadget unlocks, scene states—all must serialize.
- **Platform Agnostic**: Save format must work on PC and Android, with potential for cloud sync.
- **Save Slot Support**: Multiple save slots for different playthroughs.

**Implication**: Design saves around **serializing the GameState container** plus player-specific data. Avoid saving scene object states directly; reconstruct from flags.

---

## Closing Vision

Greenlight is not merely a game—it is a **promise to the player**. A promise that their time will be respected, their intelligence honored, and their choices remembered. It is a world that breathes, a challenge that rewards, and a journey that lingers.

Every pixel placed, every note composed, every line of code written serves this promise.

**Welcome to Greenlight. The world is watching.**

---

*This document should be treated as the authoritative reference for Greenlight's creative and philosophical direction. Technical implementations may evolve, but the spirit described herein is immutable.*
