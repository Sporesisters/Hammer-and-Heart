using Core.ECS.Events;
using Core.ECS;
using Core.ECS.Components;
using System.Collections.Generic;
using Godot;
using System.Linq;

namespace Core.Systems;

/// <summary>
/// System responsible for managing player control swapping between entities in the ECS framework.
///
/// This system keeps track of all registered entities that can be controlled by the player
/// and listens for <see cref="PlayerSwapEvent"/> to cycle control between them.
/// </summary>
public class EntitySwapSystem : IProcessSystem
{
	public EntityWorld? World { get; set; }

	public void Update(double delta)
	{
		if (World is null) return;

		// Only one entity will have ControlledByPlayerComponent
		foreach (var current in World.Query<ControlledByPlayerTag>())
		{
			var swap = current.GetComponent<EntitySwapComponent>();
			if (swap == null || !swap.ShouldSwap)
				continue;

			SwapToOther(current);
			swap.ShouldSwap = false;
		}
	}

	private void SwapToOther(Entity current)
	{
		var players = World!.Query<PlayerTag>().ToList();
		var next = players.First(e => e != current);

		current.RemoveComponent<ControlledByPlayerTag>();
		next.AddComponent(new ControlledByPlayerTag());
	}
}