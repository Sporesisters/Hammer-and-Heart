using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes;

public class Leaf(string name, IAction strategy, int priority = 0) : Node(name, priority)
{
	private readonly IAction strategy = strategy;

	public override NodeStatus Tick() => strategy.Execute();
	public override void Reset() => strategy.Reset();
}
