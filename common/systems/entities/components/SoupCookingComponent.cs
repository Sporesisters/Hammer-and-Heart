using System.Collections.Generic;
using System.Linq;
using Core.Inputs;
using Core.Stats;
using Core.UI;
using Core.Utilities.Logging;
using Godot;

namespace Core.ECS.Components;

/// <summary>
/// Annabelle's cooking: the player picks who the soup is for (Elaine, Annabelle or both, the last
/// healing less each) and she stirs it. The longer she stirs, the bigger it gets, from small to
/// medium to large, and a large soup serves itself. Pressing a soup button again serves it early,
/// so the player is never locked into a heal they no longer need; before it reaches small, that
/// just stops cooking.
/// A light hit while stirring sloshes some of it out, setting the stir back and shrinking the heal,
/// and a heavy hit spills the whole pot.
/// The request arrives through <see cref="InputCommand.SoupRequest"/>, so she only cooks when the
/// player is controlling her, or always in the two-headed unit where she is the follower.
/// </summary>
[GlobalClass]
public partial class SoupCookingComponent : ComponentBase, IInputReceiver
{
	/// <summary>
	/// Elaine, for soups meant for her. When left empty, the first sibling entity with a
	/// <see cref="Hammer"/> is used, so Annabelle still finds Elaine in scenes that forget to wire her up.
	/// </summary>
	[Export]
	public Entity? Elaine { get; set; }

	[Export]
	public float SmallHealAmount { get; set; } = 20f;

	[Export]
	public float MediumHealAmount { get; set; } = 40f;

	[Export]
	public float LargeHealAmount { get; set; } = 70f;

	/// <summary>Stirring time for the soup to reach small, the least that can be served.</summary>
	[Export]
	public float SmallCookTime { get; set; } = 2f;

	/// <summary>Stirring time for the soup to reach medium.</summary>
	[Export]
	public float MediumCookTime { get; set; } = 4f;

	/// <summary>Stirring time for the soup to reach large, when it serves itself.</summary>
	[Export]
	public float LargeCookTime { get; set; } = 7f;

	/// <summary>Share of the heal each sister gets from a soup cooked for both.</summary>
	[Export(PropertyHint.Range, "0,1,0.05")]
	public float BothHealShare { get; set; } = 0.6f;

	/// <summary>
	/// How long after a soup is served (or spilled) before Annabelle can start another one.
	/// Without it, small soups could be chained for endless healing.
	/// </summary>
	[Export]
	public float Cooldown { get; set; } = 5f;

	/// <summary>
	/// Damage from a single hit at or above which the whole pot spills. Lighter hits only slosh
	/// some of it out. The default sits between a robot punch (15) and Elaine's hammer (50).
	/// </summary>
	[Export]
	public float HeavyHitDamage { get; set; } = 30f;

	/// <summary>Share of the time to a large soup that a light hit undoes.</summary>
	[Export(PropertyHint.Range, "0,1,0.05")]
	public float LightHitSetback { get; set; } = 0.25f;

	/// <summary>Share of the remaining healing a light hit sloshes out of the pot.</summary>
	[Export(PropertyHint.Range, "0,1,0.05")]
	public float LightHitSpill { get; set; } = 0.25f;

	/// <summary>Where the cooking bar floats, above Annabelle's own health bar.</summary>
	[Export]
	public Vector3 BarOffset { get; set; } = new(0f, 1.75f, 0f);

	/// <summary>Whether Annabelle is stirring a soup right now.</summary>
	public bool IsCooking { get; private set; }

	private const float STATUS_DURATION = 1.5f;

	private static readonly Color TextOutline = new(0.22f, 0.06f, 0.16f);
	private static readonly Color RawColor = new(0.75f, 0.72f, 0.6f);
	private static readonly Color BarColor = new(1f, 0.6f, 0.82f);

	private enum Stage
	{
		Raw,
		Small,
		Medium,
		Large,
	}

	private SoupTarget _target;
	private Stage _stage;
	private float _stirred;
	private float _spillFactor;
	private float _cooldownRemaining;
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
			_stirred += (float)delta;
			UpdateCookingProgress();

			if (_stage is Stage.Large)
				FinishCooking();

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
		if (command.SoupRequest is SoupTarget.None) return;

