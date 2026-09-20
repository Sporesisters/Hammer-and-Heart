using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Core.Inputs;
using Core.Inputs.Internals;
using Core.Utilities.Logging;
using Core.ECS.Components;
using Core.ECS.Events;
using Core.ECS;

namespace Core.Systems;

[GlobalClass]
/// <summary>
/// System responsible for managing player control swapping between entities in the ECS framework.
///
/// This system keeps track of all registered entities that can be controlled by the player
/// and listens for <see cref="PlayerSwapEvent"/> to cycle control between them.
///
/// It also owns the active <see cref="Inputs.ControlScheme"/>, which can be toggled at runtime
/// with the <c>toggle_control_scheme</c> action to compare both schemes while playtesting.
/// </summary>
public partial class EntitySwapSystem : Node
{
	/// <summary>
	/// The <see cref="InputHandler"/> used to dispatch input to the currently active entity.
	/// </summary>
	public InputHandler? InputHandler { get; set; }

	[Export]
	public float FollowDistance { get; set; } = 2f;

	[Export]
	public float SwapTransitionTime { get; set; } = 2f;

	/// <summary>
	/// The control scheme in use. Change it in the inspector for the starting scheme,
	/// or at runtime with <see cref="SetControlScheme"/>.
	/// </summary>
	[Export]
	public ControlScheme ControlScheme { get; private set; } = ControlScheme.Switching;

	/// <summary>
	/// The entity that leads in <see cref="ControlScheme.TwoHeadedUnit"/> (Elaine, per GDD p.10).
	/// </summary>
	[Export]
	public Entity? UnitLeader { get; set; }

	/// <summary>
	/// Raised after the control scheme changes.
	/// </summary>
	public event Action<ControlScheme>? ControlSchemeChanged;

	private const string TOGGLE_CONTROL_SCHEME = "toggle_control_scheme";

	private Entity? _currentEntity;
	private readonly List<Entity> _controllableEntities = [];
	private readonly Dictionary<Entity, (Vector3 Target, float TimeLeft)> _swapTransitions = [];

	public override void _PhysicsProcess(double delta)
	{
		if (_currentEntity?.GetComponent<CharacterComponent>()?.Character is not { } leader) return;

		if (Input.GetVector("move_left", "move_right", "move_up", "move_down") != Vector2.Zero)
			_swapTransitions.Clear();

		// In the two-headed unit the follower is not the input target, so the kiss button reaches
		// her through the command below as her attack.
		InputCommand playerInput = InputHandler?.CollectInput() ?? InputCommand.Empty;
		bool followerAttack = ControlScheme is ControlScheme.TwoHeadedUnit && playerInput.KissPressed;

		foreach (Entity entity in _controllableEntities)
		{
			if (entity.GetComponent<CharacterComponent>()?.Character is not { } character) continue;

			Vector2 direction;

			if (_swapTransitions.TryGetValue(entity, out (Vector3 Target, float TimeLeft) transition))
			{
				Vector3 toTarget = transition.Target - character.GlobalPosition;
				direction = new Vector2(toTarget.X, toTarget.Z);
				float timeLeft = transition.TimeLeft - (float)delta;

				if (direction.Length() < 0.5f || timeLeft <= 0f)
				{
					_swapTransitions.Remove(entity);
					direction = Vector2.Zero;
				}
				else
				{
					_swapTransitions[entity] = (transition.Target, timeLeft);

					if (direction.Length() > 1.2f)
						direction = direction.Normalized() + new Vector2(-direction.Y, direction.X).Normalized() * 0.5f;
				}
			}
			else if (entity == _currentEntity)
			{
				continue;
			}
			else
			{
				Vector3 toLeader = leader.GlobalPosition - character.GlobalPosition;
				direction = new Vector2(toLeader.X, toLeader.Z);
				float distance = direction.Length();

				if (distance < 1f)
					direction = direction == Vector2.Zero ? Vector2.One : -direction;
				else if (distance <= FollowDistance)
					direction = Vector2.Zero;
			}

			// In the two-headed unit the follower also aims at the player's aim point, so her
			// kiss goes where they are pointing instead of where she happens to face.
			Vector3? followerAim = followerAttack ? playerInput.AimPoint : null;
			InputCommand command = new(direction, followerAttack && entity != _currentEntity, false, false, followerAim);

			// The entity being walked into position is still the player's, so only steer it.
			// Everyone else gets the full command, which also clears any attack input they were
			// holding when control moved away from them.
			if (entity == _currentEntity)
			{
				entity.GetComponent<MovementComponent>()?.ReceiveInput(command);
				continue;
			}

			foreach (IInputReceiver receiver in entity.GetAllComponents().Values.OfType<IInputReceiver>())
				receiver.ReceiveInput(command);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!@event.IsActionPressed(TOGGLE_CONTROL_SCHEME)) return;

		SetControlScheme(ControlScheme is ControlScheme.Switching ? ControlScheme.TwoHeadedUnit : ControlScheme.Switching);
		GetViewport().SetInputAsHandled();
	}

