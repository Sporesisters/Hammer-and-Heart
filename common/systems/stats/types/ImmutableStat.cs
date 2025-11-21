using Core.Stats.Modifiers;
using Core.Utilities.Logging;
using Godot;

namespace Core.Stats.Types;

/// <summary>
/// Represents a stat whose value is fixed and cannot be changed after creation.
/// Modifiers do not affect the value, and any attempt to set the current value externally logs a warning.
/// </summary>
public class ImmutableStat : Stat
{
    /// <summary>
    /// Initializes a new <see cref="ImmutableStat"/> with a base value, minimum, and maximum.
    /// The current value is immediately set to the base value.
    /// </summary>
    /// <param name="baseStatValue">The fixed value of the stat.</param>
    public ImmutableStat(float baseStatValue) : base(baseStatValue, 0, baseStatValue)
        => UpdateCurrentValue(BaseStatValue);

    /// <summary>
    /// Gets the current value of the stat, which is always equal to the base value.
    /// Setting this value externally is not allowed and logs a warning.
    /// </summary>
    public override float CurrentStatValue
    {
        get => base.CurrentStatValue;
        set => LoggerService.Warning("Cannot modify an ImmutableStat value.");
    }

    /// <inheritdoc/>
    public override void SetModifiers(ModifierScaleType type, float flat, float percent)
        => LoggerService.Warning("ClampedStat does not support setting modifiers.");

    /// <inheritdoc/>
    protected override void RecalculateValueBounds()
    {
        // Recalculates the bounds, ensuring the maximum is valid and the current value remains equal to the base value.

        MaximumValue = Mathf.Max(MinimumValue, MaximumValue);
        UpdateCurrentValue(BaseStatValue);
    }
}
