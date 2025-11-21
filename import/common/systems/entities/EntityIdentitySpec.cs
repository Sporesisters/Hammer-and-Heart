using System;

namespace Core.ECS;

/// <summary>
/// Specification data required to construct an <see cref="Entity.EntityIdentity"/>.
/// Used for both creating new entities and restoring from saves.
/// </summary>
/// <param name="Type">The broad category of the entity (e.g., Player, Item, Projectile).</param>
/// <param name="SubType">Optional finer classification within the type (e.g., "Sword", "HealthPotion").</param>
/// <param name="PersistentId">Optional persistent unique identifier, typically used for save/load.</param>
public readonly record struct EntityIdentitySpec(EntityType? Type = null, string? SubType = null, Guid? PersistentId = null)
{
	/// <summary>
	/// Represents an empty specification that does not override any values.
	/// Applying this spec to an entity leaves its existing <see cref="Entity.EntityIdentity"/> unchanged.
	/// Useful when you need a placeholder or want to initialize without modifying any fields.
	/// </summary>
	public static readonly EntityIdentitySpec Empty = new();
}
