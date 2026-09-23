using Core.Settings;
using Godot;

namespace Core.UI;

/// <summary>
/// Applies the brightness option to the whole screen (GDD p.20).
/// A full-screen rect multiplies what has already been drawn, so it works in every level and
/// over the 3D view without touching each scene's environment.
/// </summary>
public partial class BrightnessOverlay : CanvasLayer
{
	private const string BrightnessParameter = "brightness";

	/// <summary>
	/// The rect carrying the brightness shader.
	/// </summary>
	[Export]
	public ColorRect? Screen { get; set; }

	public override void _Ready()
	{
		GameSettings.Changed += Apply;
		Apply();
	}

	public override void _ExitTree()
	{
		GameSettings.Changed -= Apply;
	}

	private void Apply()
	{
		if (Screen?.Material is not ShaderMaterial material) return;

		material.SetShaderParameter(BrightnessParameter, GameSettings.Brightness);
	}
}
