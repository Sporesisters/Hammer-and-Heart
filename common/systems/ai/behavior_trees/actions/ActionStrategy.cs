using System;
using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Actions;

/// <summary>
/// Represents a simple stateless action node that executes a provided delegate.
/// <para>
/// The <see cref="ActionStrategy"/> runs the given <see cref="Action"/> once each time it is ticked
/// and always returns <see cref="NodeStatus.Success"/> upon completion.
/// </para>
/// <para>
/// This is useful for triggering instant or side-effect-based operations such as playing sounds,
/// toggling flags, or sending events within a behavior tree.
/// </para>
/// </summary>
/// <param name="action">
/// The delegate to execute. Must not be <see langword="null"/>.
/// </param>
public class ActionStrategy(Action action) : IAction
{
	/// <summary>
	/// The action delegate to execute.
	/// </summary>
	private readonly Action _action = action;

	public NodeStatus Execute()
	{
		_action();
		return NodeStatus.Success;
	}
}
