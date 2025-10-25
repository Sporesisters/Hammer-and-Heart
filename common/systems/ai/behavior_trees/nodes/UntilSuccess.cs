using System.Linq;
using Core.AI.BehaviourTrees.Internals;
using Godot.Collections;

namespace Core.AI.BehaviourTrees.Nodes;

public class UntilSuccess(string name) : Node(name)
{
	protected override int MaxChildren => 1;

	public override NodeStatus Tick()
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
