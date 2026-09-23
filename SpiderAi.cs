using Godot;
using Core.ECS;
using Core.ECS.Components;
using Core.ECS.Events;
using Core.Inputs;
using Core.AI.BehaviourTrees.Nodes;
using Core.Systems;
using Godot.Collections;
using Core.Utilities.Logging;

public partial class SpiderAi : Node
{
	private BehaviourTree _btRoot = null!;
	private Entity? _owner;
	private bool _isCalmed;

	public override void _ExitTree()
	{
		// The entity outlives this node, so leaving the listener behind would call into a freed node.
		if (_owner is not null && IsInstanceValid(_owner))
			_owner.EventBus.RemoveListener<MonsterCalmedEvent>(OnCalmed);
	}

	public override void _Process(double delta)
	{
		if (_isCalmed || _owner is null || !IsInstanceValid(_owner) || _owner.IsQueuedForDeletion()) return;

		_btRoot?.Tick((float)delta);
	}

	public void BuildAi(Entity entity, Array<Entity> targets, float stoppingDistance, float targetSwitchTime)
	{
		if (_owner is not null)
		{
			LoggerService.Warning($"[{Name}] BuildAi called twice. Rebuilding the tree for <{entity.Name}>.");
			_owner.EventBus.RemoveListener<MonsterCalmedEvent>(OnCalmed);
		}

		_owner = entity;
		entity.EventBus.AddListener<MonsterCalmedEvent>(OnCalmed);
		Blackboard blackboard = entity.Blackboard;
		blackboard.SetData("AllEntities", targets);
		blackboard.SetData("StoppingDistance", stoppingDistance);

		// --- PICK TARGET ---
		var pickTargetLeaf = new Leaf(
			name: "PickNextTarget",
			strategy: new PickNextTargetAction(entity)
		);

		var pickTargetCooldown = new Cooldown(
			name: "TargetPickCooldown",
			cooldownTime: targetSwitchTime
		);

		pickTargetCooldown.AddChild(pickTargetLeaf);

		// --- CHASE TARGET ---
		var chaseConditional = new Conditional(
			name: "ChaseConditional",
			condition: () => ShouldChaseTarget(entity)
		);

		chaseConditional.AddChild(new Leaf(
			name: "MoveToTarget",
			strategy: new MoveToTargetAction(entity)
		));

		// --- IDLE FALLBACK ---
		var idleLeaf = new Leaf(
			name: "Idle",
			strategy: new IdleAction(entity)
		);

		// --- SEQUENCE: Pick target → Chase target ---
		var chaseSequence = new Sequence("ChaseSequence");
		chaseSequence.AddChild(pickTargetCooldown);
		chaseSequence.AddChild(chaseConditional);

		// --- SELECTOR: Try chase sequence, else idle ---
		var rootSelector = new Selector("RootSelector");
		rootSelector.AddChild(chaseSequence);
		rootSelector.AddChild(idleLeaf);

		// --- ROOT BEHAVIOR TREE ---
		_btRoot = new BehaviourTree("SpiderBT");
		_btRoot.AddChild(rootSelector);

		_btRoot.PrintTree();
		GD.Print("[AI] Spider behavior tree initialized successfully.");
	}

	/// <summary>
	/// Stops the AI for good once Annabelle has calmed this monster (GDD p.4).
	/// </summary>
	/// <param name="_">The event payload (unused).</param>
	private void OnCalmed(MonsterCalmedEvent _)
	{
		_isCalmed = true;

		_owner?.GetComponent<MovementComponent>()?.ReceiveInput(InputCommand.Empty);
		_owner?.GetComponent<BTVisualizerComponent>()?.ShowState("Calmed");

		LoggerService.Info($"[{_owner?.Name}] Calmed, AI stopped.");
	}

	private static bool ShouldChaseTarget(Entity entity)
	{
		var bb = entity.Blackboard;

		// Get target
		if (!bb.TryGetData<Entity>("TargetEntity", out var target) || target is null)
		{
			LoggerService.Debug($"[{entity.Name}] TargetEntity key missing or null in blackboard.");
			return false;
		}

		// Get CharacterComponents
		var selfChar = entity.GetComponent<CharacterComponent>()?.Character;
		var targetChar = target.GetComponent<CharacterComponent>()?.Character;

		if (selfChar is null || targetChar is null)
		{
			LoggerService.Debug(
				$"[{entity.Name}] CharacterComponent missing -> self: {(selfChar != null)}, target: {(targetChar != null)}"
			);
			return false;
		}

		// Horizontal distance only
		Vector3 dir = targetChar.GlobalPosition - selfChar.GlobalPosition;
		dir.Y = 0; // ignore vertical
		float horizontalDistance = dir.Length();

		float stoppingDistance = bb.GetData<float>("StoppingDistance");
		return horizontalDistance > stoppingDistance;
	}
}
