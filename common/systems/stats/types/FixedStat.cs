using Core.Utilities.Logging;

namespace Core.Stats.Types;

/// <summary>
/// A <see cref="Stat"/> variant that cannot be changed once created.
/// Attempts to modify <see cref="CurrentStatValue"/> will log a warning.
/// The current value is always equal to the base value.
/// </summary>
public class FixedStat(float baseStatValue, float minimumValue, float maximumValue)
	: Stat(baseStatValue, minimumValue, maximumValue)
{
	public override float CurrentStatValue
	{
		get => base.CurrentStatValue;
		set => LoggerService.Log("Cannot modify a FixedStat value.", LogLevel.Warning);
	}

	protected override void RecalculateValueBounds()
	{
		_currentStatValue = _baseStatValue;
	}
}
