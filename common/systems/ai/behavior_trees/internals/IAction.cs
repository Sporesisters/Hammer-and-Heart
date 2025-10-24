namespace Core.AI.BehaviourTrees.Internals;

/// <summary>
/// Defines the contract for executable actions within a behavior tree.
/// <para>
/// An <see cref="IAction"/> represents the lowest-level operation
/// performed by a leaf node, such as moving, attacking, waiting, or playing an animation.
/// </para>
/// <para>
/// Each action returns a <see cref="NodeStatus"/> indicating whether
/// it is still running, has succeeded, or has failed.
/// </para>
/// </summary>
public interface IAction
{
	/// <summary>
	/// Executes a single step of the action’s logic.
	/// <para>
	/// This method is called every tick while the node is active.
	/// It should perform the required behavior and report progress
	/// via its <see cref="NodeStatus"/> return value.
	/// </para>
	/// </summary>
	/// <returns>
	/// The current <see cref="NodeStatus"/> indicating the current state of the action.
	/// </returns>
	NodeStatus Execute();

	/// <summary>
	/// Resets the action to its initial state.
	/// <para>
	/// Called when the behavior tree transitions away from this action
	/// or restarts its execution, allowing cleanup or internal variable resets.
	/// </para>
	/// </summary>
	void Reset() { }
}
