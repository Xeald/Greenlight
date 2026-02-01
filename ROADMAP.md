# Greenlight Development Roadmap (Prototype → Vertical Slice)

This roadmap is the **living “bird’s eye view”** of Greenlight’s prototype progression.
It is grounded in two truths:
- Greenlight must always serve the pillars in `PROJECT_VISION.md`.
- The “Nervous System” is largely implemented already, so the next work is **integration + playable loops**, not pure architecture.

---

## Current State (What Exists Today)

### ✅ The “Nervous System” (Foundation) — **Mostly Done**
- **Global Game State**: `GameStateSO` stores bool/int/string flags and can raise change events.
- **Event Bus**: ScriptableObject event channels exist (`GameEventSO`, `WorldFlagChangedEventSO`).
- **Dynamic Scene Composition**: `SceneInitializer` + `StateResponder` + `VariantSwapper` + `ConditionalActivator` enable flag-driven composition.
- **Serialization**: `StateSerializer` can serialize/deserialize `GameStateSO` to JSON.
- **Editor Tooling**: custom inspectors exist for fast iteration on flags and state.

### 🧩 The “Vocabulary” (Core Mechanics) — **In Progress**
- **Player Controller**: `PlayerController` routes Input System callbacks to `PlayerMotor`, `GadgetUser`, and visuals/combat hooks.
- **Movement**: `PlayerMotor` uses kinematic MovePosition + collision checks (retro-friendly).
- **Gadget Framework**: inventory + gadget definition + behaviour runtime are in place.
- **First Verb**: grappling hook behaviour + grapple points exist (needs an integrated “aha moment” scene).
- **Animation Feel**: 4-direction idle/move pipeline exists (recently completed).

### ⚔️ The “Conversation” (Combat & AI) — **Scaffolded**
- Enemy AI has a state machine + telegraph system + visuals hooks.
- Combat has a heart-based `HealthComponent`, invincibility/knockback hooks, and a feedback orchestrator.

### 💰 Economy — **Scaffolded**
- Luni drops/pickups exist.
- Merchant UI + merchant interactable exist (some interaction still uses placeholder key input; needs unification).

---

## Roadmap Principles (Non‑Negotiables)
- **No XP / no levels / no grinding loops**
- **Progression via gadgets + discovery + finite placed rewards (Hearts/Blue Hearts)**
- **World state is flags + events** (not duplicated scenes)
- **Android is first‑class** (touch-friendly, scalable UI, no tiny hit targets)

---

## Milestone -0.5 — Mobile Input & UI Anchor (Android First‑Class Validation)
**Goal**: Validate that our core loop is viable on a 6-inch screen *before* we build content around it.

- **Deliverable**
  - Implement a **Floating Joystick** for the **left 50%** of the screen.
    - Includes a **Dynamic Ring** visual feedback loop (center + radius adapt to thumb drift).
  - Implement **static action buttons** in the lower-right:
    - Hit targets are **20% larger than the visual art** (generous touch affordance).
  - Enforce **8-direction movement** (Cardinal + Intercardinal) for parity between WASD and touch.
  - Define minimum tap target standards for HUD/menus (hearts, gadget, merchant UI) and safe areas.

- **Acceptance Criteria**
  - Movement, gadget use, and interaction are playable with thumbs without constant misfires.
  - The HUD remains readable and unobtrusive at phone resolutions.
  - If touch needs assist (aim snapping, generous raycasts, context prompts), the assist rules are documented and agreed upon.

---

## Milestone -0.25 — Dependency & Boundary Audit (Assembly DAG Guardrail)
**Goal**: Prevent “spaghetti dependencies” as we scale Economy/UI/AI.

- **Deliverable**
  - Confirm assembly dependency direction stays a **DAG** (no circular refs).
  - Confirm cross-boundary communication patterns:
    - **Core** stays “bottom-level”
    - **UI** depends on what it needs to display (often Core/Economy), but **Economy must not compile-time depend on UI**
    - Use **events, interfaces in Core, or reflection** for cross-boundary calls
  - Standardize on an **Event-Driven UI** approach:
    - **Economy** and **Combat** must never compile-time depend on **UI**
    - UI components should listen to `GameEventSO` / typed event channels, or query data via **read-only interfaces defined in `Core`**
    - Gameplay assemblies must remain functional even if UI is removed from the scene (strict assembly DAG)
  - Replace risky “magic string flag keys” in gameplay components with `WorldFlagDefinitionSO` references where it materially reduces risk.

- **Acceptance Criteria**
  - No new circular dependency paths introduced.
  - The pattern used by `MerchantInteractable` (reflection to reach `Greenlight.UI`) is documented as the standard where needed.

---

## Milestone -0.1 — Standardized Feel Baseline (32 PPU + Drift‑Free)
**Goal**: Lock the baseline “snappy” feel before encounter design.

- **Deliverable**
  - Verify pixel snapping is applied to **visuals only**, never the Rigidbody root.
  - Verify player movement:
    - no drift / no sticky corners
    - consistent collision response
  - Verify camera follow does not introduce sub-pixel shimmer (if camera exists).

- **Acceptance Criteria**
  - A short “feel checklist” exists and is used to validate any future movement/combat changes.
  - The Playground scene feels good before we invest in combat/puzzle content.

---

## Milestone 0 — Integration Harness (One “Playable Playground” Scene)
**Goal**: Prove the whole stack works together end-to-end with minimal content.

