using Core.Events;
using Core.Stats;

namespace Core.ECS.Events;

///<summary>
/// Event data passed when a stat's value reaches its minimum.
/// </summary>
/// <param name="statType">The type of stat that has been depleted.</param>
/// <param name="stat">The stat instance that reached its minimum value.</param>
public readonly struct StatDepletedEvent(StatType statType, Stat stat) : IEvent
{
    /// <summary>
    /// The type of stat that has been depleted.
    /// </summary>
    public readonly StatType StatType = statType;

    /// <summary>
    /// The <see cref="Stats.Stat"/> instance that has been depleted.
    /// </summary>
    public readonly Stat Stat = stat;
}
