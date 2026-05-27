using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Actions;

/// <summary>
/// Represents a no-op (empty) action that can be used as a placeholder
/// when no real action strategy is provided to a <see cref="Nodes.Leaf"/> node.
/// </summary>
/// <remarks>
/// This action always returns <see cref="NodeStatus.Success"/> when executed
/// and has no side effects. Its <see cref="Reset"/> method performs no operation.
/// This is useful for creating optional leaf nodes or safely handling null strategies.
/// </remarks>
public class EmptyAction : IAction
{
	public NodeStatus Execute(float deltaTime) => NodeStatus.Success;
}
