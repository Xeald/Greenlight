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

---

## 💡 Lessons Learned

### Unity 6.3 Retro-Modern Standards
- **PPU (Pixels Per Unit):** Always use **32 PPU**. No sub-pixel drifting.
- **Physics:** Ensure retro-precision. Avoid standard "slippery" modern physics defaults if they conflict with the tight feel of a retro game.
- **API usage:** Verify all Unity APIs against version 6.3 documentation. Do not assume modern "shortcuts" exist if they weren't in 6.3.
