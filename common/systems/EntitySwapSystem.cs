using System.Collections.Generic;
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

	private Entity? _currentEntity;
	private readonly List<Entity> _controllableEntities = [];
	private readonly Dictionary<Entity, (Vector3 Target, float TimeLeft)> _swapTransitions = [];

	public override void _PhysicsProcess(double delta)
	{
		if (_currentEntity?.GetComponent<CharacterComponent>()?.Character is not { } leader) return;

		if (Input.GetVector("move_left", "move_right", "move_up", "move_down") != Vector2.Zero)
			_swapTransitions.Clear();

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

			entity.GetComponent<MovementComponent>()?.ReceiveInput(new InputCommand(direction, false, false));
		}
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

		if (_currentEntity is null) SwapTo(entity);
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

		_currentEntity = newEntity;
		InputHandler.InputTarget = newEntity;

		LoggerService.Info($"Swapped control to entity (ID: {newEntity.EntityIdentity.ShortId})");
	}

	/// <summary>
	/// Handles <see cref="PlayerSwapEvent"/> to cycle control to the next registered entity.
	/// Loops back to the first entity if the current is the last in the list.
	/// </summary>
	/// <param name="_">The event payload (unused).</param>
	private void OnPlayerSwap(PlayerSwapEvent _)
	{
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
