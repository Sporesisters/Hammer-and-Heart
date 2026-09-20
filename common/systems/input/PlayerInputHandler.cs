using Core.ECS.Components;
using Core.Inputs.Internals;
using Godot;

namespace Core.Inputs;

/// <summary>
/// Handles player-controlled input from keyboard and mouse.
/// Collects the current state of input actions defined in the Input Map
/// and produces an <see cref="InputCommand"/> snapshot for the ECS.
/// </summary>
[GlobalClass]
public partial class PlayerInputHandler : InputHandler
{
	private const string MOVE_LEFT = "move_left";
	private const string MOVE_RIGHT = "move_right";
	private const string MOVE_UP = "move_up";
	private const string MOVE_DOWN = "move_down";

	private const string ATTACK = "attack";
	private const string SWAP_CHARACTER = "swap_character";
	private const string KISS = "kiss";

	/// <summary>
	/// Whether the last input came from a gamepad. On a gamepad the girls simply face where they
	/// walk, which is far easier than aiming with a stick; the mouse aims freely.
	/// </summary>
	private bool _usingGamepad;

	public override InputCommand CollectInput()
	{
		Vector2 moveDirection = Input.GetVector(MOVE_LEFT, MOVE_RIGHT, MOVE_UP, MOVE_DOWN);
		bool attackPressed = Input.IsActionPressed(ATTACK);
		bool swapCharacter = Input.IsActionJustPressed(SWAP_CHARACTER);
		bool kissPressed = Input.IsActionPressed(KISS);

		return new InputCommand(moveDirection, attackPressed, swapCharacter, kissPressed, CollectAim());
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventJoypadButton or InputEventJoypadMotion) _usingGamepad = true;
		else if (@event is InputEventMouseMotion or InputEventMouseButton or InputEventKey) _usingGamepad = false;
	}

	/// <summary>
	/// Where the player is aiming, as a direction on the X/Z plane: the point on the ground the
	/// mouse is over. On a gamepad nobody aims, so characters keep facing where they walk.
	/// </summary>
	/// <returns>A normalised aim direction, or zero when the player is not aiming.</returns>
	private Vector2 CollectAim()
	{
		if (_usingGamepad) return Vector2.Zero;

		if (InputTarget?.GetComponent<CharacterComponent>()?.Character is not { } character) return Vector2.Zero;
		if (GetViewport()?.GetCamera3D() is not { } camera) return Vector2.Zero;

		// The mouse aims at the point of the character's own ground plane it is pointing at,
		// so aiming stays correct whatever angle the camera zone is using.
		Vector2 mousePosition = GetViewport().GetMousePosition();
		Vector3 rayOrigin = camera.ProjectRayOrigin(mousePosition);
		Vector3 rayDirection = camera.ProjectRayNormal(mousePosition);
		Plane groundPlane = new(Vector3.Up, character.GlobalPosition.Y);

		if (groundPlane.IntersectsRay(rayOrigin, rayDirection) is not Vector3 groundPoint) return Vector2.Zero;

		Vector3 toPoint = groundPoint - character.GlobalPosition;
		Vector2 aim = new(toPoint.X, toPoint.Z);

		return aim.Length() > 0.1f ? aim.Normalized() : Vector2.Zero;
	}
}
