using System;
using Core.Utilities.Logging;

namespace Core.Stats.Types;

/// <summary>
/// A <see cref="Stat"/> variant whose current value cannot be directly modified.
/// <para>
/// The stat still scales with modifiers (pre/post), but any attempt to set
/// <see cref="CurrentStatValue"/> will log a warning instead of changing the value.
/// </para>
/// <para>
/// Useful for stats that should always reflect their scaled maximum but not be freely editable.
/// </para>
/// </summary>
public class LockedStat(float baseStatValue, float minimumValue, float maximumValue)
	: Stat(baseStatValue, minimumValue, maximumValue)
{
	public override float CurrentStatValue
	{
		get => base.CurrentStatValue;
		set => LoggerService.Log("Cannot modify a LockedStat value.", LogLevel.Warning);
	}

	protected override void RecalculateValueBounds()
	{
		float preFlatMod = _preModifiers.FlatValueModifier;
		float prePercentageMod = _preModifiers.PercentageModifier;
		float postFlatMod = _postModifiers.FlatValueModifier;
		float postPercentageMod = _postModifiers.PercentageModifier;

		float preModified = (_baseStatValue + preFlatMod) * (1 + prePercentageMod / 100f);
		float postModified = preModified * (1 + postPercentageMod / 100f) + postFlatMod;

		_maximumValue = MathF.Max(_minimumValue, postModified);
		UpdateCurrentValue(_maximumValue);
	}
}
