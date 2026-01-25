# Greenlight Development Roadmap

This document outlines the "Bird's Eye View" of our initial prototype development, focusing on the core pillars of Greenlight.

## Phase 1: The "Nervous System" (Foundation)
This phase focuses on the Global Game State and communication. We aren't making a game yet; we’re making a world that can "remember."

- **Global Game State (GGS):** Implement ScriptableObject-based containers to track world flags (booleans), enums for story beats, and player stats like Luni and Hearts.
- **The Event Bus:** Build the architecture that allows systems to talk (e.g., an "Enemy Defeated" event that the GGS and Music System both listen to).
- **Scene Initializer:** Create the logic that, upon loading a scene, queries the GGS to decide which objects to enable (e.g., showing a "charred ruin" instead of a "house").

## Phase 2: The "Vocabulary" (Core Mechanics)
Here, we define how the player interacts with the world using **Gadgets as Grammar**.

- **Modular Gadget Framework:** Create the base class for gadgets. Instead of "weapons," these are tools with data-driven behaviors (ScriptableObjects).
- **The "First Verb":** Develop one primary traversal/combat gadget (like a grappling hook or lantern) to test the "Lock and Key" philosophy.
- **Responsive Character Controller:** Build the top-down movement engine, ensuring it feels "snappy" and supports both Controller and Touch inputs from day one.

## Phase 3: The "Conversation" (Combat & AI)
We move into **Combat as Conversation**, focusing on tactical enemy encounters rather than stats.

- **Tactical AI:** Design one or two enemy types with clear "tells" and patterns that require the Phase 2 gadget to defeat efficiently.
- **Combat Feedback:** Implement "mechanical" feedback—hitstop, screenshake, and clear audio cues—to ensure combat feels polished, not floaty.
- **The Economy Loop:** Implement Luni drops and a basic merchant interaction to test the "No XP" progression loop.

## Phase 4: The "Aha!" Moment (Environmental Logic)
The final phase of the prototype integrates **Puzzles with Weight**.

- **Multi-Scene Logic:** Build a small two-scene environment where an action in Scene A (e.g., flipping a lever) permanently alters Scene B via the GGS.
- **Tool-Gated Exploration:** Design a "revelation" moment where a previously impassable area becomes accessible only after the player understands their gadget.
- **Visual & Audio Polish:** Apply our 16-bit modern aesthetic—dynamic lighting, layered parallax, and regional musical motifs—to one "Vertical Slice" area.

---

## Pillar Audit for this Plan
- **A World That Remembers:** Secured in Phase 1; it's the very first thing we build.
- **Gadgets as Grammar:** Phase 2 ensures we don't fall into the trap of making simple "stat-stick" weapons.
- **Puzzles with Weight:** Phase 4 tests if our multi-scene logic actually feels rewarding to the player.
- **No Grinding:** By focusing on Phase 2 & 3, we prove the game is fun because of skill mastery, not killing 1,000 slimes.
