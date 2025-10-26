using System;
using System.Collections.Generic;
using System.Linq;
using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes
{
	/// <summary>
	/// Picks one child to execute based on weights.
	/// </summary>
	public class WeightedSelectorNode : Node
	{
		private readonly List<(Node node, float weight)> _children = [];
		private readonly Random _random = new Random();

		public void AddWeightedChild(Node node, float weight)
		{
			_children.Add((node, weight));
			AddChild(node);
		}

		protected override NodeStatus OnTick()
		{
			if (_children.Count == 0)
				return NodeStatus.Failure;

			float totalWeight = _children.Sum(c => c.weight);
			float r = (float)(_random.NextDouble() * totalWeight);

			foreach (var (node, weight) in _children)
			{
				if (r < weight)
					return node.Tick();
				r -= weight;
			}

			return NodeStatus.Failure;
		}
	}
}
