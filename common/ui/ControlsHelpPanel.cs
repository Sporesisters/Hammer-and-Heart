using Godot;
using Core.Inputs;
using Core.Systems;

namespace Core.UI;

/// <summary>
/// Development overlay listing the controls available right now, updated when the
/// <see cref="ControlScheme"/> changes. Press F2 (<c>toggle_controls_help</c>) to hide or show it.
/// </summary>
/// <remarks>
/// Keep the text in sync when new controls are added, and remove the panel once the final UI exists.
/// </remarks>
[GlobalClass]
public partial class ControlsHelpPanel : PanelContainer
{
	/// <summary>
	/// The system whose control scheme decides which controls are shown.
	/// </summary>
	[Export]
	public EntitySwapSystem? EntitySwapSystem { get; set; }

	private const string TOGGLE_CONTROLS_HELP = "toggle_controls_help";

	private readonly Label _label = new();

	public override void _Ready()
	{
		AddThemeStyleboxOverride("panel", new StyleBoxFlat
		{
			BgColor = new Color(0f, 0f, 0f, 0.6f),
			ContentMarginLeft = 14f,
			ContentMarginRight = 14f,
			ContentMarginTop = 10f,
			ContentMarginBottom = 10f,
			CornerRadiusTopLeft = 6,
			CornerRadiusTopRight = 6,
			CornerRadiusBottomLeft = 6,
			CornerRadiusBottomRight = 6,
		});

		_label.AddThemeFontSizeOverride("font_size", 16);
		AddChild(_label);

		if (EntitySwapSystem is not null)
			EntitySwapSystem.ControlSchemeChanged += UpdateText;

		UpdateText(EntitySwapSystem?.ControlScheme ?? ControlScheme.Switching);
	}

	public override void _ExitTree()
	{
		if (EntitySwapSystem is not null)
			EntitySwapSystem.ControlSchemeChanged -= UpdateText;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed(TOGGLE_CONTROLS_HELP)) return;

		Visible = !Visible;
		GetViewport().SetInputAsHandled();
	}

	private void UpdateText(ControlScheme scheme)
	{
		string schemeControls = scheme switch
		{
			ControlScheme.TwoHeadedUnit =>
				"Scheme: Two-headed unit\n" +
				"  Both girls act as one: Elaine leads, Annabelle follows.\n" +
				"  Each girl has her own attack button, no switching.\n" +
				"Left click / Cross - Elaine's hammer\n" +
				"Right click / Square - Annabelle's kiss",
			_ =>
				"Scheme: Switching\n" +
				"  You control one girl at a time, the other follows you.\n" +
				"  Switching makes them swap places.\n" +
				"Tab / Circle - Switch girl\n" +
				"Left click / Cross - Attack with active girl (hammer or kiss)",
		};

		_label.Text =
			"CONTROLS   (keyboard / gamepad)\n" +
			"WASD / left stick - Move\n" +
			"Mouse - Aim (on a gamepad you aim where you walk)\n" +
			schemeControls + "\n" +
			"\n" +
			"F1 / Start - Change control scheme\n" +
			"F2 / Select - Hide/show this panel";

		ResetSize();
	}
}
