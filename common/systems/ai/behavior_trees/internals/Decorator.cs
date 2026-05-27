namespace Core.AI.BehaviourTrees.Internals;

/// <summary>
/// Represents a decorator node in a Behavior Tree.
/// <para>
/// Decorators wrap a single child and can modify or control its execution.
/// Examples include Timeout, Cooldown, and Conditional nodes.
/// </para>
/// </summary>
/// <param name="name">
/// The name of this decorator node, used for debugging and identification.
/// </param>
/// <param name="priority">
/// The execution priority of this node relative to its siblings. Higher values indicate higher priority.
/// </param>
public abstract class Decorator(string name, int priority) : Node(name, priority)
{
	protected override bool RequiresChildren => true;
	protected override int MaxChildren => 1;
}
