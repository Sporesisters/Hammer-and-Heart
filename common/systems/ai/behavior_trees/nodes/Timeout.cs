using Core.AI.BehaviourTrees.Internals;
using Core.Timing.Types;

namespace Core.AI.BehaviourTrees.Nodes;

/// <summary>
/// A decorator node that enforces a maximum execution time on its child.
/// <para>
/// The <see cref="Timeout"/> node wraps a single child node and monitors how long it runs.
/// If the child exceeds the specified duration, the node returns <see cref="NodeStatus.Failure"/>.
/// Otherwise, it returns the child’s status.
/// </para>
/// </summary>
/// <param name="name">
/// The name of this node, used for identification and debugging within the behavior tree.
/// </param>
/// <param name="priority">
/// The execution priority of this node relative to its siblings. Higher values indicate higher priority.
/// </param>
/// <param name="duration">
/// The maximum allowed duration (in seconds) for the child to run before timing out. Defaults to 0.
/// </param>
public class Timeout(string name = "Timeout", int priority = 1, float duration = 0) : Decorator(name, priority)
{
	/// <summary>
	/// Internal timer used to track how long the child has been running.
	/// </summary>
	private readonly CountdownTimer _timer = new(duration);

	protected override NodeStatus OnTick(float deltaTime)
	{
		if (!_timer.IsRunning)
			_timer.Start();

		_timer.Tick(deltaTime);

		if (_timer.IsFinished)
		{
			_timer.Reset();
			return NodeStatus.Failure;
		}

		NodeStatus status = Children[0].Tick(deltaTime);

		if (status is not NodeStatus.Running)
			_timer.Reset();

		return status;
	}

	public override void Reset()
	{
		_timer.Reset();
		base.Reset();
	}
}
