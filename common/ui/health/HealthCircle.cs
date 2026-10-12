using System.Collections.Generic;
using Core.ECS;
using Core.ECS.Components;
using Core.Inputs;
using Core.Stats;
using Core.Systems;
using Godot;

namespace Core.UI;

/// <summary>
/// The girls' health HUD in the top-left corner (GDD p.20).
/// The girl being controlled gets a big medallion with her emblem inside a segmented ring and a
/// name plate with her health; the other girl sits next to it as a small badge with the swap key.
/// Hits shake the medallion, flash the ring, leave a trail that drains after a short delay and pop
/// a damage number; heals preview in green before the ring fills up. When the active girl is nearly
/// out her medallion beats like a heart and the screen edges glow red. Swapping flips the medallions.
/// </summary>
/// <remarks>
/// Assign the <see cref="EntitySwapSystem"/>. The HUD scales with the window height, so it stays the
/// same size relative to the screen.
/// </remarks>
[GlobalClass]
public partial class HealthCircle : Control
{
	/// <summary>
	/// The system that says which girls there are and which one the player is controlling.
	/// </summary>
	[Export]
	public EntitySwapSystem? EntitySwapSystem { get; set; }

	/// <summary>
	/// Window height at which the HUD is drawn at its natural size.
	/// </summary>
	[ExportGroup("Layout")]
	[Export]
	public float ReferenceHeight { get; set; } = 720f;

	/// <summary>
	/// Smallest and largest scale the HUD can reach as the window changes size.
	/// </summary>
	[Export]
	public Vector2 ScaleLimits { get; set; } = new(0.75f, 3f);

	/// <summary>
	/// Distance from the top-left corner of the screen, at natural size.
	/// </summary>
	[Export]
	public Vector2 Margin { get; set; } = new(18f, 18f);

	/// <summary>
	/// Font for names and numbers. Leave empty to use a heavy system font.
	/// </summary>
	[ExportGroup("Look")]
	[Export]
	public Font? Font { get; set; }

	/// <summary>Colour the ring and screen edges turn when health runs low.</summary>
	[Export]
	public Color LowHealthColor { get; set; } = new(1f, 0.25f, 0.3f);

	/// <summary>Colour of healing previews and numbers.</summary>
	[Export]
	public Color HealColor { get; set; } = new(0.55f, 1f, 0.7f);

	/// <summary>Colour of the trail that drains behind the ring after a hit.</summary>
	[Export]
	public Color TrailColor { get; set; } = new(1f, 0.9f, 0.85f, 0.75f);

	/// <summary>Whether the screen edges glow red while the active girl is nearly out.</summary>
	[Export]
	public bool LowHealthVignette { get; set; } = true;

	/// <summary>How fast the ring catches up with the real health.</summary>
	[ExportGroup("Feel")]
	[Export]
	public float FillSpeed { get; set; } = 10f;

	/// <summary>Seconds the trail waits after a hit before it starts draining.</summary>
	[Export]
	public float TrailDelay { get; set; } = 0.45f;

	/// <summary>Fraction of the ring the trail drains per second.</summary>
	[Export]
	public float TrailSpeed { get; set; } = 1.4f;

	/// <summary>Health below this fraction makes the medallion beat.</summary>
	[Export(PropertyHint.Range, "0,1,0.05")]
	public float LowHealthAt { get; set; } = 0.3f;

	private const string SWAP_ACTION = "swap_character";
	private const float START_ANGLE = -Mathf.Pi / 2f;
	private const float FLIP_TIME = 0.32f;
	private const float POPUP_TIME = 0.9f;

	private static readonly Vector2 BaseSize = new(300f, 162f);
	private static readonly Vector2 MainCentre = new(60f, 60f);
	private static readonly Vector2 BadgeCentre = new(112f, 130f);
	private static readonly Color Ink = new(0.05f, 0.04f, 0.07f);

	private enum Emblem { Orb, Hammer, Heart }

	/// <summary>
	/// How one girl is drawn: her bright and deep accent colours, the dark of her backplate and her emblem.
	/// </summary>
	private readonly record struct Look(Color Accent, Color Glow, Color Deep, Emblem Emblem);

