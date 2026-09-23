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
	[Export]
	public float RotationSpeed { get; set; } = 10f;

	/// <summary>
	/// The direction of movement at the time of the last frame.
	/// </summary>
	private Vector2 _inputDirection = Vector2.Zero;

	/// <summary>
	/// The point the player is aiming at, or <c>null</c> when they are not aiming.
	/// Takes priority over the movement direction when deciding which way the character faces.
	/// </summary>
	private Vector3? _aimPoint;

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
			velocity = new Vector3(_inputDirection.X, 0, _inputDirection.Y).Normalized() * speed;

		Vector2 facing = AimDirectionFrom(characterBody.GlobalPosition) ?? _inputDirection;

		if (facing != Vector2.Zero)
		{
			float targetYaw = Mathf.Atan2(-facing.X, -facing.Y);
			Vector3 rotation = characterBody.Rotation;
			rotation.Y = Mathf.LerpAngle(rotation.Y, targetYaw, RotationSpeed * (float)delta);
			characterBody.Rotation = rotation;
		}

		if (gravityComponent is not null)
			velocity += gravityComponent.TotalGravity3D() * (float)delta;

		characterBody.Velocity = velocity;
		characterBody.MoveAndSlide();
	}

	/// <summary>
	/// Direction from a position to the current aim point, on the X/Z plane.
	/// </summary>
	/// <param name="from">The position aiming.</param>
	/// <returns>The direction, or <c>null</c> when there is nothing to aim at.</returns>
	private Vector2? AimDirectionFrom(Vector3 from)
	{
		if (_aimPoint is not { } point) return null;

		Vector3 toPoint = point - from;
		Vector2 flat = new(toPoint.X, toPoint.Z);

		return flat.Length() > 0.1f ? flat.Normalized() : null;
	}

	public void ReceiveInput(InputCommand command)
	{
		_inputDirection = command.MoveDirection;
		_aimPoint = command.AimPoint;
	}
}
