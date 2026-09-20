using Core.ECS;
using Core.ECS.Components;
using Core.Stats;
using Core.Utilities.Logging;
using Godot;

public partial class KissProjectile : Area3D
{
	[Export]
	public float Speed { get; set; } = 20f;

	/// <summary>How much Calm a single kiss adds to the monster it lands on (GDD p.4).</summary>
	[Export]
	public float CalmPerHit { get; set; } = 25f;

	[Export]
	public float Lifetime { get; set; } = 3f;

	/// <summary>Downward pull applied to a lobbed shot. Straight shots ignore it.</summary>
	[Export]
	public float ArcGravity { get; set; } = 35f;

	/// <summary>Degrees above the horizon a lobbed shot launches at.</summary>
	[Export]
	public float ArcLaunchAngle { get; set; } = 30f;

	[Export]
	public Color StraightColor { get; set; } = new(1f, 0.72f, 0.82f);

	[Export]
	public Color ArcColor { get; set; } = new(1f, 0.55f, 0.75f);

	public Entity? Shooter { get; set; }

	private Vector3 _velocity;
	private float _gravity;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	/// <summary>
	/// Places the projectile at the muzzle and gives it its initial velocity.
	/// A lobbed shot leaves at <see cref="ArcLaunchAngle"/> and is pulled down by
	/// <see cref="ArcGravity"/>, so it follows a real parabola instead of a straight line.
	/// </summary>
	public void Launch(Transform3D muzzle, bool lobbed)
	{
		GlobalTransform = muzzle;

		Vector3 forward = -muzzle.Basis.Z.Normalized();

		if (lobbed)
		{
			float radians = Mathf.DegToRad(ArcLaunchAngle);
			_velocity = ((forward * Mathf.Cos(radians)) + (Vector3.Up * Mathf.Sin(radians))) * Speed;
			_gravity = ArcGravity;
		}
		else
		{
			_velocity = forward * Speed;
			_gravity = 0f;
		}

		// ponytail: a fresh material per shot, fine at this volume. Cache the two
		// materials if projectile counts ever get high enough to matter.
		if (GetNodeOrNull<CsgCombiner3D>("Heart") is { } heart)
		{
			Color tint = lobbed ? ArcColor : StraightColor;
			StandardMaterial3D material = new()
			{
				AlbedoColor = tint,
				EmissionEnabled = true,
				Emission = tint,
				EmissionEnergyMultiplier = 0.6f,
			};

			// Each CSG shape carries its own material, so tint every piece of the heart.
			foreach (Node piece in heart.GetChildren())
				if (piece is CsgPrimitive3D primitive) primitive.Set("material", material);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		// ponytail: stepped by hand on an Area3D, which has no continuous collision detection.
		// At these defaults a shot covers 0.37 units per tick against a 0.25-radius sphere, so it
		// cannot pass through the floor. Past roughly 30 u/s it can - widen the collision sphere
		// or move to a RigidBody3D with continuous_cd if Speed or ArcGravity are raised that far.
		_velocity.Y -= _gravity * (float)delta;
		GlobalPosition += _velocity * (float)delta;

		Lifetime -= (float)delta;
		if (Lifetime <= 0f) QueueFree();
	}

	/// <summary>
	/// Kisses calm monsters and never damage anything (GDD p.3, p.9).
	/// They pass through the girls and are simply consumed by robots and by the level.
	/// </summary>
	/// <param name="body">The body the kiss landed on.</param>
	private void OnBodyEntered(Node3D body)
	{
		if (body.GetParent()?.GetParent() is Entity entity)
		{
			if (entity.EntityIdentity.Type == Shooter?.EntityIdentity.Type) return;

			if (entity.IsMonster && entity.GetComponent<CalmComponent>() is { } calm)
			{
				calm.AddCalm(CalmPerHit);

				// A soft squash, so a landing kiss reads even before the monsters have reactions.
				Tween squash = body.CreateTween();
				squash.TweenProperty(body, "scale", new Vector3(1.15f, 0.9f, 1.15f), 0.06f);
				squash.TweenProperty(body, "scale", Vector3.One, 0.14f);
			}
			else
			{
				LoggerService.Debug($"KissProjectile had no effect on <{entity.EntityIdentity.ShortId}>.");
			}
		}

		QueueFree();
	}
}
