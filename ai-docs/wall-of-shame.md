# Wall of Shame: Learning & Self-Correction

This document serves as a mandatory context for AI assistants working on the Greenlight project. It tracks anti-patterns, hallucinations, and logic bugs to ensure they are not repeated.

## 📋 Protocol
- **Read this file** at the start of every new feature or major refactor.
- **Update this file** immediately if:
  1. The user corrects code because it violated project PPU (32) or Physics standards.
  2. A hallucination is discovered (e.g., non-existent Unity 6.3 APIs).
  3. A logic bug is found stemming from "modern" habits (e.g., float-based drifting) instead of "retro" precision.

## ❌ Hall of Shame

| Date | Issue Type | The "Wrong Way" | The Correct Project Standard |
| :--- | :--- | :--- | :--- |
| 2026-01-25 | Initial Setup | N/A | Always check `ai-docs/wall-of-shame.md` before starting work. |
| 2026-01-26 | Architecture | Using `FindObjectsByType` in `SceneInitializer` to find observers. | Use a **Static Registry** pattern where objects register themselves in `OnEnable`. Avoid $O(N)$ scene searches. |
| 2026-01-26 | DX / Robustness | Relying on "Magic Strings" for flag keys (e.g., `string _flagKey`). | Use **Asset References** (`WorldFlagDefinitionSO`) in the Inspector to prevent typos, then extract the key at runtime. |
| 2026-01-26 | Compilation | Putting `using UnityEditor;` in runtime scripts without guards. | Always wrap Editor-only namespaces/logic in `#if UNITY_EDITOR` to prevent build failures. |
| 2026-01-26 | Physics/Visuals | Applying pixel snapping logic on the Rigidbody object. | Separate **Logic** (Parent) from **Visuals** (Child). Snap only the visual child to the grid to prevent physics jitter. |
| 2026-01-26 | Physics/Layers | Putting the Player on the same layer as the Grapple target. | Use distinct layers (`Player`, `Grappleable`). Raycasts hitting the caster on Frame 0 break logic. |
| 2026-01-26 | Serialization | Using `System.Text.Json` for save/load in Unity. | Use **Newtonsoft.Json** (`com.unity.nuget.newtonsoft-json` package). Unity's .NET implementation doesn't include `System.Text.Json` by default. |
| 2026-01-26 | Async Movement | Using a **fixed `startPos`** in async movement loops: `Vector2.Lerp(startPos, target, t)` where `startPos` is calculated once. | Recalculate the current position each frame for **constant-speed movement**: `Vector2 currentPos = rb.position; Vector2 newPos = currentPos + direction * speed * Time.deltaTime;` Otherwise the object never moves! |
| 2026-01-27 | Combat Sequencing | Applying knockback during hitstop (`Time.timeScale = 0`) causes "slippery" physics. | **Critical sequencing**: 1) Apply hitstop FIRST, 2) Apply knockback AFTER hitstop completes, 3) Screen shake concurrent with knockback. Use `await hitstopController.ApplyHitstopAsync()` for proper timing. |
| 2026-01-27 | UI Concurrency | Multiple overlapping UI feedback messages when players button-mash. | Use `CancellationTokenSource` to cancel previous async UI tasks before starting new ones. Prevent message display conflicts with proper task cancellation. |
| 2026-01-27 | Legacy APIs | Using `FindObjectOfType<T>()` (Unity 2022 and earlier). | Use `FindFirstObjectByType<T>()` in Unity 6+. It's faster and more explicit about finding behavior. |
| 2026-01-27 | Physics TimeScale | Using `Time.fixedDeltaTime` without considering timeScale implications for knockback. | Document timeScale behavior clearly. `Time.fixedDeltaTime` scales with timeScale (good for hitstop consistency). Use `Time.fixedUnscaledDeltaTime` only if effect should ignore timeScale. |

---

## 💡 Lessons Learned

### Unity 6.3 Retro-Modern Standards
- **PPU (Pixels Per Unit):** Always use **32 PPU**. No sub-pixel drifting.
- **Physics:** Ensure retro-precision. Avoid standard "slippery" modern physics defaults if they conflict with the tight feel of a retro game.
- **API usage:** Verify all Unity APIs against version 6.3 documentation. Do not assume modern "shortcuts" exist if they weren't in 6.3.
- **Raycasts:** Always require a **Collider2D** on the target. Scripts alone are invisible to physics.

### ScriptableObject Architecture
- **State Pollution:** ScriptableObjects persist values in the Editor. Always reset runtime data in `OnEnable` (when `!Application.isPlaying`) to prevent test runs from dirtying assets.
- **References:** Prefer dragging Asset references (`.asset`) over typing String keys. It makes refactoring safer and prevents typos.
