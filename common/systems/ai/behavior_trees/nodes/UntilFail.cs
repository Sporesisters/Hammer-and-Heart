using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes;

/// <summary>
/// A decorator node that repeatedly ticks its single child until the child fails.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="UntilFail"/> node continuously executes its child node on each tick.
/// If the child returns <see cref="NodeStatus.Running"/> or <see cref="NodeStatus.Success"/>,
/// this node continues running. Once the child returns <see cref="NodeStatus.Failure"/>,
/// this node calls <see cref="Reset"/> to clear its internal state and then returns
/// <see cref="NodeStatus.Failure"/> to its parent.
/// </para>
/// <para>
/// This node is useful for tasks that should repeat until a failure condition is met,
/// such as monitoring a condition or repeatedly attempting an action until it can no longer succeed.
/// </para>
/// </remarks>
/// <param name="name">
/// The name of this node, used for identification and debugging within the behavior tree.
/// </param>
/// <param name="priority">
/// The execution priority of this node relative to its siblings. Higher values indicate higher priority.
/// </param>
public class UntilFail(string name = "UntilFail", int priority = 1) : Decorator(name, priority)
{
	protected override NodeStatus OnTick(float deltaTime)
	{
		NodeStatus status = Children[0].Tick(deltaTime);

		if (status is NodeStatus.Failure)
		{
			Reset();
			return NodeStatus.Failure;
		}

		return NodeStatus.Running;
	}
}
