namespace Core.Stats.Modifiers;

/// <summary>
/// Specifies when a modifier is applied during stat value calculation.
/// </summary>
public enum ModifierScaleType
{
	/// <summary>
	/// Applied before the base stat is scaled by percentages.
	/// <para>
	/// Example: (Base + FlatPre) * (1 + PercentPre).
	/// </para>
	/// </summary>
	PreScaling,

	/// <summary>
	/// Applied after the stat has been pre-scaled.
	/// <para>
	/// Example: (PreResult * (1 + PercentPost)) + FlatPost.
	/// </para>
	/// </summary>
	PostScaling,
}