	/// <summary>
	/// Changes the active control scheme. Entering <see cref="ControlScheme.TwoHeadedUnit"/>
	/// hands control to <see cref="UnitLeader"/> so she walks to the front.
	/// </summary>
	/// <param name="scheme">The scheme to use.</param>
	public void SetControlScheme(ControlScheme scheme)
	{
		if (scheme == ControlScheme) return;

		ControlScheme = scheme;

		if (scheme is ControlScheme.TwoHeadedUnit && UnitLeader is not null)
			SwapTo(UnitLeader);

		LoggerService.Info($"Control scheme changed to <{scheme}>.");
		ControlSchemeChanged?.Invoke(scheme);
	}

	/// <summary>
	/// Registers an entity as controllable and subscribes it to swap events.
	/// The first entity registered becomes the initial active entity if none is set.
	/// </summary>
	/// <param name="entity">The entity to register for player control swapping.</param>
	public void RegisterEntity(Entity entity)
	{
		if (!_controllableEntities.Contains(entity))
		{
			_controllableEntities.Add(entity);
			entity.EventBus.AddListener<PlayerSwapEvent>(OnPlayerSwap);
			LoggerService.Info($"<{entity.EntityIdentity.ShortId}> Registered entity for control swapping.");
		}

		if (_currentEntity is null || (ControlScheme is ControlScheme.TwoHeadedUnit && entity == UnitLeader))
			SetActiveEntity(entity);
	}

	/// <summary>
	/// Switches control to the specified entity by updating the <see cref="InputHandler.InputTarget"/>.
	/// Logs the swap for debugging and tracking.
	/// </summary>
	/// <param name="newEntity">The entity to assign player control to.</param>
	public void SwapTo(Entity newEntity)
	{
		if (newEntity == _currentEntity || InputHandler is null) return;

		if (_currentEntity?.GetComponent<CharacterComponent>()?.Character is { } oldCharacter
			&& newEntity.GetComponent<CharacterComponent>()?.Character is { } newCharacter)
		{
			_swapTransitions[newEntity] = (oldCharacter.GlobalPosition, SwapTransitionTime);
			_swapTransitions[_currentEntity] = (newCharacter.GlobalPosition, SwapTransitionTime);
		}

		SetActiveEntity(newEntity);
	}

	/// <summary>
	/// Gives control to an entity immediately, without the position swap walk.
	/// </summary>
	/// <param name="newEntity">The entity to assign player control to.</param>
	private void SetActiveEntity(Entity newEntity)
	{
		_currentEntity = newEntity;
		if (InputHandler is not null) InputHandler.InputTarget = newEntity;

		LoggerService.Info($"Swapped control to entity (ID: {newEntity.EntityIdentity.ShortId})");
	}

	/// <summary>
	/// Handles <see cref="PlayerSwapEvent"/> to cycle control to the next registered entity.
	/// Loops back to the first entity if the current is the last in the list.
	/// </summary>
	/// <param name="_">The event payload (unused).</param>
	private void OnPlayerSwap(PlayerSwapEvent _)
	{
		if (ControlScheme is ControlScheme.TwoHeadedUnit) return;

		if (_controllableEntities.Count is 0)
		{
			LoggerService.Warning("No controllable entities to swap to.");
			return;
		}

		if (_currentEntity is null)
		{
			SwapTo(_controllableEntities[0]);
			return;
		}

		int currentIndex = _controllableEntities.IndexOf(_currentEntity);
		int nextIndex = (currentIndex + 1) % _controllableEntities.Count;

		SwapTo(_controllableEntities[nextIndex]);
	}
}
