using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes;

/// <summary>
/// A composite node that ticks its children in order until one fails.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Sequence"/> node executes its child nodes sequentially.
/// It returns <see cref="NodeStatus.Failure"/> as soon as a child fails,
/// otherwise it continues to the next child. If all children succeed,
/// the node returns <see cref="NodeStatus.Success"/>.
/// </para>
/// <para>
/// This node is useful for implementing tasks that must complete all steps
/// in a specific order, such as a multi-step action or a chain of conditions.
/// </para>
/// </remarks>
/// <param name="name">
/// The name of this node, used for identification and debugging within the behavior tree.
/// </param>
/// <param name="priority">
/// The execution priority of this node relative to its siblings. Higher values indicate higher priority.
/// </param>
public class Sequence(string name = "Sequence", int priority = 1) : Composite(name, priority)
{
	protected override NodeStatus OnTick(float deltaTime)
	{
		while (currentChild < Children.Count)
		{
			NodeStatus status = Children[currentChild].Tick(deltaTime);

			switch (status)
			{
				case NodeStatus.Running:
					return NodeStatus.Running;

				case NodeStatus.Failure:
					Reset();
					return NodeStatus.Failure;

				default:
					currentChild++;
					break;
			}
		}

		Reset();
		return NodeStatus.Success;
	}
}
