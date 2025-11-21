using Core.ECS.Events;
using Core.ECS;
using Core.ECS.Components;
using System.Collections.Generic;
using Godot;

namespace Core.Systems;

/// <summary>
/// System responsible for managing player control swapping between entities in the ECS framework.
///
/// This system keeps track of all registered entities that can be controlled by the player
/// and listens for <see cref="PlayerSwapEvent"/> to cycle control between them.
/// </summary>
public class EntitySwapSystem : IEventSystem<PlayerSwapEvent>
{
	/// <inheritdoc/>
	public EntityWorld? World { get; set; }

	private int _internalIndex = 0;

	/// <inheritdoc/>
	public void OnEvent(PlayerSwapEvent @event)
	{
		if (World is null) return;

		GD.Print("Switching");

		IEnumerable<Entity> targets = World.Query<PlayerTagComponent, EntitySwapComponent>();
		List<Entity> targetList = [.. targets];

		int totalTargets = targetList.Count;

		if (totalTargets is 0) return;

		foreach (Entity entity in targetList)
			entity.GetComponent<PlayerTagComponent>()?.IsDisabled = false;

		_internalIndex++;

		if (_internalIndex >= totalTargets)
			_internalIndex = 0;

		Entity selected = targetList[_internalIndex];
		selected.GetComponent<PlayerTagComponent>()?.IsDisabled = true;
	}
}
