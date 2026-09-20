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
	/// Where the heart appears, relative to the monster's body.
	/// </summary>
	[Export]
	public Vector3 HeartOffset { get; set; } = new(0f, 1.6f, 0f);

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

	/// <summary>
	/// Raises this monster's calm. Calms it for good once the stat is full.
	/// </summary>
	/// <param name="amount">How much calm the hit adds.</param>
	/// <returns><c>true</c> if this call calmed the monster.</returns>
	public override void _EnterTree()
	{
		AddToGroup(MonsterGroup);
	}

	public bool AddCalm(float amount)
	{
		if (IsCalmed || CalmStat is not { } stat) return false;

		stat.CurrentStatValue += amount;
		LoggerService.Info($"[{EntityId}] Calm {stat.CurrentStatValue}/{stat.MaximumValue}.");

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
			Position = HeartOffset,
		};

		character.AddChild(heart);
	}
}
