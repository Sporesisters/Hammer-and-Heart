using Godot;
using Core.ECS;
using Core.ECS.Components;
using BT = Core.AI.BehaviourTrees.Internals;
using Core.Systems;
using Godot.Collections;

public class PickNextTargetAction(Entity entity) : BT.IAction
{
	private readonly Entity _entity = entity;

	public BT.NodeStatus Execute(float deltaTime)
	{
		_entity.GetComponent<BTVisualizerComponent>()?.ShowState("Picking Target");

		Blackboard blackBoard = _entity.Blackboard;
		var entities = blackBoard.GetData<Array<Entity>>("AllEntities");

		if (entities is null || entities.Count == 0)
			return BT.NodeStatus.Failure;

		int currentIndex = blackBoard.GetData<int>("CurrentTargetIndex");
		int nextIndex = (currentIndex + 1) % entities.Count;

		var nextTarget = entities[nextIndex];
		blackBoard.SetData("TargetEntity", nextTarget);
		blackBoard.SetData("CurrentTargetIndex", nextIndex);

		GD.Print($"[AI] New target selected: {nextTarget.EntityIdentity}");
		return BT.NodeStatus.Success;
	}
}
