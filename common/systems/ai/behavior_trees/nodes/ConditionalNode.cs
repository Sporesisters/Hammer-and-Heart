using System;
using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes;

/// <summary>
/// A decorator node that conditionally executes its single child node based on a provided predicate.
/// <para>
/// The <see cref="ConditionalNode"/> wraps one child node and evaluates the given <paramref name="condition"/>
/// delegate before execution. If the condition returns <c>true</c>, the child node is ticked normally.
/// If the condition returns <c>false</c>, this node immediately returns <see cref="NodeStatus.Failure"/>
/// without executing the child.
/// </para>
/// </summary>
/// <param name="name">
/// The name of this node, used for identification and debugging within the behavior tree.
/// </param>
/// <param name="priority">
/// The execution priority of this node relative to its siblings. Higher values indicate higher priority.
/// </param>
/// <param name="condition">
/// A delegate representing the condition to evaluate. If null, defaults to a function that always returns true.
/// </param>
public class ConditionalNode(string name = "Conditional", int priority = 1, Func<bool>? condition = null) : Decorator(name, priority)
{
	/// <summary>
	/// The delegate representing the condition to evaluate before ticking the child.
	/// </summary>
	private readonly Func<bool> _condition = condition ?? (() => true);

	protected override NodeStatus OnTick(float deltaTime)
		=> _condition() ? Children[0].Tick(deltaTime) : NodeStatus.Failure;
}
