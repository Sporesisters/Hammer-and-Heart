using System;
using System.Collections.Generic;
using Godot;

namespace Core.ECS.Components;

/// <summary>
/// The pot Annabelle stirs while cooking, built from primitives until the soup gets real art.
/// It steams and bubbles while cooking, throws a bowl of soup at each sister it is for when served and tips
/// over with a splash when spilled. It frees itself once it is done.
/// </summary>
public partial class SoupPot : Node3D
{
	private static readonly Color CopperColor = new(0.85f, 0.48f, 0.3f);
	private static readonly Color WoodColor = new(0.55f, 0.36f, 0.2f);
	private static readonly Color HeartColor = new(1f, 0.45f, 0.7f);

	private const float SERVE_FLIGHT_TIME = 0.6f;
	private const float BASE_SCALE = 1.5f;

	private Color _soupColor;
	private float _size;
	private MeshInstance3D _soup = null!;
	private Node3D _spoon = null!;
	private CpuParticles3D _steam = null!;
	private CpuParticles3D _bubbles = null!;
	private float _time;
	private float _progress;
	private bool _done;

	/// <summary>
	/// Builds a pot in front of a character and pops it in.
	/// </summary>
	/// <param name="character">Who is cooking.</param>
	/// <param name="soupColor">Colour of the soup, which tells the sizes apart.</param>
	/// <param name="size">Scale of the pot: bigger soups get bigger pots.</param>
	/// <returns>The new pot, already in the tree.</returns>
	public static SoupPot Spawn(Node3D character, Color soupColor, float size)
	{
		// Sized up from a real pot, so it still reads with the camera pulled back.
		size *= BASE_SCALE;
		SoupPot pot = new() { _soupColor = soupColor, _size = size, Position = new Vector3(0f, -0.4f, -0.85f) };
		character.AddChild(pot);
		pot.Build();

		pot.Scale = Vector3.One * 0.01f;
		Tween pop = pot.CreateTween();
		pop.TweenProperty(pot, "scale", Vector3.One * size, 0.3f)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

		return pot;
	}

	/// <summary>
	/// How far along the soup is, 0 to 1. The soup rises and the steam thickens as it cooks.
	/// </summary>
	/// <param name="progress">Cooking progress.</param>
	public void SetProgress(float progress) => _progress = Mathf.Clamp(progress, 0f, 1f);

	/// <summary>
	/// Recolours the soup and grows or shrinks the pot, as the soup changes size.
	/// </summary>
	/// <param name="soupColor">The new soup colour.</param>
	/// <param name="size">The new pot scale, as passed to <see cref="Spawn"/>.</param>
	public void SetLook(Color soupColor, float size)
	{
		if (_done) return;

		_soupColor = soupColor;
		_size = size * BASE_SCALE;

		if (_soup.MaterialOverride is StandardMaterial3D soupMaterial)
			soupMaterial.Emission = soupColor;
		_bubbles.Color = soupColor.Lightened(0.35f);

		Tween grow = CreateTween();
		grow.TweenProperty(this, "scale", Vector3.One * _size, 0.25f)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
	}

	public override void _Process(double delta)
	{
		if (_done) return;

		_time += (float)delta;

		// Stirring: the spoon circles the pot, faster as the soup gets close.
		_spoon.Rotation = new Vector3(0f, _time * Mathf.Lerp(5f, 9f, _progress), 0f);

		// The soup rises from the bottom of the pot and gets richer in colour.
		float level = Mathf.Lerp(0.05f, 0.27f, _progress);
		float bob = Mathf.Sin(_time * 7f) * 0.008f;
		_soup.Position = new Vector3(0f, level + bob, 0f);
		if (_soup.MaterialOverride is StandardMaterial3D soupMaterial)
			soupMaterial.AlbedoColor = _soupColor.Darkened(0.45f * (1f - _progress));

		_steam.SpeedScale = Mathf.Lerp(0.6f, 1.4f, _progress);
		_bubbles.Emitting = _progress > 0.25f;

		// Nearly ready: the pot rattles, so the player can see it about to come out.
		float rattle = _progress > 0.8f ? Mathf.Sin(_time * 40f) * 0.04f * (_progress - 0.8f) / 0.2f : 0f;
		RotationDegrees = new Vector3(0f, 0f, rattle * 30f);
	}