	/// <summary>
	/// A number floating away from a medallion after a hit or a heal.
	/// </summary>
	private sealed class Popup(string text, Color color, float drift)
	{
		public string Text { get; } = text;
		public Color Color { get; } = color;
		public float Drift { get; } = drift;
		public float Age { get; set; }
	}

	/// <summary>
	/// Everything animated for one girl.
	/// </summary>
	private sealed class Gauge(Entity entity)
	{
		public Entity Entity { get; } = entity;
		public Look Look { get; } = LookFor(entity.EntityIdentity.SubType);
		public string Name { get; } = entity.EntityIdentity.SubType.ToUpperInvariant();
		public System.Action<Stat>? Handler { get; set; }
		public Stat? Health { get; set; }
		public float Target { get; set; } = 1f;
		public float Shown { get; set; } = 1f;
		public float Trail { get; set; } = 1f;
		public float TrailHold { get; set; }
		public float Flash { get; set; }
		public float Heal { get; set; }
		public float Shake { get; set; }
		public float Squash { get; set; }
		public List<Popup> Popups { get; } = [];

		public float Maximum => Health?.MaximumValue ?? 0f;
		public bool Down => Health is not null && Target <= 0f;

		public float Ratio => Health is { MaximumValue: > 0f } health
			? Mathf.Clamp(health.CurrentStatValue / health.MaximumValue, 0f, 1f)
			: 0f;

		public bool Settled => Mathf.IsEqualApprox(Shown, Target) && Mathf.IsEqualApprox(Trail, Shown)
			&& Flash <= 0f && Heal <= 0f && Shake <= 0f && Squash <= 0f && Popups.Count is 0;
	}

	private readonly List<Gauge> _gauges = [];
	private readonly StyleBoxFlat _plate = new()
	{
		BgColor = new Color(Ink, 0.78f),
		CornerRadiusTopLeft = 16,
		CornerRadiusTopRight = 16,
		CornerRadiusBottomLeft = 16,
		CornerRadiusBottomRight = 16,
		BorderWidthBottom = 3,
		AntiAliasing = true,
	};
	private readonly StyleBoxFlat _chip = new()
	{
		BgColor = new Color(1f, 1f, 1f, 0.14f),
		BorderColor = new Color(1f, 1f, 1f, 0.35f),
		BorderWidthLeft = 1,
		BorderWidthRight = 1,
		BorderWidthTop = 1,
		BorderWidthBottom = 2,
		CornerRadiusTopLeft = 5,
		CornerRadiusTopRight = 5,
		CornerRadiusBottomLeft = 5,
		CornerRadiusBottomRight = 5,
		AntiAliasing = true,
	};

	private Font _font = null!;
	private string _swapKey = string.Empty;
	private Gauge? _active;
	private Gauge? _previous;
	private float _flip = 1f;
	private float _time;
	private float _hurt;

	public override void _Ready()
	{
		MouseFilter = MouseFilterEnum.Ignore;
		Size = BaseSize;

		_font = Font ?? new SystemFont
		{
			FontNames = ["Segoe UI Black", "Arial Rounded MT Bold", "Arial Black", "sans-serif"],
			FontWeight = 900,
			// Signed distance fields keep the text sharp however much the HUD is scaled up.
			MultichannelSignedDistanceField = true,
			MsdfPixelRange = 16,
		};
		_swapKey = SwapKeyLabel();

		GetViewport().SizeChanged += Relayout;
		Relayout();

		if (EntitySwapSystem is null) return;

		EntitySwapSystem.EntityRegistered += Track;
		EntitySwapSystem.ActiveEntityChanged += Follow;
		EntitySwapSystem.ControlSchemeChanged += OnControlSchemeChanged;

		foreach (Entity entity in EntitySwapSystem.ControllableEntities) Track(entity);

		if (EntitySwapSystem.CurrentEntity is { } current) Follow(current);
	}

