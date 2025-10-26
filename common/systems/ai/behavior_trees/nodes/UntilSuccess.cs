using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes;

public class UntilSuccess(string name) : Node(name)
{
	protected override int MaxChildren => 1;

	protected override NodeStatus OnTick()
	{
		NodeStatus status = Children[0].Tick();

		if (status == NodeStatus.Success)
		{
			Reset();
			return NodeStatus.Success;
		}

		return NodeStatus.Running;
	}
}
