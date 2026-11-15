using System.Linq;
using Core.Inputs;
using Core.Stats;
using Core.UI;
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

	/// <summary>Where the cooking bar floats, above Annabelle's own health bar.</summary>
	[Export]
	public Vector3 BarOffset { get; set; } = new(0f, 1.75f, 0f);

	/// <summary>Whether Annabelle is stirring a soup right now.</summary>
	public bool IsCooking { get; private set; }

	private const float STATUS_DURATION = 1.5f;

	private static readonly Color TextOutline = new(0.22f, 0.06f, 0.16f);

	private float _cookTimeRemaining;
	private float _cookTimeTotal;
	private float _cooldownRemaining;
	private float _healAmount;
	private float _statusTimeRemaining;
	private Stat? _annabelleHealth;
	private float _annabelleLastHealth;
	private Label3D? _statusLabel;
	private SoupPot? _pot;
	private WorldBar? _bar;

	public override void _Ready()
	{
		Elaine ??= FindElaine();

		if (Character is not { } character) return;

		_statusLabel = new Label3D
		{
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			FontSize = 64,
			HorizontalAlignment = HorizontalAlignment.Center,
			NoDepthTest = true,
			OutlineModulate = TextOutline,
			OutlineSize = 14,
			PixelSize = 0.005f,
			Position = BarOffset,
			RenderPriority = 3,
			OutlineRenderPriority = 2,
			Visible = false,
		};
		character.AddChild(_statusLabel);
	}

	public override void _Process(double delta)
	{
		PlaceStatusLabel();

		if (IsCooking)
		{
			_cookTimeRemaining -= (float)delta;
			if (_cookTimeRemaining <= 0f)
			{
				FinishCooking();
				return;
			}

			UpdateCookingProgress();
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
		if (IsCooking || command.SoupRequest is SoupSize.None) return;

		StartCooking(command.SoupRequest);
	}

	private CharacterBody3D? Character => Entity?.GetComponent<CharacterComponent>()?.Character;

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

	/// <summary>
	/// How each soup heals, cooks and looks. The colour and pot size tell them apart at a glance.
	/// </summary>
	/// <param name="size">The soup asked for.</param>
	/// <returns>Its healing, cooking time, name, colour and pot scale.</returns>
	private (float Heal, float Time, string Name, Color Color, float PotScale) Recipe(SoupSize size) => size switch
	{
		SoupSize.Small => (SmallHealAmount, SmallCookTime, "SMALL SOUP", new Color(1f, 0.8f, 0.35f), 0.8f),
		SoupSize.Medium => (MediumHealAmount, MediumCookTime, "MEDIUM SOUP", new Color(1f, 0.52f, 0.22f), 1f),
		_ => (LargeHealAmount, LargeCookTime, "LARGE SOUP", new Color(0.92f, 0.24f, 0.28f), 1.25f),
	};

	private void StartCooking(SoupSize size)
	{
		if (_cooldownRemaining > 0f)
		{
			ShowStatus($"POT STILL HOT  {Mathf.CeilToInt(_cooldownRemaining)}s", new Color(0.75f, 0.75f, 0.8f), 0.8f);
			return;
		}

		Stat? annabelleHealth = Entity?.GetComponent<StatsComponent>()?.GetStat(StatType.Health);
		if (Elaine?.GetComponent<StatsComponent>()?.GetStat(StatType.Health) is null || annabelleHealth is null)
		{
			LoggerService.Warning("[Annabelle] Cannot cook soup: a required Health stat is unavailable.");
			return;
		}

		(float heal, float time, string name, Color color, float potScale) = Recipe(size);

		_healAmount = Mathf.Max(0f, heal);
		_cookTimeRemaining = Mathf.Max(0f, time);
		_cookTimeTotal = _cookTimeRemaining;
		_annabelleHealth = annabelleHealth;
		_annabelleLastHealth = annabelleHealth.CurrentStatValue;
		annabelleHealth.OnValueChanged += OnAnnabelleHealthChanged;
		IsCooking = true;

		if (Character is { } character)
		{
			_pot = SoupPot.Spawn(character, color, potScale);
			_bar = WorldBar.Attach(character, BarOffset, color, color.Darkened(0.4f), 0.7f, 0f, WorldBarIcon.Heart);
			_bar.FillsUp = true;
		}

		ShowStatus(name, color);
		UpdateCookingProgress();
		LoggerService.Info($"[Annabelle] Started cooking soup ({_healAmount} healing, {_cookTimeRemaining}s).");
	}

	private void FinishCooking()
	{
		float healAmount = _healAmount;
		SoupPot? pot = _pot;
		StopCooking();

		Stat? health = Elaine?.GetComponent<StatsComponent>()?.GetStat(StatType.Health);
		ShowStatus("SOUP READY!", new Color(1f, 0.6f, 0.82f), STATUS_DURATION);

		// The healing lands with the bowl, so the player sees where it went.
		if (pot is not null && Elaine?.GetComponent<CharacterComponent>()?.Character is { } elaine)
			pot.Serve(elaine, () => Heal(health, healAmount));
		else
		{
			pot?.QueueFree();
			Heal(health, healAmount);
		}
	}

	/// <summary>
	/// Heals Elaine, unless she is already down: soup does not revive.
	/// </summary>
	/// <param name="health">Elaine's Health stat.</param>
	/// <param name="amount">How much to heal.</param>
	private static void Heal(Stat? health, float amount)
	{
		if (health is null)
		{
			LoggerService.Warning("[Annabelle] Soup finished, but Elaine's Health stat was unavailable.");
			return;
		}

		if (health.CurrentStatValue <= health.MinimumValue) return;

		float previousHealth = health.CurrentStatValue;
		health.CurrentStatValue += amount;
		LoggerService.Info($"[Annabelle] Soup finished. Elaine healed for {health.CurrentStatValue - previousHealth}.");
	}

	private void CancelCooking()
	{
		_pot?.Spill();
		_pot = null;
		StopCooking();
		ShowStatus("SOUP SPILLED!", new Color(1f, 0.45f, 0.4f), STATUS_DURATION);
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
		_pot = null;
		_bar?.FadeAndFree();
		_bar = null;
		StopWatchingAnnabelleHealth();
	}

	private void UpdateCookingProgress()
	{
		float progress = _cookTimeTotal > 0f ? 1f - _cookTimeRemaining / _cookTimeTotal : 1f;
		_pot?.SetProgress(progress);
		_bar?.SetRatio(progress, flash: false);
	}

	/// <summary>
	/// Keeps the status text just above the cooking bar and the same size on screen, like the bar
	/// itself, so the two never overlap however far the camera pulls back.
	/// </summary>
	private void PlaceStatusLabel()
	{
		if (_statusLabel is not { Visible: true } label || Character is not { } character) return;
		if (GetViewport()?.GetCamera3D() is not { } camera) return;

		Vector3 anchor = character.GlobalPosition + BarOffset;
		float screen = WorldBar.WorldPerScreen(camera, anchor);
		label.GlobalPosition = anchor + (camera.GlobalBasis.Orthonormalized().Y * screen * 0.05f);
		_labelSize = 0.03f * screen / (label.FontSize * label.PixelSize);
		label.Scale = Vector3.One * _labelSize * _labelPop;
	}

	private float _labelSize = 1f;
	private float _labelPop = 1f;

	private void ShowStatus(string text, Color color, float duration = 0f)
	{
		if (_statusLabel is null) return;

		_statusLabel.Text = text;
		_statusLabel.Modulate = color;
		_statusLabel.Visible = true;
		_statusTimeRemaining = duration;

		// A small pop, so a change of state catches the eye.
		_labelPop = 1.35f;
		CreateTween().TweenMethod(Callable.From<float>(pop => _labelPop = pop), 1.35f, 1f, 0.2f)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		PlaceStatusLabel();
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
