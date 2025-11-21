using System;
using System.Collections.Generic;
using Core.ECS.Events;
using Core.ECS.Internals;
using Core.Events;
using Core.Utilities.Logging;
using Godot;

namespace Core.ECS;

/// <summary>
/// Represents a container for all entities and components in the ECS system.
/// Provides creation, destruction, querying, and indexing of entities and components.
/// </summary>
[GlobalClass]
public partial class EntityWorld : Node
{
    /// <summary>
    /// Maintains a reverse lookup from component types to the entities that contain them.
    /// Used for efficient querying of entities based on their component composition.
    /// </summary>
    private readonly ComponentIndex _index = new();

    /// <summary>
    /// Keeps track of all entities currently alive in the world and provides fast lookup by runtime ID.
    /// Uses weak references to allow garbage collection of unreferenced entities.
    /// </summary>
    private readonly EntityRegistry _registry = new();

    /// <summary>
    /// Bridges the scene tree nodes (EntityRoot) to ECS entities.
    /// Allows systems or components that only have access to a Node to resolve which entity owns it.
    /// </summary>
    private readonly EntityBridge _bridge = new();

    /// <summary>
    /// Manages all systems registered with this world, including process, physics, and reactive systems.
    /// Provides centralized update and event dispatch for systems.
    /// </summary>
    private readonly SystemManager _systemManager;

    /// <summary>
    /// The event bus for this world. Systems and entities can register listeners or publish events
    /// through this bus, enabling decoupled communication.
    /// </summary>
    public EventBus EventBus { get; private set; } = new();

    /// <summary>
    /// Initializes a new instance of <see cref="EntityWorld"/>.
    /// Creates a system manager linked to this world and prepares the event bus for system and entity use.
    /// </summary>
    public EntityWorld() => _systemManager = new SystemManager(this);

    /// <inheritdoc/>
    public override void _Process(double delta)
        => _systemManager.Update(delta);

    /// <inheritdoc/>
    public override void _PhysicsProcess(double delta)
        => _systemManager.PhysicsUpdate(delta);

    /// <summary>
    /// Creates a new entity in the world, initializes it, and registers it.
    /// </summary>
    /// <param name="spec">Specification describing the entity identity.</param>
    /// <param name="entityRoot">The scene node root associated with the entity.</param>
    /// <returns>The created <see cref="Entity"/> instance.</returns>
    public Entity CreateEntity(EntityIdentitySpec spec, EntityRoot entityRoot)
    {
        Entity entity = new();
        entity.Initialize(spec, this, entityRoot);
        _registry.Add(entity);
        _bridge.RegisterRoot(entityRoot, entity);

        entity.EventBus.AddListener<EntityDestroyEvent>(DestroyEntity);
        LoggerService.Info($"Entity <{entity.EntityIdentity}> initialized successfully.");

        return entity;
    }

    /// <summary>
    /// Retrieves an entity by its runtime ID.
    /// </summary>
    /// <param name="runtimeId">The runtime ID of the entity.</param>
    /// <returns>The entity if found; otherwise, <c>null</c>.</returns>
    public Entity? GetEntity(int runtimeId) => _registry.GetEntity(runtimeId) ?? null;

    /// <summary>
    /// Retrieves an entity associated with a <see cref="Node"/>.
    /// Climb the scene tree to find an <see cref="EntityRoot"/> node and resolves the entity.
    /// </summary>
    /// <param name="node">The node to resolve to an entity.</param>
    /// <returns>The entity if found; otherwise, <c>null</c>.</returns>
    public Entity? GetEntity(Node node)
    {
        int? runtimeId = _bridge.GetEntityId(node);
        return runtimeId.HasValue ? _registry.GetEntity(runtimeId.Value) : null;
    }

    /// <summary>
    /// Destroys an entity and optionally frees its associated root node.
    /// This method is triggered through the event system to ensure that
    /// destruction is observable and systems can react before teardown.
    /// </summary>
    /// <param name="event">
    /// Contains the entity to destroy and whether its root node should be freed.
    /// </param>
    /// <remarks>
    /// The entity is removed from the world's registry. If <see cref="EntityDestroyEvent.FreeRootNode"/>
    /// is <c>true</c> and the entity has a root scene node, that node is unregistered
    /// and queued for deletion. Components handle their own cleanup via system
    /// reactions to this event; the world does not iterate components directly.
    /// </remarks>
    private void DestroyEntity(EntityDestroyEvent @event)
    {
        _registry.Remove(@event.Entity);

        if (@event.FreeRootNode)
        {
            _bridge.UnregisterRoot(@event.Entity.Root);
            @event.Entity.Root.QueueFree();
        }
    }

