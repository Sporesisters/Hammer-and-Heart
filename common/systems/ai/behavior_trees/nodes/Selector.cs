using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes;

public class Selector(string name, int priority = 0) : Node(name, priority)
{
	public override NodeStatus Tick()
	{
		while (currentChild < Children.Count)
		{
			NodeStatus status = Children[currentChild].Tick();

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
