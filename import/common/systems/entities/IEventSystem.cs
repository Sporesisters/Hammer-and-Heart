using Core.ECS.Internals;
using Core.Events;

namespace Core.ECS;

/// <summary>
/// Defines a base interface for systems that interact with an <see cref="EventBus"/>.
/// Systems implementing this interface can register and unregister themselves from the bus,
/// enabling them to respond to events in a decoupled and managed manner.
/// </summary>
public interface IEventSystem : ISystem
{
    /// <summary>
    /// Registers the system with the specified <see cref="EventBus"/>,
    /// allowing it to start receiving relevant events.
    /// </summary>
    /// <param name="bus">The event bus to attach to.</param>
    void Attach(EventBus bus);

    /// <summary>
    /// Unregisters the system from the specified <see cref="EventBus"/>,
    /// stopping it from receiving further events.
    /// </summary>
    /// <param name="bus">The event bus to detach from.</param>
    void Detach(EventBus bus);
}

/// <summary>
/// A system that reacts to a specific event type.
/// Automatically attaches / detaches itself to the event bus.
/// </summary>
/// <typeparam name="TEvent">Event type to react to.</typeparam>
public interface IEventSystem<TEvent> : IEventSystem where TEvent : IEvent
{
    /// <summary>
    /// Handles the event of type <typeparamref name="TEvent"/>.
    /// </summary>
    /// <param name="event">The event instance to process.</param>
    void OnEvent(TEvent @event);

    /// <summary>
    /// Attaches this system to the provided event bus.
    /// Registers the <see cref="OnEvent"/> handler for events of type <typeparamref name="TEvent"/>.
    /// </summary>
    /// <param name="bus">The event bus to attach to.</param>
    void IEventSystem.Attach(EventBus bus) => bus.AddListener<TEvent>(OnEvent);

    /// <summary>
    /// Detaches this system from the provided event bus.
    /// Unregisters the <see cref="OnEvent"/> handler for events of type <typeparamref name="TEvent"/>.
    /// </summary>
    /// <param name="bus">The event bus to detach from.</param>
    void IEventSystem.Detach(EventBus bus) => bus.RemoveListener<TEvent>(OnEvent);
}
