using Core.ECS;
using Core.ECS.Components;
using Core.Stats;
using Godot;

namespace Core.Systems.FSM.States;

/// <summary>
/// A state for an AI-controlled character to follow a target entity.
/// The entity will move towards the target and stop at a specified distance.
/// </summary>
public class FollowState : State
{
	private readonly Entity _target;
	private const float StoppingDistance = 2.5f; // The distance at which to stop following.

	public FollowState(FSMComponent fsm, Entity target) : base(fsm)
	{
		_target = target;
	}

	public override void PhysicsProcess(double delta)
	{
		// Ensure both the follower and the target have valid character bodies.
		CharacterBody3D? selfBody = Entity.GetComponent<CharacterComponent>()?.Character;
		CharacterBody3D? targetBody = _target.GetComponent<CharacterComponent>()?.Character;
		if (selfBody is null || targetBody is null) return;

		// Calculate distance and direction to the target.
		Vector3 directionToTarget = selfBody.GlobalPosition.DirectionTo(targetBody.GlobalPosition);
		float distance = selfBody.GlobalPosition.DistanceTo(targetBody.GlobalPosition);

		// Stop moving if within the stopping distance.
		if (distance <= StoppingDistance)
		{
			selfBody.Velocity = Vector3.Zero;
			return;
		}

		// Get stats and gravity components to calculate final velocity.
		var statsComponent = Entity.GetComponent<StatsComponent>();
		var gravityComponent = Entity.GetComponent<GravityComponent>();
		if (statsComponent is null) return;

		float speed = statsComponent.GetStat(StatType.MoveSpeed)?.CurrentStatValue ?? 0f;
		Vector3 velocity = directionToTarget * speed;

		// Apply gravity consistently with the MovementComponent.
		if (gravityComponent is not null)
		{
			velocity += gravityComponent.TotalGravity3D() * (float)delta;
		}

		selfBody.Velocity = velocity;
		selfBody.MoveAndSlide();
	}
}