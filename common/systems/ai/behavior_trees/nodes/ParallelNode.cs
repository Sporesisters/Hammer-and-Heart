using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes
{
	public class ParallelNode(ParallelPolicy successPolicy = ParallelPolicy.RequireAll,
							 ParallelPolicy failurePolicy = ParallelPolicy.RequireOne) : Node("Parallel")
	{
		private readonly ParallelPolicy _successPolicy = successPolicy;
		private readonly ParallelPolicy _failurePolicy = failurePolicy;

		protected override NodeStatus OnTick(float deltaTime)
		{
			int successCount = 0, failureCount = 0;

			foreach (var child in Children)
			{
				NodeStatus status = child.Tick(deltaTime);
				if (status == NodeStatus.Success) successCount++;
				if (status == NodeStatus.Failure) failureCount++;
			}

			if ((_successPolicy == ParallelPolicy.RequireAll && successCount == Children.Count) ||
				(_successPolicy == ParallelPolicy.RequireOne && successCount > 0))
				return NodeStatus.Success;

			if ((_failurePolicy == ParallelPolicy.RequireAll && failureCount == Children.Count) ||
				(_failurePolicy == ParallelPolicy.RequireOne && failureCount > 0))
				return NodeStatus.Failure;

			return NodeStatus.Running;
		}
	}
}
