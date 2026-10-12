using Godot;

namespace Core.UI;

/// <summary>
/// The badge drawn at the left end of a <see cref="WorldBar"/>.
/// </summary>
public enum WorldBarIcon
{
	None,
	Gear,
	Heart,
	Hammer,
}

/// <summary>
/// A small bar that floats above a character and always faces the camera: health for robots,
/// calm for monsters (GDD p.20).
/// </summary>
/// <remarks>
/// Drawn by <c>world_bar.gdshader</c> on a single quad, so any entity can get one without editing
/// its scene. It has an icon badge, a segmented gradient fill that eases towards its target and a
/// trail that drains after a short delay. Changes flash it and give it a little kick, and it can pop
/// floating numbers and fade away.
/// </remarks>
public partial class WorldBar : Node3D
{
	/// <summary>Width of the bar in metres, not counting the icon.</summary>
	public float Width { get; private set; } = 0.9f;

	/// <summary>How fast the fill catches up with the real value. Higher is snappier.</summary>
	public float FillSpeed { get; set; } = 10f;

	/// <summary>Seconds the trail waits after a change before it starts catching up.</summary>
	public float TrailDelay { get; set; } = 0.35f;

	/// <summary>Fraction of the bar the trail covers per second.</summary>
	public float TrailSpeed { get; set; } = 1.3f;

	/// <summary>How long the white flash after a change lasts, in seconds.</summary>
	public float FlashTime { get; set; } = 0.2f;

	/// <summary>
	/// Whether the bar is good news as it fills up, like a monster's calm. It then beats when it is
	/// nearly full instead of when it is nearly empty.
	/// </summary>
	public bool FillsUp { get; set; }

	/// <summary>
	/// Fraction of the screen height a bar with an icon takes up. Bars keep this size wherever the
	/// character is, so they stay readable however far the camera pulls back.
	/// </summary>
	public float ScreenHeight { get; set; } = 0.032f;

	private const float HEIGHT = 0.15f;
	private const float ICON_HEIGHT = 0.27f;
	private const float NUMBER_TIME = 0.85f;

	private static readonly Shader BarShader = GD.Load<Shader>("res://common/ui/world_bar/world_bar.gdshader");

	private static Font? _numberFont;

	private MeshInstance3D _quad = null!;
	private ShaderMaterial _material = null!;

	private WorldBarIcon _icon;
	private Vector3 _restPosition;
	private float _target = 1f;
	private float _shown = 1f;
	private float _trailValue = 1f;
	private float _trailHold;
	private float _flash;
	private float _kick;
	private float _spin;
	private float _spinSpeed;
	private float _time;
	private float _fadeOut = -1f;
	private float _fadeLeft;
	private float _quadHeight = HEIGHT;

	/// <summary>
	/// Builds a bar and parents it to a character.
	/// </summary>
	/// <param name="parent">Node the bar hangs from, usually the character's body.</param>
	/// <param name="offset">Position above the character.</param>
	/// <param name="fillColor">Colour of the filled part.</param>
	/// <param name="trailColor">Colour of the trailing part that shows the recent change.</param>
	/// <param name="width">Width of the bar in metres.</param>
	/// <param name="startRatio">Fill to start from, 0 to 1.</param>
	/// <param name="icon">Badge at the left end of the bar.</param>
	/// <param name="iconColor">Colour of the badge, or the fill colour when <c>null</c>.</param>
	/// <param name="segments">How many notches split the bar.</param>
	/// <returns>The new bar, already in the tree.</returns>
	public static WorldBar Attach(Node3D parent, Vector3 offset, Color fillColor, Color trailColor, float width = 0.9f,
		float startRatio = 1f, WorldBarIcon icon = WorldBarIcon.None, Color? iconColor = null, int segments = 5)
	{
		WorldBar bar = new()
		{
			Position = offset,
			Width = width,
			_icon = icon,
			_restPosition = offset,
			_target = startRatio,
			_shown = startRatio,
			_trailValue = startRatio,
		};

		parent.AddChild(bar);
		bar.Build(fillColor, trailColor, iconColor ?? fillColor, segments);

		return bar;
	}

