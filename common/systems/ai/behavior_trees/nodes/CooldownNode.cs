using Core.AI.BehaviourTrees.Internals;
using Core.Timing.Types;
using Core.Utilities.Logging;

namespace Core.AI.BehaviourTrees.Nodes
{
	/// <summary>
	/// Only allows a child node to run if cooldown has elapsed.
	/// </summary>
	public class CooldownNode(float cooldownTime) : Node("Cooldown")
	{
		protected override int MaxChildren => 1;
		private readonly CountdownTimer _timer = new CountdownTimer(cooldownTime);

		public void SetCooldown(float cooldown)
		{
			_timer.ResetWithNewDuration(cooldown);
		}

		protected override NodeStatus OnTick()
		{
			if (Children.Count == 0)
			{
				LoggerService.Warning($"CooldownNode '{Name}' has no child.");
				return NodeStatus.Failure;
			}

			if (!_timer.IsRunning)
				_timer.Start();

			_timer.Tick(1); //change

			if (_timer.IsRunning)
				return NodeStatus.Failure;

			NodeStatus status = Children[0].Tick();
			if (status != NodeStatus.Running)
				_timer.ResetWithNewDuration(_timer.InitialTime);

			return status;
		}

		public override void Reset()
		{
			base.Reset();
			_timer.Reset();
		}
	}
}