		if (!IsCooking)
			StartCooking(command.SoupRequest);
		else if (_stage is Stage.Raw)
			StopEarly();
		else
			FinishCooking();
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
	/// How each size heals and looks. The colour and pot size tell them apart at a glance.
	/// </summary>
	/// <param name="stage">How far the soup has cooked.</param>
	/// <returns>Its full healing, name, colour and pot scale.</returns>
	private (float Heal, string Name, Color Color, float PotScale) Recipe(Stage stage) => stage switch
	{
		Stage.Raw => (0f, "COOKING...", RawColor, 0.8f),
		Stage.Small => (SmallHealAmount, "SMALL SOUP", new Color(1f, 0.8f, 0.35f), 0.8f),
		Stage.Medium => (MediumHealAmount, "MEDIUM SOUP", new Color(1f, 0.52f, 0.22f), 1f),
		_ => (LargeHealAmount, "LARGE SOUP", new Color(0.92f, 0.24f, 0.28f), 1.25f),
	};

	private Stage StageFor(float stirred)
	{
		if (stirred >= LargeCookTime) return Stage.Large;
		if (stirred >= MediumCookTime) return Stage.Medium;
		return stirred >= SmallCookTime ? Stage.Small : Stage.Raw;
	}

	/// <summary>
	/// Who a soup for this target heals, as Health stats paired with the bodies the bowls fly to.
	/// </summary>
	private List<(Stat Health, Node3D? Body, string Name)> Recipients(SoupTarget target)
	{
		List<(Stat, Node3D?, string)> recipients = [];

		if (target is SoupTarget.Elaine or SoupTarget.Both
			&& Elaine?.GetComponent<StatsComponent>()?.GetStat(StatType.Health) is { } elaineHealth)
			recipients.Add((elaineHealth, Elaine.GetComponent<CharacterComponent>()?.Character, "Elaine"));

		if (target is SoupTarget.Annabelle or SoupTarget.Both
			&& Entity?.GetComponent<StatsComponent>()?.GetStat(StatType.Health) is { } annabelleHealth)
			recipients.Add((annabelleHealth, Character, "Annabelle"));

		return recipients;
	}

	private static string TargetLabel(SoupTarget target) => target switch
	{
		SoupTarget.Elaine => "FOR ELAINE",
		SoupTarget.Annabelle => "FOR ANNABELLE",
		_ => "FOR BOTH",
	};

	private void StartCooking(SoupTarget target)
	{
		if (_cooldownRemaining > 0f)
		{
			ShowStatus($"POT STILL HOT  {Mathf.CeilToInt(_cooldownRemaining)}s", new Color(0.75f, 0.75f, 0.8f), 0.8f);
			return;
		}

		Stat? annabelleHealth = Entity?.GetComponent<StatsComponent>()?.GetStat(StatType.Health);
		int expected = target is SoupTarget.Both ? 2 : 1;
		if (annabelleHealth is null || Recipients(target).Count < expected)
		{
			LoggerService.Warning("[Annabelle] Cannot cook soup: a required Health stat is unavailable.");
			return;
		}

		_target = target;
		_stirred = 0f;
		_stage = Stage.Raw;
		_spillFactor = 1f;
		_annabelleHealth = annabelleHealth;
		_annabelleLastHealth = annabelleHealth.CurrentStatValue;
		annabelleHealth.OnValueChanged += OnAnnabelleHealthChanged;
		IsCooking = true;

		(_, _, Color color, float potScale) = Recipe(Stage.Raw);
		if (Character is { } character)
		{
			_pot = SoupPot.Spawn(character, color, potScale);
			_bar = WorldBar.Attach(character, BarOffset, BarColor, BarColor.Darkened(0.4f), 0.7f, 0f, WorldBarIcon.Heart);
			_bar.FillsUp = true;
		}

		ShowStatus($"SOUP {TargetLabel(target)}", BarColor);
		UpdateCookingProgress();
		LoggerService.Info($"[Annabelle] Started cooking a soup {TargetLabel(target).ToLowerInvariant()}.");
	}

	/// <summary>
	/// Serves the soup at whatever size it has reached, a large one when it served itself.
	/// </summary>
	private void FinishCooking()
	{
		(float heal, string name, _, _) = Recipe(_stage);
		float healEach = heal * _spillFactor * (_target is SoupTarget.Both ? BothHealShare : 1f);
		List<(Stat Health, Node3D? Body, string Name)> recipients = Recipients(_target);
		SoupPot? pot = _pot;
		StopCooking();

		ShowStatus($"{name} READY!", new Color(1f, 0.6f, 0.82f), STATUS_DURATION);

		// The healing lands with each bowl, so the player sees where it went.
		List<(Node3D Target, System.Action OnArrive)> bowls = [];
		foreach ((Stat health, Node3D? body, string who) in recipients)
		{
			if (pot is not null && body is not null)
				bowls.Add((body, () => Heal(health, healEach, who)));
			else
				Heal(health, healEach, who);
		}

		if (pot is null) return;

		if (bowls.Count > 0)
			pot.Serve(bowls);
		else
			pot.QueueFree();
	}

