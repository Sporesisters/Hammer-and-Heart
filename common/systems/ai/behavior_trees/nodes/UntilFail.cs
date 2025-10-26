using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes;

public class UntilFail(string name) : Node(name)
{
	protected override int MaxChildren => 1;

	protected override NodeStatus OnTick(float deltaTime)
	{
		NodeStatus status = Children[0].Tick(deltaTime);

		if (status == NodeStatus.Failure)
		{
			Reset();
			return NodeStatus.Failure;
		}

		return NodeStatus.Running;
	}
}
