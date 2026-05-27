using Godot;

namespace Core.Stats.Types;

/// <summary>
/// A <see cref="Stat"/> variant that can scale dynamically based on applied modifiers.
/// </summary>
/// <remarks>
/// The maximum value of this stat is recalculated using pre- and post-modifiers.
/// This allows the value to grow or shrink beyond the initial base value,
/// making it suitable for stats that are highly dynamic and influenced by gameplay effects.
/// </remarks>
public class ScaledStat(float baseStatValue, float minimumValue, float maximumValue)
	: Stat(baseStatValue, minimumValue, maximumValue)
{
	protected override void RecalculateValueBounds()
	{
		float preFlatMod = _preModifiers.FlatValueModifier;
		float prePercentageMod = _preModifiers.PercentageModifier;
		float postFlatMod = _postModifiers.FlatValueModifier;
		float postPercentageMod = _postModifiers.PercentageModifier;

		float preModified = (_baseStatValue + preFlatMod) * (1 + prePercentageMod / 100f);
		float postModified = preModified * (1 + postPercentageMod / 100f) + postFlatMod;

		_maximumValue = Mathf.Max(_minimumValue, postModified);
		UpdateCurrentValue(_currentStatValue);
	}
}
