namespace Core.AI.BehaviourTrees.Internals;

/// <summary>
/// Represents a composite node in a Behavior Tree.
/// <para>
/// Composite nodes manage multiple children and define a strategy
/// for ticking them (e.g., Sequence, Selector, Parallel).
/// </para>
/// </summary>
/// <param name="name">
/// The name of this composite node, used for debugging and identification.
/// </param>
/// <param name="priority">
/// The execution priority of this node relative to its siblings. Higher values indicate higher priority.
/// </param>
public abstract class Composite(string name, int priority) : Node(name, priority)
{
	protected override bool RequiresChildren => true;
	protected override int MaxChildren => -1;
}