    /// <summary>
    /// Called when a component is added to an entity.
    /// Updates the component index for efficient querying.
    /// </summary>
    /// <param name="entity">The entity receiving the component.</param>
    /// <param name="componentType">The type of the component added.</param>
    public void OnComponentAdded(Entity entity, Type componentType)
        => _index.AddComponentReference(entity.EntityIdentity.RuntimeId, componentType);

    /// <summary>
    /// Called when a component is removed from an entity.
    /// Updates the component index to reflect the removal.
    /// </summary>
    /// <param name="entity">The entity losing the component.</param>
    /// <param name="componentType">The type of the component removed.</param>
    public void OnComponentRemoved(Entity entity, Type componentType)
        => _index.RemoveComponentReference(entity.EntityIdentity.RuntimeId, componentType);

    /// <summary>
    /// Queries all entities that contain the specified component types.
    /// </summary>
    /// <param name="componentTypes">The component types to query for.</param>
    /// <returns>An enumerable of entities matching all specified components.</returns>
    private IEnumerable<Entity> Query(params Type[] componentTypes)
    {
        foreach (int id in _index.GetEntitiesWith(componentTypes))
        {
            Entity? entity = _registry.GetEntity(id);

            if (entity is not null)
                yield return entity;
        }
    }

    /// <summary>
    /// Queries all entities that contain a component of type <typeparamref name="T1"/>.
    /// </summary>
    /// <typeparam name="T1">The type of component the entities must contain.</typeparam>
    /// <returns>
    /// An enumerable of <see cref="Entity"/> instances that have a <typeparamref name="T1"/> component.
    /// </returns>
    public IEnumerable<Entity> Query<T1>() where T1 : ComponentBase
        => Query(typeof(T1));

    /// <summary>
    /// Queries all entities that contain both <typeparamref name="T1"/> and <typeparamref name="T2"/> components.
    /// </summary>
    /// <typeparam name="T1">The first component type the entities must contain.</typeparam>
    /// <typeparam name="T2">The second component type the entities must contain.</typeparam>
    /// <returns>
    /// An enumerable of <see cref="Entity"/> instances that have both <typeparamref name="T1"/> and <typeparamref name="T2"/> components.
    /// </returns>
    public IEnumerable<Entity> Query<T1, T2>()
        where T1 : ComponentBase
        where T2 : ComponentBase
        => Query(typeof(T1), typeof(T2));

    /// <summary>
    /// Queries all entities that contain <typeparamref name="T1"/>, <typeparamref name="T2"/>, and <typeparamref name="T3"/> components.
    /// </summary>
    /// <typeparam name="T1">The first component type the entities must contain.</typeparam>
    /// <typeparam name="T2">The second component type the entities must contain.</typeparam>
    /// <typeparam name="T3">The third component type the entities must contain.</typeparam>
    /// <returns>
    /// An enumerable of <see cref="Entity"/> instances that have all three specified components.
    /// </returns>
    public IEnumerable<Entity> Query<T1, T2, T3>()
        where T1 : ComponentBase
        where T2 : ComponentBase
        where T3 : ComponentBase
        => Query(typeof(T1), typeof(T2), typeof(T3));

    /// <summary>
    /// Adds a system of type <typeparamref name="T"/> to this world.
    /// The system is automatically assigned this world via <see cref="ISystem.World"/>.
    /// Event systems are attached to the world’s <see cref="EventBus"/>.
    /// </summary>
    /// <typeparam name="T">The type of system to add. Must have a parameterless constructor.</typeparam>
    /// <returns>The created system instance.</returns>
    public T AddSystem<T>() where T : ISystem, new() => _systemManager.AddSystem<T>();

    /// <summary>
    /// Removes the system of type <typeparamref name="T"/> from this world.
    /// Event systems are automatically detached from the world’s <see cref="EventBus"/>.
    /// </summary>
    /// <typeparam name="T">The type of system to remove.</typeparam>
    /// <returns>True if a system was removed; otherwise, false.</returns>
    public bool RemoveSystem<T>() where T : ISystem => _systemManager.RemoveSystem<T>();

    /// <summary>
    /// Clears the world, removing all entities, components, and optionally systems and runtime counter.
    /// </summary>
    /// <param name="resetCounter">If <c>true</c>, the entity runtime counter is reset.</param>
    /// <param name="resetSystems">If <c>true</c>, all systems are cleared and event systems detached from the bus.</param>
    public void ResetWorld(bool resetCounter = false, bool resetSystems = false)
    {
        _registry.Clear(resetCounter);
        _index.Clear();
        _bridge.Clear();

        if (resetSystems)
            _systemManager.Clear();
    }
}
