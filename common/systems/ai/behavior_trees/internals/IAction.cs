namespace Core.AI.BehaviourTrees.Internals;

/// <summary>
/// Defines an executable behavior for a <see cref="Nodes.Leaf"/> node.
/// </summary>
/// <remarks>
/// Represents low-level actions like movement, attacks, waiting, or animations.
/// Each call to <see cref="Execute"/> advances the action and returns its
/// current <see cref="NodeStatus"/>.
/// <see cref="Reset"/> restores the action to its initial state.
/// </remarks>
public interface IAction
{
	/// <summary>
	/// Executes one update step of the action.
	/// </summary>
	/// <param name="deltaTime">Time since last tick in seconds.</param>
	/// <returns>The current <see cref="NodeStatus"/> of the action.</returns>
	NodeStatus Execute(float deltaTime);

	/// <summary>
	/// Restores the action to its initial state.
	/// </summary>
	void Reset() { }
}
