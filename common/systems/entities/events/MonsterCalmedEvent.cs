namespace Core.ECS.Events;

using Core.Events;

/// <summary>
/// Published when a monster's Calm stat reaches its maximum and it becomes friendly (GDD p.4).
/// Raised on the monster's own <see cref="EventBus"/> and on the global one, so counters,
/// field effects and AI can react to it.
/// </summary>
/// <param name="Monster">The monster that has just been calmed.</param>
public readonly record struct MonsterCalmedEvent(Entity Monster) : IEvent;
