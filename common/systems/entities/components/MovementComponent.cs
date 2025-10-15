using Core.Inputs;
using Core.Stats;
using Godot;

namespace Core.ECS.Components;

/// <summary>
/// Handles 3D movement for an entity.
/// Reads input, applies move speed from <see cref="StatsComponent"/>,
/// adds dash from <see cref="DashComponent"/>, and external forces from <see cref="GravityComponent"/>.
/// </summary>
[GlobalClass]
public partial class MovementComponent : ComponentBase, IInputReceiver
{
	/// <summary>
	/// The direction of movement at the time of the last frame.
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

	public void ReceiveInput(InputCommand command)
	{
		_inputDirection = command.MoveDirection;
	}
}
