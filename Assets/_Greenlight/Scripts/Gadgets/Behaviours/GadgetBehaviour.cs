using UnityEngine;

namespace Greenlight.Gadgets
{
    /// <summary>
    /// Abstract base class for gadget behaviours - the "Body" that handles execution.
    /// Lives on a prefab, instantiated by GadgetUser when equipped.
    /// 
    /// Design Philosophy (Data + Behavior Pairs):
    /// - Brain: GadgetDefinitionSO (ScriptableObject with metadata)
    /// - Body: This MonoBehaviour (handles physics, VFX, scene interactions)
    /// - Separation enables data-driven gadget creation
    /// </summary>
    public abstract class GadgetBehaviour : MonoBehaviour
    {
        /// <summary>
        /// The definition (Brain) this behaviour is executing for.
        /// Set by Initialize().
        /// </summary>
        protected GadgetDefinitionSO Definition { get; private set; }

        /// <summary>
        /// The GadgetUser that owns this behaviour.
        /// Set by Initialize().
        /// </summary>
        protected GadgetUser User { get; private set; }

    /// <summary>
    /// Is this gadget currently executing?
    /// Used to prevent overlapping executions.
    /// </summary>
    public bool IsExecuting { get; protected set; }

    /// <summary>
    /// Does this gadget lock player movement while executing?
    /// Override to return true for traversal or other movement-controlling gadgets.
    /// </summary>
    public virtual bool LocksMovement => false;

        /// <summary>
        /// Called by GadgetUser after instantiation.
        /// Override to perform custom initialization.
        /// </summary>
        /// <param name="definition">The gadget definition (Brain).</param>
        /// <param name="user">The user who owns this gadget.</param>
        public virtual void Initialize(GadgetDefinitionSO definition, GadgetUser user)
        {
            Definition = definition;
            User = user;
        }

        /// <summary>
        /// Check if the gadget can currently be used.
        /// Override to add mana checks, cooldowns, etc.
        /// </summary>
        /// <param name="context">Execution context with user state.</param>
        /// <returns>True if gadget can execute, false otherwise.</returns>
        public virtual bool CanExecute(GadgetExecutionContext context)
        {
            return !IsExecuting;
        }

        /// <summary>
        /// Execute the gadget's primary action.
        /// Override to implement gadget-specific logic.
        /// </summary>
        /// <param name="context">Execution context with user state.</param>
        public abstract void Execute(GadgetExecutionContext context);

        /// <summary>
        /// Called when gadget is unequipped or player swaps away.
        /// Override to clean up VFX, cancel async operations, etc.
        /// </summary>
        public virtual void Terminate()
        {
            IsExecuting = false;
        }
    }
}
