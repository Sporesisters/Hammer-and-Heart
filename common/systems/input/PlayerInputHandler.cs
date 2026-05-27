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

	public override InputCommand CollectInput()
	{
		Vector2 moveDirection = Input.GetVector(MOVE_LEFT, MOVE_RIGHT, MOVE_UP, MOVE_DOWN);
		bool attackPressed = Input.IsActionPressed(ATTACK);
		bool swapCharacter = Input.IsActionJustPressed(SWAP_CHARACTER);

		return new InputCommand(moveDirection, attackPressed, swapCharacter);
	}
}
