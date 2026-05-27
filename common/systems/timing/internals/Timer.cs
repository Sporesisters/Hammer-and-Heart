using System;
using Godot;

namespace Core.Timing.Internals;

/// <summary>
/// Abstract base class for all timer types.
///
/// A <see cref="Timer"/> provides lifecycle management for time-based operations.
/// It tracks elapsed progress, manages running state, and integrates with a <see cref="TimerManager"/>.
///
/// Subclasses define how time progresses (e.g., countdowns or accumulative timers)
/// by implementing <see cref="Tick(float)"/> and <see cref="IsFinished"/>.
/// </summary>
/// <param name="duration">The initial duration of the timer in seconds.</param>
public abstract class Timer(float duration)
{
	/// <summary>
	/// The current time remaining or elapsed, depending on implementation.
	/// </summary>
	public float CurrentTime { get; protected set; } = duration;

	/// <summary>
	/// The starting duration assigned when the timer was created or last reset.
	/// </summary>
	public float InitialTime { get; protected set; } = duration;

	/// <summary>
	/// Indicates whether the timer is currently active and being updated by the <see cref="TimerManager"/>.
	/// </summary>
	public bool IsRunning { get; protected set; }

	/// <summary>
	/// Returns the normalized progress of the timer in the range [0, 1].
	/// </summary>
	public float Progress => Mathf.Clamp(CurrentTime / InitialTime, 0f, 1f);

	/// <summary>
	/// Gets whether the timer has completed its cycle.
	/// </summary>
	public abstract bool IsFinished { get; }

	/// <summary>
	/// Tracks whether the timer has already been disposed.
	/// </summary>
	private bool _isDisposed;

	/// <summary>
	/// Invoked when the timer starts.
	/// </summary>
	public event Action? OnTimerStart;

	/// <summary>
	/// Invoked when the timer stops.
	/// </summary>
	public event Action? OnTimerStop;

	/// <summary>
	/// Starts the timer. If already running, it resets before starting.
	/// </summary>
	public void Start()
	{
		CurrentTime = InitialTime;

		if (!IsRunning)
		{
			IsRunning = true;
			TimerManager.RegisterTimer(this);
			OnTimerStart?.Invoke();
		}
	}

	/// <summary>
	/// Stops the timer and unregisters it from the <see cref="TimerManager"/>.
	/// </summary>
	public void Stop()
	{
		if (IsRunning)
		{
			IsRunning = false;
			TimerManager.UnregisterTimer(this);
			OnTimerStop?.Invoke();
		}
	}

	/// <summary>
	/// Temporarily halts the timer without unregistering it.
	/// </summary>
	public void Pause() => IsRunning = false;

	/// <summary>
	/// Resumes a previously paused timer.
	/// </summary>
	public void Resume() => IsRunning = true;

	/// <summary>
	/// Resets the timer to its initial duration.
	/// </summary>
	public virtual void Reset() => CurrentTime = InitialTime;

	/// <summary>
	/// Resets the timer with a new duration value.
	/// </summary>
	/// <param name="duration">The new duration to set.</param>
	public virtual void ResetWithNewDuration(float duration)
	{
		InitialTime = duration;
		Reset();
	}

	/// <summary>
	/// Updates the timer with the given delta time.
	/// Must be implemented by subclasses.
	/// </summary>
	/// <param name="deltaTime">The frame delta time (in seconds).</param>
	public abstract void Tick(float deltaTime);

	/// <summary>
	/// Disposes of the timer, ensuring it is stopped and cannot be reused.
	/// </summary>
	public void Dispose()
	{
		if (_isDisposed) return;

		Stop();
		_isDisposed = true;
	}
}
