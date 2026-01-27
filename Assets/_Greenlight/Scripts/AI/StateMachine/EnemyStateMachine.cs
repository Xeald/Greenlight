using System;
using System.Collections.Generic;
using UnityEngine;

namespace Greenlight.AI
{
    /// <summary>
    /// State machine for enemy AI that manages state transitions.
    /// Provides type-safe state management with debugging support.
    /// </summary>
    public class EnemyStateMachine
    {
        private readonly Dictionary<Type, EnemyState> _states = new();
        private EnemyState _currentState;
        private EnemyController _owner;

        /// <summary>
        /// Current active state.
        /// </summary>
        public EnemyState CurrentState => _currentState;

        /// <summary>
        /// Name of the current state (for debugging).
        /// </summary>
        public string CurrentStateName => _currentState?.GetType().Name ?? "None";

        /// <summary>
        /// Initialize the state machine with its owner.
        /// </summary>
        /// <param name="owner">The enemy controller that owns this state machine</param>
        public void Initialize(EnemyController owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Add a state to the state machine.
        /// </summary>
        /// <typeparam name="T">Type of state to add</typeparam>
        /// <param name="state">State instance to add</param>
        public void AddState<T>(T state) where T : EnemyState
        {
            Type stateType = typeof(T);
            
            if (_states.ContainsKey(stateType))
            {
                Debug.LogWarning($"[EnemyStateMachine] State {stateType.Name} already exists. Replacing.");
            }

            state.Initialize(_owner, this);
            _states[stateType] = state;
        }

        /// <summary>
        /// Check if the state machine has a specific state type.
        /// </summary>
        /// <typeparam name="T">Type of state to check</typeparam>
        /// <returns>True if the state exists</returns>
        public bool HasState<T>() where T : EnemyState
        {
            return _states.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Get a specific state by type.
        /// </summary>
        /// <typeparam name="T">Type of state to get</typeparam>
        /// <returns>State instance or null if not found</returns>
        public T GetState<T>() where T : EnemyState
        {
            _states.TryGetValue(typeof(T), out EnemyState state);
            return state as T;
        }

        /// <summary>
        /// Transition to a specific state type.
        /// </summary>
        /// <typeparam name="T">Type of state to transition to</typeparam>
        public void TransitionTo<T>() where T : EnemyState
        {
            Type stateType = typeof(T);

            if (!_states.TryGetValue(stateType, out EnemyState targetState))
            {
                Debug.LogError($"[EnemyStateMachine] Cannot transition to {stateType.Name} - state not found!");
                return;
            }

            TransitionTo(targetState);
        }

        /// <summary>
        /// Transition to a specific state instance.
        /// </summary>
        /// <param name="targetState">State to transition to</param>
        public void TransitionTo(EnemyState targetState)
        {
            if (targetState == null)
            {
                Debug.LogError("[EnemyStateMachine] Cannot transition to null state!");
                return;
            }

            if (_currentState == targetState)
                return; // Already in target state

            // Exit current state
            _currentState?.Exit();

            // Store previous state for debugging
            string previousStateName = _currentState?.GetType().Name ?? "None";
            
            // Transition to new state
            _currentState = targetState;
            _currentState.Enter();

#if UNITY_EDITOR
            if (_owner != null)
            {
                Debug.Log($"[{_owner.name}] State transition: {previousStateName} -> {_currentState.GetType().Name}");
            }
#endif
        }

        /// <summary>
        /// Start the state machine with an initial state.
        /// </summary>
        /// <typeparam name="T">Type of initial state</typeparam>
        public void Start<T>() where T : EnemyState
        {
            if (_currentState != null)
            {
                Debug.LogWarning("[EnemyStateMachine] State machine already started!");
                return;
            }

            TransitionTo<T>();
        }

        /// <summary>
        /// Update the state machine (call from owner's Update).
        /// </summary>
        public void Update()
        {
            if (_currentState == null)
                return;

            // Execute current state
            _currentState.Execute();

            // Check for state transitions
            EnemyState nextState = _currentState.CheckTransitions();
            if (nextState != null && nextState != _currentState)
            {
                TransitionTo(nextState);
            }
        }

        /// <summary>
        /// Handle damage taken (forwards to current state).
        /// </summary>
        /// <param name="damage">Amount of damage taken</param>
        /// <param name="source">Source of the damage</param>
        public void OnDamageTaken(int damage, Transform source)
        {
            _currentState?.OnDamageTaken(damage, source);
        }

        /// <summary>
        /// Handle health depletion (forwards to current state).
        /// </summary>
        public void OnHealthDepleted()
        {
            _currentState?.OnHealthDepleted();
        }

        /// <summary>
        /// Stop the state machine and clean up.
        /// </summary>
        public void Stop()
        {
            _currentState?.Exit();
            _currentState = null;
        }

        /// <summary>
        /// Check if the state machine is currently in a specific state type.
        /// </summary>
        /// <typeparam name="T">Type of state to check</typeparam>
        /// <returns>True if currently in that state</returns>
        public bool IsInState<T>() where T : EnemyState
        {
            return _currentState != null && _currentState.GetType() == typeof(T);
        }

        /// <summary>
        /// Force a state transition without calling Exit/Enter (for special cases).
        /// </summary>
        /// <typeparam name="T">Type of state to force to</typeparam>
        public void ForceTransitionTo<T>() where T : EnemyState
        {
            Type stateType = typeof(T);

            if (!_states.TryGetValue(stateType, out EnemyState targetState))
            {
                Debug.LogError($"[EnemyStateMachine] Cannot force transition to {stateType.Name} - state not found!");
                return;
            }

            _currentState = targetState;

#if UNITY_EDITOR
            if (_owner != null)
            {
                Debug.Log($"[{_owner.name}] Force transition to: {_currentState.GetType().Name}");
            }
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Get debug information about the state machine.
        /// </summary>
        /// <returns>Debug info string</returns>
        public string GetDebugInfo()
        {
            string info = $"Current State: {CurrentStateName}\n";
            info += $"Available States: {string.Join(", ", _states.Keys)}\n";
            
            if (_currentState != null)
            {
                info += $"Time in State: {_currentState.TimeInState:F2}s";
            }

            return info;
        }
#endif
    }
}