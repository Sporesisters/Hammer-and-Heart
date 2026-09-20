using Core.Inputs;
using Core.Utilities.Logging;
using Godot;

namespace Core.ECS.Components;

[GlobalClass]
public partial class KissAttackComponent : ComponentBase, IInputReceiver
{
	[Export]
	public PackedScene? ProjectileScene { get; set; }

	[Export]
	public float Cooldown { get; set; } = 0.5f;

	/// <summary>
	/// How far off a monster can be for a kiss to curve onto it. Zero disables aim assist.
	/// </summary>
	[Export(PropertyHint.Range, "0,90,0.5")]
	public float AimAssistAngle { get; set; } = 18f;

	/// <summary>
	/// How far away aim assist still looks for a monster.
	/// </summary>
	[Export]
	public float AimAssistRange { get; set; } = 15f;

	private float _cooldownLeft;
	private bool _firePressed;
	private bool _lobNextShot;
	private Vector3? _aimPoint;

	public override void _PhysicsProcess(double delta)
	{
		_cooldownLeft -= (float)delta;

		if (!_firePressed || _cooldownLeft > 0f) return;

		var character = Entity?.GetComponent<CharacterComponent>()?.Character;
		if (character is null || ProjectileScene is null) return;

		Node instance = ProjectileScene.Instantiate();

		if (instance is not KissProjectile projectile)
		{
			// Freed and forgotten: firing is held down, so this would otherwise leak a node per tick.
			LoggerService.Error($"[{EntityId}] ProjectileScene is not a KissProjectile. No kiss fired.");
			instance.QueueFree();
			ProjectileScene = null;
			return;
		}

		_cooldownLeft = Cooldown;
		projectile.Shooter = Entity;

		Node spawnParent = GetTree().CurrentScene ?? GetTree().Root;
		spawnParent.AddChild(projectile);

		// Orthonormalized: a hit-reaction tween scales the body to 1.4x for a moment, which would
		// otherwise spawn the shot further out and launch it faster.
		Transform3D muzzle = AimedMuzzle(character);
		(Transform3D assisted, float? targetDistance) = NudgeTowardsMonster(muzzle);

		// Shots alternate straight, lobbed, straight, ... so one button covers both.
		projectile.Launch(assisted, _lobNextShot, targetDistance);
		_lobNextShot = !_lobNextShot;
	}

	/// <summary>
	/// The muzzle transform, facing the player's aim point when there is one. Aiming straight at
	/// the point beats the body's own rotation, which only catches up over a few frames.
	/// </summary>
	/// <param name="character">The body firing the kiss.</param>
	/// <returns>The muzzle to fire from.</returns>
	private Transform3D AimedMuzzle(CharacterBody3D character)
	{
		Transform3D muzzle = character.GlobalTransform.Orthonormalized().TranslatedLocal(new Vector3(0, 0.5f, -0.3f));

		if (_aimPoint is not { } point) return muzzle;

		Vector3 toPoint = point - muzzle.Origin;
		Vector2 flat = new(toPoint.X, toPoint.Z);

		if (flat.Length() < 0.1f) return muzzle;

		return new Transform3D(Basis.FromEuler(new Vector3(0f, Mathf.Atan2(-flat.X, -flat.Y), 0f)), muzzle.Origin);
	}

	/// <summary>
	/// Turns the muzzle towards the monster closest to the centre of the shot, within
	/// <see cref="AimAssistAngle"/>, so kisses land without pixel-perfect aiming.
	/// Calmed monsters and anything that is not a monster are ignored.
	/// </summary>
	/// <param name="muzzle">Where the kiss would be fired without assist.</param>
	/// <returns>The muzzle aimed at the chosen monster, and how far away it is.</returns>
	private (Transform3D Muzzle, float? TargetDistance) NudgeTowardsMonster(Transform3D muzzle)
	{
		if (AimAssistAngle <= 0f) return (muzzle, null);

		Vector3 forward = -muzzle.Basis.Z;
		Vector2 aim = new Vector2(forward.X, forward.Z).Normalized();

		Vector3? bestPosition = null;
		float bestAngle = Mathf.DegToRad(AimAssistAngle);

		foreach (Node node in GetTree().GetNodesInGroup(CalmComponent.MonsterGroup))
		{
			if (node is not CalmComponent { IsCalmed: false } calm) continue;
			if (calm.Entity is not { IsMonster: true } monster) continue;
			if (monster.GetComponent<CharacterComponent>()?.Character is not { } target) continue;

			Vector3 toTarget = target.GlobalPosition - muzzle.Origin;
			Vector2 flat = new(toTarget.X, toTarget.Z);

			if (flat.Length() > AimAssistRange || flat.Length() < 0.01f) continue;

			float angle = Mathf.Abs(aim.AngleTo(flat.Normalized()));
			if (angle > bestAngle) continue;

			bestAngle = angle;
			bestPosition = target.GlobalPosition;
		}

		if (bestPosition is not { } aimPoint) return (muzzle, null);

		Vector3 toAimPoint = aimPoint - muzzle.Origin;
		float yaw = Mathf.Atan2(-toAimPoint.X, -toAimPoint.Z);
		float distance = new Vector2(toAimPoint.X, toAimPoint.Z).Length();

		return (new Transform3D(Basis.FromEuler(new Vector3(0f, yaw, 0f)), muzzle.Origin), distance);
	}

	public void ReceiveInput(InputCommand command)
	{
		_firePressed = command.AttackPressed;
		_aimPoint = command.AimPoint;
	}
}
