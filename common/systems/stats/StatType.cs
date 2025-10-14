namespace Core.Stats;

/// <summary>
/// Enumerates all gameplay stats tracked for entities.
/// <para>
/// Each value acts as a key for looking up an entity's current stat value.
/// Note that this enum does not specify whether the value is base, modified, min, or max;
/// that context is determined by the stat system.
/// </para>
/// </summary>
public enum StatType
{
	/// <summary>
	/// The current health of the entity.
	/// </summary>
	Health,

	/// <summary>
	/// The base damage the entity can deal per hit before any modifiers.
	/// </summary>
	Damage,

	/// <summary>
	/// The entity's movement speed.
	/// </summary>
	MoveSpeed,
}
