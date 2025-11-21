using Core.Events;

namespace Core.ECS.Events;

/// <summary>
/// Event data passed when an entity is destroyed.
/// </summary>
/// <param name="entity">The entity to be destroyed.</param>
/// <param name="freeRootNode">
/// Whether the entity's root scene node should also be freed.
/// </param>
public readonly struct EntityDestroyEvent(Entity entity, bool freeRootNode) : IEvent
{
    /// <summary>
    /// The entity targeted for destruction.
    /// </summary>
    public readonly Entity Entity = entity;

    /// <summary>
    /// If <c>true</c>, the entity's associated root node is unregistered and freed.
    /// </summary>
    public readonly bool FreeRootNode = freeRootNode;
}
