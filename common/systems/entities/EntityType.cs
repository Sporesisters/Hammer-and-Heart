namespace Core.ECS;

/// <summary>
/// Broad categories of entities used in the ECS system.
/// This enum provides high-level classification of entities
/// and should be combined with <see cref="EntityIdentity"/> for
/// more specific identification.
/// </summary>
public enum EntityType
{
	/// <summary>
	/// Represents an unknown or unassigned entity type.
	/// </summary>
	Unknown,

	/// <summary>
	/// Represents a player-controlled character.
	/// </summary>
	Player,

	/// <summary>
	/// Represents a hostile creature or character controlled by the game.
	/// </summary>
	Enemy,

	/// <summary>
	/// Represents a non-player character that may be neutral, friendly, or scripted.
	/// </summary>
	NPC,

	/// <summary>
	/// Represents a general item, often used as a base category for inventory objects.
	/// </summary>
	Item,

	/// <summary>
	/// Represents an item that can be used to deal damage or perform attacks.
	/// </summary>
	Weapon,

	/// <summary>
	/// Represents an item that can be consumed for an effect (e.g., healing, buffs).
	/// </summary>
	Consumable,

	/// <summary>
	/// Represents a trap that may harm or hinder entities.
	/// </summary>
	Trap,

	/// <summary>
	/// Represents a world object that can be interacted with (e.g., switches, levers).
	/// </summary>
	Interactable,

	/// <summary>
	/// Represents a projectile entity (e.g., bullet, missile, bomb).
	/// </summary>
	Projectile,
}
