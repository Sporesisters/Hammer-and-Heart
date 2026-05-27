using Core.AI.BehaviourTrees.Internals;
using Core.AI.BehaviourTrees.Actions;

namespace Core.AI.BehaviourTrees.Nodes;

/// <summary>
/// A leaf node that executes a single action strategy.
/// <para>
/// Leaf nodes are terminal nodes in a behavior tree and do not have children.
/// They wrap an <see cref="IAction"/> strategy that is executed when the node ticks.
/// </para>
/// </summary>
/// <param name="name">
/// The name of this node, used for identification and debugging within the behavior tree.
/// </param>
/// <param name="priority">
/// The execution priority of this node relative to its siblings. Higher values indicate higher priority.
/// </param>
/// <param name="strategy">
/// The action strategy to execute. If null, a default <see cref="EmptyAction"/> is used.
/// </param>
public class Leaf(string name = "Leaf", int priority = 1, IAction? strategy = null) : Node(name, priority)
{
	/// <summary>
	/// The action strategy executed by this leaf node.
	/// </summary>
	private readonly IAction _strategy = strategy ?? new EmptyAction();

	protected override NodeStatus OnTick(float deltaTime) => _strategy.Execute(deltaTime);

	public override void Reset() => _strategy.Reset();
}
