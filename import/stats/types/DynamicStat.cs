using Godot;

namespace Core.Stats.Types;

/// <summary>
/// Represents a stat that can dynamically scale with modifiers.
/// The current value is clamped within the scaled bounds and can change freely.
/// </summary>
/// <param name="baseStatValue">The base value of the stat.</param>
/// <param name="minimumValue">The minimum allowable value of the stat.</param>
/// <param name="maximumValue">The maximum allowable value of the stat before scaling.</param>
public class DynamicStat(float baseStatValue, float minimumValue, float maximumValue)
    : Stat(baseStatValue, minimumValue, maximumValue)
{
    /// <inheritdoc/>
    protected override void RecalculateValueBounds()
    {
        // Recalculates the maximum value based on modifiers.
        // If the maximum increases, the current value is boosted by the difference (additive scaling).
        // If the maximum decreases or stays the same, the current value is clamped to ensure it remains valid.

        float oldMax = MaximumValue;
        float oldCurrent = CurrentStatValue;

        float scaledMax = EvaluateScaledMax();
        MaximumValue = Mathf.Max(MinimumValue, scaledMax);

        if (MaximumValue > oldMax)
        {
            float valueDiffernce = MaximumValue - oldMax;
            UpdateCurrentValue(oldCurrent + valueDiffernce);
        }
        else
        {
            UpdateCurrentValue(oldCurrent);
        }
    }
}
