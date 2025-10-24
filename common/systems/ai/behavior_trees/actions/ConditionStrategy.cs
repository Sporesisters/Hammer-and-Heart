using System;

namespace Core.AI.BehaviourTrees
{
	// -------------------------
	// Example Leaf Strategies
	// -------------------------
	public class ConditionStrategy : IStrategy
	{
		private readonly Func<bool> predicate;

		public ConditionStrategy(Func<bool> predicate)
		{
			this.predicate = predicate;
		}

		public BTStatus Process() => predicate() ? BTStatus.Success : BTStatus.Failure;
	}
}
