using System.Collections.Generic;

namespace Core.ECS.Internals;

/// <summary>
/// Stores all live <see cref="Entity"/> instances and assigns each one
/// a unique runtime ID. The registry owns strong references, so entities
/// remain alive until explicitly removed or cleared.
/// </summary>
public class EntityRegistry
{
    /// <summary>
    /// Monotonically increasing counter used to assign new runtime IDs.
    /// </summary>
    private int _runtimeCounter = 0;

    /// <summary>
    /// Direct mapping from runtime ID to entity instance.
    /// </summary>
    private readonly Dictionary<int, Entity> _lookup = [];

    /// <summary>
    /// Adds an entity to the registry and assigns it a new runtime ID.
    /// </summary>
    /// <param name="entity">The entity being registered.</param>
    public void Add(Entity entity)
    {
        int id = ++_runtimeCounter;
        entity.EntityIdentity.ResetRuntimeId(id);
        _lookup[id] = entity;
    }

    /// <summary>
    /// Removes an entity from the registry.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    public void Remove(Entity entity) => _lookup.Remove(entity.EntityIdentity.RuntimeId);

    /// <summary>
    /// Fetches an entity by its assigned runtime ID.
    /// Returns null if the ID does not exist.
    /// </summary>
    /// <param name="runtimeId">The runtime ID assigned to the entity.</param>
    /// <returns>The matching entity, or null if not found.</returns>
    public Entity? GetEntity(int runtimeId)
    {
        return _lookup.TryGetValue(runtimeId, out Entity? entity)
            ? entity
            : null;
    }

    /// <summary>
    /// Clears all entities from the registry.
    /// Optionally resets the ID counter so future IDs start from 1 again.
    /// </summary>
    /// <param name="resetCounter">
    /// If true, resets the counter to zero before adding new entities.
    /// </param>
    public void Clear(bool resetCounter = false)
    {
        _lookup.Clear();

        if (resetCounter)
            _runtimeCounter = 0;
    }
}