	public override void _ExitTree()
	{
		GetViewport().SizeChanged -= Relayout;

		if (EntitySwapSystem is not null)
		{
			EntitySwapSystem.EntityRegistered -= Track;
			EntitySwapSystem.ActiveEntityChanged -= Follow;
			EntitySwapSystem.ControlSchemeChanged -= OnControlSchemeChanged;
		}

		foreach (Gauge gauge in _gauges)
			if (gauge.Health is not null) gauge.Health.OnValueChanged -= gauge.Handler;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		bool busy = _flip < 1f || _hurt > 0f;

		_time += dt;
		_flip = Mathf.Min(1f, _flip + (dt / FLIP_TIME));
		_hurt = Mathf.Max(0f, _hurt - (dt * 2.5f));

		foreach (Gauge gauge in _gauges)
		{
			if (gauge.Health is null) Resolve(gauge);

			busy |= !gauge.Settled || IsLow(gauge);
			Animate(gauge, dt);
		}

		// Nothing moves while health is steady, so the HUD only redraws when something changed.
		if (busy) QueueRedraw();
	}

	public override void _Draw()
	{
		Gauge? main = _flip < 0.5f ? _previous ?? _active : _active;
		if (main is null) return;

		// The medallions flip like a card: they close up, change girl and open again.
		float open = Mathf.Max(0.02f, Mathf.Abs(Mathf.Cos(_flip * Mathf.Pi)));

		if (_active is not null && LowHealthVignette) DrawVignette(_active);

		DrawPlate(main, open);
		DrawMedallion(MainCentre, 46f, 11f, main, open, true);
		DrawPopups(main, new Vector2(MainCentre.X + 230f, 74f), 22);

		int slot = 0;

		foreach (Gauge gauge in _gauges)
		{
			if (gauge == main) continue;

			Vector2 centre = BadgeCentre + new Vector2(slot * 128f, 0f);
			DrawBadge(centre, gauge, open, slot is 0);
			DrawPopups(gauge, centre + new Vector2(150f, 6f), 15);
			slot++;
		}
	}

	/// <summary>
	/// Keeps the HUD the same size relative to the window.
	/// </summary>
	private void Relayout()
	{
		float scale = Mathf.Clamp(GetViewportRect().Size.Y / ReferenceHeight, ScaleLimits.X, ScaleLimits.Y);

		Scale = new Vector2(scale, scale);
		Position = Margin * scale;
		Size = BaseSize;
		QueueRedraw();
	}

	/// <summary>
	/// Starts showing a girl's health, if she is not shown already.
	/// </summary>
	/// <param name="entity">A girl the player can control.</param>
	private void Track(Entity entity)
	{
		if (_gauges.Exists(gauge => gauge.Entity == entity)) return;

		Gauge gauge = new(entity);
		gauge.Handler = _ => OnHealthChanged(gauge);

		_gauges.Add(gauge);
		Resolve(gauge);
		QueueRedraw();
	}

	/// <summary>
	/// Hooks a gauge up to its girl's Health stat once she has one.
	/// </summary>
	private static void Resolve(Gauge gauge)
	{
		gauge.Health = gauge.Entity.GetComponent<StatsComponent>()?.GetStat(StatType.Health);
		if (gauge.Health is null) return;

		gauge.Health.OnValueChanged += gauge.Handler;
		gauge.Target = gauge.Ratio;
		gauge.Shown = gauge.Target;
		gauge.Trail = gauge.Target;
	}

	/// <summary>
	/// Moves the big medallion to the girl now being controlled.
	/// </summary>
	/// <param name="entity">The entity now being controlled.</param>
	private void Follow(Entity entity)
	{
		Track(entity);

		Gauge next = _gauges.Find(gauge => gauge.Entity == entity)!;
		if (next == _active) return;

		_previous = _active;
		_active = next;

		// The very first girl just appears; later swaps flip the medallions.
		_flip = _previous is null ? 1f : 0f;
		QueueRedraw();
	}

	private void OnControlSchemeChanged(ControlScheme _) => QueueRedraw();

