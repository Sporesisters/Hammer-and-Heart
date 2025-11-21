using System;
using Core.Utilities.Logging;

namespace Core.Events.Internals;

/// <summary>
/// Represents a listener for payload-style events that carry event data.
/// </summary>
/// <typeparam name="T">
/// The event type this listener is bound to. Must implement <see cref="IEvent"/>.
/// </typeparam>
/// <param name="callback">The delegate to invoke with the event instance.</param>
/// <param name="priorityValue">Determines the order of execution relative to other listeners.</param>
/// <param name="subPriority">
/// Fine-grained ordering within the same <paramref name="priorityValue"/>.
/// </param>
/// <param name="sequence">The global registration order used as a final tiebreaker.</param>
/// <param name="oneShot">
/// If true, the listener automatically disables itself after the first invocation.
/// </param>
public class PayloadListener<T>(Action<T> callback, EventPriority priorityValue, int subPriority, long sequence, bool oneShot)
	: ListenerBase<T>(priorityValue, subPriority, sequence, oneShot) where T : IEvent
{
	/// <summary>
	/// The delegate invoked when the event is published.
	/// </summary>
	public readonly Action<T> Callback = callback;

	/// <inheritdoc/>
	public override void Invoke(T @event)
	{
		if (IsActive)
		{
			Callback(@event);
			return;
		}

		LoggerService.Warning($"{nameof(PayloadListener<T>)} for event type {typeof(T).Name} is inactive; skipping invocation.");
	}

	/// <inheritdoc/>
	public override bool Matches(Action<T> callback) => Callback == callback;

	/// <inheritdoc/>
	public override bool MatchesNoArgs(Action callback) => false;
}
