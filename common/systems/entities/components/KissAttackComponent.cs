using System.Linq;
using Core.Inputs;
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

	public override void _PhysicsProcess(double delta)
	{
		_cooldownLeft -= (float)delta;

		if (!_firePressed || _cooldownLeft > 0f) return;

		var character = Entity?.GetComponent<CharacterComponent>()?.Character;
		if (character is null || ProjectileScene?.Instantiate() is not KissProjectile projectile) return;

		_cooldownLeft = Cooldown;
		projectile.Shooter = Entity;

		Node spawnParent = GetTree().CurrentScene ?? GetTree().Root;
		spawnParent.AddChild(projectile);

		// Orthonormalized: a hit-reaction tween scales the body to 1.4x for a moment, which would
		// otherwise spawn the shot further out and launch it faster.
		// Shots alternate straight, lobbed, straight, ... so one button covers both.
		Transform3D muzzle = character.GlobalTransform.Orthonormalized().TranslatedLocal(new Vector3(0, 0.5f, -0.3f));

		projectile.Launch(NudgeTowardsMonster(muzzle), _lobNextShot);
		_lobNextShot = !_lobNextShot;
	}

	/// <summary>
	/// Turns the muzzle towards the closest monster inside <see cref="AimAssistAngle"/>, so kisses
	/// land without pixel-perfect aiming. Calmed monsters are ignored.
	/// </summary>
	/// <param name="muzzle">Where the kiss would be fired without assist.</param>
	/// <returns>The muzzle, aimed at the chosen monster when there is one.</returns>
	private Transform3D NudgeTowardsMonster(Transform3D muzzle)
	{
		if (AimAssistAngle <= 0f) return muzzle;

		Vector3 forward = -muzzle.Basis.Z;
		Vector2 aim = new Vector2(forward.X, forward.Z).Normalized();

		Vector3? bestPosition = null;
		float bestAngle = Mathf.DegToRad(AimAssistAngle);

		foreach (Node node in GetTree().GetNodesInGroup(CalmComponent.MonsterGroup))
		{
			if (node is not CalmComponent { IsCalmed: false } calm) continue;
			if (calm.Entity?.GetComponent<CharacterComponent>()?.Character is not { } target) continue;

			Vector3 toTarget = target.GlobalPosition - muzzle.Origin;
			Vector2 flat = new(toTarget.X, toTarget.Z);

			if (flat.Length() > AimAssistRange || flat.Length() < 0.01f) continue;

			float angle = Mathf.Abs(aim.AngleTo(flat.Normalized()));
			if (angle > bestAngle) continue;

			bestAngle = angle;
			bestPosition = target.GlobalPosition;
		}

		if (bestPosition is not { } aimPoint) return muzzle;

		Vector3 toAimPoint = aimPoint - muzzle.Origin;
		float yaw = Mathf.Atan2(-toAimPoint.X, -toAimPoint.Z);

		return new Transform3D(Basis.FromEuler(new Vector3(0f, yaw, 0f)), muzzle.Origin);
	}

	public void ReceiveInput(InputCommand command)
	{
		_firePressed = command.AttackPressed;
	}
}