	private void OnHealthChanged(Gauge gauge)
	{
		float previous = gauge.Target;
		gauge.Target = gauge.Ratio;

		int amount = Mathf.RoundToInt(Mathf.Abs(gauge.Target - previous) * gauge.Maximum);
		float drift = (float)GD.RandRange(-10.0, 10.0);

		if (gauge.Target < previous)
		{
			gauge.Flash = 1f;
			gauge.Shake = 1f;
			gauge.Squash = 1f;
			gauge.TrailHold = TrailDelay;

			if (amount > 0) gauge.Popups.Add(new Popup($"-{amount}", LowHealthColor.Lightened(0.15f), drift));
			if (gauge == _active) _hurt = 1f;
		}
		else if (gauge.Target > previous)
		{
			gauge.Heal = 1f;

			if (amount > 0) gauge.Popups.Add(new Popup($"+{amount}", HealColor, drift));
		}

		QueueRedraw();
	}

	private void Animate(Gauge gauge, float dt)
	{
		// Heals fill up more slowly than hits drain, so the green preview has time to read.
		float speed = gauge.Target > gauge.Shown ? FillSpeed * 0.35f : FillSpeed;
		gauge.Shown = Mathf.Lerp(gauge.Shown, gauge.Target, 1f - Mathf.Exp(-speed * dt));
		if (Mathf.Abs(gauge.Shown - gauge.Target) < 0.001f) gauge.Shown = gauge.Target;

		gauge.TrailHold = Mathf.Max(0f, gauge.TrailHold - dt);

		if (gauge.Trail <= gauge.Shown) gauge.Trail = gauge.Shown;
		else if (gauge.TrailHold <= 0f) gauge.Trail = Mathf.Max(gauge.Shown, gauge.Trail - (TrailSpeed * dt));

		gauge.Flash = Mathf.Max(0f, gauge.Flash - (dt * 4f));
		gauge.Heal = Mathf.Max(0f, gauge.Heal - (dt * 1.2f));
		gauge.Shake = Mathf.Max(0f, gauge.Shake - (dt * 3.5f));
		gauge.Squash = Mathf.Max(0f, gauge.Squash - (dt * 5f));

		foreach (Popup popup in gauge.Popups) popup.Age += dt;
		gauge.Popups.RemoveAll(popup => popup.Age >= POPUP_TIME);
	}

	private bool IsLow(Gauge gauge) => gauge.Health is not null && gauge.Target > 0f && gauge.Shown <= LowHealthAt;

	/// <summary>
	/// A double thump, faster the closer she is to running out, between 0 and 1.
	/// </summary>
	private float Heartbeat(Gauge gauge)
	{
		if (!IsLow(gauge)) return 0f;

		float rate = Mathf.Lerp(1.7f, 1.1f, gauge.Shown / Mathf.Max(LowHealthAt, 0.01f));
		float phase = (_time * rate) % 1f;

		return Mathf.Max(Thump(phase, 0.08f), Thump(phase, 0.3f) * 0.65f);

		static float Thump(float phase, float at) => Mathf.Exp(-Mathf.Pow((phase - at) / 0.06f, 2f));
	}

	/// <summary>
	/// The ring colour for one segment, from the deep accent at the start to the bright one at the end,
	/// turning red and grey as she gets hurt and downed.
	/// </summary>
	private Color SegmentColor(Gauge gauge, int index, int count)
	{
		Color color = gauge.Look.Glow.Lerp(gauge.Look.Accent, count > 1 ? (float)index / (count - 1) : 1f);

		if (IsLow(gauge)) color = color.Lerp(LowHealthColor, 0.55f + (Heartbeat(gauge) * 0.45f));

		return color.Lerp(Colors.White, gauge.Flash * 0.85f).Lerp(HealColor, gauge.Heal * 0.35f);
	}

	private static int SegmentCount(Gauge gauge) => Mathf.Clamp(Mathf.RoundToInt(gauge.Maximum / 10f), 4, 10);

