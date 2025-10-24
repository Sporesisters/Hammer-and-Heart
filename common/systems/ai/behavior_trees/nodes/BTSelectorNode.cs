namespace Core.AI.BehaviourTrees
{
	public class BTSelectorNode : BTNode
	{
		public BTSelectorNode(string name, int priority = 0) : base(name, priority) { }

		public override BTStatus Process()
		{
			while (currentChild < Children.Count)
			{
				switch (Children[currentChild].Process())
				{
					case BTStatus.Running:
						return BTStatus.Running;
					case BTStatus.Success:
						Reset();
						return BTStatus.Success;
					default:
						currentChild++;
						break;
				}
			}

			Reset();
			return BTStatus.Failure;
		}
	}
}
