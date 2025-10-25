using System.Collections.Generic;
using Core.AI.BehaviourTrees.Internals;
using Core.Utilities.Extensions;

namespace Core.AI.BehaviourTrees.Nodes;

public class RandomSelector(string name, int priority = 0) : PrioritySelector(name, priority)
{
	protected override List<Node> SortChildren() => [.. Children.Shuffle()];
}
