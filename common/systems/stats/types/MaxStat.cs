using Core.Utilities.Logging;
using Godot;

namespace Core.Stats.Types;

/// <summary>
/// Represents a stat whose current value is always equal to its maximum value.
/// Modifiers can affect the maximum, but the current value cannot be set externally.
/// </summary>
public class MaxStat : Stat
{
    /// <summary>
    /// Initializes a new <see cref="MaxStat"/> with a base value, minimum, and maximum.
    /// The current value is immediately set to the scaled maximum.
    /// </summary>
    /// <param name="baseStatValue">The base value of the stat.</param>
    public MaxStat(float baseStatValue) : base(baseStatValue, 0, baseStatValue)
        => RecalculateValueBounds();

    /// <summary>
    /// Gets the current value of the stat, which is always equal to the maximum.
    /// Setting this value externally is not allowed and logs a warning.
    /// </summary>
    public override float CurrentStatValue
    {
        get => base.CurrentStatValue;
        set => LoggerService.Warning("Cannot modify a MaxStat value.");
    }

    /// <inheritdoc/>
    protected override void RecalculateValueBounds()
    {
        // Recalculates the maximum value based on modifiers and immediately updates the current value to match.

        float scaledMax = EvaluateScaledMax();
        MaximumValue = Mathf.Max(MinimumValue, scaledMax);
        UpdateCurrentValue(MaximumValue);
    }
}
