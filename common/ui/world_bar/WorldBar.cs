using Godot;

namespace Core.UI;

/// <summary>
/// A small bar that floats above a character and always faces the camera: health for robots,
/// calm for monsters (GDD p.20).
/// </summary>
/// <remarks>
/// Built from flat quads in code so any entity can get one without editing its scene. The fill
/// eases towards its target instead of snapping, flashes when it is hit, and can fade away.
/// </remarks>
public partial class WorldBar : Node3D
{
	/// <summary>Width of the bar in metres.</summary>
	public float Width { get; private set; } = 0.9f;

	/// <summary>How fast the fill catches up with the real value. Higher is snappier.</summary>
	public float FillSpeed { get; set; } = 9f;

	/// <summary>How fast the trailing "recent change" bar catches up.</summary>
	public float TrailSpeed { get; set; } = 2.5f;

	/// <summary>How long the white flash after a change lasts, in seconds.</summary>
	public float FlashTime { get; set; } = 0.18f;

	private MeshInstance3D _background = null!;
	private MeshInstance3D _trail = null!;
	private MeshInstance3D _fill = null!;
	private StandardMaterial3D _fillMaterial = null!;

	private Color _fillColor = Colors.White;
	private float _target = 1f;
	private float _shown = 1f;
	private float _trailValue = 1f;
	private float _flash;
	private float _fadeOut = -1f;
	private float _fadeLeft;

	/// <summary>
	/// Builds a bar and parents it to a character.
	/// </summary>
	/// <param name="parent">Node the bar hangs from, usually the character's body.</param>
	/// <param name="offset">Position above the character.</param>
	/// <param name="fillColor">Colour of the filled part.</param>
	/// <param name="trailColor">Colour of the trailing part that shows the recent change.</param>
	/// <param name="width">Width of the bar in metres.</param>
	/// <param name="startRatio">Fill to start from, 0 to 1.</param>
	/// <returns>The new bar, already in the tree.</returns>
	public static WorldBar Attach(Node3D parent, Vector3 offset, Color fillColor, Color trailColor, float width = 0.9f, float startRatio = 1f)
	{
		WorldBar bar = new()
		{
			Position = offset,
			Width = width,
			_fillColor = fillColor,
			_target = startRatio,
			_shown = startRatio,
			_trailValue = startRatio,
		};

		parent.AddChild(bar);
		bar.Build(trailColor);

		return bar;
	}

	/// <summary>
	/// Sets the value the bar eases towards and flashes it.
	/// </summary>
	/// <param name="ratio">The new value, 0 to 1.</param>
	/// <param name="flash">Whether to flash the bar white for a moment.</param>
	public void SetRatio(float ratio, bool flash = true)
	{
		_target = Mathf.Clamp(ratio, 0f, 1f);

		if (flash) _flash = FlashTime;
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

		// Flat quads, so the whole bar is turned to face the camera rather than billboarding
		// each piece, which would break the way the fill is offset.
		if (GetViewport()?.GetCamera3D() is { } camera) GlobalBasis = camera.GlobalBasis;

		_shown = Mathf.Lerp(_shown, _target, 1f - Mathf.Exp(-FillSpeed * dt));
		_trailValue = _trailValue < _shown
			? _shown
			: Mathf.Lerp(_trailValue, _target, 1f - Mathf.Exp(-TrailSpeed * dt));

		Resize(_fill, _shown);
		Resize(_trail, _trailValue);

		if (_flash > 0f)
		{
			_flash = Mathf.Max(0f, _flash - dt);
			_fillMaterial.AlbedoColor = _fillColor.Lerp(Colors.White, _flash / FlashTime);
		}

		if (_fadeOut > 0f) StepFade(dt);
	}

	private void StepFade(float dt)
	{
		_fadeLeft -= dt;

		float alpha = Mathf.Clamp(_fadeLeft / _fadeOut, 0f, 1f);
		Scale = new Vector3(1f, Mathf.Lerp(0.2f, 1f, alpha), 1f);

		foreach (MeshInstance3D piece in new[] { _background, _trail, _fill })
			if (piece.MaterialOverride is StandardMaterial3D material)
				material.AlbedoColor = material.AlbedoColor with { A = alpha };

		if (_fadeLeft <= 0f) QueueFree();
	}

	/// <summary>
	/// Scales a piece from its left edge, so the bar fills up instead of growing from the middle.
	/// </summary>
	/// <param name="piece">The piece to resize.</param>
	/// <param name="ratio">How much of the bar it covers, 0 to 1.</param>
	private void Resize(MeshInstance3D piece, float ratio)
	{
		piece.Scale = new Vector3(Mathf.Max(ratio, 0.0001f), 1f, 1f);
		piece.Position = new Vector3(-Width * (1f - ratio) * 0.5f, 0f, piece.Position.Z);
	}

	private void Build(Color trailColor)
	{
		_background = NewPiece(new Color(0.07f, 0.07f, 0.09f, 0.85f), Width + 0.06f, 0.16f, -0.002f);
		_trail = NewPiece(trailColor, Width, 0.11f, -0.001f);
		_fill = NewPiece(_fillColor, Width, 0.11f, 0f);

		_fillMaterial = (StandardMaterial3D)_fill.MaterialOverride;

		AddChild(_background);
		AddChild(_trail);
		AddChild(_fill);
	}

	/// <summary>
	/// One flat, unshaded piece of the bar.
	/// </summary>
	/// <param name="color">Colour of the piece.</param>
	/// <param name="width">Width in metres.</param>
	/// <param name="height">Height in metres.</param>
	/// <param name="depth">Local Z offset, so the pieces do not fight for the same pixels.</param>
	/// <returns>The mesh to add to the bar.</returns>
	private static MeshInstance3D NewPiece(Color color, float width, float height, float depth)
	{
		return new MeshInstance3D
		{
			Mesh = new QuadMesh { Size = new Vector2(width, height) },
			MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = color,
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				NoDepthTest = true,
			},
			Position = new Vector3(0f, 0f, depth),
		};
	}
}
