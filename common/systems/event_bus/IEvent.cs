namespace Core.Events;

/// <summary>
/// Marker interface for all events dispatched through the <see cref="EventBus"/>.
/// </summary>
/// <remarks>
/// Implementing this interface designates a type as an event that can be published,
/// subscribed to, and processed by the event bus system.
/// <para>
/// Events are typically implemented as lightweight, immutable data structures that
/// carry the information needed by listeners.
/// </para>
/// </remarks>
public interface IEvent { }
