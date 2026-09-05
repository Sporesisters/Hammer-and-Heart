using Core.ECS;
using Core.ECS.Components;
using Core.Stats;
using Core.Utilities.Logging;
using Godot;

public partial class KissProjectile : Area3D
{
	[Export]
	public float Speed { get; set; } = 20f;

	[Export]
	public float Damage { get; set; } = 10f;

	[Export]
	public float Lifetime { get; set; } = 3f;

	/// <summary>Downward pull applied to a lobbed shot. Straight shots ignore it.</summary>
	[Export]
	public float ArcGravity { get; set; } = 35f;

	/// <summary>Degrees above the horizon a lobbed shot launches at.</summary>
	[Export]
	public float ArcLaunchAngle { get; set; } = 30f;

	[Export]
	public Color StraightColor { get; set; } = new(1f, 0.35f, 0.65f);

	[Export]
	public Color ArcColor { get; set; } = new(0.55f, 0.45f, 1f);

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
		if (GetNodeOrNull<MeshInstance3D>("MeshInstance3D") is { } mesh)
		{
			Color tint = lobbed ? ArcColor : StraightColor;
			mesh.MaterialOverride = new StandardMaterial3D
			{
				AlbedoColor = tint,
				EmissionEnabled = true,
				Emission = tint,
				EmissionEnergyMultiplier = 0.6f,
			};
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

	private void OnBodyEntered(Node3D body)
	{
		if (body.GetParent()?.GetParent() is Entity entity)
		{
			if (entity.EntityIdentity.Type == Shooter?.EntityIdentity.Type) return;

			var health = entity.GetComponent<StatsComponent>()?.GetStat(StatType.Health);

			if (health is not null)
			{
				health.CurrentStatValue -= Damage;
				LoggerService.Info($"KissProjectile hit <{entity.EntityIdentity.ShortId}> for {Damage} damage ({health.CurrentStatValue}/{health.MaximumValue} HP left).");

				Tween punch = body.CreateTween();
				punch.TweenProperty(body, "scale", Vector3.One * 1.4f, 0.05f);
				punch.TweenProperty(body, "scale", Vector3.One, 0.12f);

				if (health.CurrentStatValue <= health.MinimumValue)
				{
					LoggerService.Info($"<{entity.EntityIdentity.ShortId}> defeated by kiss.");
					punch.TweenCallback(Callable.From(entity.QueueFree));
				}
			}
		}

		QueueFree();
	}
}
