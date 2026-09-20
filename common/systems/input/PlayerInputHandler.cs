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

	private const string AIM_LEFT = "aim_left";
	private const string AIM_RIGHT = "aim_right";
	private const string AIM_UP = "aim_up";
	private const string AIM_DOWN = "aim_down";

	/// <summary>
	/// How far the right stick has to be pushed before it takes over from the mouse.
	/// </summary>
	[Export]
	public float StickAimDeadzone { get; set; } = 0.25f;

	public override InputCommand CollectInput()
	{
		Vector2 moveDirection = Input.GetVector(MOVE_LEFT, MOVE_RIGHT, MOVE_UP, MOVE_DOWN);
		bool attackPressed = Input.IsActionPressed(ATTACK);
		bool swapCharacter = Input.IsActionJustPressed(SWAP_CHARACTER);
		bool kissPressed = Input.IsActionPressed(KISS);

		return new InputCommand(moveDirection, attackPressed, swapCharacter, kissPressed, CollectAim());
	}

	/// <summary>
	/// Where the player is aiming, as a direction on the X/Z plane.
	/// The right stick wins when it is pushed; otherwise the mouse position on the ground is used.
	/// </summary>
	/// <returns>A normalised aim direction, or zero when the player is not aiming.</returns>
	private Vector2 CollectAim()
	{
		Vector2 stick = Input.GetVector(AIM_LEFT, AIM_RIGHT, AIM_UP, AIM_DOWN);

		if (stick.Length() >= StickAimDeadzone) return stick.Normalized();

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
