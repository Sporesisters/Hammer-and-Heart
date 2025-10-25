using System;
using System.Collections.Generic;
using System.Linq;
using Core.AI.BehaviourTrees.Internals;
using Core.Utilities.Logging;

namespace Core.AI.BehaviourTrees.Nodes
{
	/// <summary>
	/// A leaf node that executes an IAction strategy.
	/// </summary>
	public class Leaf(string name, IAction strategy, int priority = 0) : Node(name, priority)
	{
		private readonly IAction _strategy = strategy;

		public override NodeStatus Tick() => _strategy.Execute();
		public override void Reset() => _strategy.Reset();
	}

	/// <summary>
	/// Repeats a child node a fixed number of times (or indefinitely if repeatCount = -1).
	/// </summary>
	public class Repeat(int repeatCount = -1) : Node("Repeat")
	{
		protected override int MaxChildren => 1;
		private readonly int _repeatCount = repeatCount;
		private int _currentCount = 0;

		public override NodeStatus Tick()
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

	/// <summary>
	/// Ticks a child node but fails if it exceeds a duration.
	/// </summary>
	public class TimeoutNode(float duration) : Node("Timeout")
	{
		protected override int MaxChildren => 1;
		private readonly CountdownTimer _timer = new CountdownTimer(duration);

		public override NodeStatus Tick()
		{
			if (Children.Count == 0)
			{
				LoggerService.Warning($"TimeoutNode '{Name}' has no child.");
				return NodeStatus.Failure;
			}

			if (!_timer.IsRunning)
				_timer.Start();

			_timer.Tick(1); //change

			if (_timer.IsFinished)
			{
				_timer.Reset();
				return NodeStatus.Failure;
			}

			NodeStatus status = Children[0].Tick();

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

	/// <summary>
	/// Executes a child node only if a condition is true.
	/// </summary>
	public class ConditionalNode(Func<bool> condition) : Node("Conditional")
	{
		protected override int MaxChildren => 1;
		private readonly Func<bool> _condition = condition;

		public override NodeStatus Tick()
		{
			if (Children.Count == 0)
			{
				LoggerService.Warning($"ConditionalNode '{Name}' has no child.");
				return NodeStatus.Failure;
			}

			return _condition() ? Children[0].Tick() : NodeStatus.Failure;
		}
	}

	/// <summary>
	/// Executes all children in parallel, succeeds or fails depending on the policies.
	/// </summary>
	public enum ParallelPolicy { RequireAll, RequireOne }

	public class ParallelNode(ParallelPolicy successPolicy = ParallelPolicy.RequireAll,
							 ParallelPolicy failurePolicy = ParallelPolicy.RequireOne) : Node("Parallel")
	{
		private readonly ParallelPolicy _successPolicy = successPolicy;
		private readonly ParallelPolicy _failurePolicy = failurePolicy;

		public override NodeStatus Tick()
		{
			int successCount = 0, failureCount = 0;

			foreach (var child in Children)
			{
				NodeStatus status = child.Tick();
				if (status == NodeStatus.Success) successCount++;
				if (status == NodeStatus.Failure) failureCount++;
			}

			if ((_successPolicy == ParallelPolicy.RequireAll && successCount == Children.Count) ||
				(_successPolicy == ParallelPolicy.RequireOne && successCount > 0))
				return NodeStatus.Success;

			if ((_failurePolicy == ParallelPolicy.RequireAll && failureCount == Children.Count) ||
				(_failurePolicy == ParallelPolicy.RequireOne && failureCount > 0))
				return NodeStatus.Failure;

			return NodeStatus.Running;
		}
	}

	/// <summary>
	/// Picks one child to execute based on weights.
	/// </summary>
	public class WeightedSelectorNode : Node
	{
		private readonly List<(Node node, float weight)> _children = new List<(Node node, float weight)>();
		private readonly Random _random = new Random();

		public void AddWeightedChild(Node node, float weight)
		{
			_children.Add((node, weight));
			AddChild(node);
		}

		public override NodeStatus Tick()
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

		public override NodeStatus Tick()
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
