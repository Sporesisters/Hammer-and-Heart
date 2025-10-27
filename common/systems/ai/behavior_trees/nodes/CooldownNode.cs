using Core.AI.BehaviourTrees.Internals;
using Core.Timing.Types;

namespace Core.AI.BehaviourTrees.Nodes;

/// <summary>
/// A decorator node that only allows its child to run if a cooldown period has elapsed.
/// <para>
/// After the child node executes (success or failure), the cooldown starts.
/// During the cooldown, the node always returns <see cref="NodeStatus.Failure"/>.
/// </para>
/// </summary>
/// <param name="name">
/// The name of this node, used for identification and debugging within the behavior tree.
/// </param>
/// <param name="priority">
/// The execution priority of this node relative to its siblings. Higher values indicate higher priority.
/// </param>
/// <param name="cooldownTime">
/// The cooldown duration in seconds. Defaults to 0 (no cooldown).
/// </param>
public class Cooldown(string name = "Cooldown", int priority = 1, float cooldownTime = 0) : Decorator(name, priority)
{
	/// <summary>
	/// The internal countdown timer used to track the cooldown.
	/// </summary>
	private readonly CountdownTimer _timer = new(cooldownTime);

	protected override NodeStatus OnTick(float deltaTime)
	{
		if (_timer.IsRunning)
		{
			_timer.Tick(deltaTime);
			return NodeStatus.Failure;
		}

		NodeStatus status = Children[0].Tick(deltaTime);

		if (status is not NodeStatus.Running)
			_timer.ResetWithNewDuration(_timer.InitialTime);

		return status;
	}

	public override void Reset()
	{
		_timer.Reset();
		base.Reset();
	}
}
