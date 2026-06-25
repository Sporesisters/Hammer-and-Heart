using Godot;
using Core.ECS;
using Core.ECS.Components;
using BT = Core.AI.BehaviourTrees.Internals;
using Core.Systems;
using Core.Utilities.Logging;
using Core.Inputs;

public class MoveToTargetAction(Entity entity) : BT.IAction
{
	private readonly Entity _entity = entity;

	public BT.NodeStatus Execute(float deltaTime)
	{
		Blackboard bb = _entity.Blackboard;

		if (!bb.ContainsData("TargetEntity"))
		{
			LoggerService.Warning($"[MoveToTargetAction] No 'TargetEntity' key found in blackboard for '{_entity.Name}'.");
			return BT.NodeStatus.Failure;
		}

		_entity.GetComponent<BTVisualizerComponent>()?.ShowState("Chasing my Prey");

		Entity? target = bb.GetData<Entity>("TargetEntity");
		if (target is null || target.IsQueuedForDeletion() || !target.IsInsideTree())
		{
			bb.RemoveData("TargetEntity");
			LoggerService.Warning($"Invalid or missing target in blackboard for entity '{_entity.Name}'.");
			return BT.NodeStatus.Failure;
		}

		var selfChar = _entity.GetComponent<CharacterComponent>()?.Character;
		var targetChar = target.GetComponent<CharacterComponent>()?.Character;
		var movement = _entity.GetComponent<MovementComponent>();

		if (selfChar is null || targetChar is null || movement is null)
			return BT.NodeStatus.Failure;

		// 3D direction and distance
		Vector3 direction3D = targetChar.GlobalPosition - selfChar.GlobalPosition;
		direction3D.Y = 0; // ignore vertical
		float distance = direction3D.Length();

		float stopDist = bb.GetData<float>("StoppingDistance");

		// Only stop moving input if already inside stopping distance
		if (distance <= stopDist)
		{
			movement.ReceiveInput(new InputCommand(Vector2.Zero, false, false)); // stop moving
			return BT.NodeStatus.Running; // keep sequence running
		}

		// Move towards target
		Vector2 moveDir = new Vector2(direction3D.X, direction3D.Z).Normalized();
		InputCommand command = new(moveDir, false, false);
		movement.ReceiveInput(command);

		return BT.NodeStatus.Running;
	}
}
