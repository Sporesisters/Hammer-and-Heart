using System.Collections.Generic;
using Core.AI.BehaviourTrees.Internals;
using Core.Utilities.Extensions;

namespace Core.AI.BehaviourTrees.Nodes;

/// <summary>
/// A composite selector node that ticks its child nodes in a random order on each tick.
/// <para>
/// Inherits from <see cref="PrioritySelector"/>, but overrides the child sorting
/// behavior so that the execution order is randomized rather than priority-based.
/// </para>
/// <para>
/// This is useful when you want nondeterministic AI behavior, such as randomly
/// choosing between several equally valid actions each update.
/// </para>
/// </summary>
/// <param name="name">
/// The name of this node, used for identification and debugging within the behavior tree.
/// </param>
/// <param name="priority">
/// The execution priority of this node relative to its siblings. Higher values indicate higher priority.
/// </param>
public class RandomSelector(string name, int priority = 1) : PrioritySelector(name, priority)
{
	/// <summary>
	/// Overrides the child sorting to shuffle the children randomly.
	/// A new randomized order is generated on each tick.
	/// </summary>
	protected override List<Node> SortChildren() => [.. Children.Shuffle()];
}
