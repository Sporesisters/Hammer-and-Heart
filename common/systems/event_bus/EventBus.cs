using System;
using Core.Events.Internals;

namespace Core.Events;

/// <summary>
/// Provides a high-level API to register, remove, and publish events of different types.
/// Wraps an internal <see cref="EventBusEngine"/> to handle per-type event channels and listener management.
/// </summary>
public class EventBus
{
	/// <summary>
	/// Internal engine responsible for managing event channels, listeners, and publishing logic.
	/// All public methods delegate to this engine.
	/// </summary>
	private readonly EventBusEngine _engine = new();

	/// <summary>
	/// Registers a listener for a specific event type with payload.
	/// </summary>
	/// <typeparam name="T">Type of the event.</typeparam>
	/// <param name="listener">Callback to invoke when the event is published.</param>
	/// <param name="priority">Execution priority of the listener <c>(default: Normal)</c>.</param>
	/// <param name="subPriority">Secondary priority to resolve ties <c>(default: 0)</c>.</param>
	/// <param name="oneShot">If <c>true</c>, the listener is removed after first invocation.</param>
	/// <returns>The registered listener handle, or null if duplicate.</returns>
	public ListenerBase<T>? AddListener<T>(Action<T> listener, EventPriority priority = EventPriority.Normal, int subPriority = 0, bool oneShot = false) where T : IEvent
		=> _engine.AddListener(listener, priority, subPriority, oneShot);

	/// <summary>
	/// Registers a listener for a specific event type with no payload.
	/// </summary>
	/// <typeparam name="T">Type of the event.</typeparam>
	/// <param name="listener">Callback to invoke when the event is published.</param>
	/// <param name="priority">Execution priority of the listener <c>(default: Normal)</c>.</param>
	/// <param name="subPriority">Secondary priority to resolve ties <c>(default: 0)</c>.</param>
	/// <param name="oneShot">If <c>true</c>, the listener is removed after first invocation.</param>
	/// <returns>The registered listener handle, or null if duplicate.</returns>
	public ListenerBase<T>? AddListener<T>(Action listener, EventPriority priority = EventPriority.Normal, int subPriority = 0, bool oneShot = false) where T : IEvent
		=> _engine.AddListenerNoArgs<T>(listener, priority, subPriority, oneShot);

	/// <summary>Removes a listener registered with payload.</summary>
	public void RemoveListener<T>(Action<T> listener) where T : IEvent
		=> _engine.RemoveListener(listener);

	/// <summary>Removes a listener registered with no payload.</summary>
	public void RemoveListener<T>(Action listener) where T : IEvent
		=> _engine.RemoveListenerNoArgs<T>(listener);

	/// <summary>Removes a listener via its handle.</summary>
	public void RemoveListener<T>(ListenerBase<T> listener) where T : IEvent
		=> _engine.RemoveRegisteredListener(listener);

	/// <summary>Publishes an event to all registered listeners.</summary>
	public void Publish<T>(T eventData) where T : IEvent
		=> _engine.Publish(eventData);

	/// <summary>Clears all listeners for a specific event type.</summary>
	public void Clear<T>() where T : IEvent => _engine.Clear<T>();

	/// <summary>Clears all listeners for all event types.</summary>
	public void ClearAll() => _engine.ClearAll();
}
