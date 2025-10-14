namespace Core.Stats.Modifiers;

/// <summary>
/// Represents a set of modifiers that can be applied to a <see cref="Stat"/>.
/// </summary>
public class StatModifier
{
	/// <summary>
	/// The flat value modifier applied to a stat.
	/// This is added directly to the base stat when calculating the final value.
	/// </summary>
	private float _flatValueModifier = 0;

	/// <summary>
	/// The percentage modifier applied to a stat.
	/// This is applied as a percentage increase/decrease after flat modifiers.
	/// </summary>
	private float _percentageModifier = 0;

	/// <summary>
	/// Gets the current flat value modifier.
	/// This value is added directly to the base stat when calculating the modified stat value.
	/// </summary>
	public float FlatValueModifier => _flatValueModifier;

	/// <summary>
	/// Gets the current percentage modifier.
	/// This value is applied as a percentage increase/decrease to the stat after flat modifiers.
	/// </summary>
	public float PercentageModifier => _percentageModifier;

	/// <summary>
	/// Sets the modifiers for the stat.
	/// Replaces any previous modifier values.
	/// </summary>
	/// <param name="flatValue">The flat value to add to the stat.</param>
	/// <param name="percentageValue">The percentage modifier to apply to the stat.</param>
	public void SetModifiers(float flatValue, float percentageValue)
	{
		_flatValueModifier = flatValue;
		_percentageModifier = percentageValue;
	}
}
