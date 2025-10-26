using System;
using Core.AI.BehaviourTrees.Internals;
using Core.Utilities.Logging;

namespace Core.AI.BehaviourTrees.Nodes
{
	/// <summary>
	/// Executes a child node only if a condition is true.
	/// </summary>
	public class ConditionalNode(Func<bool> condition) : Node("Conditional")
	{
		protected override int MaxChildren => 1;
		private readonly Func<bool> _condition = condition;

		protected override NodeStatus OnTick(float deltaTime)
		{
			if (Children.Count == 0)
			{
				LoggerService.Warning($"ConditionalNode '{Name}' has no child.");
				return NodeStatus.Failure;
			}

			return _condition() ? Children[0].Tick(deltaTime) : NodeStatus.Failure;
		}
	}
}
