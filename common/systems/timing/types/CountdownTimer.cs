using Core.Timing.Internals;

namespace Core.Timing.Types;

/// <summary>
/// A timer that counts down from a specified duration to zero.
/// </summary>
/// <param name="duration">The starting time (in seconds) to count down from.</param>
public class CountdownTimer(float duration) : Timer(duration)
{
	/// <summary>
	/// Indicates whether the countdown has reached zero.
	/// </summary>
	public override bool IsFinished => CurrentTime <= 0f;

	/// <summary>
	/// Decrements the timer by <paramref name="deltaTime"/> each frame.
	/// Stops automatically when the countdown reaches zero.
	/// </summary>
	/// <param name="deltaTime">Frame delta time (in seconds).</param>
	public override void Tick(float deltaTime)
	{
		if (!IsRunning || IsFinished) return;

		CurrentTime -= deltaTime;

		if (IsFinished)
			Stop();
	}
}
