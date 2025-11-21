namespace Core.Stats;

/// <summary>
/// Specifies which value of a stat is being targeted for retrieval or modification.
/// </summary>
public enum StatValueTarget
{
	/// <summary>
	/// The minimum allowable value of the stat.
	/// </summary>
	MinimumValue,

	/// <summary>
	/// The maximum allowable value of the stat.
	/// </summary>
	MaximumValue,

	/// <summary>
	/// The current value of the stat.
	/// </summary>
	CurrentValue,

	/// <summary>
	/// The flat modifier applied to the stat.
	/// </summary>
	FlatModifierValue,

	/// <summary>
	/// The percentage modifier applied to the stat.
	/// </summary>
	PercentModifierValue,
}
