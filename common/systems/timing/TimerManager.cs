using System.Collections.Generic;
using Core.Timing.Internals;

namespace Core.Timing;

/// <summary>
/// Manages and updates all active <see cref="Timer"/> instances.
/// Designed for per-frame update in the game loop.
/// </summary>
public static class TimerManager
{
	/// <summary>
	/// Active timers currently managed by the <see cref="TimerManager"/>.
	/// Uses a <see cref="HashSet{T}"/> for fast lookup and uniqueness.
	/// </summary>
	private static readonly HashSet<Timer> _timers = [];

	/// <summary>
	/// Reused buffer for safe iteration during <see cref="UpdateTimers(float)"/>.
	/// Prevents allocations and modification errors while updating timers.
	/// </summary>
	private static readonly List<Timer> _updateBuffer = [];

	/// <summary>
	/// Registers a timer to be updated each frame.
	/// </summary>
	public static void RegisterTimer(Timer timer) => _timers.Add(timer);

	/// <summary>
	/// Unregisters a timer from active updates.
	/// </summary>
	public static void UnregisterTimer(Timer timer) => _timers.Remove(timer);

	/// <summary>
	/// Updates all active timers with the given delta time.
	/// Should be called once per frame from a central update loop.
	/// </summary>
	public static void UpdateTimers(float deltaTime)
	{
		if (_timers.Count is 0) return;

		// Copy references into a buffer
		_updateBuffer.Clear();
		_updateBuffer.AddRange(_timers);

		for (int i = 0; i < _updateBuffer.Count; i++)
		{
			Timer timer = _updateBuffer[i];

			// The timer may have been unregistered during Tick
			if (!_timers.Contains(timer))
				continue;

			timer.Tick(deltaTime);
		}
	}

	/// <summary>
	/// Clears all active timers.
	/// </summary>
	public static void ClearTimers()
	{
		_timers.Clear();
		_updateBuffer.Clear();
	}
}
