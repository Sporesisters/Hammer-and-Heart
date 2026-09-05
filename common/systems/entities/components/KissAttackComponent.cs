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

		projectile.Launch(muzzle, _lobNextShot);
		_lobNextShot = !_lobNextShot;
	}

	public void ReceiveInput(InputCommand command)
	{
		_firePressed = command.AttackPressed;
	}
}