	/// <summary>
	/// Sets the value the bar eases towards, flashes it and gives it a kick.
	/// </summary>
	/// <param name="ratio">The new value, 0 to 1.</param>
	/// <param name="flash">Whether to flash the bar white for a moment.</param>
	public void SetRatio(float ratio, bool flash = true)
	{
		float previous = _target;
		_target = Mathf.Clamp(ratio, 0f, 1f);

		if (!flash) return;

		_flash = FlashTime;
		_kick = 1f;
		_trailHold = TrailDelay;

		// The gear turns a notch with every hit.
		_spinSpeed = Mathf.Sign(previous - _target) * 14f;
	}

	/// <summary>
	/// Pops a number that floats up from the bar and fades, like "-25" on a hit.
	/// </summary>
	/// <param name="text">What to show.</param>
	/// <param name="color">Colour of the text.</param>
	public void ShowNumber(string text, Color color)
	{
		if (GetParent() is not Node3D parent || GetViewport()?.GetCamera3D() is not { } camera) return;

		_numberFont ??= new SystemFont
		{
			FontNames = ["Segoe UI Black", "Arial Rounded MT Bold", "Arial Black", "sans-serif"],
			FontWeight = 900,
			MultichannelSignedDistanceField = true,
			MsdfPixelRange = 16,
		};

		// Sized against the screen like the bar, starting just above its right half.
		float screen = WorldPerScreen(camera, GlobalPosition);
		float size = 0.026f * screen / (72 * 0.0042f);
		Basis view = camera.GlobalBasis.Orthonormalized();
		Vector3 start = GlobalPosition + (view * new Vector3(((float)GD.RandRange(-0.4, 0.4) + 0.5f) * screen * 0.03f, screen * 0.035f, 0f));
		Vector3 end = start + (view.Y * screen * 0.06f);

		// Hung from the character rather than the bar, so it keeps floating after the bar fades.
		Label3D number = new()
		{
			Text = text,
			Font = _numberFont,
			FontSize = 72,
			OutlineSize = 18,
			OutlineModulate = new Color(0.05f, 0.04f, 0.07f),
			Modulate = color,
			PixelSize = 0.0042f,
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			NoDepthTest = true,
			RenderPriority = 2,
			OutlineRenderPriority = 1,
			Scale = Vector3.One * size * 1.6f,
		};

		parent.AddChild(number);
		number.GlobalPosition = start;

		Tween tween = number.CreateTween().SetParallel();
		tween.TweenProperty(number, "global_position", end, NUMBER_TIME)
			.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(number, "scale", Vector3.One * size, 0.18f)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		tween.TweenProperty(number, "modulate:a", 0f, NUMBER_TIME * 0.4f).SetDelay(NUMBER_TIME * 0.6f);
		tween.TweenProperty(number, "outline_modulate:a", 0f, NUMBER_TIME * 0.4f).SetDelay(NUMBER_TIME * 0.6f);
		tween.Chain().TweenCallback(Callable.From(number.QueueFree));
	}

