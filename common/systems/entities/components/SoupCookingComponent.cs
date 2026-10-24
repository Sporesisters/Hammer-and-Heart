using Core.ECS;
using Core.Stats;
using Core.Utilities.Logging;
using Godot;

namespace Core.ECS.Components;

[GlobalClass]
public partial class SoupCookingComponent : ComponentBase
{
	[Export]
	public Entity? Elaine { get; set; }

	[Export]
	public float SmallHealAmount { get; set; } = 20f;

	[Export]
	public float MediumHealAmount { get; set; } = 40f;

	[Export]
	public float LargeHealAmount { get; set; } = 70f;

	[Export]
	public float SmallCookTime { get; set; } = 2f;

	[Export]
	public float MediumCookTime { get; set; } = 4f;

	[Export]
	public float LargeCookTime { get; set; } = 7f;

	private bool _isCooking;
	private float _cookTimeRemaining;
	private float _cookTimeTotal;
	private float _healAmount;
	private float _statusTimeRemaining;
	private string _soupSizeLabel = string.Empty;
	private Stat? _annabelleHealth;
	private float _annabelleHealthAtStart;
	private Label3D? _statusLabel;

	public override void _Ready()
	{
		if (Entity?.GetComponent<CharacterComponent>()?.Character is not { } character) return;

		_statusLabel = new Label3D
		{
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			FontSize = 64,
			HorizontalAlignment = HorizontalAlignment.Center,
			Modulate = new Color(1f, 0.82f, 0.94f),
			NoDepthTest = true,
			OutlineModulate = new Color(0.22f, 0.06f, 0.16f),
			OutlineSize = 12,
			PixelSize = 0.005f,
			Position = new Vector3(0f, 2.5f, 0f),
			Visible = false,
		};
		character.AddChild(_statusLabel);
	}

	public override void _Process(double delta)
	{
		if (_isCooking)
		{
			_cookTimeRemaining -= (float)delta;
			if (_cookTimeRemaining <= 0f)
			{
				FinishCooking();
				return;
			}

			UpdateCookingStatus();
			return;
		}

		if (_statusTimeRemaining > 0f)
		{
			_statusTimeRemaining -= (float)delta;
			if (_statusTimeRemaining <= 0f && _statusLabel is not null)
				_statusLabel.Visible = false;
		}

		if (Input.IsActionJustPressed("heal_small"))
			StartCooking(SmallHealAmount, SmallCookTime, "SMALL");
		else if (Input.IsActionJustPressed("heal_medium"))
			StartCooking(MediumHealAmount, MediumCookTime, "MEDIUM");
		else if (Input.IsActionJustPressed("heal_large"))
			StartCooking(LargeHealAmount, LargeCookTime, "LARGE");
	}

	private void FinishCooking()
	{
		_isCooking = false;
		StopWatchingAnnabelleHealth();
		if (Elaine?.GetComponent<StatsComponent>()?.GetStat(StatType.Health) is not { } health)
		{
			LoggerService.Warning("[Annabelle] Soup finished, but Elaine's Health stat was unavailable.");
			ShowStatus("SOUP READY!", 1.5f);
			return;
		}

		float previousHealth = health.CurrentStatValue;
		health.CurrentStatValue += _healAmount;
		int healedAmount = Mathf.RoundToInt(health.CurrentStatValue - previousHealth);
		ShowStatus(healedAmount > 0 ? $"SOUP READY!\n+{healedAmount} HP" : "SOUP READY!", 1.5f);
		LoggerService.Info($"[Annabelle] Soup finished. Elaine healed for {_healAmount}.");
	}

	public override void _ExitTree()
	{
		StopWatchingAnnabelleHealth();
		if (_statusLabel is not null && GodotObject.IsInstanceValid(_statusLabel))
			_statusLabel.QueueFree();
	}

	private void StartCooking(float healAmount, float cookTime, string sizeLabel)
	{
		Stat? annabelleHealth = Entity?.GetComponent<StatsComponent>()?.GetStat(StatType.Health);
		if (Elaine?.GetComponent<StatsComponent>()?.GetStat(StatType.Health) is null || annabelleHealth is null)
		{
			LoggerService.Warning("[Annabelle] Cannot cook soup: a required Health stat is unavailable.");
			return;
		}

		_healAmount = Mathf.Max(0f, healAmount);
		_cookTimeRemaining = Mathf.Max(0f, cookTime);
		_cookTimeTotal = _cookTimeRemaining;
		_soupSizeLabel = sizeLabel;
		_annabelleHealth = annabelleHealth;
		_annabelleHealthAtStart = annabelleHealth.CurrentStatValue;
		annabelleHealth.OnValueChanged += OnAnnabelleHealthChanged;
		_isCooking = true;
		UpdateCookingStatus();
		LoggerService.Info($"[Annabelle] Started cooking soup ({_healAmount} healing, {_cookTimeRemaining}s).");
	}

	private void UpdateCookingStatus()
	{
		if (_statusLabel is null) return;

		float progress = _cookTimeTotal > 0f ? 1f - _cookTimeRemaining / _cookTimeTotal : 1f;
		int filledSegments = Mathf.Clamp(Mathf.FloorToInt(progress * 8f), 0, 8);
		string progressBar = new string('#', filledSegments) + new string('-', 8 - filledSegments);
		int secondsRemaining = Mathf.CeilToInt(_cookTimeRemaining);
		ShowStatus($"STIRRING {_soupSizeLabel}\n[{progressBar}] {secondsRemaining}s");
	}

	private void ShowStatus(string text, float duration = 0f)
	{
		if (_statusLabel is null) return;

		_statusLabel.Text = text;
		_statusLabel.Visible = true;
		_statusTimeRemaining = duration;
	}

	private void OnAnnabelleHealthChanged(Stat health)
	{
		if (_isCooking && health.CurrentStatValue < _annabelleHealthAtStart)
			CancelCooking();
	}

	private void CancelCooking()
	{
		_isCooking = false;
		_cookTimeRemaining = 0f;
		_healAmount = 0f;
		StopWatchingAnnabelleHealth();
		ShowStatus("SOUP SPILLED!", 1.5f);
		LoggerService.Info("[Annabelle] Took damage while cooking. Soup was lost.");
	}

	private void StopWatchingAnnabelleHealth()
	{
		if (_annabelleHealth is null) return;

		_annabelleHealth.OnValueChanged -= OnAnnabelleHealthChanged;
		_annabelleHealth = null;
	}
}