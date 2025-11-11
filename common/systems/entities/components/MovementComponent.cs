using Core.Stats;
using Godot;

namespace Core.ECS.Components;

/// <summary>
/// Handles 3D movement for an entity.
/// Is driven by FSM states which provide a movement direction.
/// Applies move speed from <see cref="StatsComponent"/> and external forces from <see cref="GravityComponent"/>.
/// </summary>
[GlobalClass]
public partial class MovementComponent : ComponentBase
{
	/// <summary>
	/// The current direction of movement, typically set by an FSM state.
	/// </summary>
	private Vector2 _inputDirection = Vector2.Zero;

	public override void _PhysicsProcess(double delta)
	{
		CharacterBody3D? characterBody = Entity?.GetComponent<CharacterComponent>()?.Character;
		if (characterBody is null) return;

		var statsComponent = Entity?.GetComponent<StatsComponent>();
		var gravityComponent = Entity?.GetComponent<GravityComponent>();

		if (statsComponent is null) return;

		float speed = statsComponent.GetStat(StatType.MoveSpeed)?.CurrentStatValue ?? 0f;
		Vector3 velocity = Vector3.Zero;

		if (_inputDirection != Vector2.Zero)
		{
			velocity = new Vector3(_inputDirection.X, 0, _inputDirection.Y).Normalized() * speed;
		}

		if (gravityComponent is not null)
		{
			velocity += gravityComponent.TotalGravity3D() * (float)delta;
		}

		characterBody.Velocity = velocity;
		characterBody.MoveAndSlide();
	}

	/// <summary>
	/// Sets the intended movement direction for the component.
	/// This is called by FSM states to control the entity's movement.
	/// </summary>
	/// <param name="direction">The desired 2D movement direction.</param>
	public void Move(Vector2 direction)
	{
		_inputDirection = direction;
	}
}