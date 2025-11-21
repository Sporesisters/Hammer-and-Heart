using System;
using System.Collections.Generic;
using Core.ECS.Internals;

namespace Core.ECS;

/// <summary>
/// Manages all ECS systems for a given <see cref="EntityWorld"/>, including process, physics,
/// and event-driven systems. Systems are automatically registered to the world's event bus
/// and have their <see cref="ISystem.World"/> property set to the provided world.
/// </summary>
/// <param name="world">The <see cref="EntityWorld"/> that owns this system manager and provides the event bus.</param>
public class SystemManager(EntityWorld world)
{
    /// <summary>
    /// Maps each process system type to its instance.
    /// Ensures only one system of a given type exists at a time and allows type-based access or removal.
    /// </summary>
    private readonly Dictionary<Type, IProcessSystem> _processSystems = [];

    /// <summary>
    /// Maps each physics system type to its instance.
    /// Guarantees a single instance per type and allows type-based removal or retrieval.
    /// </summary>
    private readonly Dictionary<Type, IPhysicsSystem> _physicsSystems = [];

    /// <summary>
    /// Maps each event-driven system type to its instance.
    /// Event systems are automatically attached to the event bus when added and detached when removed.
    /// </summary>
    private readonly Dictionary<Type, IEventSystem> _eventSystems = [];

    /// <summary>
    /// The <see cref="EntityWorld"/> that owns this <see cref="SystemManager"/>.
    /// Provides access to the world's <see cref="EntityWorld.EventBus"/> for automatically
    /// attaching or detaching event-driven systems, and serves as a reference
    /// for systems that need to interact with the world.
    /// </summary>
    private readonly EntityWorld _world = world;

    /// <summary>
    /// Adds a system to the manager. If the system is an event system, it is automatically attached to the bus.
    /// Replaces any existing system of the same type.
    /// </summary>
    /// <typeparam name="T">The type of system to add.</typeparam>
    /// <returns>The added system.</returns>
    public T AddSystem<T>() where T : ISystem, new()
    {
        T system = new();
        Type type = typeof(T);

        system.World = _world;

        if (system is IProcessSystem processSystem)
            _processSystems[type] = processSystem;

        if (system is IPhysicsSystem physicsSystem)
            _physicsSystems[type] = physicsSystem;

        if (system is IEventSystem eventSystem)
        {
            eventSystem.Attach(_world.EventBus);
            _eventSystems[type] = eventSystem;
        }

        return system;
    }

    /// <summary>
    /// Removes a system from the manager by type. Event systems are automatically detached from the bus.
    /// </summary>
    /// <typeparam name="T">The type of system to remove.</typeparam>
    /// <returns>True if a system was removed; otherwise, false.</returns>
    public bool RemoveSystem<T>() where T : ISystem
    {
        Type type = typeof(T);
        bool removed = false;

        if (_processSystems.Remove(type))
            removed = true;

        if (_physicsSystems.Remove(type))
            removed = true;

        if (_eventSystems.TryGetValue(type, out IEventSystem? eventSystem))
        {
            eventSystem.Detach(_world.EventBus);
            _eventSystems.Remove(type);
            removed = true;
        }

        return removed;
    }

    /// <summary>
    /// Updates all registered process systems.
    /// </summary>
    /// <param name="delta">Time in seconds since the last update.</param>
    public void Update(double delta)
    {
        foreach (IProcessSystem processSystem in _processSystems.Values)
            processSystem.Update(delta);
    }

    /// <summary>
    /// Updates all registered physics systems.
    /// </summary>
    /// <param name="delta">Time in seconds since the last physics update.</param>
    public void PhysicsUpdate(double delta)
    {
        foreach (IPhysicsSystem physicsSystem in _physicsSystems.Values)
            physicsSystem.PhysicsUpdate(delta);
    }

    /// <summary>
    /// Clears all systems from the manager, detaching event systems from the bus.
    /// </summary>
    public void Clear()
    {
        foreach (IEventSystem eventSystem in _eventSystems.Values)
            eventSystem.Detach(_world.EventBus);

        _processSystems.Clear();
        _physicsSystems.Clear();
        _eventSystems.Clear();
    }
}
