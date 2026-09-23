using Core.ECS;
using Core.ECS.Components;
using Core.Stats;
using Core.Systems;
using Godot;

namespace Core.UI;

/// <summary>
/// The player's health, drawn as a circle in the top-left corner (GDD p.20).
/// It follows the active girl, eases towards her health instead of snapping, leaves a trail
/// behind a hit so the damage reads, flashes when she is hurt and pulses when she is nearly out.
/// </summary>
/// <remarks>
/// Assign the <see cref="EntitySwapSystem"/> so the circle can follow character swaps.
/// </remarks>
[GlobalClass]
public partial class HealthCircle : Control
{
	/// <summary>
	/// The system that says which girl the player is controlling.
	/// </summary>
	[Export]
	public EntitySwapSystem? EntitySwapSystem { get; set; }

	/// <summary>
	/// Thickness of the ring in pixels.
	/// </summary>
	[ExportGroup("Look")]
	[Export]
	public float RingWidth { get; set; } = 11f;

	/// <summary>Ring colour at full health.</summary>
	[Export]
	public Color FullColor { get; set; } = new(0.45f, 0.92f, 0.58f);

	/// <summary>Ring colour as health runs out.</summary>
	[Export]
	public Color EmptyColor { get; set; } = new(0.96f, 0.32f, 0.32f);

	/// <summary>Colour of the trail that drains behind the ring after a hit.</summary>
	[Export]
	public Color TrailColor { get; set; } = new(1f, 0.84f, 0.38f, 0.85f);

	/// <summary>Colour of the empty part of the ring.</summary>
	[Export]
	public Color TrackColor { get; set; } = new(0.08f, 0.08f, 0.1f, 0.85f);

	/// <summary>How fast the ring catches up with the real health.</summary>
	[ExportGroup("Feel")]
	[Export]
	public float FillSpeed { get; set; } = 9f;

	/// <summary>How fast the trail catches up after a hit.</summary>
	[Export]
	public float TrailSpeed { get; set; } = 2f;

	/// <summary>Health below this fraction makes the circle pulse.</summary>
	[Export(PropertyHint.Range, "0,1,0.05")]
	public float LowHealthAt { get; set; } = 0.3f;

	private Stat? _health;
	private string _name = string.Empty;

	private float _target = 1f;
	private float _shown = 1f;
	private float _trail = 1f;
	private float _flash;
	private float _pulse;

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(104f, 104f);
		Size = CustomMinimumSize;

		if (EntitySwapSystem is null) return;

		EntitySwapSystem.ActiveEntityChanged += Follow;

		if (EntitySwapSystem.CurrentEntity is { } current) Follow(current);
	}

	public override void _ExitTree()
	{
		if (EntitySwapSystem is not null) EntitySwapSystem.ActiveEntityChanged -= Follow;

		Unsubscribe();
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		_shown = Mathf.Lerp(_shown, _target, 1f - Mathf.Exp(-FillSpeed * dt));

		// The trail only lags behind on the way down, so damage leaves a visible tail.
		_trail = _trail < _shown ? _shown : Mathf.Lerp(_trail, _target, 1f - Mathf.Exp(-TrailSpeed * dt));

		_flash = Mathf.Max(0f, _flash - (dt * 4f));
		_pulse += dt * 4f;

		QueueRedraw();
	}

	public override void _Draw()
	{
		Vector2 centre = Size * 0.5f;
		float lowness = _shown <= LowHealthAt && _shown > 0f ? 1f : 0f;
		float pulse = 1f + (lowness * Mathf.Sin(_pulse) * 0.04f);
		float radius = ((Mathf.Min(Size.X, Size.Y) - RingWidth) * 0.5f) * pulse;

		// Starts at the top and fills clockwise, like a clock face.
		const float start = -Mathf.Pi / 2f;
		const int segments = 64;

		DrawCircle(centre, radius, new Color(0f, 0f, 0f, 0.35f));
		DrawArc(centre, radius, 0f, Mathf.Tau, segments, TrackColor, RingWidth, true);

		if (_trail > 0.001f)
			DrawArc(centre, radius, start, start + (Mathf.Tau * _trail), segments, TrailColor, RingWidth, true);

		if (_shown > 0.001f)
		{
			Color ring = EmptyColor.Lerp(FullColor, _shown).Lerp(Colors.White, _flash);
			DrawArc(centre, radius, start, start + (Mathf.Tau * _shown), segments, ring, RingWidth, true);
		}

		DrawLabels(centre);
	}

	/// <summary>
	/// Draws the health number inside the ring and the girl's name under it.
	/// </summary>
	/// <param name="centre">Centre of the circle.</param>
	private void DrawLabels(Vector2 centre)
	{
		Font font = GetThemeDefaultFont();
		int value = Mathf.RoundToInt(_health?.CurrentStatValue ?? 0f);

		string amount = value.ToString();
		Vector2 amountSize = font.GetStringSize(amount, HorizontalAlignment.Center, -1f, 26);
		DrawString(font, centre + new Vector2(-amountSize.X * 0.5f, 9f), amount, HorizontalAlignment.Center, -1f, 26);

		if (_name.Length is 0) return;

		Vector2 nameSize = font.GetStringSize(_name, HorizontalAlignment.Center, -1f, 14);
		DrawString(font, new Vector2(centre.X - (nameSize.X * 0.5f), Size.Y + 16f), _name, HorizontalAlignment.Center, -1f, 14);
	}

	/// <summary>
	/// Points the circle at another entity's Health stat.
	/// </summary>
	/// <param name="entity">The entity now being controlled.</param>
	private void Follow(Entity entity)
	{
		Unsubscribe();

		_health = entity.GetComponent<StatsComponent>()?.GetStat(StatType.Health);
		_name = entity.EntityIdentity.SubType;

		if (_health is not null) _health.OnValueChanged += OnHealthChanged;

		// A swap is not damage, so the ring simply moves to the other girl's health.
		_target = Ratio;
		_shown = _target;
		_trail = _target;

		QueueRedraw();
	}

	/// <summary>
	/// Current health between 0 and 1, or 0 when there is nothing to show.
	/// </summary>
	private float Ratio => _health is { MaximumValue: > 0f } health
		? Mathf.Clamp(health.CurrentStatValue / health.MaximumValue, 0f, 1f)
		: 0f;

	private void Unsubscribe()
	{
		if (_health is not null) _health.OnValueChanged -= OnHealthChanged;

		_health = null;
	}

	private void OnHealthChanged(Stat _)
	{
		float previous = _target;
		_target = Ratio;

		if (_target < previous) _flash = 1f;
	}
}
