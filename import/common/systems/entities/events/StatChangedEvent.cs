using Core.Events;
using Core.Stats;

namespace Core.ECS.Events;

/// <summary>
/// Event data passed when when a stat's value changes.
/// </summary>
/// <param name="statType">The type of stat that changed.</param>
/// <param name="stat">The stat instance containing the updated value.</param>
public readonly struct StatChangedEvent(StatType statType, Stat stat) : IEvent
{
    /// <summary>
    /// The type of stat that changed.
    /// </summary>
    public readonly StatType StatType = statType;

    /// <summary>
    /// The <see cref="Stat"/> instance containing the new value.
    /// </summary>
    public readonly Stat Stat = stat;
}
