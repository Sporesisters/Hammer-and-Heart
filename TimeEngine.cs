using System;
using System.Collections.Generic;
using Godot;

public static class TimerManager
{
	private static readonly List<Timer> _timers = [];

	public static void RegisterTimer(Timer timer) => _timers.Add(timer);
	public static void UnregisterTimer(Timer timer) => _timers.Remove(timer);

	public static void UpdateTimers(float deltaTime)
	{
		foreach (Timer timer in new List<Timer>(_timers))
		{
			timer.Tick(deltaTime);
		}
	}

	public static void ClearTimers() => _timers.Clear();
}

public abstract class Timer : IDisposable
{
	public float CurrentTime { get; protected set; }
	public bool IsRunning { get; protected set; }

	public float InitialTime { get; protected set; }

	public float Progress => Mathf.Clamp(CurrentTime / InitialTime, 0, 1);

	public Action OnTimerStart = delegate { };
	public Action OnTimerStop = delegate { };
	public Action OnTimerInternal = delegate { };


	protected Timer(float value)
	{
		InitialTime = value;
	}

	public void Start()
	{
		CurrentTime = InitialTime;

		if (!IsRunning)
		{
			IsRunning = true;
			TimerManager.RegisterTimer(this);
			OnTimerStart.Invoke();
		}
	}

	public void Stop()
	{
		if (IsRunning)
		{
			IsRunning = false;
			TimerManager.UnregisterTimer(this);
			OnTimerStop.Invoke();
		}
	}


	public abstract void Tick(float deltaTime);
	public abstract bool IsFinished { get; }


	public void Resume() => IsRunning = true;
	public void Pause() => IsRunning = false;

	public virtual void Reset() => CurrentTime = InitialTime;
	public virtual void ResetWithNewDuration(float duration)
	{
		InitialTime = duration;
		Reset();
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposed) return;

		if (disposing)
		{
			TimerManager.UnregisterTimer(this);
		}

		disposed = true;
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	~Timer()
	{
		Dispose(false);
	}

	bool disposed;
}

public class CountdownTimer(float value) : Timer(value)
{
	public override bool IsFinished => CurrentTime <= 0;
	public override void Tick(float deltaTime)
	{
		if (IsRunning && CurrentTime > 0)
		{
			CurrentTime -= deltaTime;
		}

		if (IsRunning && CurrentTime <= 0)
		{
			Stop();
		}
	}
}
