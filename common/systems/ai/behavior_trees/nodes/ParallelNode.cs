using Core.AI.BehaviourTrees.Internals;
using static Core.AI.BehaviourTrees.Nodes.ParallelNode;

namespace Core.AI.BehaviourTrees.Nodes;

/// <summary>
/// A composite node that executes all of its child nodes concurrently within a single tick.
/// <para>
/// The <see cref="ParallelNode"/> determines its <see cref="NodeStatus"/> based on the results of its children
/// and the configured <paramref name="successPolicy"/> and <paramref name="failurePolicy"/>:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <see cref="ParallelPolicy.RequireAll"/> — succeeds/fails only if all children succeed/fail.
/// </description></item>
/// <item><description>
/// <see cref="ParallelPolicy.RequireOne"/> — succeeds/fails if at least one child succeeds/fails.
/// </description></item>
/// <item>
/// <description><see cref="ParallelPolicy.Majority"/> — succeeds/fails when the majority of children succeed/fail.
/// </description></item>
/// <item><description>
/// <see cref="ParallelPolicy.None"/> — succeeds/fails only if no children succeed/fail (inverse of RequireOne).
/// </description></item>
/// </list>
/// <para>
/// Useful for running multiple actions or checks simultaneously, such as animating multiple character actions
/// or monitoring multiple conditions concurrently.
/// </para>
/// </summary>
/// <param name="name">
/// The name of this node, used for identification and debugging within the behavior tree.
/// </param>
/// <param name="priority">
/// The execution priority of this node relative to its siblings. Higher values indicate higher priority.
/// </param>
/// <param name="successPolicy">
/// Determines how the node evaluates success based on its children's results.
/// </param>
/// <param name="failurePolicy">
/// Determines how the node evaluates failure based on its children's results.
/// </param>
public class ParallelNode(string name = "Parallel", int priority = 1, ParallelPolicy successPolicy = ParallelPolicy.RequireAll, ParallelPolicy failurePolicy = ParallelPolicy.RequireOne) : Composite(name, priority)
{
	/// <summary>
	/// Policies used by <see cref="ParallelNode"/> to determine success or failure based on its children's results.
	/// </summary>
	public enum ParallelPolicy
	{
		/// <summary>All children must succeed/fail for the node to succeed/fail.</summary>
		RequireAll,

		/// <summary>At least one child must succeed/fail for the node to succeed/fail.</summary>
		RequireOne,

		/// <summary>The node succeeds/fails when a majority of children succeed/fail.</summary>
		Majority,

		/// <summary>The node succeeds/fails only if no children succeed/fail (inverse of RequireOne).</summary>
		None,
	}

	/// <summary>
	/// The policy that determines when the <see cref="ParallelNode"/> considers itself successful
	/// based on the results of its child nodes.
	/// </summary>
	private readonly ParallelPolicy _successPolicy = successPolicy;

	/// <summary>
	/// The policy that determines when the <see cref="ParallelNode"/> considers itself failed
	/// based on the results of its child nodes.
	/// </summary>
	private readonly ParallelPolicy _failurePolicy = failurePolicy;

	protected override NodeStatus OnTick(float deltaTime)
	{
		int successCount = 0;
		int failureCount = 0;

		foreach (Node child in Children)
		{
			NodeStatus status = child.Tick(deltaTime);

			switch (status)
			{
				case NodeStatus.Success:
					successCount++;
					break;

				case NodeStatus.Failure:
					failureCount++;
					break;
			}

			if (IsSuccessPolicyMet(successCount)) return NodeStatus.Success;
			if (IsFailurePolicyMet(failureCount)) return NodeStatus.Failure;
		}

		return NodeStatus.Running;
	}

	/// <summary>
	/// Evaluates whether the node meets the success policy based on the number of successful children.
	/// </summary>
	/// <param name="successCount">The number of child nodes that succeeded.</param>
	/// <returns><c>true</c> if the success policy is satisfied; otherwise, <c>false</c>.</returns>
	private bool IsSuccessPolicyMet(int successCount)
	{
		return _successPolicy switch
		{
			ParallelPolicy.RequireAll => successCount == Children.Count,
			ParallelPolicy.RequireOne => successCount > 0,
			ParallelPolicy.Majority => successCount > Children.Count / 2,
			ParallelPolicy.None => successCount == 0,
			_ => false
		};
	}

	/// <summary>
	/// Evaluates whether the node meets the failure policy based on the number of failed children.
	/// </summary>
	/// <param name="failureCount">The number of child nodes that failed.</param>
	/// <returns><c>true</c> if the failure policy is satisfied; otherwise, <c>false</c>.</returns>
	private bool IsFailurePolicyMet(int failureCount)
	{
		return _failurePolicy switch
		{
			ParallelPolicy.RequireAll => failureCount == Children.Count,
			ParallelPolicy.RequireOne => failureCount > 0,
			ParallelPolicy.Majority => failureCount > Children.Count / 2,
			ParallelPolicy.None => failureCount == 0,
			_ => false
		};
	}
}
