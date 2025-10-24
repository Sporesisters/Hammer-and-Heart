namespace Core.AI.BehaviourTrees
{
	// -------------------------
	// Control Nodes
	// -------------------------
	public class BTSequenceNode : BTNode
	{
		public BTSequenceNode(string name, int priority = 0) : base(name, priority) { }

		public override BTStatus Process()
		{
			while (currentChild < Children.Count)
			{
				switch (Children[currentChild].Process())
				{
					case BTStatus.Running:
						return BTStatus.Running;
					case BTStatus.Failure:
						Reset();
						return BTStatus.Failure;
					default:
						currentChild++;
						break;
				}
			}

			Reset();
			return BTStatus.Success;
		}
	}
}