	/// <summary>
	/// Throws a bowl of soup in an arc at each target and calls back as each lands, then puts the pot away.
	/// </summary>
	/// <param name="bowls">Who each bowl is for, followed while in flight, and what to do when it
	/// lands, to apply the healing.</param>
	public void Serve(IEnumerable<(Node3D Target, Action OnArrive)> bowls)
	{
		_done = true;
		_steam.Emitting = false;
		_bubbles.Emitting = false;

		foreach ((Node3D target, Action onArrive) in bowls)
			ThrowBowl(target, onArrive);

		PutAway();
	}

	/// <summary>
	/// Shrinks the pot away without serving anything, as when Annabelle stops cooking early.
	/// </summary>
	public void PutAway()
	{
		_done = true;
		_steam.Emitting = false;
		_bubbles.Emitting = false;

		Vector3 restScale = Scale;
		Tween away = CreateTween();
		away.TweenProperty(this, "scale", restScale * 1.2f, 0.1f);
		away.TweenProperty(this, "scale", Vector3.One * 0.01f, 0.25f)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.In);
		away.TweenCallback(Callable.From(QueueFree));
	}

	private void ThrowBowl(Node3D target, Action onArrive)
	{
		Node3D bowl = BuildBowl();
		Node root = GetTree().CurrentScene ?? GetTree().Root;
		root.AddChild(bowl);
		Vector3 start = GlobalPosition + new Vector3(0f, 0.4f * _size, 0f);
		bowl.GlobalPosition = start;

		Tween flight = bowl.CreateTween();
		flight.TweenMethod(Callable.From<float>(t =>
		{
			if (!IsInstanceValid(target)) return;

			Vector3 end = target.GlobalPosition + new Vector3(0f, 0.4f, 0f);
			Vector3 position = start.Lerp(end, t) + new Vector3(0f, Mathf.Sin(t * Mathf.Pi) * 1.4f, 0f);
			bowl.GlobalPosition = position;
			bowl.RotateY(0.25f);
		}), 0f, 1f, SERVE_FLIGHT_TIME).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		flight.TweenCallback(Callable.From(() =>
		{
			if (IsInstanceValid(target)) HeartBurst(target);
			onArrive();
			bowl.QueueFree();
		}));
	}

	/// <summary>
	/// A light knock: the pot jolts and a little soup splashes out, but it keeps cooking.
	/// </summary>
	public void Slosh()
	{
		if (_done) return;

		CpuParticles3D splash = Particles(SphereMeshOf(0.04f), 10, 0.5f, _soupColor);
		splash.OneShot = true;
		splash.Explosiveness = 0.9f;
		splash.Direction = Vector3.Up;
		splash.Spread = 40f;
		splash.InitialVelocityMin = 1.2f;
		splash.InitialVelocityMax = 2.2f;
		splash.Gravity = new Vector3(0f, -12f, 0f);
		splash.Position = new Vector3(0f, 0.32f, 0f);
		AddChild(splash);
		splash.Emitting = true;
		splash.Finished += splash.QueueFree;

		// _Process owns the pot's rotation, so the jolt is a quick squash instead of a tilt.
		Vector3 restScale = Vector3.One * _size;
		Tween jolt = CreateTween();
		jolt.TweenProperty(this, "scale", restScale * new Vector3(1.15f, 0.85f, 1.15f), 0.06f);
		jolt.TweenProperty(this, "scale", restScale, 0.25f)
			.SetTrans(Tween.TransitionType.Elastic).SetEase(Tween.EaseType.Out);
	}

	/// <summary>
	/// Tips the pot over with a splash and a puddle that fades away.
	/// </summary>
	public void Spill()
	{
		_done = true;
		_steam.Emitting = false;
		_bubbles.Emitting = false;
		_soup.Visible = false;

		// Left behind in the world, so the puddle stays where it fell while Annabelle walks off.
		Transform3D where = GlobalTransform;
		Node root = GetTree().CurrentScene ?? GetTree().Root;
		Reparent(root);
		GlobalTransform = where;

		CpuParticles3D splash = Particles(SphereMeshOf(0.05f), 28, 0.7f, _soupColor);
		splash.OneShot = true;
		splash.Explosiveness = 0.95f;
		splash.Direction = new Vector3(0f, 1f, -1f);
		splash.Spread = 55f;
		splash.InitialVelocityMin = 2f;
		splash.InitialVelocityMax = 3.5f;
		splash.Gravity = new Vector3(0f, -12f, 0f);
		splash.Position = new Vector3(0f, 0.3f, 0f);
		AddChild(splash);
		splash.Emitting = true;

		MeshInstance3D puddle = new()
		{
			Mesh = new CylinderMesh { TopRadius = 0.5f, BottomRadius = 0.5f, Height = 0.01f },
			MaterialOverride = Material(_soupColor.Darkened(0.2f), 0.2f, transparent: true),
			Position = new Vector3(0f, 0.02f, -0.45f),
			Scale = new Vector3(0.1f, 1f, 0.1f),
		};
		AddChild(puddle);

		Tween tip = CreateTween().SetParallel();
		tip.TweenProperty(this, "rotation_degrees:x", -100f, 0.3f)
			.SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
		tip.TweenProperty(puddle, "scale", new Vector3(1.4f, 1f, 1.4f), 0.4f).SetDelay(0.1f)
			.SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);

		// The puddle is not rotated with the pot.
		puddle.TopLevel = true;
		puddle.GlobalPosition = where.Origin + new Vector3(0f, -0.3f * _size, 0f) + (where.Basis.Z * -0.5f * _size);

		Tween fade = CreateTween();
		fade.TweenInterval(1.2f);
		fade.TweenProperty(this, "scale", Vector3.One * 0.01f, 0.3f);
		fade.Parallel().TweenProperty(puddle.MaterialOverride, "albedo_color:a", 0f, 0.6f);
		fade.TweenCallback(Callable.From(QueueFree));
	}

	private void Build()
	{
		// Bright copper rather than iron, so the pot stands out against the dark floor.
		StandardMaterial3D iron = Material(CopperColor, 0.35f);
		iron.Metallic = 0.55f;
		iron.EmissionEnabled = true;
		iron.Emission = CopperColor;
		iron.EmissionEnergyMultiplier = 0.15f;

		AddChild(new MeshInstance3D
		{
			Mesh = new CylinderMesh { TopRadius = 0.3f, BottomRadius = 0.24f, Height = 0.32f, RadialSegments = 20 },
			MaterialOverride = iron,
			Position = new Vector3(0f, 0.16f, 0f),
		});
		AddChild(new MeshInstance3D
		{
			Mesh = new TorusMesh { InnerRadius = 0.28f, OuterRadius = 0.34f, Rings = 20 },
			MaterialOverride = iron,
			Position = new Vector3(0f, 0.32f, 0f),
		});

		// Two little handles, so it reads as a pot and not a bucket.
		foreach (float side in new[] { -1f, 1f })
		{
			AddChild(new MeshInstance3D
			{
				Mesh = new TorusMesh { InnerRadius = 0.04f, OuterRadius = 0.07f },
				MaterialOverride = iron,
				Position = new Vector3(side * 0.34f, 0.26f, 0f),
				RotationDegrees = new Vector3(90f, 0f, 0f),
			});
		}

		StandardMaterial3D soup = Material(_soupColor, 0.25f);
		soup.EmissionEnabled = true;
		soup.Emission = _soupColor;
		soup.EmissionEnergyMultiplier = 0.6f;
		_soup = new MeshInstance3D
		{
			Mesh = new CylinderMesh { TopRadius = 0.27f, BottomRadius = 0.27f, Height = 0.02f, RadialSegments = 20 },
			MaterialOverride = soup,
		};
		AddChild(_soup);

		// The spoon leans against the rim and circles with the stirring.
		_spoon = new Node3D { Position = new Vector3(0f, 0.2f, 0f) };
		AddChild(_spoon);
		_spoon.AddChild(new MeshInstance3D
		{
			Mesh = new CylinderMesh { TopRadius = 0.02f, BottomRadius = 0.02f, Height = 0.6f },
			MaterialOverride = Material(WoodColor, 0.8f),
			Position = new Vector3(0.12f, 0.2f, 0f),
			RotationDegrees = new Vector3(0f, 0f, -25f),
		});

		_steam = Particles(SphereMeshOf(0.09f), 14, 1.4f, new Color(1f, 1f, 1f, 0.55f));
		_steam.Position = new Vector3(0f, 0.35f, 0f);
		_steam.Direction = Vector3.Up;
		_steam.Spread = 12f;
		_steam.InitialVelocityMin = 0.5f;
		_steam.InitialVelocityMax = 0.9f;
		_steam.Gravity = new Vector3(0f, 0.2f, 0f);
		_steam.EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere;
		_steam.EmissionSphereRadius = 0.15f;
		_steam.ScaleAmountCurve = GrowThenShrink();
		AddChild(_steam);

		_bubbles = Particles(SphereMeshOf(0.035f), 10, 0.5f, _soupColor.Lightened(0.35f));
		_bubbles.Position = new Vector3(0f, 0.28f, 0f);
		_bubbles.Direction = Vector3.Up;
		_bubbles.InitialVelocityMin = 0.2f;
		_bubbles.InitialVelocityMax = 0.5f;
		_bubbles.Gravity = Vector3.Zero;
		_bubbles.EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere;
		_bubbles.EmissionSphereRadius = 0.2f;
		_bubbles.ScaleAmountCurve = GrowThenShrink();
		_bubbles.Emitting = false;
		AddChild(_bubbles);
	}

	/// <summary>
	/// A steaming bowl with a heart floating over it: the soup on its way to a sister.
	/// </summary>
	private Node3D BuildBowl()
	{
		Node3D bowl = new();

		StandardMaterial3D soup = Material(_soupColor, 0.3f);
		soup.EmissionEnabled = true;
		soup.Emission = _soupColor;
		soup.EmissionEnergyMultiplier = 0.8f;

		bowl.AddChild(new MeshInstance3D
		{
			Mesh = new CylinderMesh { TopRadius = 0.2f, BottomRadius = 0.12f, Height = 0.14f },
			MaterialOverride = Material(new Color(0.95f, 0.92f, 0.86f), 0.5f),
		});
		bowl.AddChild(new MeshInstance3D
		{
			Mesh = new CylinderMesh { TopRadius = 0.18f, BottomRadius = 0.18f, Height = 0.02f },
			MaterialOverride = soup,
			Position = new Vector3(0f, 0.06f, 0f),
		});
		bowl.AddChild(new Label3D
		{
			Text = "♥",
			FontSize = 96,
			PixelSize = 0.004f,
			Modulate = HeartColor,
			OutlineSize = 12,
			OutlineModulate = new Color(0.25f, 0.05f, 0.18f),
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			NoDepthTest = true,
			Position = new Vector3(0f, 0.35f, 0f),
		});

		CpuParticles3D trail = Particles(SphereMeshOf(0.06f), 20, 0.4f, _soupColor.Lightened(0.3f));
		trail.LocalCoords = false;
		trail.Gravity = Vector3.Zero;
		trail.InitialVelocityMax = 0.2f;
		trail.ScaleAmountCurve = GrowThenShrink();
		bowl.AddChild(trail);

		return bowl;
	}

	/// <summary>
	/// A burst of hearts around whoever got the soup, so the healing is impossible to miss.
	/// </summary>
	/// <param name="target">Who was healed.</param>
	private static void HeartBurst(Node3D target)
	{
		for (int i = 0; i < 7; i++)
		{
			float angle = Mathf.Tau * i / 7f;
			Label3D heart = new()
			{
				Text = "♥",
				FontSize = 96,
				PixelSize = 0.003f,
				Modulate = HeartColor,
				OutlineSize = 10,
				OutlineModulate = new Color(0.25f, 0.05f, 0.18f),
				Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
				NoDepthTest = true,
				Position = new Vector3(0f, 0.3f, 0f),
			};
			target.AddChild(heart);

			Vector3 end = new Vector3(Mathf.Cos(angle) * 0.8f, 1.1f + (i % 2) * 0.3f, Mathf.Sin(angle) * 0.8f);
			Tween tween = heart.CreateTween().SetParallel();
			tween.TweenProperty(heart, "position", end, 0.7f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
			tween.TweenProperty(heart, "modulate:a", 0f, 0.35f).SetDelay(0.45f);
			tween.TweenProperty(heart, "outline_modulate:a", 0f, 0.35f).SetDelay(0.45f);
			tween.Chain().TweenCallback(Callable.From(heart.QueueFree));
		}
	}

	private static StandardMaterial3D Material(Color color, float roughness, bool transparent = false) => new()
	{
		AlbedoColor = color,
		Roughness = roughness,
		Transparency = transparent ? BaseMaterial3D.TransparencyEnum.Alpha : BaseMaterial3D.TransparencyEnum.Disabled,
	};

	private static SphereMesh SphereMeshOf(float radius)
	{
		StandardMaterial3D material = new()
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			VertexColorUseAsAlbedo = true,
		};

		return new SphereMesh { Radius = radius, Height = radius * 2f, RadialSegments = 8, Rings = 4, Material = material };
	}

	private static CpuParticles3D Particles(Mesh mesh, int amount, float lifetime, Color color) => new()
	{
		Mesh = mesh,
		Amount = amount,
		Lifetime = lifetime,
		Color = color,
		CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
	};

	private static Curve GrowThenShrink()
	{
		Curve curve = new();
		curve.AddPoint(new Vector2(0f, 0.3f));
		curve.AddPoint(new Vector2(0.3f, 1f));
		curve.AddPoint(new Vector2(1f, 0f));
		return curve;
	}
}
