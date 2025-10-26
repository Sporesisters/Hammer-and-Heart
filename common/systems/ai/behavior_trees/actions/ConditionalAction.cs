using System;
using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Actions;

/// <summary>
/// Represents a conditional check within a behavior tree.
/// <para>
/// The <see cref="ConditionalAction"/> evaluates a provided <see cref="Func{Boolean}"/> delegate
/// to determine if a condition is satisfied. If the predicate returns <c>true</c>,
/// the node returns <see cref="NodeStatus.Success"/>; otherwise, it returns <see cref="NodeStatus.Failure"/>.
/// </para>
/// <para>
/// This action is stateless — it performs a single evaluation per tick
/// without storing or modifying any internal state.
/// </para>
/// </summary>
/// <param name="predicate">
/// A delegate representing the condition to evaluate.
/// </param>
public class ConditionalAction(Func<bool> predicate) : IAction
{
	/// <summary>
	/// The predicate function used to evaluate the condition.
	/// </summary>
	private readonly Func<bool> _predicate = predicate;

	public NodeStatus Execute(float delatTime)
	{
		return _predicate() ? NodeStatus.Success : NodeStatus.Failure;
	}
}
