using Core.Settings;
using Core.Utilities.Logging;
using Godot;

namespace Core.UI;

/// <summary>
/// In-level pause menu (GDD p.10, p.20). Escape or Start pauses the game and opens it,
/// with Resume, Options and Quit, and sliders for music, sound and brightness.
/// </summary>
/// <remarks>
/// Lives in the GameCore autoload, so every level has it. It runs while the tree is paused and
/// grabs focus when it opens, so it can be used with a gamepad as well as mouse and keyboard.
/// </remarks>
public partial class PauseMenu : CanvasLayer
{
	private const string PAUSE = "pause";

	[Export]
	public Control? MainPanel { get; set; }

	[Export]
	public Control? OptionsPanel { get; set; }

	[Export]
	public Button? ResumeButton { get; set; }

	[Export]
	public Button? OptionsButton { get; set; }

	[Export]
	public Button? QuitButton { get; set; }

	[Export]
	public Button? BackButton { get; set; }

	[Export]
	public Slider? MusicSlider { get; set; }

	[Export]
	public Slider? SfxSlider { get; set; }

	[Export]
	public Slider? BrightnessSlider { get; set; }

	/// <summary>
	/// Whether the menu is open and the game paused.
	/// </summary>
	public bool IsOpen => Visible;

	public override void _Ready()
	{
		Visible = false;

		if (ResumeButton is not null) ResumeButton.Pressed += Close;
		if (OptionsButton is not null) OptionsButton.Pressed += ShowOptions;
		if (BackButton is not null) BackButton.Pressed += ShowMain;
		if (QuitButton is not null) QuitButton.Pressed += Quit;

		if (MusicSlider is not null) MusicSlider.ValueChanged += value => GameSettings.SetMusicVolume((float)value);
		if (SfxSlider is not null) SfxSlider.ValueChanged += value => GameSettings.SetSfxVolume((float)value);

		if (BrightnessSlider is not null)
		{
			BrightnessSlider.MinValue = GameSettings.MinBrightness;
			BrightnessSlider.MaxValue = GameSettings.MaxBrightness;
			BrightnessSlider.ValueChanged += value => GameSettings.SetBrightness((float)value);
		}

		ReadSettingsIntoSliders();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed(PAUSE)) return;

		GetViewport().SetInputAsHandled();

		// While the options are open, pause backs out of them instead of closing the menu.
		if (IsOpen && OptionsPanel is { Visible: true })
		{
			ShowMain();
			return;
		}

		if (IsOpen) Close();
		else Open();
	}

	/// <summary>
	/// Pauses the game and shows the menu.
	/// </summary>
	public void Open()
	{
		ReadSettingsIntoSliders();
		ShowMain();

		Visible = true;
		GetTree().Paused = true;

		LoggerService.Info("Game paused.");
	}

	/// <summary>
	/// Hides the menu and lets the game run again.
	/// </summary>
	public void Close()
	{
		Visible = false;
		GetTree().Paused = false;

		LoggerService.Info("Game resumed.");
	}

	private void ShowMain()
	{
		if (MainPanel is not null) MainPanel.Visible = true;
		if (OptionsPanel is not null) OptionsPanel.Visible = false;

		ResumeButton?.GrabFocus();
	}

	private void ShowOptions()
	{
		if (MainPanel is not null) MainPanel.Visible = false;
		if (OptionsPanel is not null) OptionsPanel.Visible = true;

		MusicSlider?.GrabFocus();
	}

	private void Quit()
	{
		GetTree().Paused = false;
		GetTree().Quit();
	}

	/// <summary>
	/// Puts the saved options into the sliders without firing their change handlers back.
	/// </summary>
	private void ReadSettingsIntoSliders()
	{
		if (MusicSlider is not null) MusicSlider.SetValueNoSignal(GameSettings.MusicVolume);
		if (SfxSlider is not null) SfxSlider.SetValueNoSignal(GameSettings.SfxVolume);
		if (BrightnessSlider is not null) BrightnessSlider.SetValueNoSignal(GameSettings.Brightness);
	}
}
