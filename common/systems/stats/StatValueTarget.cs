namespace Core.Stats;

/// <summary>
/// Specifies which aspect of a <see cref="Stat"/> is being referenced or modified.
/// </summary>
public enum StatValueTarget
{
	/// <summary>
	/// The minimum allowed value for the stat.
	/// </summary>
	MinimumValue,

	/// <summary>
	/// The maximum allowed value for the stat.
	/// </summary>
	MaximumValue,

	/// <summary>
	/// The current value of the stat after all calculations and modifiers.
	/// </summary>
	CurrentValue,

	/// <summary>
	/// A fixed (flat) value added to or subtracted from the stat.
	/// </summary>
	FlatModifierValue,

	/// <summary>
	/// A percentage-based modifier applied to the stat.
	/// </summary>
	PercentModifierValue,
}