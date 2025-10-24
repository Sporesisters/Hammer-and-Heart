namespace Core.BehaviourTrees
{
	public class BTLeafNode : BTNode
	{
		private readonly IStrategy strategy;

		public BTLeafNode(string name, IStrategy strategy, int priority = 0) : base(name, priority)
		{
			this.strategy = strategy;
		}

		public override BTStatus Process() => strategy.Process();
		public override void Reset() => strategy.Reset();
	}
}
