using Core.Stats;
using Core.UI;
using Godot;

namespace Core.ECS.Components;

/// <summary>
/// Shows an entity's health on a bar above its head (GDD p.20): robot bars drain,
/// unlike monster calm bars, which fill up pink.
/// </summary>
/// <remarks>
/// The bar only appears once the entity is hit and hides again after a few quiet seconds, so
/// untouched enemies stay clean. It fades away when the entity is destroyed.
/// </remarks>
[GlobalClass]
public partial class HealthBarComponent : ComponentBase
{
	/// <summary>
	/// Where the bar sits, relative to the entity's body.
	/// </summary>
	[Export]
	public Vector3 BarOffset { get; set; } = new(0f, 1.5f, 0f);

	/// <summary>
	/// Width of the bar in metres.
	/// </summary>
	[Export]
	public float BarWidth { get; set; } = 0.9f;

	/// <summary>
	/// How long the bar stays on screen after the last hit, in seconds.
	/// </summary>
	[Export]
	public float HideDelay { get; set; } = 4f;

	/// <summary>
	/// Whether the bar is visible from the start instead of waiting for the first hit.
	/// </summary>
	[Export]
	public bool AlwaysVisible { get; set; }

	/// <summary>
	/// Colour of the filled part of the bar.
	/// </summary>
	[Export]
	public Color FullColor { get; set; } = new(0.95f, 0.35f, 0.35f);

	/// <summary>
	/// Colour of the trail that drains behind the bar after a hit, so the damage reads.
	/// </summary>
	[Export]
	public Color TrailColor { get; set; } = new(1f, 0.92f, 0.85f, 0.85f);

	/// <summary>
	/// Badge at the left of the bar. <see cref="WorldBarIcon.None"/> picks one from the entity:
	/// a gear for robots, and each girl's own emblem.
	/// </summary>
	[Export]
	public WorldBarIcon Icon { get; set; } = WorldBarIcon.None;

	/// <summary>
	/// Whether hits pop a floating damage number above the bar.
	/// </summary>
	[Export]
	public bool ShowDamageNumbers { get; set; } = true;

	private WorldBar? _bar;
	private Stat? _health;
	private float _hideCountdown;
	private float _lastValue;

	public override void _Ready()
	{
		_health = Entity?.GetComponent<StatsComponent>()?.GetStat(StatType.Health);

		if (_health is null)
		{
			// Stats are usually added a frame later by the level, so try again then.
			CallDeferred(MethodName.BindHealth);
			return;
		}

		BindHealth();
	}

	public override void _ExitTree()
	{
		if (_health is not null) _health.OnValueChanged -= OnHealthChanged;
	}

	public override void _Process(double delta)
	{
		if (_bar is null || AlwaysVisible || _hideCountdown <= 0f) return;

		_hideCountdown -= (float)delta;

		if (_hideCountdown <= 0f) _bar.Visible = false;
	}

	/// <summary>
	/// Hooks the bar up to the entity's Health stat, once that stat exists.
	/// </summary>
	private void BindHealth()
	{
		_health ??= Entity?.GetComponent<StatsComponent>()?.GetStat(StatType.Health);

		if (_health is null) return;

		_health.OnValueChanged += OnHealthChanged;
		_lastValue = _health.CurrentStatValue;

		if (AlwaysVisible) ShowBar();
	}

	private void OnHealthChanged(Stat health)
	{
		ShowBar();

		float ratio = health.MaximumValue > 0f ? health.CurrentStatValue / health.MaximumValue : 0f;
		_bar?.SetRatio(ratio);

		int change = Mathf.RoundToInt(health.CurrentStatValue - _lastValue);
		_lastValue = health.CurrentStatValue;

		if (ShowDamageNumbers && change < 0) _bar?.ShowNumber(change.ToString(), new Color(1f, 0.95f, 0.85f));
		else if (ShowDamageNumbers && change > 0) _bar?.ShowNumber($"+{change}", new Color(0.55f, 1f, 0.7f));

		if (ratio <= 0f) _bar?.FadeAndFree();
	}

	/// <summary>
	/// Creates the bar the first time it is needed and restarts its hide countdown.
	/// </summary>
	private void ShowBar()
	{
		if (_bar is null || !IsInstanceValid(_bar))
		{
			if (Entity?.GetComponent<CharacterComponent>()?.Character is not { } character) return;

			float ratio = _health is { MaximumValue: > 0f } health
				? health.CurrentStatValue / health.MaximumValue
				: 1f;

			_bar = WorldBar.Attach(character, BarOffset, FullColor, TrailColor, BarWidth, ratio, ResolveIcon(), IconColor());
		}

		_bar.Visible = true;
		_hideCountdown = HideDelay;
	}

	/// <summary>
	/// The badge to show: the exported one, or one that suits the entity.
	/// </summary>
	private WorldBarIcon ResolveIcon()
	{
		if (Icon is not WorldBarIcon.None || Entity is null) return Icon;
		if (Entity.IsRobot) return WorldBarIcon.Gear;

		return Entity.EntityIdentity.SubType switch
		{
			"Elaine" => WorldBarIcon.Hammer,
			"Annabelle" => WorldBarIcon.Heart,
			_ => WorldBarIcon.None,
		};
	}

	/// <summary>
	/// Robots get a steel gear; everyone else's badge matches their bar.
	/// </summary>
	private Color? IconColor() => Entity?.IsRobot is true ? new Color(0.72f, 0.82f, 0.92f) : null;
}
