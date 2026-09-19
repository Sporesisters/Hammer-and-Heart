using Godot;
using System;
using Core.Utilities.Logging;
using Core.ECS;
using Core.ECS.Components;
using Core.Stats;
using Core.Inputs;
using System.Collections.Generic;

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
	/// The <see cref="AnimationPlayer"/> used Animate the Hammer.
	/// </summary>
	[Export]
	public AnimationPlayer? SwingHammer { get; private set; }

	/// <summary>
	/// The amount of damage dealt by a successful hammer hit.
	/// </summary>
	[Export]
	public float Damage { get; private set; } = 10f;

	[Export]
	private Timer? ComboTimer { get; set; }
	/// <summary>
	/// Tracks whether the player can queue the next attack in the combo.
	/// </summary>
	private bool _comboWindowOpen = false;

	/// <summary>
	/// Tracks whether the next attack has been queued.
	/// </summary>
	private bool _comboQueued = false;

	/// <summary>
	/// Tracks which attack in the combo is currently being performed.
	/// 0 = none, 1 = first swing, 2 = second swing.
	/// </summary>
	private int _attackIndex = 0;

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
	private readonly HashSet<Entity> _hitTargets = [];
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

		if (ComboTimer is null)
		{
			LoggerService.Warning($"<{GetType().Name}> has no ComboTimer assigned.");
			return;
		}

		ComboTimer.OneShot = true;
		ComboTimer.Timeout += OnComboTimerTimeout;

		// The hitbox starts disabled and is enabled only during an attack.
		Hitbox.Monitoring = false;

		Hitbox.BodyEntered += OnBodyEntered;
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
	public void ReceiveInput(InputCommand command)
	{
		if (!command.AttackPressed)
			return;

		if (Hitbox is null || AttackTimer is null || SwingHammer is null)
			return;

		// If already attacking, only allow a combo input during the combo window.
		if (_isAttacking)
		{
			if (_attackIndex == 1 && _comboWindowOpen)
			{
				_comboQueued = true;
			}

			return;
		}

		// Start the first attack.
		_isAttacking = true;
		_attackIndex = 1;
		_comboQueued = false;
		_hitTargets.Clear();

		SwingHammer.Play("swing1");
		AttackTimer.Start(AttackDuration);
	}

	/// <summary>
	/// Processes a detected body and applies damage if it is a valid target.
	/// </summary>
	/// <param name="body">The physics body detected by the hammer hitbox.</param>
	private void OnBodyEntered(Node3D body)
	{
		if (!_isAttacking)
			return;

		if (body.GetParent()?.GetParent() is not Entity entity)
			return;

		if (entity.EntityIdentity.Type != EntityType.Enemy)
			return;

		if (entity.EntityIdentity.SubType != "Spider")
			return;
		if (!_hitTargets.Add(entity))
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
	/// Enables the hammer hitbox during the impact portion of the swing.
	/// </summary>
	private void StartHitWindow()
	{
		if (Hitbox is null)
			return;

		Hitbox.Monitoring = true;

		// Catch targets that are already inside the hitbox.
		foreach (Node3D body in Hitbox.GetOverlappingBodies())
		{
			OnBodyEntered(body);
		}
	}

	/// <summary>
	/// Disables the hammer hitbox after the impact portion of the swing.
	/// </summary>
	private void EndHitWindow()
	{
		if (Hitbox is null)
			return;

		Hitbox.Monitoring = false;
	}

	/// <summary>
	/// Opens the combo window and allows the player to queue the second swing.
	/// Called by the first swing animation.
	/// </summary>
	private void OpenComboWindow()
	{
		if (_attackIndex != 1 || ComboTimer is null)
			return;

		_comboWindowOpen = true;
		ComboTimer.Start(0.5f);
	}

	/// <summary>
	/// Closes the combo window when its duration expires.
	/// </summary>
	private void OnComboTimerTimeout()
	{
		_comboWindowOpen = false;
	}

	private void OnAttackTimerTimeout()
	{
		if (Hitbox is not null)
			Hitbox.Monitoring = false;

		if (_attackIndex == 1 && _comboQueued)
		{
			_attackIndex = 2;
			_comboQueued = false;
			_comboWindowOpen = false;
			_hitTargets.Clear();

			SwingHammer?.Play("swing2");
			AttackTimer?.Start(AttackDuration);

			return;
		}

		_isAttacking = false;
		_attackIndex = 0;
		_comboQueued = false;
		_comboWindowOpen = false;
	}
}