using System;
using System.Collections.Generic;
using Core.Utilities.Logging;

namespace Core.Events.Internals;

/// <summary>
/// Represents a channel that manages listeners and dispatches events of type <typeparamref name="T"/>.
/// Supports priority-based ordering, one-shot listeners, and recursion protection with a configurable maximum depth.
/// </summary>
/// <typeparam name="T">Type of event this channel handles. Must implement <see cref="IEvent"/>.</typeparam>
internal sealed class EventChannel<T> where T : IEvent
{
	/// <summary>Registered listeners for this event channel.</summary>
	private readonly List<ListenerBase<T>> _listeners = [];

	/// <summary>Queue of events waiting to be dispatched.</summary>
	private readonly Queue<T> _eventQueue = new();

	/// <summary>Sequence number used to maintain stable listener ordering.</summary>
	private long _nextEventSequence = 0;

	/// <summary><c>True</c> while events are currently being dispatched.</summary>
	private bool _isPublishing = false;

	/// <summary>Maximum allowed recursive event dispatches before clearing the queue.</summary>
	private const int MAX_RECURSION = 10;

	/// <summary>
	/// Compares listeners by priority, sub-priority, and sequence order.
	/// Higher priority listeners execute first. Within the same priority, earlier-added listeners execute first.
	/// </summary>
	private static int CompareListeners(ListenerBase<T> a, ListenerBase<T> b)
	{
		int priorityComparison = b.PriorityValue.CompareTo(a.PriorityValue); // high -> low
		if (priorityComparison != 0) return priorityComparison;

		int subPriorityComparison = b.SubPriority.CompareTo(a.SubPriority); // high -> low
		if (subPriorityComparison != 0) return subPriorityComparison;

		return a.Sequence.CompareTo(b.Sequence); // earlier added first
	}

	/// <summary>
	/// Adds a typed listener that receives the event payload.
	/// </summary>
	/// <param name="callback">Action to invoke when the event is published.</param>
	/// <param name="priority">Execution priority of the listener.</param>
	/// <param name="subPriority">Secondary priority to break ties.</param>
	/// <param name="oneShot">If <c>true</c>, listener is removed after first invocation.</param>
	/// <returns>The registered listener object, or null if a duplicate exists.</returns>
	public ListenerBase<T>? AddListener(Action<T> callback, EventPriority priority, int subPriority, bool oneShot)
	{
		if (_listeners.Exists(l => l.Matches(callback)))
		{
			LoggerService.Warning($"Listener for {typeof(T).Name} already exists. Skipping add.");
			return null;
		}

		var listener = new PayloadListener<T>(callback, priority, subPriority, _nextEventSequence++, oneShot);
		_listeners.Add(listener);
		_listeners.Sort(CompareListeners);

		LoggerService.Debug($"Added listener to {typeof(T).Name}: priority = {priority}, subPriority = {subPriority}, oneShot = {oneShot}.");
		return listener;
	}

	/// <summary>
	/// Adds a parameterless listener.
	/// </summary>
	/// <param name="callback">Action to invoke when the event is published.</param>
	/// <param name="priority">Execution priority of the listener.</param>
	/// <param name="subPriority">Secondary priority to break ties.</param>
	/// <param name="oneShot">If <c>true</c>, listener is removed after first invocation.</param>
	/// <returns>The registered listener object, or null if a duplicate exists.</returns>
	public ListenerBase<T>? AddListener(Action callback, EventPriority priority, int subPriority, bool oneShot)
	{
		if (_listeners.Exists(l => l.MatchesNoArgs(callback)))
		{
			LoggerService.Warning($"No-args listener for {typeof(T).Name} already exists. Skipping add.");
			return null;
		}

		var listener = new SignalListener<T>(callback, priority, subPriority, _nextEventSequence++, oneShot);
		_listeners.Add(listener);
		_listeners.Sort(CompareListeners);

		LoggerService.Debug($"Added no-args listener to {typeof(T).Name}: priority = {priority}, subPriority = {subPriority}, oneShot = {oneShot}.");
		return listener;
	}

	/// <summary>
	/// Removes a listener by reference.
	/// </summary>
	/// <param name="listener">The listener instance to remove.</param>
	public void RemoveListener(ListenerBase<T> listener)
	{
		if (_listeners.Remove(listener))
		{
			LoggerService.Debug($"Removed listener from {typeof(T).Name}");
			return;
		}

		LoggerService.Debug($"Failed to remove listener from {typeof(T).Name}: not found.");
	}

	/// <summary>
	/// Removes all typed listeners matching the specified callback.
	/// </summary>
	/// <param name="callback">The callback to match against.</param>
	public void RemoveListener(Action<T> callback)
	{
		int removed = _listeners.RemoveAll(l => l.Matches(callback));

		if (removed > 0)
		{
			LoggerService.Debug($"Removed {removed} listener(s) from {typeof(T).Name}");
		}
	}

	/// <summary>
	/// Removes all parameterless listeners matching the specified callback.
	/// </summary>
	/// <param name="callback">The callback to match against.</param>
	public void RemoveListener(Action callback)
	{
		int removed = _listeners.RemoveAll(l => l.MatchesNoArgs(callback));

		if (removed > 0)
		{
			LoggerService.Debug($"Removed {removed} no-args listener(s) from {typeof(T).Name}");
		}
	}

	/// <summary>
	/// Publishes an event to all listeners.
	/// Enqueues incoming events and protects against infinite recursion with a maximum depth.
	/// </summary>
	/// <param name="event">The event instance to publish.</param>
	public void Publish(T @event)
	{
		_eventQueue.Enqueue(@event);
		LoggerService.Debug($"Event queued: {typeof(T).Name}");

		if (_isPublishing) return;

		_isPublishing = true;
		int depth = 0;

		try
		{
			while (_eventQueue.Count > 0)
			{
				if (depth++ >= MAX_RECURSION)
				{
					LoggerService.Warning($"Max recursion {MAX_RECURSION} reached for {typeof(T).Name}, clearing queue!");
					_eventQueue.Clear();
					break;
				}

				T currentEvent = _eventQueue.Dequeue();
				Dispatch(currentEvent);
			}
		}
		finally
		{
			_isPublishing = false;
		}
	}

	/// <summary>
	/// Dispatches the event to all active listeners.
	/// Handles one-shot listener removal and maintains listener order.
	/// </summary>
	private void Dispatch(T @event)
	{
		if (_listeners.Count == 0) return;

		List<ListenerBase<T>> snapshot = [.. _listeners];
		bool removedAny = false;

		foreach (ListenerBase<T> listener in snapshot)
		{
			if (listener.IsActive)
			{
				LoggerService.Debug($"Invoking listener for {typeof(T).Name}");
				listener.Invoke(@event);
			}

			if (listener.OneShot)
			{
				_listeners.Remove(listener);
				removedAny = true;
			}
		}

		if (removedAny && _listeners.Count > 1)
		{
			_listeners.Sort(CompareListeners);
		}
	}
}
