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

	private Vector3 _velocity = Vector3.Zero;
	private bool _jumpTriggered = false;

	public override void _PhysicsProcess(double delta)
	{
		CharacterBody3D? characterBody = Entity?.GetComponent<CharacterComponent>()?.Character;
		if (characterBody is null) return;

		var statsComponent = Entity?.GetComponent<StatsComponent>();
		var gravityComponent = Entity?.GetComponent<GravityComponent>();

		if (statsComponent is null) return;

		float speed = statsComponent.GetStat(StatType.MoveSpeed)?.CurrentStatValue ?? 0f;

		if (_inputDirection != Vector2.Zero)
		{
			_velocity.X = _inputDirection.X * 10;
			_velocity.Z = _inputDirection.Y * 10;
		}
		else 
		{
			_velocity.X = 0;
			_velocity.Z = 0;
		}

		if(_jumpTriggered)
		{
			_velocity.Y = 5;
			_jumpTriggered = false;
		}
		
		_velocity.Y -= 8.0f * (float)delta;
		
		if (gravityComponent is not null)
		{
			//_velocity += gravityComponent.TotalGravity3D() * (float)delta;
		}
			
		characterBody.Velocity = _velocity;
		characterBody.MoveAndSlide();
	}

	public void ReceiveInput(InputCommand command)
	{
		_inputDirection = command.MoveDirection;
		_jumpTriggered = command.JumpTriggered;
	}
}
