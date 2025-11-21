using System;
using System.Collections.Generic;
using Core.Utilities.Logging;

namespace Core.Events.Internals;

/// <summary>
/// Central engine that manages all event channels and provides
/// methods to register, remove, and publish events.
/// </summary>
public class EventBusEngine
{
	/// <summary>
	/// Maps event types to their channels.
	/// </summary>
	private readonly Dictionary<Type, object> _channels = [];

	/// <summary>
	/// Gets or creates an event channel for a specific event type.
	/// </summary>
	/// <typeparam name="T">The type of event for which to get or create a channel.</typeparam>
	/// <returns></returns>
	private EventChannel<T> GetOrCreate<T>() where T : IEvent
	{
		Type type = typeof(T);

		if (!_channels.TryGetValue(type, out object? obj))
		{
			EventChannel<T> channel = new();
			_channels[type] = channel;
			return channel;
		}

		return (EventChannel<T>)obj;
	}

	/// <summary>
	/// Registers a listener for a specific event type.
	/// </summary>
	/// <typeparam name="T">The type of event to listen for.</typeparam>
	/// <param name="listener">The action to invoke when the event is published.</param>
	/// <param name="priority">The listener's execution priority (default is <see cref="EventPriority.Normal"/>).</param>
	/// <param name="subPriority">Secondary priority to break ties when multiple listeners have the same priority.</param>
	/// <param name="oneShot">
	/// If <c>true</c>, the listener will automatically be removed after it is invoked once.
	/// </param>
	/// <returns>
	/// The registered listener object, or <c>null</c> if a listener with the same callback already exists.
	/// </returns>
	public ListenerBase<T>? AddListener<T>(Action<T> listener, EventPriority priority, int subPriority, bool oneShot) where T : IEvent
	{
		ListenerBase<T>? result = GetOrCreate<T>().AddListener(listener, priority, subPriority, oneShot);
		return result;
	}

	/// <summary>
	/// Adds a no-argument listener for an event type. Returns the listener handle, or null if duplicate.
	/// </summary>
	/// <typeparam name="T">The type of event to listen for.</typeparam>
	/// <param name="listener">The action to invoke when the event is published.</param>
	/// <param name="priority">The listener's execution priority (default is <see cref="EventPriority.Normal"/>).</param>
	/// <param name="subPriority">Secondary priority to break ties when multiple listeners have the same priority.</param>
	/// <param name="oneShot">
	/// If <c>true</c>, the listener will automatically be removed after it is invoked once.
	/// </param>
	/// <returns>
	/// The registered listener object, or <c>null</c> if a listener with the same callback already exists.
	/// </returns>
	public ListenerBase<T>? AddListenerNoArgs<T>(Action listener, EventPriority priority, int subPriority, bool oneShot) where T : IEvent
	{
		ListenerBase<T>? result = GetOrCreate<T>().AddListener(listener, priority, subPriority, oneShot);
		return result;
	}

	/// <summary>Removes a listener by callback.</summary>
	public void RemoveListener<T>(Action<T> listener) where T : IEvent
		=> GetOrCreate<T>().RemoveListener(listener);

	/// <summary>Removes a no-argument listener by callback.</summary>
	public void RemoveListenerNoArgs<T>(Action listener) where T : IEvent
		=> GetOrCreate<T>().RemoveListener(listener);

	/// <summary>Removes a listener by handle.</summary>
	public void RemoveRegisteredListener<T>(ListenerBase<T> listener) where T : IEvent
		=> GetOrCreate<T>().RemoveListener(listener);

	/// <summary>Publishes an event to all listeners of its type.</summary>
	public void Publish<T>(T eventData) where T : IEvent
		=> GetOrCreate<T>().Publish(eventData);

	/// <summary>Clears all listeners for a specific event type.</summary>
	public void Clear<T>() where T : IEvent
		=> _channels.Remove(typeof(T));

	/// <summary>Clears all event channels and listeners.</summary>
	public void ClearAll()
	{
		_channels.Clear();
		LoggerService.Debug("Cleared all event channels and listeners.");
	}
}