	/// <summary>
	/// Draws a girl's medallion: shadow, backplate, segmented ring, lit inner disc and emblem.
	/// </summary>
	/// <param name="centre">Centre of the medallion.</param>
	/// <param name="radius">Radius of the ring's centre line.</param>
	/// <param name="width">Thickness of the ring.</param>
	/// <param name="gauge">The girl to draw.</param>
	/// <param name="open">Horizontal scale of the swap flip, 1 when not flipping.</param>
	/// <param name="main">Whether this is the big medallion, which gets more detail.</param>
	private void DrawMedallion(Vector2 centre, float radius, float width, Gauge gauge, float open, bool main)
	{
		float beat = Heartbeat(gauge);
		float pop = 1f + (beat * 0.06f) + (gauge.Heal * 0.04f);
		Vector2 shake = gauge.Shake <= 0f ? Vector2.Zero
			: new Vector2(Mathf.Sin(_time * 93f), Mathf.Cos(_time * 71f)) * (gauge.Shake * gauge.Shake * radius * 0.12f);

		DrawSetTransform(centre + shake, 0f, new Vector2(open * pop, pop));

		float outer = radius + (width * 0.5f) + 4f;
		float inner = radius - (width * 0.5f) - 4f;
		Look look = gauge.Look;
		bool down = gauge.Down;

		DrawCircle(new Vector2(0f, outer * 0.1f), outer + 3f, new Color(0f, 0f, 0f, 0.35f));

		if (beat > 0f)
			DrawArc(Vector2.Zero, outer + 3f, 0f, Mathf.Tau, 64, new Color(LowHealthColor, beat * 0.6f), 6f, true);

		DrawCircle(Vector2.Zero, outer, Grey(look.Deep.Darkened(0.45f), down));
		DrawArc(Vector2.Zero, outer - 1f, 0f, Mathf.Tau, 64, Grey(new Color(look.Accent, 0.55f), down), 2f, true);

		int count = SegmentCount(gauge);

		DrawRing(radius, width, count, 0f, 1f, _ => new Color(0f, 0f, 0f, 0.55f));
		DrawRing(radius, width, count, gauge.Shown, gauge.Trail, _ => TrailColor);

		if (gauge.Target > gauge.Shown)
			DrawRing(radius, width, count, gauge.Shown, gauge.Target, _ => new Color(HealColor, 0.45f + (0.35f * Mathf.Sin(_time * 12f))));

		DrawRing(radius, width, count, 0f, gauge.Shown, index => Grey(SegmentColor(gauge, index, count), down));

		// The inner disc is lit from above: rings from dark at the edge to her colour in the middle.
		const int steps = 10;

		for (int step = 0; step <= steps; step++)
		{
			float t = (float)step / steps;
			Color color = look.Deep.Lerp(look.Glow, t * 0.5f);
			DrawCircle(new Vector2(0f, -inner * 0.18f * t), inner * (1f - (t * 0.8f)), Grey(color, down));
		}

		DrawArc(Vector2.Zero, inner * 0.74f, Mathf.DegToRad(205f), Mathf.DegToRad(335f), 24, new Color(1f, 1f, 1f, 0.12f), inner * 0.2f, true);

		float size = inner * (main ? 0.62f : 0.66f) * (1f + (gauge.Squash * 0.18f) + (beat * 0.08f));
		DrawEmblem(look.Emblem, size, Grey(look.Accent.Lightened(0.1f), down), down);

		if (down && main)
		{
			DrawLine(new Vector2(-inner * 0.5f, -inner * 0.5f), new Vector2(inner * 0.5f, inner * 0.5f), new Color(LowHealthColor, 0.9f), 5f, true);
			DrawLine(new Vector2(inner * 0.5f, -inner * 0.5f), new Vector2(-inner * 0.5f, inner * 0.5f), new Color(LowHealthColor, 0.9f), 5f, true);
		}

		DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
	}

