using Core.AI.BehaviourTrees.Internals;
using Core.Utilities.Logging;

namespace Core.AI.BehaviourTrees.Nodes
{
	/// <summary>
	/// Repeats a child node a fixed number of times (or indefinitely if repeatCount = -1).
	/// </summary>
	public class Repeat(int repeatCount = -1) : Node("Repeat")
	{
		protected override int MaxChildren => 1;
		private readonly int _repeatCount = repeatCount;
		private int _currentCount = 0;

		protected override NodeStatus OnTick()
		{
			if (Children.Count == 0)
			{
				LoggerService.Warning($"Repeat node '{Name}' has no child.");
				return NodeStatus.Failure;
			}

			if (_repeatCount >= 0 && _currentCount >= _repeatCount)
				return NodeStatus.Success;

			NodeStatus status = Children[0].Tick();
			if (status != NodeStatus.Running)
				_currentCount++;

			return NodeStatus.Running;
		}

		public override void Reset()
		{
			base.Reset();
			_currentCount = 0;
		}
	}
}
