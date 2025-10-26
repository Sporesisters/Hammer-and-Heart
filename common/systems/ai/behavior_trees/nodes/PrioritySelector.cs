using System.Collections.Generic;
using System.Linq;
using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes;

public class PrioritySelector(string name, int priority = 0) : Selector(name, priority)
{
	private List<Node>? _sortedChildren;
	protected List<Node> SortedChildren => _sortedChildren ??= SortChildren();

	protected virtual List<Node> SortChildren() => [.. Children.OrderByDescending(child => child.Priority)];

	protected override NodeStatus OnTick(float deltaTime)
	{
		foreach (Node child in SortedChildren)
		{
			NodeStatus status = child.Tick(deltaTime);

			switch (status)
			{
				case NodeStatus.Running:
					return NodeStatus.Running;

				case NodeStatus.Success:
					Reset();
					return NodeStatus.Success;

				default:
					continue;
			}
		}

		Reset();
		return NodeStatus.Failure;
	}

	public override void Reset()
	{
		base.Reset();
		_sortedChildren = null;
	}
}