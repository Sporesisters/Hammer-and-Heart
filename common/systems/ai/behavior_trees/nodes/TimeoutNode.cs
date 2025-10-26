using Core.AI.BehaviourTrees.Internals;
using Core.Timing.Types;
using Core.Utilities.Logging;

namespace Core.AI.BehaviourTrees.Nodes
{
	/// <summary>
	/// Ticks a child node but fails if it exceeds a duration.
	/// </summary>
	public class TimeoutNode(float duration) : Node("Timeout")
	{
		protected override int MaxChildren => 1;
		private readonly CountdownTimer _timer = new CountdownTimer(duration);

		protected override NodeStatus OnTick(float deltaTime)
		{
			if (Children.Count == 0)
			{
				LoggerService.Warning($"TimeoutNode '{Name}' has no child.");
				return NodeStatus.Failure;
			}

			if (!_timer.IsRunning)
				_timer.Start();

			_timer.Tick(deltaTime);

			if (_timer.IsFinished)
			{
				_timer.Reset();
				return NodeStatus.Failure;
			}

			NodeStatus status = Children[0].Tick(deltaTime);

			if (status != NodeStatus.Running)
				_timer.Reset();

			return status;
		}

		public override void Reset()
		{
			base.Reset();
			_timer.Reset();
		}
	}
}
