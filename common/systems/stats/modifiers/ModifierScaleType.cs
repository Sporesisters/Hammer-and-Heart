namespace Core.Stats.Modifiers;

/// <summary>
/// Determines where a modifier is applied within the stat calculation pipeline.
/// </summary>
public enum ModifierScaleType
{
	/// <summary>
	/// The modifier adjusts the raw/base value before scaling is applied.
	/// </summary>
	PreScaling,

	/// <summary>
	/// The modifier adjusts the final scaled value after all scaling operations.
	/// </summary>
	PostScaling,
}
