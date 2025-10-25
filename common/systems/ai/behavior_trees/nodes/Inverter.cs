using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes;

public class Inverter(string name) : Node(name)
{
	protected override int MaxChildren => 1;

	public override NodeStatus Tick()
	{
		NodeStatus status = Children[0].Tick();

		return status switch
		{
			NodeStatus.Success => NodeStatus.Failure,
			NodeStatus.Failure => NodeStatus.Success,
			_ => NodeStatus.Running
		};
	}
}
