using Godot;
using System.Collections.Generic;
using Core.Systems.FSM;
using Core.Systems.FSM.States;
using Core.ECS;
using Core.ECS.Components;
using Core.Stats;
using Core.Stats.Types;
using Core.Inputs.Internals;
using Core.Systems;

public partial class TestScene : Node
{
	[Export] private Godot.Collections.Array<Entity> _entities = [];
	[Export] private EntitySwapSystem entitySwapSystem = null!;
	[Export] private InputHandler _inputHandler = null!;

	private readonly Dictionary<StatType, Stat> _entityStatsMapping = new()
	{
		[StatType.Health] = new ScaledStat(100, 0, 100),
		[StatType.Damage] = new ScaledStat(50, 0, 50),
		[StatType.MoveSpeed] = new LockedStat(30, 0, 30),
	};

	public override void _Ready()
	{
		float spawnHeight = 5f;
		float spacing = 2f;
		int index = 0;

		foreach (Entity entity in _entities)
		{
			entity.Initialize(new EntityIdentitySpec());

			var statsComponent = entity.GetComponent<StatsComponent>();
			statsComponent?.AddStats(_entityStatsMapping);

			var characterComponent = entity.GetComponent<CharacterComponent>();

			if (characterComponent is { Character: { } character })
			{
				float xOffset = index % 5 * spacing;
				float zOffset = index / 5 * spacing;
				character.Position = new Vector3(xOffset, spawnHeight, zOffset);
			}

			var fsmComponent = entity.GetComponent<FSMComponent>();
			if (fsmComponent != null)
			{
				var initialState = new PlayerIdleState(fsmComponent);
				fsmComponent.Initialize(initialState);
			}

			entitySwapSystem.RegisterEntity(entity);
			index++;
		}

		entitySwapSystem.InputHandler = _inputHandler;
	}
}