	/// <summary>
	/// Fades the bar out and frees it, for when its owner is destroyed or calmed.
	/// </summary>
	/// <param name="seconds">How long the fade takes.</param>
	public void FadeAndFree(float seconds = 0.35f)
	{
		_fadeOut = seconds;
		_fadeLeft = seconds;
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		_time += dt;

		_shown = Mathf.Lerp(_shown, _target, 1f - Mathf.Exp(-FillSpeed * dt));
		_trailHold = Mathf.Max(0f, _trailHold - dt);

		if (_trailValue < _shown) _trailValue = _shown;
		else if (_trailHold <= 0f) _trailValue = Mathf.Max(_shown, _trailValue - (TrailSpeed * dt));

		_flash = Mathf.Max(0f, _flash - dt);
		_kick = Mathf.Max(0f, _kick - (dt * 4f));
		_spin += _spinSpeed * dt;
		_spinSpeed = Mathf.Lerp(_spinSpeed, 0f, 1f - Mathf.Exp(-6f * dt));

		float alpha = 1f;
		float squash = 1f;

		if (_fadeOut > 0f)
		{
			_fadeLeft -= dt;
			alpha = Mathf.Clamp(_fadeLeft / _fadeOut, 0f, 1f);
			squash = Mathf.Lerp(0.2f, 1f, alpha);

			if (_fadeLeft <= 0f)
			{
				QueueFree();
				return;
			}
		}

		// A quick overshoot on every change, plus a shake that dies out with it.
		float pop = 1f + (Mathf.Sin(_kick * Mathf.Pi) * 0.18f);
		Vector3 shake = new Vector3(Mathf.Sin(_time * 87f), Mathf.Cos(_time * 69f), 0f) * (_kick * _kick * 0.035f);

		// Flat quad, so the whole bar is turned to face the camera and sized against the screen.
		if (GetViewport()?.GetCamera3D() is { } camera && GetParent() is Node3D parent)
		{
			Vector3 anchor = parent.GlobalTransform * _restPosition;
			Basis view = camera.GlobalBasis.Orthonormalized();
			float size = ScreenHeight * WorldPerScreen(camera, anchor) / ICON_HEIGHT;

			GlobalBasis = view * Basis.FromScale(new Vector3(pop * size, pop * squash * size, 1f));
			// Lifted by half its height, so a bar sized for the screen still clears the head below it.
			GlobalPosition = anchor + (view * ((shake * size) + new Vector3(0f, _quadHeight * size * 0.5f, 0f)));
		}

		_material.SetShaderParameter("fill", _shown);
		_material.SetShaderParameter("trail", _trailValue);
		_material.SetShaderParameter("flash", FlashTime > 0f ? _flash / FlashTime : 0f);
		_material.SetShaderParameter("pulse", Pulse());
		_material.SetShaderParameter("spin", _spin);
		_material.SetShaderParameter("alpha", alpha);
	}

	/// <summary>
	/// A heartbeat while the bar is in its urgent zone: nearly empty, or nearly full for <see cref="FillsUp"/> bars.
	/// </summary>
	private float Pulse()
	{
		bool urgent = FillsUp ? _shown >= 0.7f && _target < 1f : _shown <= 0.3f && _target > 0f;
		if (!urgent) return 0f;

		float phase = (_time * 1.4f) % 1f;
		return Mathf.Max(Thump(phase, 0.1f), Thump(phase, 0.32f) * 0.6f);

		static float Thump(float phase, float at) => Mathf.Exp(-Mathf.Pow((phase - at) / 0.07f, 2f));
	}

	/// <summary>
	/// How many metres the whole screen height covers at a point, for sizing things in pixels.
	/// </summary>
	public static float WorldPerScreen(Camera3D camera, Vector3 at)
	{
		if (camera.Projection is Camera3D.ProjectionType.Orthogonal) return camera.Size;

		float depth = Mathf.Max(0.1f, (at - camera.GlobalPosition).Dot(-camera.GlobalBasis.Z));
		return 2f * depth * Mathf.Tan(Mathf.DegToRad(camera.Fov) * 0.5f);
	}

	private void Build(Color fillColor, Color trailColor, Color iconColor, int segments)
	{
		bool hasIcon = _icon is not WorldBarIcon.None;
		float height = hasIcon ? ICON_HEIGHT : HEIGHT;
		_quadHeight = height;
		Vector2 size = new(Width + (hasIcon ? height * 0.7f : 0f), height);

		_material = new ShaderMaterial { Shader = BarShader, RenderPriority = 1 };
		_material.SetShaderParameter("size", size);
		_material.SetShaderParameter("fill_top", fillColor.Lightened(0.12f));
		_material.SetShaderParameter("fill_bottom", fillColor.Darkened(0.25f));
		_material.SetShaderParameter("trail_color", trailColor);
		_material.SetShaderParameter("icon_color", iconColor);
		_material.SetShaderParameter("icon", (int)_icon);
		_material.SetShaderParameter("segments", segments);

		_quad = new MeshInstance3D
		{
			Mesh = new QuadMesh { Size = size },
			MaterialOverride = _material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};

		AddChild(_quad);
	}
}
