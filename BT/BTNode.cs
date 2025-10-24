using System.Collections.Generic;

namespace Core.BehaviourTrees
{
	public class BTNode
	{
		public readonly string Name;
		public readonly int Priority;
		public readonly List<BTNode> Children = new();

		protected int currentChild;

		public BTNode(string name = "Node", int priority = 0)
		{
			Name = name;
			Priority = priority;
		}

		public void AddChild(BTNode child) => Children.Add(child);

		public virtual BTStatus Process() => Children[currentChild].Process();

		public virtual void Reset()
		{
			currentChild = 0;
			foreach (var child in Children)
				child.Reset();
		}
	}
}
