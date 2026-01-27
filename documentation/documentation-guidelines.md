# Documentation Guidelines

**Version 1.0** | *Project Standard*

This document defines the **documentation standards** for the Greenlight project. All AI agents and developers must adhere to this format to ensure the codebase remains maintainable and accessible to both technical and non-technical team members.

---

## 📝 Core Philosophy

Documentation in Greenlight is not an afterthought; it is a **deliverable**. It bridges the gap between raw code and Unity implementation, ensuring that designers and administrators can configure systems without reading C#.

### The "Gemmy" Documentation Style
- **Clear & Metaphorical**: Use analogies (e.g., "The Brain", "The Nerves") to explain abstract concepts.
- **Emoji-Signaled**: Use specific emojis to denote section types (see below).
- **Action-Oriented**: Focus on "How to Use" rather than just "How it Works."

---

## 🏗️ Structure Standard

Every technical documentation file must follow this Markdown structure:

### 1. Header
- **Title**: Clear and descriptive (e.g., `# The Nervous System: Technical Documentation`).
- **Version**: Current version and phase (e.g., `**Version 1.0** | *Phase 1 Architecture*`).
- **Abstract**: A 2-3 sentence high-level summary of what this system does and why it exists.

### 2. 🧩 Core Concepts (Theory)
- Explain the major components.
- Use a bulleted list for each component detailing:
  - **Type**: (ScriptableObject / MonoBehaviour / Pure C#)
  - **Location**: (Where the asset lives in the project)
  - **Function**: (What it actually does)

### 3. 🛠️ How to Use (Practice)
- **Target Audience**: Designers & Unity Admins.
- **Format**: Step-by-step tutorials for common scenarios (e.g., "Scenario A: Creating a New Item").
- **Requirement**: Must include specific instructions like "Right-click → Create..." or "Drag asset into slot...".

### 4. 💻 Coding Standards (For Devs)
- **Target Audience**: Programmers & AI Agents.
- **Content**: Best practices, code snippets, and API usage examples.
- **Goal**: Prevent anti-patterns (e.g., "Don't use GameObject.Find, do X instead").

### 5. ⚙️ Administrator Configuration
- **Target Audience**: Technical Artists / Admins setting up scenes.
- **Content**: Setup checklists, required components, debug tools, and troubleshooting steps.

### 6. 📂 File Structure
- A visual tree representation of where the relevant scripts and assets are located.

---

## 🎨 Formatting Rules

| Element | Style | Example |
| :--- | :--- | :--- |
| **Section Headers** | H2 with Emoji | `## 🧩 Core Concepts` |
| **Subsection Headers** | H3 | `### 1. Global Game State` |
| **Key Terms** | Bold | **Game State** |
| **File Paths/Code** | Backticks | `Assets/_Greenlight/Data` |
| **External Links** | Standard MD | `[Roadmap](Dev-plans/ROADMAP.md)` |

### Approved Emojis
- 🧩 **Core Concepts** (Theory / Architecture)
- 🛠️ **How to Use** (Designer Workflows)
- 💻 **Coding Standards** (API / Code Snippets)
- ⚙️ **Configuration** (Setup / Debugging)
- 📂 **File Structure** (Project Hierarchy)
- 🚀 **Getting Started** (Quick Start Guides)

---

## 🚨 When to Update Documentation

AI Agents and Developers **MUST** update or create documentation when:

1. **New Systems**: A new major system (e.g., Dialogue, Combat) is architected. -> *Create new file.*
2. **Workflow Changes**: The way a designer interacts with the system changes (e.g., "We now use Asset Flags instead of Strings"). -> *Update 🛠️ How to Use.*
3. **New Tools**: A new Editor Tool or Debug Window is created. -> *Update ⚙️ Administrator Configuration.*
4. **Breaking Changes**: An API change requires existing code to be refactored. -> *Update 💻 Coding Standards.*

### Verification Protocol
Before marking a task as "Complete," ask:
> *"Can a new team member configure this system using ONLY this document?"*
> If the answer is **No**, the documentation is incomplete.

---

## 📂 Documentation Registry

Keep the `documentation/` folder organized:

- `documentation/nervous-system.md` (Phase 1: Global State & Events)
- `documentation/phase2-vocabulary.md` (Phase 2: Player Controller & Gadgets)  
- `documentation/phase3-conversation.md` (Phase 3: Combat, AI & Economy)
- `documentation/phase3-quick-reference.md` (Phase 3: Developer Quick Reference)
- `documentation/camera-system.md` (Camera & Rendering Setup)
- `documentation/documentation-guidelines.md` (This file)
- `README.md` (Project Root - Entry Point)
