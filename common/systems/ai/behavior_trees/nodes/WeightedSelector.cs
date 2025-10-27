using System;
using System.Linq;
using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes;

/// <summary>
/// A composite node that selects one of its children to execute based on assigned weights.
/// </summary>
/// <remarks>
/// <para>
/// The <c>WeightedSelector</c> node evaluates all its children and chooses one to tick
/// according to their respective weights. Higher-weighted children have a higher chance
/// of being selected. This allows for probabilistic decision-making in behavior trees.
/// </para>
/// <para>
/// If the selected child returns <see cref="NodeStatus.Running"/>, the <c>WeightedSelector</c>
/// continues ticking that child in subsequent updates. If it returns <see cref="NodeStatus.Success"/>
/// or <see cref="NodeStatus.Failure"/>, the node may re-evaluate and select a new child on the next tick.
/// </para>
/// </remarks>
/// <param name="name">
/// The name of this node. Used for debugging and identification in the behavior tree.
/// </param>
/// <param name="priority">
/// The execution priority of this node relative to its siblings. Higher values indicate higher priority.
/// </param>
public class WeightedSelector(string name = "WeightedSelector", int priority = 1) : Composite(name, priority)
{
	/// <summary>
	/// A shared random number generator used for weighted selection.
	/// <para>
	/// Using a single static instance avoids re-seeding overhead and ensures
	/// consistent randomness across all instances of <see cref="WeightedSelector"/>.
	/// </para>
	/// </summary>
	private static readonly Random RandomGenerator = new();

	protected override NodeStatus OnTick(float deltaTime)
	{
		float totalPriority = Children.Sum(node => Math.Max(0, node.Priority));

		if (totalPriority <= 0f)
			return NodeStatus.Failure;

		float randomThreshold = (float)(RandomGenerator.NextDouble() * totalPriority);

		foreach (Node childNode in Children)
		{
			float weight = Math.Max(0, childNode.Priority);

			if (randomThreshold < weight)
				return childNode.Tick(deltaTime);

			randomThreshold -= weight;
		}

		return NodeStatus.Failure;
	}
}
