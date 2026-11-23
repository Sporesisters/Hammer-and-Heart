namespace Core.Inputs;

/// <summary>
/// Who the player asked Annabelle to cook a soup for this frame. While she is already cooking,
/// any soup button serves the soup early instead.
/// </summary>
public enum SoupTarget
{
	/// <summary>No soup requested.</summary>
	None,

	/// <summary>A big heal for Elaine.</summary>
	Elaine,

	/// <summary>A big heal for Annabelle herself.</summary>
	Annabelle,

	/// <summary>A smaller heal for both sisters.</summary>
	Both,
}
