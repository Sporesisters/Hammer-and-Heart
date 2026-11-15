using System.Linq;
using Core.Inputs;
using Core.Stats;
using Core.Utilities.Logging;
using Godot;

namespace Core.ECS.Components;

/// <summary>
/// Annabelle's cooking: she stirs a soup for a while and, when it is ready, it heals Elaine.
/// Bigger soups heal more but take longer, and any damage Annabelle takes while stirring spills it.
/// The request arrives through <see cref="InputCommand.SoupRequest"/>, so she only cooks when the
/// player is controlling her, or always in the two-headed unit where she is the follower.
/// </summary>
[GlobalClass]
public partial class SoupCookingComponent : ComponentBase, IInputReceiver
{
	/// <summary>
	/// Who the soup heals. When left empty, the first sibling entity with a <see cref="Hammer"/>
	/// is used, so Annabelle still finds Elaine in scenes that forget to wire her up.
	/// </summary>
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

	/// <summary>
	/// How long after a soup is served (or spilled) before Annabelle can start another one.
	/// Without it, small soups could be chained for endless healing.
	/// </summary>
	[Export]
	public float Cooldown { get; set; } = 5f;

	/// <summary>Whether Annabelle is stirring a soup right now.</summary>
	public bool IsCooking { get; private set; }

	private const float STATUS_DURATION = 1.5f;

	private float _cookTimeRemaining;
	private float _cookTimeTotal;
	private float _cooldownRemaining;
	private float _healAmount;
	private float _statusTimeRemaining;
	private string _soupSizeLabel = string.Empty;
	private Stat? _annabelleHealth;
	private float _annabelleLastHealth;
	private Label3D? _statusLabel;

	public override void _Ready()
	{
		Elaine ??= FindElaine();

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
			PixelSize = 0.01f,
			Position = new Vector3(0f, 3.2f, 0f),
			Visible = false,
		};
		character.AddChild(_statusLabel);
	}

	public override void _Process(double delta)
	{
		if (IsCooking)
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

		_cooldownRemaining -= (float)delta;

		if (_statusTimeRemaining > 0f)
		{
			_statusTimeRemaining -= (float)delta;
			if (_statusTimeRemaining <= 0f && _statusLabel is not null)
				_statusLabel.Visible = false;
		}
	}

	public override void _ExitTree()
	{
		StopWatchingAnnabelleHealth();
		if (_statusLabel is not null && IsInstanceValid(_statusLabel))
			_statusLabel.QueueFree();
	}

	public void ReceiveInput(InputCommand command)
	{
		if (IsCooking) return;

		switch (command.SoupRequest)
		{
			case SoupSize.Small:
				StartCooking(SmallHealAmount, SmallCookTime, "SMALL");
				break;
			case SoupSize.Medium:
				StartCooking(MediumHealAmount, MediumCookTime, "MEDIUM");
				break;
			case SoupSize.Large:
				StartCooking(LargeHealAmount, LargeCookTime, "LARGE");
				break;
		}
	}

	/// <summary>
	/// The first other entity next to Annabelle that swings a hammer.
	/// </summary>
	/// <returns>Elaine, or <c>null</c> when she is not in the scene.</returns>
	private Entity? FindElaine()
	{
		return Entity?.GetParent()?.GetChildren()
			.OfType<Entity>()
			.FirstOrDefault(entity => entity != Entity && entity.HasComponent<Hammer>());
	}

	private void StartCooking(float healAmount, float cookTime, string sizeLabel)
	{
		if (_cooldownRemaining > 0f)
		{
			ShowStatus($"POT STILL HOT\n{Mathf.CeilToInt(_cooldownRemaining)}s", 0.8f);
			return;
		}

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
		_annabelleLastHealth = annabelleHealth.CurrentStatValue;
		annabelleHealth.OnValueChanged += OnAnnabelleHealthChanged;
		IsCooking = true;
		UpdateCookingStatus();
		LoggerService.Info($"[Annabelle] Started cooking soup ({_healAmount} healing, {_cookTimeRemaining}s).");
	}

	private void FinishCooking()
	{
		StopCooking();

		if (Elaine?.GetComponent<StatsComponent>()?.GetStat(StatType.Health) is not { } health)
		{
			LoggerService.Warning("[Annabelle] Soup finished, but Elaine's Health stat was unavailable.");
			ShowStatus("SOUP READY!", STATUS_DURATION);
			return;
		}

		// No reviving with soup: once Elaine is down she stays down until a proper death system decides.
		if (health.CurrentStatValue <= health.MinimumValue)
		{
			ShowStatus("TOO LATE...", STATUS_DURATION);
			return;
		}

		float previousHealth = health.CurrentStatValue;
		health.CurrentStatValue += _healAmount;
		int healedAmount = Mathf.RoundToInt(health.CurrentStatValue - previousHealth);
		ShowStatus(healedAmount > 0 ? $"SOUP READY!\n+{healedAmount} HP" : "SOUP READY!", STATUS_DURATION);
		LoggerService.Info($"[Annabelle] Soup finished. Elaine healed for {healedAmount}.");
	}

	private void CancelCooking()
	{
		StopCooking();
		ShowStatus("SOUP SPILLED!", STATUS_DURATION);
		LoggerService.Info("[Annabelle] Took damage while cooking. Soup was lost.");
	}

	/// <summary>
	/// Ends the current soup, served or spilled, and starts the cooldown.
	/// </summary>
	private void StopCooking()
	{
		IsCooking = false;
		_cookTimeRemaining = 0f;
		_cooldownRemaining = Cooldown;
		StopWatchingAnnabelleHealth();
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

	/// <summary>
	/// Spills the soup on any drop in Annabelle's health, even if she was healed earlier in the stir.
	/// </summary>
	/// <param name="health">Annabelle's Health stat.</param>
	private void OnAnnabelleHealthChanged(Stat health)
	{
		bool tookDamage = health.CurrentStatValue < _annabelleLastHealth;
		_annabelleLastHealth = health.CurrentStatValue;

		if (IsCooking && tookDamage)
			CancelCooking();
	}

	private void StopWatchingAnnabelleHealth()
	{
		if (_annabelleHealth is null) return;

		_annabelleHealth.OnValueChanged -= OnAnnabelleHealthChanged;
		_annabelleHealth = null;
	}
}
