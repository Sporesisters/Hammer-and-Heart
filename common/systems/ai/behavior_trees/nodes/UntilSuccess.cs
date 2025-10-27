using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes;

/// <summary>
/// A decorator node that repeatedly ticks its single child until the child succeeds.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="UntilSuccess"/> node continuously executes its child node each tick.
/// If the child returns <see cref="NodeStatus.Running"/> or <see cref="NodeStatus.Failure"/>,
/// this node continues running. Once the child returns <see cref="NodeStatus.Success"/>,
/// this node calls <see cref="Reset"/> to clear its internal state and then returns
/// <see cref="NodeStatus.Success"/> to its parent.
/// </para>
/// <para>
/// This node is useful for tasks that must repeat until successful, such as waiting
/// for a condition to become true or retrying an action until it succeeds.
/// </para>
/// </remarks>
/// <param name="name">
/// The name of this node, used for identification and debugging purposes in the behavior tree.
/// </param>
/// <param name="priority">
/// The execution priority of this node relative to its siblings. Higher values indicate higher priority.
/// </param>
public class UntilSuccess(string name = "UntilSuccess", int priority = 1) : Decorator(name, priority)
{
	protected override NodeStatus OnTick(float deltaTime)
	{
		NodeStatus status = Children[0].Tick(deltaTime);

		if (status is NodeStatus.Success)
		{
			Reset();
			return NodeStatus.Success;
		}

		return NodeStatus.Running;
	}
}
