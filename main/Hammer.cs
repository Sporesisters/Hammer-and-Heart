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
	/// Meta set on a robot while its destruction plays, so nothing hits it or lets it act again.
	/// </summary>
	public const string DestroyingMeta = "destroying";

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

	private bool _awaitingNextSwing = false;

	/// <summary>
	/// Tracks which attack in the combo is currently being performed.
	/// 0 = none, 1 = first swing, 2 = second swing, 3 = finisher.
	/// </summary>
	private int _attackIndex = 0;

	/// <summary>
	/// The duration of the hammer attack before another attack can be started.
	/// </summary>
	[Export]
	private float AttackDuration { get; set; } = 0.4f;

	private const float FinisherDuration = 0.7f;
	private const float FinisherDamageMultiplier = 1.75f;
	private const float ComboGraceDuration = 0.25f;

	/// <summary>
	/// Timer used to determine when the current hammer attack has finished.
	/// </summary>
	[Export]
	private Timer? AttackTimer { get; set; }

	[Export]
	private Entity? Annabelle { get; set; }

	/// <summary>
	/// Tracks whether Elaine is attacking or waiting for the next combo input.
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
		// One swing per press: holding the button down must not swing over and over.
		if (!command.AttackJustPressed)
			return;

		if (Hitbox is null || AttackTimer is null || ComboTimer is null || SwingHammer is null)
			return;

		// If already attacking, only allow a combo input during the combo window.
		if (_isAttacking)
		{
			if (_attackIndex < 3 && _comboWindowOpen)
			{
				if (_awaitingNextSwing)
					StartSwing(_attackIndex + 1);
				else
					_comboQueued = true;
			}

			return;
		}

		StartSwing(1);
	}

	private void StartSwing(int attackIndex)
	{
		if (SwingHammer is null || AttackTimer is null || ComboTimer is null)
		{
			LoggerService.Warning("[Hammer] Cannot start swing: animation player or timer not assigned.");
			ResetCombo();
			return;
		}

		string animation;
		float duration;
		switch (attackIndex)
		{
			case 1:
				animation = "swing1";
				duration = AttackDuration;
				break;
			case 2:
				animation = "swing2";
				duration = AttackDuration;
				break;
			case 3:
				animation = "swing3";
				duration = FinisherDuration;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(attackIndex));
		}

		_isAttacking = true;
		_attackIndex = attackIndex;
		_comboQueued = false;
		_comboWindowOpen = false;
		_awaitingNextSwing = false;
		_hitTargets.Clear();

		ComboTimer.Stop();
		SwingHammer.Stop();
		SwingHammer.Play(animation);
		// Apply the new starting pose before the next rendered frame.
		SwingHammer.Advance(0);
		AttackTimer.Start(duration);
	}

	/// <summary>
	/// Processes a detected body and applies damage if it is a valid target.
	/// </summary>
	/// <param name="body">The physics body detected by the hammer hitbox.</param>
	private void OnBodyEntered(Node3D body)
	{
		if (!_isAttacking || Hitbox is not { Monitoring: true })
			return;

		if (body.GetParent()?.GetParent() is not Entity entity)
			return;

		if (entity.EntityIdentity.Type != EntityType.Enemy)
			return;

		if (!_hitTargets.Add(entity))
			return;

		if (!TryGetDamage(out float damage))
			return;

		switch (entity.EntityIdentity.Faction)
		{
			case EntityFaction.Robot:
				DamageRobot(entity, damage);
				break;

			case EntityFaction.Monster:
				DamageAnnabelle(damage);
				break;
		}
	}

	private void DamageAnnabelle(float damage)
	{
		if (Annabelle is not { } annabelle)
		{
			LoggerService.Warning("[Hammer] Cannot damage Annabelle: target not assigned.");
			return;
		}

		Stat? health = annabelle
			.GetComponent<StatsComponent>()?
			.GetStat(StatType.Health);

		if (health is null)
		{
			LoggerService.Warning("[Annabelle] Cannot take hammer damage: Health stat not found.");
			return;
		}

		health.CurrentStatValue -= damage;
		LoggerService.Info(
			$"[{annabelle.EntityIdentity.ShortId}] Health {health.CurrentStatValue}/{health.MaximumValue}.");
	}

	/// <summary>
	/// Applies hammer damage and destroys the robot when its health is depleted.
	/// </summary>
	private static void DamageRobot(Entity robot, float damage)
	{
		if (robot.HasMeta(DestroyingMeta))
			return;

		Stat? health = robot.GetComponent<StatsComponent>()?.GetStat(StatType.Health);

		if (health is null)
		{
			LoggerService.Warning($"[{robot.EntityIdentity.ShortId}] Robot has no Health stat.");
			return;
		}

		health.CurrentStatValue -= damage;
		LoggerService.Info($"[{robot.EntityIdentity.ShortId}] Health {health.CurrentStatValue}/{health.MaximumValue}.");

		if (health.CurrentStatValue <= health.MinimumValue)
			DestroyRobot(robot);
	}

	/// <summary>
	/// Removes a robot from the scene after its health is depleted.
	/// </summary>
	private static void DestroyRobot(Entity robot)
	{
		LoggerService.Info($"[{robot.EntityIdentity.ShortId}] Robot destroyed.");
		robot.SetMeta(DestroyingMeta, true);

		if (robot.GetComponent<CharacterComponent>()?.Character is not { } character)
		{
			robot.QueueFree();
			return;
		}

		character.CollisionLayer = 0;
		character.CollisionMask = 0;
		Tween destruction = robot.CreateTween();
		destruction.TweenProperty(character, "scale", Vector3.One * 0.05f, 0.2f);
		destruction.TweenCallback(Callable.From(robot.QueueFree));
	}

	/// <summary>
	/// Enables the hammer hitbox during the impact portion of the swing.
	/// </summary>
	private void StartHitWindow()
	{
		if (!_isAttacking || Hitbox is null)
			return;

		Hitbox.Monitoring = true;
		OpenComboWindow();

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
		if (Hitbox is not null)
			Hitbox.Monitoring = false;
		if (!_awaitingNextSwing)
			_comboWindowOpen = false;
	}

	/// <summary>
	/// Opens the combo window during the hit window for the first two swings.
	/// </summary>
	private void OpenComboWindow()
	{
		if (!_isAttacking || _attackIndex >= 3 || Hitbox is not { Monitoring: true } || ComboTimer is null)
			return;

		_comboWindowOpen = true;
	}

	/// <summary>
	/// Resets the combo when the grace period expires.
	/// </summary>
	private void OnComboTimerTimeout()
	{
		if (_awaitingNextSwing)
			ResetCombo();
	}

	private void OnAttackTimerTimeout()
	{
		EndHitWindow();

		switch (_attackIndex)
		{
			case 1 when _comboQueued:
				StartSwing(2);
				break;
			case 2 when _comboQueued:
				StartSwing(3);
				break;
			case 1:
			case 2:
				if (ComboTimer is null)
				{
					LoggerService.Warning("[Hammer] Cannot continue combo: combo timer not assigned.");
					ResetCombo();
					break;
				}
				_awaitingNextSwing = true;
				_comboWindowOpen = true;
				ComboTimer.Start(ComboGraceDuration);
				break;
			default:
				ResetCombo();
				break;
		}
	}

	private void ResetCombo()
	{
		_awaitingNextSwing = false;
		ComboTimer?.Stop();
		EndHitWindow();
		_isAttacking = false;
		_attackIndex = 0;
		_comboQueued = false;
		_hitTargets.Clear();
	}

	private bool TryGetDamage(out float damage)
	{
		damage = 0f;

		Stat? damageStat = Entity?
			.GetComponent<StatsComponent>()?
			.GetStat(StatType.Damage);

		if (damageStat is null)
		{
			LoggerService.Warning($"[{EntityId}] Cannot attack: Damage stat not found.");
			return false;
		}

		damage = damageStat.CurrentStatValue * (_attackIndex == 3 ? FinisherDamageMultiplier : 1f);
		return true;
	}
}