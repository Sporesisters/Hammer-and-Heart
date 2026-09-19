using System;

namespace Core.ECS;

/// <summary>
/// Specification data required to construct an <see cref="EntityIdentity"/>.
/// Used for both creating new entities and restoring from saves.
/// </summary>
/// <param name="Type">The broad category of the entity (e.g., Player, Item, Projectile).</param>
/// <param name="SubType">Optional finer classification within the type (e.g., "Sword", "HealthPotion").</param>
/// <param name="PersistentId">Optional persistent unique identifier, typically used for save/load.</param>
/// <param name="Faction">Optional Monster/Robot faction. Keeps the value set in the scene when null.</param>
public readonly record struct EntityIdentitySpec(EntityType? Type = null, string? SubType = null, Guid? PersistentId = null, EntityFaction? Faction = null);
