# Assembly Definitions & Architecture Boundaries

**Version 1.0** | *Project Architecture Standard*

This document explains how Greenlight uses **Assembly Definitions** (`.asmdef`) to structure code, enforce architectural boundaries, and speed up compilation. Understanding this system is critical for avoiding circular dependency errors and maintaining clean code organization.

---

## 🧩 Core Concepts

### 1. What Are Assembly Definitions?

Assembly Definitions (`.asmdef` files) tell Unity to compile groups of scripts into separate **assemblies** (DLLs). This provides:

- **Faster Compilation**: Only changed assemblies recompile, not the entire project
- **Clear Dependencies**: You explicitly declare which assemblies depend on others
- **Enforced Architecture**: Prevents spaghetti code by making illegal references cause compile errors

### 2. The Greenlight Assembly Structure

```
Greenlight.Core
    ↓
    ├─→ Greenlight.Combat
    ├─→ Greenlight.AI (also depends on Combat, Gadgets)
    ├─→ Greenlight.Gadgets
    ├─→ Greenlight.Economy
    └─→ Greenlight.Environment
         ↓
    Greenlight.UI (depends on Core, Combat, Economy)
```

**Key Rule**: Dependencies form a **Directed Acyclic Graph (DAG)**. No circular references allowed.

### 3. Assembly Locations

| Assembly | Location | Purpose |
| :--- | :--- | :--- |
| `Greenlight.Core` | `Scripts/Core/` | Game state, events, utilities |
| `Greenlight.Combat` | `Scripts/Combat/` | Damage, health, feedback |
| `Greenlight.AI` | `Scripts/AI/` | Enemy behaviors, state machines |
| `Greenlight.Gadgets` | `Scripts/Gadgets/` | Player abilities, gadget system |
| `Greenlight.Economy` | `Scripts/Economy/` | Luni, merchants, items |
| `Greenlight.Environment` | `Scripts/Environment/` | Interactive world objects |
| `Greenlight.UI` | `Scripts/UI/` | HUD, menus, merchant panels |
| `Greenlight.Player` | `Scripts/Player/` | Player controller, input |

---

## 🛠️ How to Use

### Scenario A: Adding a Script to an Existing Module

**Example**: Creating a new enemy type in the AI module.

1. **Create your script** in the appropriate folder:
   - `Assets/_Greenlight/Scripts/AI/Enemies/MyNewEnemy.cs`

2. **Use the module's namespace**:
   ```csharp
   namespace Greenlight.AI
   {
       public class MyNewEnemy : EnemyController
       {
           // Your code here
       }
   }
   ```

3. **No `.asmdef` changes needed** - it's automatically part of `Greenlight.AI`.

### Scenario B: Using Types from Other Assemblies

**Example**: Your AI script needs to use `DamageType` from the Combat assembly.

1. **Add the `using` directive** in your code:
   ```csharp
   using UnityEngine;
   using Greenlight.Combat;  // ← Import Combat namespace
   
   namespace Greenlight.AI
   {
       public class MyEnemy : MonoBehaviour
       {
           public void Attack(DamageType damageType) { }
       }
   }
   ```

2. **Verify assembly reference exists** in `Greenlight.AI.asmdef`:
   ```json
   {
       "name": "Greenlight.AI",
       "references": [
           "Greenlight.Core",
           "Greenlight.Combat",  // ← Must be listed!
           "Greenlight.Gadgets"
       ]
   }
   ```

3. **If missing**, you'll get error `CS0234: The type or namespace name 'Combat' does not exist`.

### Scenario C: Avoiding Circular Dependencies

**Problem**: Economy wants to use `MerchantPanel` from UI, but UI already depends on Economy.

**❌ Wrong Approach**:
```json
// Economy.asmdef - DON'T DO THIS
{
    "references": [
        "Greenlight.UI"  // ← Creates circular dependency!
    ]
}
```

**✅ Correct Approach** - Use reflection:
```csharp
// In MerchantInteractable.cs (Economy assembly)
var merchantPanelType = System.Type.GetType("Greenlight.UI.MerchantPanel, Greenlight.UI");
if (merchantPanelType != null)
{
    var component = gameObject.GetComponent(merchantPanelType);
    var method = merchantPanelType.GetMethod("InitializeMerchant");
    method?.Invoke(component, new object[] { inventory, gameState });
}
```

---

## 💻 Coding Standards

### 1. Namespace Convention

