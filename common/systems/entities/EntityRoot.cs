using System;
using Core.ECS;
using Godot;

/// <summary>
/// Base node for all ECS-backed scene entities.
///
/// This node sits at the top of an entity’s scene representation and is responsible for
/// binding scene data (nodes, visuals, state) to the ECS runtime through the factory system.
///
/// Concrete entity scenes must derive from this type and implement <see cref="Setup"/>,
/// which receives strongly-typed spawn data and applies any initial configuration
/// (positioning, stats, visuals, child-node wiring, etc.).
///
/// EntityRoot has no built-in logic. Its sole role is to act as the handshake point
/// between a Godot scene instance and the entity creation pipeline.
/// </summary>
[GlobalClass]
public abstract partial class EntityRoot : Node
{
    /// <summary>
    /// Configures this scene instance using the provided spawn data.
    ///
    /// Called automatically by an <see cref="EntityFactory"/> when the entity scene is created.
    /// Implementations should pull all relevant information from <paramref name="data"/>
    /// and apply it to nodes, components, or ECS state.
    ///
    /// This method should be considered the authoritative construction step for any
    /// scene-based entity.
    /// </summary>
    public abstract void Setup(Entity entity, EntitySpawnData data);

    /// <summary>
    /// Ensures that the supplied <see cref="EntitySpawnData"/> is of the expected
    /// concrete type. This is a convenience helper for derived <see cref="Setup"/>
    /// methods that require strongly-typed spawn data.
    /// </summary>
    /// <typeparam name="TCast">The required spawn data type.</typeparam>
    /// <param name="data">The raw spawn data provided by the entity factory.</param>
    /// <returns>The spawn data cast to the required type.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="data"/> is not of type <typeparamref name="TCast"/>.
    /// </exception>
    protected static TCast RequireSpawnData<TCast>(EntitySpawnData data)
        where TCast : EntitySpawnData
    {
        if (data is not TCast cast)
            throw new InvalidOperationException($"Expected {typeof(TCast).Name}");

        return cast;
    }

    /// <summary>
    /// Ensures that a required exported node or object is assigned in the inspector.
    /// Throws an <see cref="InvalidOperationException"/> if the node is null.
    /// </summary>
    /// <typeparam name="T">The type of the node or object being validated.</typeparam>
    /// <param name="node">The exported field to check for assignment.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="node"/> is null, indicating it was not assigned in the inspector.
    /// </exception>
    protected static void AssertAssigned<T>(T node)
    {
        if (node is null)
            throw new InvalidOperationException($"Required export '{nameof(node)}' is not assigned in the inspector.");
    }
}