### Prerequisites (Gates)
- **First Verb is decided** (see Open Decisions #3) and the roadmap is aligned to that choice.
- Milestones **-0.5**, **-0.25**, and **-0.1** are completed (or explicitly waived with a written reason).

- **Deliverable**: One scene (working name: `Prototype_Playground`) that wires:
  - `SceneInitializer` + a real `GameStateSO` asset + `WorldFlagChangedEventSO`
  - Player movement + 4-dir animation
  - Grapple points + **Grapple (First Verb)** gadget
  - 1 enemy encounter (idle → chase → telegraph → attack → death)
  - Luni drop + pickup
  - 1 merchant interaction (open/close UI + purchase)

- **Acceptance Criteria**
  - **First Verb is Grapple** (finalized).
  - Player movement must be restricted to **8 directions** (Cardinal + Intercardinal) to ensure parity between PC (WASD) and Android (Joystick).
  - Movement feels **snappy‑but‑smooth** (smooth sub‑pixel physics + strong deceleration / “high friction”).
  - Grapple shows the hook extension *before* movement, and respects collisions during pull.
  - Enemy encounter produces readable feedback (telegraph + hitstop/screen shake where applicable).
  - Luni reliably increases and is visible in UI when picked up or purchased changes currency.
  - `System_Input_Locked` properly disables player control during shop/interaction.
  - **Physical Android Deployment Test (Phone-in-Hand)**:
    - The Playground loop must be successfully deployed to a **physical Android device** (not Editor simulation only)
    - A user must be able to complete **movement, combat, and merchant loops** using **only touch controls**
    - No obvious **performance degradation** and no major **UI overlap/obstruction** issues (thumb occlusion, unreadable HUD, blocked buttons)

---

## Milestone 1 — The First Verb (Grapple as Grammar)
**Goal**: Turn the grappling hook from “it works” into “it teaches the game.”

- **Deliverable**
  - A small traversal space where grapple enables a *re-read* of the environment:
    - A previously visible, unreachable ledge becomes reachable once you understand the hook.
  - A clear flag gate (e.g., `Gadget_Grapple_Acquired`) that changes scene composition.

- **Acceptance Criteria**
  - Player learns the gadget via play, not tutorial text.
  - The world “remembers” gadget acquisition across scene reloads.

---

## Milestone 2 — Combat as Conversation (One Enemy That Wants the Gadget)
**Goal**: One enemy type that is meaningfully different with/without gadget mastery.

- **Deliverable**
  - One enemy where:
    - Telegraph is readable
    - Mistakes are punishable but fair
    - Correct gadget use creates advantage (positioning, stun window, pull, etc.)

- **Acceptance Criteria**
  - Combat feels mechanical (clear feedback, no floaty ambiguity).
  - Enemy defeat triggers drops and a meaningful world-state update (at least one flag or consequence).

---

## Milestone 3 — Economy Loop (No XP Progression)
**Goal**: Validate “Luni as meaningful choice,” not grinding.

- **Deliverable**
  - A merchant sells at least:
    - One **HealthUpgrade** (**Heart Container purchase**: permanent Max Health upgrade)
    - One **KeyItem** or **Gadget-related** purchase that changes world access
  - Heart Container requirements:
    - **Finite** (not farmable), **high-cost**, meaningful choice
    - Updates Global Game State (`Player_MaxHealth` / `Player_Health`) and persists across saves
  - First **Gadget Evolution**:
    - **Whale Hook** upgrade for the Grapple (new verb / context-sensitive behavior)

- **Acceptance Criteria**
  - Purchases are persistent (unique items mark flags, currency persists).
  - UI communicates affordability, sold-out state, and feedback clearly.

---

## Milestone 4 — The “Aha!” Moment (Two-Scene World Memory)
**Goal**: Prove “Puzzles with Weight” across scenes.

- **Deliverable**
  - Scene A: perform an action (lever, cleanse, repair, etc.) that sets a flag.
  - Scene B: uses `VariantSwapper` / `ConditionalActivator` to show a changed world state.

- **Acceptance Criteria**
  - Action in Scene A permanently changes Scene B.
  - The change persists through save → quit → load.

---

## Milestone 5 — Save/Load v1 (Platform-Agnostic)
**Goal**: Make “world remembers” real beyond a single play session.

- **Deliverable**
  - **Local JSON storage only** (prototype standard).
  - A basic save file workflow using existing JSON serialization.
  - Load restores `GameStateSO` and forces scene re-initialization.
  - **Schema Extensibility**:
    - The save system must utilize a **versioned JSON schema**
    - It must support **forward compatibility**: adding new world flags or inventory items in future milestones must not invalidate or corrupt prototype save files

- **Acceptance Criteria**
  - Save/Load works reliably on PC and is Android-ready (no editor-only APIs).
  - Loading does not require duplicating scenes or storing per-object transforms as the primary mechanism.
  - Save files remain valid as the project grows:
    - **Partial loads** are supported (missing/new flags default safely)
    - New systems/flags/items added later do not corrupt or invalidate existing saves

---

## Milestone 6 — Vertical Slice Area (Polish Pass)
**Goal**: A 5–10 minute playable slice that communicates the pillars with confidence.

- **Deliverable**
  - One themed area with:
    - One traversal “aha”
    - One combat encounter
    - One consequence that changes the world
    - A simple economy choice

- **Acceptance Criteria**
  - Minimal readable HUD (hearts + equipped gadget + currency where relevant).
  - Visual/audio polish is applied intentionally (not everywhere, but where it matters).

---

## Open Decisions
1. **Vertical slice theme**: what is the first “region fantasy” (forest shrine, ruined village edge, swamp, etc.)?
2. **Target platforms for the next 2 milestones**: PC/controller first, or do we prioritize touch parity immediately?

## Closed Decisions
3. **First verb commitment (CLOSED)**: evaluated **Grapple** vs **Gust Jar** vs **Magnetic Wand** → **Final decision: Grapple** (official first grammar gadget).
