using Godot;
using System;
using Core.Utilities.Logging;
using Core.ECS;
using Core.ECS.Components;
using Core.Stats;
using Core.Inputs;

/// <summary>
/// Handles Elaine's hammer combat.
/// Receives attack input, enables the hammer hitbox for the duration
/// of the attack, and applies damage to valid enemy targets.
/// </summary>
public partial class Hammer : ComponentBase, IInputReceiver
{
	/// <summary>
	/// The <see cref="Area3D"/> used to detect entities inside the hammer's attack range.
	/// </summary>
	[Export]
	public Area3D? Hitbox { get; private set; }

	/// <summary>
	/// The amount of damage dealt by a successful hammer hit.
	/// </summary>
	[Export]
	public float Damage { get; private set; } = 10f;

	/// <summary>
	/// The duration of the hammer attack before another attack can be started.
	/// </summary>
	[Export]
	private float AttackDuration { get; set; } = 0.8f;

	/// <summary>
	/// Timer used to determine when the current hammer attack has finished.
	/// </summary>
	[Export]
	private Timer? AttackTimer { get; set; }

	/// <summary>
	/// Tracks whether Elaine is currently performing a hammer attack.
	/// Prevents new attacks from starting until the current attack finishes.
	/// </summary>
	private bool _isAttacking = false;

	/// <summary>
	/// Initializes the hammer and connects the attack timer.
	/// </summary>
	public override void _Ready()
	{
		if (Hitbox is null)
		{
			LoggerService.Warning($"<{GetType().Name}> has no Hitbox assigned.");
			return;
		}

		if (AttackTimer is null)
		{
			LoggerService.Warning($"<{GetType().Name}> has no AttackTimer assigned.");
			return;
		}

		// The hitbox starts disabled and is enabled only during an attack.
		Hitbox.Monitoring = false;

		// The timer should only fire once per attack.
		AttackTimer.OneShot = true;

		// Reset the attack state when the timer finishes.
		AttackTimer.Timeout += OnAttackTimerTimeout;
	}

	/// <summary>
	/// Receives the current input snapshot and starts a hammer attack
	/// when the attack button is pressed.
	/// </summary>
	/// <param name="command">The current input snapshot.</param>
	public async void ReceiveInput(InputCommand command)
	{
		if (_isAttacking)
			return;

		if (!command.AttackPressed)
			return;

		if (Hitbox is null || AttackTimer is null)
			return;

		// Enable the hitbox and mark the hammer as attacking.
		Hitbox.Monitoring = true;
		_isAttacking = true;

		// Wait one physics frame so the Area3D can update its overlap state.
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

		// Check all bodies currently inside the hammer's attack range.
		foreach (Node3D body in Hitbox.GetOverlappingBodies())
		{
			OnAttackHit(body);
		}

		// Start the attack timer. New attacks are blocked until it finishes.
		AttackTimer.Start(AttackDuration);
	}

	/// <summary>
	/// Processes a detected body and applies damage if it is a valid target.
	/// </summary>
	/// <param name="body">The physics body detected by the hammer hitbox.</param>
	private void OnAttackHit(Node3D body)
	{
		if (body.GetParent()?.GetParent() is not Entity entity)
			return;

		if (entity.EntityIdentity.Type != EntityType.Enemy)
			return;

		if (entity.EntityIdentity.SubType != "Spider")
			return;

		StatsComponent? spiderStats = entity.GetComponent<StatsComponent>();

		if (spiderStats is null)
		{
			LoggerService.Warning("Spider has no StatsComponent!");
			return;
		}

		Stat? health = spiderStats.GetStat(StatType.Health);

		if (health is null)
		{
			LoggerService.Warning("Spider has no Health stat!");
			return;
		}

		// Apply hammer damage through the stat's public setter.
		health.CurrentStatValue -= Damage;

		LoggerService.Warning(
			$"Spider health after hit: {health.CurrentStatValue}"
		);
	}

	/// <summary>
	/// Ends the current attack and re-enables the hammer for the next attack.
	/// </summary>
	private void OnAttackTimerTimeout()
	{
		if (Hitbox is null)
			return;

		Hitbox.Monitoring = false;
		_isAttacking = false;
	}
}