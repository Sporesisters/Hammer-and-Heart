using System;
using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Actions;

/// <summary>
/// Represents a simple stateless action that executes a provided delegate instantly.
/// <para>
/// The <see cref="InstantAction"/> runs the given <see cref="Action"/> once per tick
/// and always returns <see cref="NodeStatus.Success"/>.
/// </para>
/// <para>
/// Useful for immediate or side-effect-based operations such as triggering animations,
/// playing sounds, or sending events.
/// </para>
/// </summary>
/// <param name="action">
/// The delegate to execute. Must not be <see langword="null"/>.
/// </param>
public class InstantAction(Action<float> action) : IAction
{
	/// <summary>
	/// The action delegate to execute. Must not be <see langword="null"/>.
	/// </summary>
	private readonly Action<float> _action = action;

	public NodeStatus Execute(float deltaTime)
	{
		_action(deltaTime);
		return NodeStatus.Success;
	}
}
