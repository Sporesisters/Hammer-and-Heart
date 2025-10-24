using System;

namespace Core.AI.BehaviourTrees
{
	public class ActionStrategy : IStrategy
	{
		private readonly Action action;

		public ActionStrategy(Action action) => this.action = action;

		public BTStatus Process()
		{
			action();
			return BTStatus.Success;
		}
	}
}
