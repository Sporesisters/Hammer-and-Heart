using System.Linq;
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
	private EntityType _shooterType = EntityType.Unknown;
	private bool _consumed;

	public override void _Ready()
	{
		BodyEntered += OnBodyEntered;
	}

	/// <summary>
	/// Places the projectile at the muzzle and gives it its initial velocity.
	/// A lobbed shot leaves at an angle and is pulled down by <see cref="ArcGravity"/>, so it
	/// follows a real parabola instead of a straight line.
	/// </summary>
	/// <param name="muzzle">Where the shot starts and which way it faces.</param>
	/// <param name="lobbed">Whether this shot arcs instead of flying straight.</param>
	/// <param name="targetDistance">
	/// Distance to the monster being aimed at, used to pick the launch angle so the arc lands on it.
	/// Without it the shot uses <see cref="ArcLaunchAngle"/> and lands at its own fixed range.
	/// </param>
	public void Launch(Transform3D muzzle, bool lobbed, float? targetDistance = null)
	{
		GlobalTransform = muzzle;

		// Remembered now: the shooter can be freed while the kiss is still in the air.
		_shooterType = Shooter?.EntityIdentity.Type ?? EntityType.Unknown;

		Vector3 forward = -muzzle.Basis.Z.Normalized();

		if (lobbed)
		{
			float radians = Mathf.DegToRad(LaunchAngleFor(targetDistance));
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

	/// <summary>
	/// The angle a lobbed shot leaves at. Solved from the range equation so the arc lands on the
	/// target; falls back to <see cref="ArcLaunchAngle"/> when there is no target or it is out of
	/// reach, which would otherwise send every second kiss sailing over a nearby monster.
	/// </summary>
	/// <param name="targetDistance">Horizontal distance to the target, if any.</param>
	/// <returns>The launch angle in degrees.</returns>
	private float LaunchAngleFor(float? targetDistance)
	{
		if (targetDistance is not { } distance || Speed <= 0f || ArcGravity <= 0f) return ArcLaunchAngle;

		float sine = ArcGravity * distance / (Speed * Speed);
		if (sine is > 1f or < 0f) return ArcLaunchAngle;

		return Mathf.RadToDeg(0.5f * Mathf.Asin(sine));
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
		// QueueFree only takes effect at the end of the frame, so without this a kiss landing
		// between two monsters would calm both of them.
		if (_consumed) return;

		if (body.GetParent()?.GetParent() is Entity entity)
		{
			if (entity.EntityIdentity.Type == _shooterType) return;

			if (entity.IsMonster && entity.GetComponent<CalmComponent>() is { } calm)
			{
				calm.AddCalm(CalmPerHit);

				Squash(body);
			}
			else
			{
				LoggerService.Debug($"KissProjectile had no effect on <{entity.EntityIdentity.ShortId}>.");
			}
		}

		_consumed = true;
		SetDeferred(Area3D.PropertyName.Monitoring, false);
		QueueFree();
	}

	/// <summary>
	/// A soft squash on the monster's mesh, so a landing kiss reads even before the monsters have
	/// their own reactions. The mesh is scaled and not the body, which would also scale its
	/// collision shape and anything else parented to it.
	/// </summary>
	/// <param name="body">The body that was hit.</param>
	private static void Squash(Node3D body)
	{
		if (body.GetChildren().OfType<VisualInstance3D>().FirstOrDefault() is not { } mesh) return;

		Tween squash = mesh.CreateTween();
		squash.TweenProperty(mesh, "scale", new Vector3(1.15f, 0.9f, 1.15f), 0.06f);
		squash.TweenProperty(mesh, "scale", Vector3.One, 0.14f);
	}
}
