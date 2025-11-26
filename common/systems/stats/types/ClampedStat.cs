using Core.Stats.Modifiers;
using Core.Utilities.Logging;
using Godot;

namespace Core.Stats.Types;

/// <summary>
/// Represents a stat that is strictly clamped within a defined minimum and maximum.
/// Modifiers do not affect the value or the bounds; the value can only move within the fixed range.
/// </summary>
/// <param name="baseStatValue">The base value of the stat.</param>
/// <param name="minimumValue">The minimum allowable value of the stat.</param>
/// <param name="maximumValue">The maximum allowable value of the stat.</param>
public class ClampedStat(float baseStatValue, float minimumValue, float maximumValue)
    : Stat(baseStatValue, minimumValue, maximumValue)
{
    /// <inheritdoc/>
    public override void SetModifiers(ModifierScaleType type, float flat, float percent)
        => LoggerService.Warning("ClampedStat does not support setting modifiers.");

    /// <inheritdoc/>
    protected override void RecalculateValueBounds()
    {
        // Recalculates the bounds and clamps the current value within them.
        // This ensures the stat never exceeds its defined minimum or maximum.

        MaximumValue = Mathf.Max(MinimumValue, MaximumValue);
        UpdateCurrentValue(CurrentStatValue);
    }
}
