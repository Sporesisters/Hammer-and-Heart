using System;
using Godot;

namespace Core.UI;

/// <summary>Shows the game-over prompt and forwards the player's respawn request.</summary>
public partial class GameOverMenu : CanvasLayer
{
	[Export]
	private Button _respawnButton = null!;

	public event Action? RespawnRequested;

	public override void _Ready()
	{
		_respawnButton.Pressed += () => RespawnRequested?.Invoke();
	}

	public void Open()
	{
		Show();
		_respawnButton.GrabFocus();
	}
}
