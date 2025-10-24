using System;
using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Actions;

/// <summary>
/// Represents a conditional action used within a behavior tree.
/// <para>
/// The <see cref="ConditionStrategy"/> evaluates a provided <see cref="Func{Boolean}"/> delegate
/// to determine whether a condition is met. If the predicate returns <c>true</c>,
/// the action reports <see cref="NodeStatus.Success"/>; otherwise, it reports <see cref="NodeStatus.Failure"/>.
/// </para>
/// <para>
/// This class is stateless and performs a one-step evaluation without maintaining
/// any persistent state between ticks.
/// </para>
/// </summary>
/// <param name="predicate">
/// A boolean function representing the condition to evaluate.
/// </param>
public class ConditionStrategy(Func<bool> predicate) : IAction
{
	/// <summary>
	/// The predicate function used to evaluate the condition.
	/// </summary>
	private readonly Func<bool> _predicate = predicate;

	public NodeStatus Execute()
	{
		return _predicate() ? NodeStatus.Success : NodeStatus.Failure;
	}
}