**Always use the assembly's namespace**:
```csharp
// In Greenlight.Combat assembly
namespace Greenlight.Combat
{
    public class HealthComponent : MonoBehaviour { }
}
```

**Never use wrong namespaces**:
```csharp
// ❌ DON'T: Script is in AI folder but uses Combat namespace
namespace Greenlight.Combat  // WRONG!
{
    public class EnemyController { }
}
```

### 2. Assembly Reference Format

**Prefer GUID references** over string names:

**❌ String-based (fragile)**:
```json
{
    "references": [
        "Greenlight.Combat"
    ]
}
```

**✅ GUID-based (robust)**:
```json
{
    "references": [
        "GUID:d8f75c77bab1096489170b119932edff"
    ]
}
```

**Finding GUIDs**: Check the `.meta` file next to the `.asmdef`:
```yaml
# Greenlight.Combat.asmdef.meta
guid: d8f75c77bab1096489170b119932edff
```

### 3. Checking Dependencies Before Adding Types

**Before using a type from another assembly:**

1. Check where the type is defined:
   ```bash
   # PowerShell
   Get-ChildItem -Recurse -Filter "*.cs" | Select-String "class TypeName"
   ```

2. Check the namespace:
   ```csharp
   namespace Greenlight.SomeModule { }
   ```

3. Verify your `.asmdef` references that assembly.

---

## ⚙️ Administrator Configuration

### Troubleshooting Compilation Errors

#### Error: `CS0234: The type or namespace name 'X' does not exist in the namespace 'Greenlight'`

**Cause**: Missing assembly reference in `.asmdef`.

**Fix**:
1. Find which assembly contains the type (e.g., `DamageType` is in `Greenlight.Combat`)
2. Open your module's `.asmdef` file
3. Add the assembly to the `references` array (prefer GUID)
4. Save and let Unity reimport

#### Error: `CS0246: The type or namespace name 'TypeName' could not be found`

**Cause**: Missing `using` directive OR missing assembly reference.

**Fix**: Add BOTH:
1. Add `using Greenlight.TargetModule;` to your code
2. Ensure `.asmdef` references the target assembly

#### Error: Circular dependency detected

**Cause**: Assembly A depends on B, and B depends on A.

**Fix**: 
- Use **reflection** to call across the boundary without compile-time reference
- Use **interfaces** defined in `Core` that both assemblies implement
- Use **events** to communicate indirectly

### Verification Checklist

When adding a new `.asmdef`:
- ✅ File is in the correct folder (`Scripts/ModuleName/`)
- ✅ `name` matches the folder structure (`Greenlight.ModuleName`)
- ✅ `rootNamespace` matches `name`
- ✅ All required dependencies are in `references`
- ✅ No circular dependencies exist
- ✅ Scripts in the folder use the correct namespace

---

## 📂 File Structure

```
Assets/_Greenlight/Scripts/
├── Core/
│   └── Greenlight.Core.asmdef
├── Combat/
│   ├── Interfaces/
│   ├── Runtime/
│   ├── Feedback/
│   └── Greenlight.Combat.asmdef
├── AI/
│   ├── Data/
│   ├── Enemies/
│   ├── StateMachine/
│   └── Greenlight.AI.asmdef
├── Gadgets/
│   └── Greenlight.Gadgets.asmdef
├── Economy/
│   ├── Data/
│   ├── Runtime/
│   └── Greenlight.Economy.asmdef
├── Environment/
│   └── Greenlight.Environment.asmdef
├── UI/
│   ├── Merchant/
│   ├── HUD/
│   └── Greenlight.UI.asmdef
└── Player/
    └── Greenlight.Player.asmdef
```

Each `.asmdef` file controls compilation for all scripts in its folder and subfolders.

---

## 🚨 Common Pitfalls

### 1. "I added `using` but still get errors"
→ **Check your `.asmdef`** - the using directive is not enough!

### 2. "Unity won't let me reference assembly X"
→ **Check for circular dependencies** - draw the dependency graph on paper.

### 3. "My new script isn't compiling with the rest"
→ **Check the namespace** - it must match the assembly's root namespace.

### 4. "Changes to `.asmdef` don't take effect"
→ **Restart Unity** - sometimes assembly changes need a full reimport.

### 5. "Editor scripts can't find runtime types"
→ **Editor scripts need separate `.asmdef`** - create `Greenlight.ModuleName.Editor.asmdef`.
