using Godot;

namespace Core.Inputs;

/// <summary>
/// Represents a snapshot of input for an entity at a given frame.
/// <para>
/// This struct is immutable and captures directional movement and action inputs.
/// </para>
/// </summary>
/// <param name="moveDirection">Directional movement input.</param>
/// <param name="attackPressed">Attack input state.</param>
/// <param name="parryPressed">Parry input state.</param>
/// <param name="swapWeaponPressed">Swap weapon input state.</param>
public readonly struct InputCommand(Vector2 moveDirection, bool attackPressed, bool parryPressed, bool swapWeaponPressed)
{
	/// <summary>
	/// The directional movement input (e.g., from keyboard or joystick).
	/// </summary>
	public Vector2 MoveDirection { get; } = moveDirection;

	/// <summary>
	/// Whether the attack action is currently pressed.
	/// </summary>
	public bool AttackPressed { get; } = attackPressed;

	/// <summary>
	/// Whether the parry action is currently pressed.
	/// </summary>
	public bool ParryPressed { get; } = parryPressed;

	/// <summary>
	/// Whether the swap weapon action was just pressed.
	/// </summary>
	public bool SwapWeaponPressed { get; } = swapWeaponPressed;

	/// <summary>
	/// Returns an empty input command where all actions are inactive and movement is zero.
	/// </summary>
	public static InputCommand Empty => new(
		moveDirection: Vector2.Zero,
		attackPressed: false,
		parryPressed: false,
		swapWeaponPressed: false
	);
}