	/// <summary>
	/// Draws the part of a segmented ring between two fractions, leaving small gaps between segments.
	/// </summary>
	private void DrawRing(float radius, float width, int count, float from, float to, System.Func<int, Color> color)
	{
		if (to - from <= 0.0005f) return;

		float gap = Mathf.DegToRad(count > 6 ? 3.5f : 5f);

		for (int index = 0; index < count; index++)
		{
			float segmentStart = (float)index / count;
			float segmentEnd = (float)(index + 1) / count;
			float start = Mathf.Max(from, segmentStart);
			float end = Mathf.Min(to, segmentEnd);

			if (end - start <= 0.0005f) continue;

			// Only the real ends of a segment are pulled in, so a partly full segment ends cleanly.
			float startAngle = START_ANGLE + (start * Mathf.Tau) + (start <= segmentStart + 0.0001f ? gap * 0.5f : 0f);
			float endAngle = START_ANGLE + (end * Mathf.Tau) - (end >= segmentEnd - 0.0001f ? gap * 0.5f : 0f);

			if (endAngle <= startAngle) continue;

			int points = Mathf.Max(3, Mathf.CeilToInt((endAngle - startAngle) / Mathf.Tau * 96f));
			DrawArc(Vector2.Zero, radius, startAngle, endAngle, points, color(index), width, true);
		}
	}

	/// <summary>
	/// Draws a girl's emblem around the current origin: Elaine's hammer, Annabelle's heart.
	/// </summary>
	private void DrawEmblem(Emblem emblem, float size, Color color, bool down)
	{
		Color shadow = new(0f, 0f, 0f, 0.35f);
		Color shine = new(1f, 1f, 1f, down ? 0.15f : 0.55f);

		switch (emblem)
		{
			case Emblem.Heart:
				Vector2[] heart = new Vector2[48];

				for (int i = 0; i < heart.Length; i++)
				{
					float t = Mathf.Tau * i / heart.Length;
					float x = 16f * Mathf.Pow(Mathf.Sin(t), 3f);
					float y = (13f * Mathf.Cos(t)) - (5f * Mathf.Cos(2f * t)) - (2f * Mathf.Cos(3f * t)) - Mathf.Cos(4f * t);
					heart[i] = new Vector2(x, -y + 2f) * (size / 17f);
				}

				DrawColoredPolygon(Shifted(heart, new Vector2(0f, size * 0.08f)), shadow);
				DrawColoredPolygon(heart, color);
				DrawCircle(new Vector2(-size * 0.42f, -size * 0.38f), size * 0.15f, shine);
				break;

			case Emblem.Hammer:
				const float tilt = -0.65f;
				Color handle = color.Darkened(0.45f);

				Vector2[] shaft = Box(-0.09f, -0.2f, 0.09f, 0.95f, size, tilt);
				Vector2[] head = Box(-0.62f, -0.72f, 0.62f, -0.2f, size, tilt);
				Vector2[] band = Box(-0.62f, -0.72f, -0.42f, -0.2f, size, tilt);
				Vector2[] face = Box(0.42f, -0.72f, 0.62f, -0.2f, size, tilt);

				DrawColoredPolygon(Shifted(head, new Vector2(0f, size * 0.08f)), shadow);
				DrawColoredPolygon(shaft, handle);
				DrawColoredPolygon(head, color);
				DrawColoredPolygon(band, color.Darkened(0.2f));
				DrawColoredPolygon(face, color.Darkened(0.2f));
				DrawColoredPolygon(Box(-0.3f, -0.68f, 0.3f, -0.6f, size, tilt), shine);
				break;

			default:
				DrawCircle(new Vector2(0f, size * 0.08f), size * 0.6f, shadow);
				DrawCircle(Vector2.Zero, size * 0.6f, color);
				DrawCircle(new Vector2(-size * 0.2f, -size * 0.22f), size * 0.16f, shine);
				break;
		}
	}

	/// <summary>
	/// A rectangle given in fractions of <paramref name="size"/>, centred and rotated by <paramref name="angle"/>.
	/// </summary>
	private static Vector2[] Box(float left, float top, float right, float bottom, float size, float angle)
	{
		// The hammer's middle sits slightly below its head, so shift it up to centre it in the disc.
		Vector2 offset = new(0f, -0.12f);

		return
		[
			((new Vector2(left, top) - offset) * size).Rotated(angle),
			((new Vector2(right, top) - offset) * size).Rotated(angle),
			((new Vector2(right, bottom) - offset) * size).Rotated(angle),
			((new Vector2(left, bottom) - offset) * size).Rotated(angle),
		];
	}

