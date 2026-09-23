using Core.ECS.Events;
using Core.Stats;
using Core.Stats.Types;
using Core.Systems;
using Core.UI;
using Core.Utilities.Logging;
using Godot;

namespace Core.ECS.Components;

/// <summary>
/// Makes a monster calmable instead of killable (GDD p.4).
/// Annabelle's kisses raise its Calm stat and, once full, the monster is calmed for good:
/// it stops attacking, shows a heart above its head and publishes a <see cref="MonsterCalmedEvent"/>.
/// </summary>
/// <remarks>
/// Add it to monster entities only. It creates the <see cref="StatType.Calm"/> stat on the
/// entity's <see cref="StatsComponent"/> the first time it is needed.
/// </remarks>
[GlobalClass]
public partial class CalmComponent : ComponentBase
{
	/// <summary>
	/// Group every calmable monster joins, so aim assist and other systems can find them.
	/// </summary>
	public const string MonsterGroup = "monsters";

	/// <summary>
	/// Calm needed to fully calm this monster.
	/// </summary>
	[Export]
	public float MaxCalm { get; set; } = 100f;

	/// <summary>
	/// Where the calm bar and the heart appear, relative to the monster's body.
	/// </summary>
	[Export]
	public Vector3 IndicatorOffset { get; set; } = new(0f, 1.6f, 0f);

	/// <summary>
	/// Width of the calm bar in metres.
	/// </summary>
	[Export]
	public float CalmBarWidth { get; set; } = 0.9f;

	/// <summary>Colour the calm bar fills up with.</summary>
	[Export]
	public Color CalmColor { get; set; } = new(1f, 0.55f, 0.75f);

	/// <summary>Colour of the trail that follows the fill after each kiss.</summary>
	[Export]
	public Color CalmTrailColor { get; set; } = new(0.6f, 0.3f, 0.45f);

	/// <summary>
	/// Whether this monster has already been calmed. Once calmed it never turns hostile again.
	/// </summary>
	public bool IsCalmed { get; private set; }

	/// <summary>
	/// Current calm between 0 and 1, for bars and other feedback.
	/// </summary>
	public float CalmRatio => CalmStat is { } stat && stat.MaximumValue > 0f
		? stat.CurrentStatValue / stat.MaximumValue
		: 0f;

	private Stat? CalmStat
	{
		get
		{
			var stats = Entity?.GetComponent<StatsComponent>();
			if (stats is null) return null;

			if (!stats.HasStat(StatType.Calm))
				stats.AddStats(new() { [StatType.Calm] = new BoundedStat(0f, 0f, MaxCalm) });

			return stats.GetStat(StatType.Calm);
		}
	}

	private WorldBar? _calmBar;

	public override void _EnterTree()
	{
		AddToGroup(MonsterGroup);
	}

	/// <summary>
	/// Raises this monster's calm. Calms it for good once the stat is full.
	/// </summary>
	/// <param name="amount">How much calm the hit adds.</param>
	/// <returns><c>true</c> if this call calmed the monster.</returns>
	public bool AddCalm(float amount)
	{
		if (IsCalmed || CalmStat is not { } stat) return false;

		stat.CurrentStatValue += amount;
		LoggerService.Info($"[{EntityId}] Calm {stat.CurrentStatValue}/{stat.MaximumValue}.");

		ShowCalmBar();

		if (stat.CurrentStatValue < stat.MaximumValue) return false;

		Calm();
		return true;
	}

	/// <summary>
	/// Calms the monster immediately, whatever its current calm is.
	/// </summary>
	public void Calm()
	{
		if (IsCalmed || Entity is null) return;

		IsCalmed = true;

		if (_calmBar is not null && IsInstanceValid(_calmBar)) _calmBar.FadeAndFree();
		ShowHeart();

		LoggerService.Info($"[{EntityId}] Monster calmed.");

		EventBus?.Publish(new MonsterCalmedEvent(Entity));
		GameCore.Instance?.EventBus?.Publish(new MonsterCalmedEvent(Entity));
	}

	/// <summary>
	/// Places a small pink heart above the monster's head.
	/// Placeholder feedback until the monsters have their own reactions.
	/// </summary>
	private void ShowHeart()
	{
		if (Entity?.GetComponent<CharacterComponent>()?.Character is not { } character) return;

		Label3D heart = new()
		{
			Text = "\u2665",
			Modulate = new Color(1f, 0.55f, 0.75f),
			FontSize = 96,
			PixelSize = 0.005f,
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			NoDepthTest = true,
			Position = IndicatorOffset,
		};

		character.AddChild(heart);

		// A little pop, so the moment a monster is won over reads without any animation on it yet.
		heart.Scale = Vector3.Zero;

		Tween pop = heart.CreateTween();
		pop.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		pop.TweenProperty(heart, "scale", Vector3.One * 1.15f, 0.35f);
		pop.TweenProperty(heart, "scale", Vector3.One, 0.15f);

		// Then it keeps breathing gently while the monster stays calm.
		Tween breathe = heart.CreateTween();
		breathe.SetLoops();
		breathe.TweenProperty(heart, "position", HeartRestPosition + new Vector3(0f, 0.12f, 0f), 1.1f)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		breathe.TweenProperty(heart, "position", HeartRestPosition, 1.1f)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
	}

	/// <summary>
	/// Where the heart floats once the monster is calmed.
	/// </summary>
	private Vector3 HeartRestPosition => IndicatorOffset;

	/// <summary>
	/// Shows how calm the monster is on a small bar above its head, filling up pink (GDD p.20).
	/// It only appears once the first kiss lands, so monsters nobody has kissed stay clean.
	/// </summary>
	private void ShowCalmBar()
	{
		if (_calmBar is null || !IsInstanceValid(_calmBar))
		{
			if (Entity?.GetComponent<CharacterComponent>()?.Character is not { } character) return;

			_calmBar = WorldBar.Attach(character, IndicatorOffset, CalmColor, CalmTrailColor, CalmBarWidth, 0f);
		}

		_calmBar.Visible = true;
		_calmBar.SetRatio(CalmRatio);
	}
}
