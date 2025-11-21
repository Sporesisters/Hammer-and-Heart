using Godot;
using System.Collections.Generic;

namespace Core.ECS.Internals;

/// <summary>
/// Maintains a lookup from scene nodes (EntityRoot) to Entities.
/// This allows components or systems that only have access to a Node
/// to resolve which entity owns it.
/// </summary>
public class EntityBridge
{
    /// <summary>
    /// Maps the root Node of an entity's scene (EntityRoot) to its entity ID.
    /// Using EntityRoot ensures we always bind the top-level node of the entity.
    /// </summary>
    private readonly Dictionary<EntityRoot, int> _rootToEntityId = [];

    /// <summary>
    /// Binds a node to an entity.
    /// Only the root node of the entity's scene should be bound.
    /// </summary>
    /// <param name="root">The EntityRoot node representing the entity in the scene.</param>
    /// <param name="entity">The ECS entity to bind.</param>
    public void RegisterRoot(EntityRoot root, Entity entity)
        => _rootToEntityId[root] = entity.EntityIdentity.RuntimeId;

    /// <summary>
    /// Unbinds a node from its entity.
    /// Removes the entry from the lookup.
    /// </summary>
    /// <param name="root">The EntityRoot node to unbind.</param>
    public void UnregisterRoot(EntityRoot root) => _rootToEntityId.Remove(root);

    /// <summary>
    /// Retrieves the entity ID associated with a node.
    /// Climb the scene tree until an EntityRoot is found and return its entity ID.
    /// Returns null if no associated entity is found.
    /// </summary>
    /// <param name="node">The Node to resolve.</param>
    /// <returns>The entity ID owning the node, or null if none is found.</returns>
    public int? GetEntityId(Node node)
    {
        while (node is not null)
        {
            if (node is EntityRoot root && _rootToEntityId.TryGetValue(root, out int entityId))
                return entityId;

            node = node.GetParent();
        }

        return null;
    }

    /// <summary>
    /// Clears all bindings. Should be called when resetting a level or unloading entities.
    /// </summary>
    public void Clear() => _rootToEntityId.Clear();
}
