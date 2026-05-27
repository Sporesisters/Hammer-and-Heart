using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes;

/// <summary>
/// A decorator node that repeats its child a specified number of times or indefinitely.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Repeat"/> node ticks its single child repeatedly. Each time the child
/// finishes (returns <see cref="NodeStatus.Success"/> or <see cref="NodeStatus.Failure"/>),
/// the repetition count is incremented.
/// </para>
/// <para>
/// Behavior:
/// <list type="bullet">
/// <item><description>
/// If the child is running, this node returns <see cref="NodeStatus.Running"/>.
/// </description></item>
/// <item><description>
/// If the child finishes and the repeat count has not been reached, it resets the child and ticks again on the next update.
/// </description></item>
/// <item><description>
/// Once the repeat count is reached (for finite repeats), this node returns <see cref="NodeStatus.Success"/>.
/// </description></item>
/// <item><description>
/// If <paramref name="repeatCount"/> is negative, it repeats indefinitely and always returns <see cref="NodeStatus.Running"/>.
/// </description></item>
/// </list>
/// </para>
/// </remarks>
/// <param name="name">
/// The name of this node, used for identification and debugging within the behavior tree.
/// </param>
/// <param name="priority">
/// The execution priority of this node relative to its siblings. Higher values indicate higher priority.
/// </param>
/// <param name="repeatCount">
/// Number of times to repeat the child. Set to a negative value to repeat indefinitely.
/// </param>
public class Repeat(string name = "Repeat", int priority = 1, int repeatCount = -1) : Decorator(name, priority)
{
	/// <summary>
	/// The total number of times the child node should be repeated.
	/// A negative value indicates infinite repetition.
	/// </summary>
	private readonly int _repeatCount = repeatCount;

	/// <summary>
	/// Tracks how many times the child node has completed execution so far.
	/// Increments each time the child finishes (Success or Failure).
	/// </summary>
	private int _currentCount = 0;

	protected override NodeStatus OnTick(float deltaTime)
	{
		if (_repeatCount >= 0 && _currentCount >= _repeatCount)
			return NodeStatus.Success;

		NodeStatus status = Children[0].Tick(deltaTime);

		if (status is not NodeStatus.Running)
		{
			_currentCount++;
			Children[0].Reset();
		}

		return _repeatCount < 0 ? NodeStatus.Running : NodeStatus.Running;
	}

	public override void Reset()
	{
		_currentCount = 0;
		base.Reset();
	}
}