	/// <summary>
	/// The player changed their mind before the soup was ready: the pot is put away, nothing is
	/// served and no cooldown starts, since nothing was gained.
	/// </summary>
	private void StopEarly()
	{
		_pot?.PutAway();
		StopCooking();
		_cooldownRemaining = 0f;
		ShowStatus("STOPPED COOKING", new Color(0.75f, 0.75f, 0.8f), STATUS_DURATION);
		LoggerService.Info("[Annabelle] Stopped cooking before the soup was ready.");
	}

	/// <summary>
	/// Heals one sister, unless she is already down: soup does not revive.
	/// </summary>
	/// <param name="health">Her Health stat.</param>
	/// <param name="amount">How much to heal.</param>
	/// <param name="who">Her name, for the log.</param>
	private static void Heal(Stat health, float amount, string who)
	{
		if (health.CurrentStatValue <= health.MinimumValue) return;

		float previousHealth = health.CurrentStatValue;
		health.CurrentStatValue += amount;
		LoggerService.Info($"[Annabelle] Soup served. {who} healed for {health.CurrentStatValue - previousHealth}.");
	}

	private void CancelCooking()
	{
		_pot?.Spill();
		_pot = null;
		StopCooking();
		ShowStatus("SOUP SPILLED!", new Color(1f, 0.45f, 0.4f), STATUS_DURATION);
		LoggerService.Info("[Annabelle] Heavy hit while cooking. Soup was lost.");
	}

	/// <summary>
	/// Ends the current soup, served or spilled, and starts the cooldown.
	/// </summary>
	private void StopCooking()
	{
		IsCooking = false;
		_stirred = 0f;
		_stage = Stage.Raw;
		_cooldownRemaining = Cooldown;
		_pot = null;
		_bar?.FadeAndFree();
		_bar = null;
		StopWatchingAnnabelleHealth();
	}

	/// <summary>
	/// Moves the bar and the pot along, and when the soup grows to the next size, recolours and
	/// resizes the pot and calls out the new size.
	/// </summary>
	private void UpdateCookingProgress()
	{
		float progress = LargeCookTime > 0f ? Mathf.Clamp(_stirred / LargeCookTime, 0f, 1f) : 1f;
		_pot?.SetProgress(progress);
		_bar?.SetRatio(progress, flash: false);

		Stage stage = StageFor(_stirred);
		if (stage == _stage) return;

		bool grew = stage > _stage;
		_stage = stage;
		(_, string name, Color color, float potScale) = Recipe(stage);
		_pot?.SetLook(color, potScale);

		if (grew && stage is not Stage.Large)
		{
			_bar?.SetRatio(progress);
			ShowStatus(name, color);
		}
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
	/// A light hit sloshes some soup out: the stir goes back, which can shrink the soup a size,
	/// and what is left heals less.
	/// </summary>
	private void SloshSoup()
	{
		_stirred = Mathf.Max(0f, _stirred - (LargeCookTime * LightHitSetback));
		_spillFactor *= 1f - LightHitSpill;
		_pot?.Slosh();
		UpdateCookingProgress();
		ShowStatus("SOME SOUP SPILLED!", new Color(1f, 0.7f, 0.45f));
		LoggerService.Info($"[Annabelle] Light hit while cooking. Soup set back to {_stirred:0.0}s, healing x{_spillFactor:0.00}.");
	}

	/// <summary>
	/// Reacts to any drop in Annabelle's health, even if she was healed earlier in the stir: a heavy
	/// hit, or one that knocks her out, spills the whole pot, and a light one only some of it.
	/// </summary>
	/// <param name="health">Annabelle's Health stat.</param>
	private void OnAnnabelleHealthChanged(Stat health)
	{
		float damage = _annabelleLastHealth - health.CurrentStatValue;
		_annabelleLastHealth = health.CurrentStatValue;

		if (!IsCooking || damage <= 0f) return;

		if (damage >= HeavyHitDamage || health.CurrentStatValue <= health.MinimumValue)
			CancelCooking();
		else
			SloshSoup();
	}

	private void StopWatchingAnnabelleHealth()
	{
		if (_annabelleHealth is null) return;

		_annabelleHealth.OnValueChanged -= OnAnnabelleHealthChanged;
		_annabelleHealth = null;
	}
}
