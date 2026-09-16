namespace Core.Inputs;

/// <summary>
/// The control schemes being playtested (GDD p.6). Only one will stay in the final game.
/// </summary>
public enum ControlScheme
{
	/// <summary>
	/// The player controls one girl at a time and switches between them.
	/// The attack button uses the active girl's attack (hammer or kiss).
	/// </summary>
	Switching,

	/// <summary>
	/// Both girls act as one two-headed unit with Elaine in front. No switching:
	/// the attack button swings Elaine's hammer and the kiss button fires Annabelle's kiss.
	/// </summary>
	TwoHeadedUnit,
}
