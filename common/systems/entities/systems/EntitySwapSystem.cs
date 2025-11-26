using Core.ECS;
using Core.ECS.Components;
using Core.ECS.Tags;
using System.Collections.Generic;

namespace Core.Systems;

/// <summary>
/// System responsible for handling player-controlled entity swapping.
/// Entities with <see cref="EntitySwapComponent"/> can trigger a swap, which transfers
/// control from the current player-controlled entity to another entity with <see cref="PlayerTag"/>.
/// </summary>
public class EntitySwapSystem : IProcessSystem
{
	/// <inheritdoc/>
	public EntityWorld? World { get; set; }

	/// <inheritdoc/>
	public void Update(double delta)
	{
		if (World is null) return;

		foreach (Entity current in World.Query<PlayerControlledTag>())
		{
			var swapComponent = current.GetComponent<EntitySwapComponent>();

			if (swapComponent is null)
				continue;

			if (swapComponent.ConsumeSwap())
				SwapToOther(current);
		}
	}

	/// <summary>
	/// Transfers player control from the current entity to the next available player entity.
	/// If there is only one entity, no swap occurs.
	/// </summary>
	/// <param name="currentEntity">The entity currently under player control.</param>
	private void SwapToOther(Entity currentEntity)
	{
		if (World is null) return;

		List<Entity> players = [.. World.Query<PlayerTag>()];

		if (players.Count <= 1) return;

		int index = players.IndexOf(currentEntity);

		if (index < 0) return;

		int nextIndex = (index + 1) % players.Count;
		Entity nextEntity = players[nextIndex];

		var tag = currentEntity.GetComponent<PlayerControlledTag>();

		if (tag is null) return;

		currentEntity.RemoveComponent<PlayerControlledTag>();
		nextEntity.AddComponent(tag);
	}
}
