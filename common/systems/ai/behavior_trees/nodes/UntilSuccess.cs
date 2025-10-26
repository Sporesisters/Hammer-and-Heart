using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes;

public class UntilSuccess(string name) : Node(name)
{
	protected override int MaxChildren => 1;

	protected override NodeStatus OnTick(float deltaTime)
	{
		NodeStatus status = Children[0].Tick(deltaTime);

		if (status == NodeStatus.Success)
		{
			Reset();
			return NodeStatus.Success;
		}

		return NodeStatus.Running;
	}
}
