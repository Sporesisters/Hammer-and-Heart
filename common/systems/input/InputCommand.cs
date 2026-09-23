using Godot;

namespace Core.Inputs;

/// <summary>
/// A lightweight, immutable snapshot of input at a given time.
/// This is passed to entities so their components can react.
/// </summary>
public readonly struct InputCommand(Vector2 moveDirection, bool attackPressed, bool swapCharacter, bool kissPressed = false, Vector3? aimPoint = null)
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
	/// The world point the player is aiming at, or <c>null</c> when they are not aiming.
	/// Every receiver works out its own direction to it, so a follower standing somewhere else
	/// still aims at the same spot.
	/// </summary>
	public Vector3? AimPoint { get; } = aimPoint;

	/// <summary>Creates an empty input command.</summary>
	public static InputCommand Empty => new(Vector2.Zero, false, false);
}
