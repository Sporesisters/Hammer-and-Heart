using System;

namespace Core.Events.Internals;

/// <summary>
/// Base class for typed event listeners used by the <see cref="EventChannel{T}"/>.
/// </summary>
/// <typeparam name="T">The event type handled by this listener.</typeparam>
/// <remarks>
/// Encapsulates metadata such as <see cref="EventPriority"/>, sub-priority ordering,
/// sequence index, and one-shot behavior.
/// <para>
/// Concrete implementations must provide <see cref="Invoke"/> logic as well as
/// callback-matching semantics for deduplication and removal.
/// </para>
/// </remarks>
public abstract class ListenerBase<T>(EventPriority priorityValue, int subPriority, long sequence, bool oneShot) where T : IEvent
{
	/// <summary>
	/// Gets the priority assigned to this listener.
	/// </summary>
	public EventPriority PriorityValue { get; } = priorityValue;

	/// <summary>
	/// Gets the sub-priority value, used to resolve ordering between listeners of the same <see cref="PriorityValue"/>.
	/// </summary>
	public int SubPriority { get; } = subPriority;

	/// <summary>
	/// Gets the sequence number representing registration order (used for stable ordering).
	/// </summary>
	public long Sequence { get; } = sequence;

	/// <summary>
	/// Gets a value indicating whether this listener should automatically unsubscribe after its first invocation.
	/// </summary>
	public bool OneShot { get; } = oneShot;

	/// <summary>
	/// Gets a value indicating whether this listener is currently active.
	/// </summary>
	public bool IsActive { get; private set; } = true;

	/// <summary>
	/// Disables this listener, preventing further invocations without removal.
	/// </summary>
	public void Disable() => IsActive = false;

	/// <summary>
	/// Enables this listener if previously disabled.
	/// </summary>
	public void Enable() => IsActive = true;

	/// <summary>
	/// Invokes the listener with the provided <paramref name="event"/>.
	/// </summary>
	/// <param name="event">The event instance to pass to the callback.</param>
	public abstract void Invoke(T @event);

	/// <summary>
	/// Determines whether this listener matches the given typed callback reference.
	/// </summary>
	/// <param name="callback">The callback reference to compare.</param>
	/// <returns><c>true</c> if the callback matches; otherwise, <c>false</c>.</returns>
	public abstract bool Matches(Action<T> callback);

	/// <summary>
	/// Determines whether this listener matches the given parameterless callback reference.
	/// </summary>
	/// <param name="callback">The callback reference to compare.</param>
	/// <returns><c>true</c> if the callback matches; otherwise, <c>false</c>.</returns>
	public abstract bool MatchesNoArgs(Action callback);
}
