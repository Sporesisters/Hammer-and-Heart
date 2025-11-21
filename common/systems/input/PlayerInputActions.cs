namespace Core.Inputs;

/// <summary>
/// Defines constant identifiers for the player's input actions.
/// These correspond to action names defined in Godot's InputMap.
/// Use these constants to reference input actions consistently across the codebase.
/// </summary>
public static class PlayerInputActions
{
    /// <summary>
    /// Action for moving the player character to the left.
    /// </summary>
    public const string MOVE_LEFT = "move_left";

    /// <summary>
    /// Action for moving the player character to the right.
    /// </summary>
    public const string MOVE_RIGHT = "move_right";

    /// <summary>
    /// Action for moving the player character upward.
    /// </summary>
    public const string MOVE_UP = "move_up";

    /// <summary>
    /// Action for moving the player character downward.
    /// </summary>
    public const string MOVE_DOWN = "move_down";

    /// <summary>
    /// Action to perform a basic attack.
    /// </summary>
    public const string ATTACK = "attack";

    /// <summary>
    /// Action to swap the current active character.
    /// </summary>
    public const string SWAP_CHARACTER = "swap_character";
}
