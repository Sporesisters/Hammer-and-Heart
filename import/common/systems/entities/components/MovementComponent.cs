using Core.Inputs;
using Core.Stats;
using Godot;

namespace Core.ECS.Components;

/// <summary>
/// Handles movement for an entity by applying input and optional gravity.
/// Integrates with <see cref="CharacterComponent"/>, <see cref="StatsComponent"/>, and <see cref="GravityComponent"/>.
/// Implements <see cref="IInputReceiver"/> to receive movement commands.
/// </summary>
public class MovementComponent : ComponentBase, IInputReceiver
{
	/// <summary>
	/// The current movement direction received from input.
	/// </summary>
	private Vector2 _inputDirection = Vector2.Zero;

	/// <summary>
	/// Applies movement to the associated <see cref="CharacterBody2D"/>.
	/// Takes input direction, movement speed from stats, and gravity into account.
	/// </summary>
	/// <param name="delta">The frame delta time for smooth movement.</param>
	public void Move(double delta)
	{
		CharacterBody2D? characterBody = Entity?.GetComponent<CharacterComponent>()?.Character;

		if (characterBody is null) return;

		var statsComponent = Entity?.GetComponent<StatsComponent>();
		var gravityComponent = Entity?.GetComponent<GravityComponent>();

		if (statsComponent is null) return;

		float speed = statsComponent.GetStat(StatType.MoveSpeed)?.CurrentStatValue ?? 0f;

		Vector2 velocity = _inputDirection != Vector2.Zero
			? _inputDirection.Normalized() * speed
			: Vector2.Zero;

		if (gravityComponent is not null)
			velocity += gravityComponent.TotalGravity2D() * (float)delta;

		characterBody.Velocity = velocity;
		characterBody.MoveAndSlide();
	}

	/// <inheritdoc/>
	public void ReceiveInput(InputCommand command)
		=> _inputDirection = command.MoveDirection;
}