	private static Vector2[] Shifted(Vector2[] points, Vector2 by)
	{
		Vector2[] shifted = new Vector2[points.Length];
		for (int i = 0; i < points.Length; i++) shifted[i] = points[i] + by;
		return shifted;
	}

	/// <summary>
	/// The name plate behind the big medallion: name, state tag and health that counts as it changes.
	/// </summary>
	private void DrawPlate(Gauge gauge, float open)
	{
		Look look = gauge.Look;
		bool down = gauge.Down;
		float alpha = open * open;

		_plate.BorderColor = Grey(look.Accent, down);
		DrawStyleBox(_plate, new Rect2(MainCentre.X, 26f, 222f, 66f));

		const float left = 126f;
		Color nameColor = Grey(look.Accent.Lightened(0.15f), down);
		Text(new Vector2(left, 48f), gauge.Name, 15, new Color(nameColor, alpha), 4);

		float nameWidth = _font.GetStringSize(gauge.Name, HorizontalAlignment.Left, -1f, 15).X;
		string tag = down ? "DOWN" : IsLow(gauge) ? "LOW" : string.Empty;

		// LOW blinks with her heartbeat; DOWN stays put.
		if (tag.Length > 0)
		{
			float blink = down ? 1f : 0.45f + (Heartbeat(gauge) * 0.55f);
			Text(new Vector2(left + nameWidth + 8f, 48f), tag, 12, new Color(LowHealthColor, blink * alpha), 4);
		}

		int current = Mathf.RoundToInt(gauge.Shown * gauge.Maximum);
		string amount = current.ToString();
		Color amountColor = Colors.White.Lerp(LowHealthColor, gauge.Flash * 0.8f).Lerp(HealColor, gauge.Heal * 0.6f);

		Text(new Vector2(left, 80f), amount, 30, new Color(amountColor, alpha), 6);

		float amountWidth = _font.GetStringSize(amount, HorizontalAlignment.Left, -1f, 30).X;
		Text(new Vector2(left + amountWidth + 5f, 80f), $"/ {Mathf.RoundToInt(gauge.Maximum)}", 14, new Color(1f, 1f, 1f, 0.55f * alpha), 4);
	}

	/// <summary>
	/// The small badge for a girl who is not being controlled, with her health and the swap key.
	/// </summary>
	private void DrawBadge(Vector2 centre, Gauge gauge, float open, bool showSwapKey)
	{
		DrawMedallion(centre, 18f, 5f, gauge, open, false);

		float left = centre.X + 30f;
		Color nameColor = Grey(gauge.Look.Accent.Lightened(0.1f), gauge.Down);
		string health = gauge.Down ? "DOWN" : Mathf.RoundToInt(gauge.Shown * gauge.Maximum).ToString();
		Color healthColor = gauge.Down || IsLow(gauge) ? LowHealthColor : Colors.White;

		Text(new Vector2(left, centre.Y - 2f), gauge.Name, 11, new Color(nameColor, 0.9f), 3);
		Text(new Vector2(left, centre.Y + 14f), health, 14, healthColor.Lerp(Colors.White, gauge.Flash), 4);

		bool canSwap = EntitySwapSystem?.ControlScheme is not ControlScheme.TwoHeadedUnit;
		if (!showSwapKey || !canSwap || _swapKey.Length is 0) return;

		float width = Mathf.Max(
			_font.GetStringSize(gauge.Name, HorizontalAlignment.Left, -1f, 11).X,
			_font.GetStringSize(health, HorizontalAlignment.Left, -1f, 14).X);
		float keyWidth = _font.GetStringSize(_swapKey, HorizontalAlignment.Left, -1f, 11).X + 12f;
		Rect2 chip = new(left + width + 10f, centre.Y - 9f, keyWidth, 20f);

		DrawStyleBox(_chip, chip);
		Text(new Vector2(chip.Position.X + 6f, chip.Position.Y + 14.5f), _swapKey, 11, new Color(1f, 1f, 1f, 0.85f), 0);
	}

