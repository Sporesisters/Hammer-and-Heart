namespace Core.Inputs;

/// <summary>
/// Which soup the player asked Annabelle to cook this frame.
/// </summary>
public enum SoupSize
{
	/// <summary>No soup requested.</summary>
	None,

	/// <summary>Quick to cook, heals a little.</summary>
	Small,

	/// <summary>A middle ground between speed and healing.</summary>
	Medium,

	/// <summary>Slow to cook, heals a lot.</summary>
	Large,
}
