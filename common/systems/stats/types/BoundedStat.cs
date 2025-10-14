using Godot;

namespace Core.Stats.Types;

/// <summary>
/// A <see cref="Stat"/> variant that strictly enforces a minimum and maximum range.
/// The current value is clamped within these bounds and cannot exceed them.
/// </summary>
public class BoundedStat(float baseStatValue, float minimumValue, float maximumValue)
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

		_maximumValue = Mathf.Max(_minimumValue, _maximumValue);
		UpdateCurrentValue(postModified);
	}
}
