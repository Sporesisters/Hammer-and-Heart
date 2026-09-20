using Core.ECS.Events;
using Core.Stats;
using Core.Stats.Types;
using Core.Systems;
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

	private Node3D? _calmBar;
	private MeshInstance3D? _calmBarFill;

	public override void _EnterTree()
	{
		AddToGroup(MonsterGroup);
	}

	public override void _Process(double delta)
	{
		// The bar is built from flat quads, so it is turned to face the camera here instead of
		// billboarding each piece, which would break the way the fill is offset.
		if (_calmBar is null || !_calmBar.Visible) return;

		if (GetViewport()?.GetCamera3D() is { } camera)
			_calmBar.GlobalBasis = camera.GlobalBasis;
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

		if (_calmBar is not null) _calmBar.Visible = false;
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
	}

	/// <summary>
	/// Shows how calm the monster is on a small bar above its head, filling up pink (GDD p.20).
	/// It only appears once the first kiss lands and is built on demand, so monsters nobody has
	/// kissed stay clean. Placeholder until the proper enemy bars exist.
	/// </summary>
	private void ShowCalmBar()
	{
		if (_calmBar is null)
		{
			if (Entity?.GetComponent<CharacterComponent>()?.Character is not { } character) return;

			_calmBar = new Node3D { Position = IndicatorOffset };
			_calmBar.AddChild(NewBarQuad(new Color(0.1f, 0.1f, 0.12f, 0.85f), CalmBarWidth, 0.14f, -0.001f));

			_calmBarFill = NewBarQuad(new Color(1f, 0.55f, 0.75f), CalmBarWidth, 0.1f, 0f);
			_calmBar.AddChild(_calmBarFill);

			character.AddChild(_calmBar);
		}

		_calmBar.Visible = true;

		if (_calmBarFill is null) return;

		// Scaled from the left edge, so the bar fills up instead of growing from the middle.
		float ratio = Mathf.Clamp(CalmRatio, 0f, 1f);
		_calmBarFill.Scale = new Vector3(ratio, 1f, 1f);
		_calmBarFill.Position = new Vector3(-CalmBarWidth * (1f - ratio) * 0.5f, 0f, 0f);
	}

	/// <summary>
	/// One flat, unshaded piece of the calm bar.
	/// </summary>
	/// <param name="color">Colour of the piece.</param>
	/// <param name="width">Width in metres.</param>
	/// <param name="height">Height in metres.</param>
	/// <param name="depth">Local Z offset, to keep the fill in front of its background.</param>
	/// <returns>The mesh to add to the bar.</returns>
	private static MeshInstance3D NewBarQuad(Color color, float width, float height, float depth)
	{
		return new MeshInstance3D
		{
			Mesh = new QuadMesh { Size = new Vector2(width, height) },
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = color,
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = color.A < 1f ? BaseMaterial3D.TransparencyEnum.Alpha : BaseMaterial3D.TransparencyEnum.Disabled,
				NoDepthTest = true,
			},
			Position = new Vector3(0f, 0f, depth),
		};
	}
}
