namespace Core.Stats.Modifiers;

/// <summary>
/// Represents a simple stat modifier containing a flat adjustment
/// and a percentage-based adjustment.
///
/// <para>
/// <b>FlatValueModifier:</b> Added directly to the stat before or after scaling,
/// depending on the pipeline rules.
/// </para>
///
/// <para>
/// <b>PercentageModifier:</b> Multiplies the stat value by the given percentage.
/// This is applied as (value * PercentageModifier), not (value * (1 + percentage)).
/// </para>
/// </summary>
public class StatModifier
{
	/// <summary>
	/// Direct additive adjustment to the stat.
	/// </summary>
	public float FlatValueModifier { get; private set; }

	/// <summary>
	/// Percentage multiplier applied to the stat.
	/// Expected as a decimal (e.g., 0.10 for +10%).
	/// </summary>
	public float PercentageModifier { get; private set; }

	/// <summary>
	/// Sets both the flat and percentage modifiers.
	/// </summary>
	/// <param name="flatValue">The additive modifier value.</param>
	/// <param name="percentageValue">
	/// The multiplier expressed as a decimal (e.g., 0.25 for +25%).
	/// </param>
	public void SetModifiers(float flatValue, float percentageValue)
	{
		FlatValueModifier = flatValue;
		PercentageModifier = percentageValue;
	}

	/// <summary>
	/// Resets both modifiers to <b>zero</b>.
	/// </summary>
	public void ClearModifiers()
	{
		FlatValueModifier = 0;
		PercentageModifier = 0;
	}
}
