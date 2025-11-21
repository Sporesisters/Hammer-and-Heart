using System.Collections.Generic;
using Godot;
using Core.Utilities.Logging;
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
	// public InputHandler? InputHandler { get; set; }

	// private Entity? _currentEntity;
	// private readonly List<Entity> _controllableEntities = new();

	/// <summary>
	/// Registers an entity as controllable and subscribes it to swap events.
	/// The first entity registered becomes the initial active entity if none is set.
	/// </summary>
	/// <param name="entity">The entity to register for player control swapping.</param>
	// public void RegisterEntity(Entity entity)
	// {
	// 	if (!_controllableEntities.Contains(entity))
	// 	{
	// 		_controllableEntities.Add(entity);
	// 		entity.EventBus.AddListener<PlayerSwapEvent>(OnPlayerSwap);
	// 		LoggerService.Info($"<{entity.EntityIdentity.ShortId}> Registered entity for control swapping.");
	// 	}

	// 	if (_currentEntity is null) SwapTo(entity);
	// }

	/// <summary>
	/// Switches control to the specified entity by updating the <see cref="InputHandler.InputTarget"/>.
	/// Logs the swap for debugging and tracking.
	/// </summary>
	/// <param name="newEntity">The entity to assign player control to.</param>
	// public void SwapTo(Entity newEntity)
	// {
	// 	if (newEntity == _currentEntity || InputHandler is null) return;

	// 	_currentEntity = newEntity;
	// 	InputHandler.InputTarget = newEntity;

	// 	LoggerService.Info($"Swapped control to entity (ID: {newEntity.EntityIdentity.ShortId})");
	// }

	/// <summary>
	/// Handles <see cref="PlayerSwapEvent"/> to cycle control to the next registered entity.
	/// Loops back to the first entity if the current is the last in the list.
	/// </summary>
	/// <param name="_">The event payload (unused).</param>
	// private void OnPlayerSwap(PlayerSwapEvent _)
	// {
	// 	if (_controllableEntities.Count == 0)
	// 	{
	// 		LoggerService.Warning("No controllable entities to swap to.");
	// 		return;
	// 	}

	// 	if (_currentEntity is null)
	// 	{
	// 		SwapTo(_controllableEntities[0]);
	// 		return;
	// 	}

	// 	int currentIndex = _controllableEntities.IndexOf(_currentEntity);
	// 	int nextIndex = (currentIndex + 1) % _controllableEntities.Count;

	// 	SwapTo(_controllableEntities[nextIndex]);
	// }
}
