using Core.ECS;
using Core.ECS.Components;
using BT = Core.AI.BehaviourTrees.Internals;

public class IdleAction(Entity entity) : BT.IAction
{
	private readonly Entity _entity = entity;

	public BT.NodeStatus Execute(float deltaTime)
	{
		_entity.GetComponent<BTVisualizerComponent>()?.ShowState("Idle");
		return BT.NodeStatus.Running;
	}
}
