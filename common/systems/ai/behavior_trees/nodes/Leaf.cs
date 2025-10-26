using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes
{
	/// <summary>
	/// A leaf node that executes an IAction strategy.
	/// </summary>
	public class Leaf(string name, IAction strategy, int priority = 0) : Node(name, priority)
	{
		private readonly IAction _strategy = strategy;

		protected override NodeStatus OnTick() => _strategy.Execute();
		public override void Reset() => _strategy.Reset();
	}
}
