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
	/// How far an axis has to move before it counts as the player picking up a gamepad.
	/// Sticks and triggers report small values while resting, which would otherwise switch
	/// aiming off for someone playing with mouse and keyboard.
	/// </summary>
	[Export]
	public float GamepadAxisDeadzone { get; set; } = 0.25f;

	/// <summary>
	/// Whether the last input came from a gamepad. On a gamepad the girls simply face where they
	/// walk, which is far easier than aiming with a stick; the mouse aims freely.
	/// </summary>
	private bool _usingGamepad;

	private Vector3? _cachedAimPoint;
	private ulong _cachedAimFrame = ulong.MaxValue;

	public override InputCommand CollectInput()
	{
		Vector2 moveDirection = Input.GetVector(MOVE_LEFT, MOVE_RIGHT, MOVE_UP, MOVE_DOWN);
		bool attackPressed = Input.IsActionPressed(ATTACK);
		bool attackJustPressed = Input.IsActionJustPressed(ATTACK);
		bool swapCharacter = Input.IsActionJustPressed(SWAP_CHARACTER);
		bool kissPressed = Input.IsActionPressed(KISS);

		return new InputCommand(moveDirection, attackPressed, swapCharacter, kissPressed, CollectAimPoint(), attackJustPressed);
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventJoypadButton
			|| @event is InputEventJoypadMotion motion && Mathf.Abs(motion.AxisValue) > GamepadAxisDeadzone)
		{
			_usingGamepad = true;
		}
		else if (@event is InputEventMouseMotion or InputEventMouseButton or InputEventKey)
		{
			_usingGamepad = false;
		}
	}

	/// <summary>
	/// The point on the ground the mouse is over. On a gamepad nobody aims, so characters keep
	/// facing where they walk. Cached per frame, because input is collected more than once.
	/// </summary>
	/// <returns>The aim point, or <c>null</c> when the player is not aiming.</returns>
	private Vector3? CollectAimPoint()
	{
		ulong frame = Engine.GetProcessFrames();
		if (frame == _cachedAimFrame) return _cachedAimPoint;

		_cachedAimFrame = frame;
		_cachedAimPoint = null;

		if (_usingGamepad) return null;
		if (InputTarget?.GetComponent<CharacterComponent>()?.Character is not { } character) return null;
		if (GetViewport()?.GetCamera3D() is not { } camera) return null;

		// The mouse aims at the point of the character's own ground plane it is pointing at,
		// so aiming stays correct whatever angle the camera zone is using.
		Vector2 mousePosition = GetViewport().GetMousePosition();
		Vector3 rayOrigin = camera.ProjectRayOrigin(mousePosition);
		Vector3 rayDirection = camera.ProjectRayNormal(mousePosition);
		Plane groundPlane = new(Vector3.Up, character.GlobalPosition.Y);

		if (groundPlane.IntersectsRay(rayOrigin, rayDirection) is not Vector3 groundPoint) return null;

		_cachedAimPoint = groundPoint;
		return _cachedAimPoint;
	}
}
