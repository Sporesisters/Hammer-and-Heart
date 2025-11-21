using System;
using System.Collections.Generic;
using Godot;

namespace Core.ECS;

/// <summary>
/// A central factory for creating entities based on spawn data.
/// </summary>
/// <remarks>
/// This static class allows registration of factory handlers for different types of <see cref="EntitySpawnData"/>.
/// It decouples entity creation logic from the world and entity implementations.
/// </remarks>
public static class EntityFactory
{
    /// <summary>
    /// Internal mapping from a concrete <see cref="EntitySpawnData"/> type
    /// to a factory function that creates the corresponding <see cref="EntityRoot"/>.
    /// </summary>
    private static readonly Dictionary<Type, Func<EntitySpawnData, EntityRoot>> _handlers = [];

    /// <summary>
    /// Registers a factory handler for a specific type of <see cref="EntitySpawnData"/>.
    /// </summary>
    /// <typeparam name="TData">The concrete type of spawn data.</typeparam>
    /// <param name="handler">A function that creates an <see cref="EntityRoot"/> from the spawn data.</param>
    public static void Register<TData>(Func<TData, EntityRoot> handler) where TData : EntitySpawnData
        => _handlers[typeof(TData)] = data => handler((TData)data);

    /// <summary>
    /// Spawns an entity using the provided spawn data and adds it to the given parent node in the scene tree.
    /// </summary>
    /// <typeparam name="TData">The concrete type of spawn data.</typeparam>
    /// <param name="data">The spawn data describing the entity to create.</param>
    /// <param name="parent">The Godot node to which the entity root will be added.</param>
    /// <returns>The spawned <see cref="Entity"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no factory handler has been registered for the type of spawn data.</exception>
    public static Entity Spawn<TData>(TData data, Node parent) where TData : EntitySpawnData
    {
        if (!_handlers.TryGetValue(typeof(TData), out Func<EntitySpawnData, EntityRoot>? handler))
            throw new InvalidOperationException($"No factory registered for {typeof(TData).Name}");

        EntityRoot root = handler(data);
        Entity entity = data.World.CreateEntity(data.Spec, root);

        root.Setup(entity, data);
        parent.AddChild(root);

        return entity;
    }
}
