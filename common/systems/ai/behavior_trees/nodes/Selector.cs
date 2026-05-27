using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes;

/// <summary>
/// A composite node that ticks its children in order until one succeeds.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Selector"/> node iterates over its child nodes sequentially.
/// It returns <see cref="NodeStatus.Success"/> as soon as a child succeeds,
/// otherwise it continues to the next child. If all children fail, the node
/// returns <see cref="NodeStatus.Failure"/>.
/// </para>
/// <para>
/// This node is useful for implementing fallback or alternative behaviors,
/// where multiple options are tried in order until one succeeds.
/// </para>
/// </remarks>
/// <param name="name">
/// The name of this node, used for identification and debugging within the behavior tree.
/// </param>
/// <param name="priority">
/// The execution priority of this node relative to its siblings. Higher values indicate higher priority.
/// </param>
public class Selector(string name = "Selector", int priority = 1) : Composite(name, priority)
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

				case NodeStatus.Success:
					Reset();
					return NodeStatus.Success;

				default:
					currentChild++;
					break;
			}
		}

		Reset();
		return NodeStatus.Failure;
	}
}
