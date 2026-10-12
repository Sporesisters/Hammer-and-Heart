using Core.AI.BehaviourTrees.Nodes;
using Core.ECS;
using Core.ECS.Components;
using Core.Inputs;
using Core.Stats;
using Core.Systems;
using Core.Timing;
using Core.Utilities.Logging;
using Godot;
using Godot.Collections;
using BTAction = Core.AI.BehaviourTrees.Internals.IAction;
using BTNodeStatus = Core.AI.BehaviourTrees.Internals.NodeStatus;

[GlobalClass]
public partial class RobotAi : Node
{
	private Entity? _robot;
	private BehaviourTree? _tree;
	private float _detectionRange;
	private float _attackRange;
	private Vector3 _spawnPosition;

	public override void _Process(double delta)
	{
		if (_robot is null || !IsInstanceValid(_robot) || _robot.IsQueuedForDeletion()) return;

		if (!TrySelectTarget(out _))
		{
			_tree?.Reset();
			ReturnToSpawn();
			return;
		}

		_tree?.Tick((float)delta);
	}

	public void BuildAi(Entity robot, Array<Entity> targets, float detectionRange = 12f, float attackRange = 1.5f, float attackCooldown = 1.2f)
	{
		_robot = robot;
		_detectionRange = Mathf.Max(attackRange, detectionRange);
		_attackRange = Mathf.Max(0f, attackRange);
		_spawnPosition = robot.GetComponent<CharacterComponent>()?.Character?.GlobalPosition ?? Vector3.Zero;
		robot.Blackboard.SetData("AllEntities", targets);
		robot.Blackboard.SetData("StoppingDistance", _attackRange);

		var attackCooldownNode = new Cooldown("AttackCooldown", cooldownTime: attackCooldown);
		attackCooldownNode.AddChild(new Leaf("AttackTarget", strategy: new AttackAction(robot)));

		var attackConditional = new Conditional("TargetInAttackRange", priority: 3, condition: HasTargetInAttackRange);
		attackConditional.AddChild(attackCooldownNode);

		var chaseConditional = new Conditional("TargetDetected", priority: 2, condition: HasDetectedTarget);
		chaseConditional.AddChild(new Leaf("ChaseTarget", strategy: new MoveToTargetAction(robot)));

		var root = new PrioritySelector("RobotBehavior");
		root.AddChild(attackConditional);
		root.AddChild(chaseConditional);
		root.AddChild(new Leaf("Idle", priority: 1, strategy: new IdleAction(robot)));

		_tree = new BehaviourTree("RobotBT");
		_tree.AddChild(root);
	}

	private void ReturnToSpawn()
	{
		if (_robot is null) return;

		CharacterBody3D? character = _robot.GetComponent<CharacterComponent>()?.Character;
		MovementComponent? movement = _robot.GetComponent<MovementComponent>();
		if (character is null || movement is null) return;

		Vector3 direction = _spawnPosition - character.GlobalPosition;
		direction.Y = 0f;
		if (direction.Length() <= 0.1f)
		{
			movement.ReceiveInput(InputCommand.Empty);
			return;
		}

		Vector2 moveDirection = new(direction.X, direction.Z);
		movement.ReceiveInput(new InputCommand(moveDirection.Normalized(), false, false));
	}

	private bool HasTargetInAttackRange()
	{
		return TrySelectTarget(out Entity? target)
			&& target is not null
			&& GetDistanceTo(target) <= _attackRange;
	}

	private bool HasDetectedTarget()
	{
		return TrySelectTarget(out _);
	}

	private bool TrySelectTarget(out Entity? selectedTarget)
	{
		selectedTarget = null;
		if (_robot is null || _robot.IsQueuedForDeletion()) return false;

		CharacterBody3D? robotCharacter = _robot.GetComponent<CharacterComponent>()?.Character;
		if (robotCharacter is null) return false;

		Array<Entity>? targets = _robot.Blackboard.GetData<Array<Entity>>("AllEntities");
		if (targets is null) return false;

		float closestDistance = _detectionRange;

		foreach (Entity target in targets)
		{
			if (target is null || !IsInstanceValid(target) || !target.IsInsideTree() || target.IsQueuedForDeletion())
				continue;

			CharacterBody3D? targetCharacter = target.GetComponent<CharacterComponent>()?.Character;
			Stat? health = target.GetComponent<StatsComponent>()?.GetStat(StatType.Health);
			if (targetCharacter is null || health is null || health.CurrentStatValue <= health.MinimumValue)
				continue;

			float distance = HorizontalDistance(robotCharacter.GlobalPosition, targetCharacter.GlobalPosition);
			if (distance >= closestDistance) continue;

			closestDistance = distance;
			selectedTarget = target;
		}

		if (selectedTarget is null)
		{
			_robot.Blackboard.RemoveData("TargetEntity");
			return false;
		}

		_robot.Blackboard.SetData("TargetEntity", selectedTarget);
		return true;
	}

	private float GetDistanceTo(Entity target)
	{
		CharacterBody3D? robotCharacter = _robot?.GetComponent<CharacterComponent>()?.Character;
		CharacterBody3D? targetCharacter = target.GetComponent<CharacterComponent>()?.Character;
		return robotCharacter is null || targetCharacter is null
			? float.PositiveInfinity
			: HorizontalDistance(robotCharacter.GlobalPosition, targetCharacter.GlobalPosition);
	}

	private static float HorizontalDistance(Vector3 from, Vector3 to)
	{
		Vector3 direction = to - from;
		direction.Y = 0f;
		return direction.Length();
	}

	private sealed class AttackAction(Entity robot) : BTAction
	{
		public BTNodeStatus Execute(float deltaTime)
		{
			if (!robot.Blackboard.TryGetData<Entity>("TargetEntity", out Entity? target)
				|| target is null || target.IsQueuedForDeletion())
				return BTNodeStatus.Failure;

			Stat? damage = robot.GetComponent<StatsComponent>()?.GetStat(StatType.Damage);
			Stat? health = target.GetComponent<StatsComponent>()?.GetStat(StatType.Health);
			CharacterBody3D? character = target.GetComponent<CharacterComponent>()?.Character;
			if (damage is null || health is null || character is null || health.CurrentStatValue <= health.MinimumValue)
				return BTNodeStatus.Failure;

			health.CurrentStatValue = Mathf.Max(health.MinimumValue, health.CurrentStatValue - damage.CurrentStatValue);
			ShowDamageNumber(character, damage.CurrentStatValue);
			LoggerService.Info($"[{robot.EntityIdentity.ShortId}] hit [{target.EntityIdentity.ShortId}]: health {health.CurrentStatValue}/{health.MaximumValue}.");
			return BTNodeStatus.Success;
		}

		private static void ShowDamageNumber(CharacterBody3D character, float damage)
		{
			Label3D label = new()
			{
				Text = $"-{Mathf.RoundToInt(damage)}",
				FontSize = 48,
				OutlineSize = 8,
				Modulate = new Color(1f, 0.25f, 0.2f),
				Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
				Position = Vector3.Up * 2.2f,
			};
			character.AddChild(label);

			Tween animation = label.CreateTween();
			animation.SetParallel();
			animation.TweenProperty(label, "position", label.Position + Vector3.Up * 0.8f, 0.55f);
			animation.TweenProperty(label, "modulate", new Color(1f, 0.25f, 0.2f, 0f), 0.55f);
			animation.Chain().TweenCallback(Callable.From(label.QueueFree));
		}
	}
}