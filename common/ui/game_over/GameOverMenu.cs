using System;
using Godot;

namespace Core.UI;

/// <summary>Shows the game-over prompt and forwards the player's respawn request.</summary>
public partial class GameOverMenu : CanvasLayer
{
	[Export]
	private Control _backdrop = null!;

	[Export]
	private Control _fit = null!;

	[Export]
	private Control _card = null!;

	[Export]
	private Control _sisterMarks = null!;

	[Export]
	private Button _respawnButton = null!;

	public event Action? RespawnRequested;

	private Tween? _transition;
	private Tween? _buttonTween;
	private bool _respawning;

	public override void _Ready()
	{
		GetViewport().SizeChanged += FitCard;
		_fit.Resized += FitCard;
		_card.Resized += UpdatePivots;
		_respawnButton.Resized += UpdatePivots;
		_sisterMarks.Resized += UpdatePivots;
		_respawnButton.MouseEntered += () => AnimateButton(1.035f);
		_respawnButton.MouseExited += () => AnimateButton(1f);
		_respawnButton.ButtonDown += () => AnimateButton(0.95f);
		_respawnButton.ButtonUp += () => AnimateButton(_respawnButton.IsHovered() ? 1.035f : 1f);
		_respawnButton.Pressed += ConfirmRespawn;
	}

	public override void _ExitTree() => GetViewport().SizeChanged -= FitCard;

	public void Open()
	{
		if (Visible) return;

		_transition?.Kill();
		_buttonTween?.Kill();
		_respawning = false;
		_respawnButton.Disabled = false;
		_respawnButton.Scale = Vector2.One;
		_backdrop.Modulate = Colors.Transparent;
		_card.Modulate = Colors.Transparent;
		_card.Scale = Vector2.One * 0.88f;
		_sisterMarks.Scale = new Vector2(0.3f, 1f);

		Show();
		FitCard();
		UpdatePivots();
		_respawnButton.GrabFocus();

		_transition = CreateTween().SetParallel();
		_transition.TweenProperty(_backdrop, "modulate", Colors.White, 0.3);
		_transition.TweenProperty(_card, "modulate", Colors.White, 0.23);
		_transition.TweenProperty(_card, "scale", Vector2.One, 0.45)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		_transition.TweenProperty(_sisterMarks, "scale", Vector2.One, 0.35)
			.SetDelay(0.1).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
	}

	private void FitCard()
	{
		_fit.PivotOffset = _fit.Size * 0.5f;
		Vector2 available = GetViewport().GetVisibleRect().Size - new Vector2(32f, 32f);
		Vector2 scale = available / _fit.Size.Max(Vector2.One);
		_fit.Scale = Vector2.One * Mathf.Clamp(Mathf.Min(scale.X, scale.Y), 0.1f, 1f);
	}

	private void UpdatePivots()
	{
		_card.PivotOffset = _card.Size * 0.5f;
		_sisterMarks.PivotOffset = _sisterMarks.Size * 0.5f;
		_respawnButton.PivotOffset = _respawnButton.Size * 0.5f;
	}

	private void AnimateButton(float scale)
	{
		if (_respawning || !Visible) return;

		_buttonTween?.Kill();
		_buttonTween = CreateTween();
		_buttonTween.TweenProperty(_respawnButton, "scale", Vector2.One * scale, 0.14)
			.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
	}

	private void ConfirmRespawn()
	{
		if (_respawning || !Visible) return;

		_respawning = true;
		_respawnButton.Disabled = true;
		_transition?.Kill();
		_buttonTween?.Kill();

		_transition = CreateTween().SetParallel();
		_transition.TweenProperty(_respawnButton, "scale", Vector2.One * 1.06f, 0.16)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		_transition.TweenProperty(_card, "scale", Vector2.One * 0.95f, 0.2)
			.SetDelay(0.08).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
		_transition.TweenProperty(_card, "modulate", Colors.Transparent, 0.2).SetDelay(0.08);
		_transition.TweenProperty(_backdrop, "modulate", Colors.Transparent, 0.22).SetDelay(0.09);
		_transition.Chain().TweenCallback(Callable.From(() => RespawnRequested?.Invoke()));
	}
}
