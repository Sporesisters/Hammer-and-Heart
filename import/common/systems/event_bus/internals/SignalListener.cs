using System;
using Core.Utilities.Logging;

namespace Core.Events.Internals;

/// <summary>
/// Represents a listener for signal-style events that do not carry any payload.
/// </summary>
/// <typeparam name="T">
/// The event type this listener is bound to. Must implement <see cref="IEvent"/>.
/// </typeparam>
/// <param name="callback">The delegate to invoke when the event is published.</param>
/// <param name="priorityValue">Determines the order of execution relative to other listeners.</param>
/// <param name="subPriority">
/// Fine-grained ordering within the same <paramref name="priorityValue"/>.
/// </param>
/// <param name="sequence">The global registration order used as a final tiebreaker.</param>
/// <param name="oneShot">
/// If true, the listener automatically disables itself after the first invocation.
/// </param>
public class SignalListener<T>(Action callback, EventPriority priorityValue, int subPriority, long sequence, bool oneShot)
	: ListenerBase<T>(priorityValue, subPriority, sequence, oneShot) where T : IEvent
{
	/// <summary>
	/// The delegate invoked when the event is published.
	/// </summary>
	public readonly Action Callback = callback;

	/// <inheritdoc/>
	public override void Invoke(T @event)
	{
		if (IsActive)
		{
			Callback();
			return;
		}

		LoggerService.Warning($"{nameof(SignalListener<T>)} for event type {typeof(T).Name} is inactive; skipping invocation.");
	}

	/// <inheritdoc/>
	public override bool Matches(Action<T> callback) => false;

	/// <inheritdoc/>
	public override bool MatchesNoArgs(Action callback) => Callback == callback;
}
