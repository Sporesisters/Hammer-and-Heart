using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes;

public class UntilFail(string name) : Node(name)
{
	protected override int MaxChildren => 1;

	protected override NodeStatus OnTick()
	{
		NodeStatus status = Children[0].Tick();

		if (status == NodeStatus.Failure)
		{
			Reset();
			return NodeStatus.Failure;
		}

		return NodeStatus.Running;
	}
}