	/// <summary>
	/// Damage and heal numbers rising away from a medallion.
	/// </summary>
	private void DrawPopups(Gauge gauge, Vector2 anchor, int fontSize)
	{
		foreach (Popup popup in gauge.Popups)
		{
			float t = popup.Age / POPUP_TIME;
			float rise = 1f - Mathf.Pow(1f - t, 3f);
			float alpha = t < 0.6f ? 1f : 1f - ((t - 0.6f) / 0.4f);
			float grow = t < 0.15f ? Mathf.Lerp(1.5f, 1f, t / 0.15f) : 1f;
			int size = Mathf.RoundToInt(fontSize * grow);

			Vector2 position = anchor + new Vector2(popup.Drift * rise, -28f * rise);
			Text(position, popup.Text, size, new Color(popup.Color, alpha), 5);
		}
	}

	/// <summary>
	/// Red screen edges that beat while the active girl is nearly out and flash briefly when she is hit.
	/// </summary>
	private void DrawVignette(Gauge gauge)
	{
		float strength = Mathf.Max((Heartbeat(gauge) * 0.28f) + (IsLow(gauge) ? 0.06f : 0f), _hurt * _hurt * 0.18f);
		if (strength <= 0.005f) return;

		// Draw in screen space, since the HUD itself only covers the corner.
		DrawSetTransformMatrix(GetGlobalTransform().AffineInverse());

		Vector2 screen = GetViewportRect().Size;
		Vector2 inset = screen * new Vector2(0.12f, 0.16f);
		Color edge = new(LowHealthColor.Darkened(0.2f), strength);
		Color clear = new(LowHealthColor, 0f);

		Vector2 a = Vector2.Zero, b = new(screen.X, 0f), c = screen, d = new(0f, screen.Y);
		Vector2 ia = inset, ib = new(screen.X - inset.X, inset.Y), ic = screen - inset, id = new(inset.X, screen.Y - inset.Y);
		Color[] colors = [edge, edge, clear, clear];

		DrawPolygon([a, b, ib, ia], colors);
		DrawPolygon([b, c, ic, ib], colors);
		DrawPolygon([c, d, id, ic], colors);
		DrawPolygon([d, a, ia, id], colors);

		DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
	}

	private void Text(Vector2 position, string text, int size, Color color, int outline)
	{
		if (outline > 0)
			DrawStringOutline(_font, position, text, HorizontalAlignment.Left, -1f, size, outline, new Color(Ink, color.A * 0.9f));

		DrawString(_font, position, text, HorizontalAlignment.Left, -1f, size, color);
	}

	/// <summary>
	/// Washes a colour out to grey when the girl is down.
	/// </summary>
	private static Color Grey(Color color, bool down)
	{
		if (!down) return color;

		float value = color.Luminance * 0.8f;
		return new Color(value, value, value, color.A);
	}

	private static Look LookFor(string name) => name switch
	{
		"Elaine" => new Look(new Color("ffc062"), new Color("ff6f3c"), new Color("4a2216"), Emblem.Hammer),
		"Annabelle" => new Look(new Color("ff9ccf"), new Color("ff4f9a"), new Color("4a1638"), Emblem.Heart),
		_ => new Look(new Color("8ff0b0"), new Color("3fbf74"), new Color("163a2c"), Emblem.Orb),
	};

	/// <summary>
	/// The key bound to swapping characters, as it should be printed on the badge.
	/// </summary>
	private static string SwapKeyLabel()
	{
		if (!InputMap.HasAction(SWAP_ACTION)) return string.Empty;

		foreach (InputEvent inputEvent in InputMap.ActionGetEvents(SWAP_ACTION))
		{
			if (inputEvent is not InputEventKey key) continue;

			// The physical keycode is named after the US layout, which is right for keys like Tab and
			// avoids asking the display server, which headless runs do not support.
			Key keycode = key.Keycode is not Key.None ? key.Keycode : key.PhysicalKeycode;
			return OS.GetKeycodeString(keycode).ToUpperInvariant();
		}

		return string.Empty;
	}
}
