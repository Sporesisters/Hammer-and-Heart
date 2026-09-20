using Godot;

namespace Core.Inputs;

/// <summary>
/// A lightweight, immutable snapshot of input at a given time.
/// This is passed to entities so their components can react.
/// </summary>
public readonly struct InputCommand(Vector2 moveDirection, bool attackPressed, bool swapCharacter, bool kissPressed = false, Vector2 aimDirection = default)
{
	/// <summary>The direction of movement at the time of the input snapshot.</summary>
	public Vector2 MoveDirection { get; } = moveDirection;

	/// <summary>Whether the player is currently pressing the attack button.</summary>
	public bool AttackPressed { get; } = attackPressed;

	/// <summary>Whether the player is currently swapping characters.</summary>
	public bool SwapCharacter { get; } = swapCharacter;

	/// <summary>Whether the player is currently pressing the kiss button (two-headed unit scheme).</summary>
	public bool KissPressed { get; } = kissPressed;

	/// <summary>
	/// Where the player is aiming on the X/Z plane (mouse or right stick), or zero when not aiming.
	/// Characters face this direction instead of their movement direction while it is set.
	/// </summary>
	public Vector2 AimDirection { get; } = aimDirection;

	/// <summary>Creates an empty input command.</summary>
	public static InputCommand Empty => new(Vector2.Zero, false, false);
}
