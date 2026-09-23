using Godot;
using System.Collections.Generic;
using Core.ECS;
using Core.ECS.Components;
using Core.Stats;
using Core.Stats.Types;
using Core.Inputs.Internals;
using Core.Systems;
using Core.Utilities.Logging;
using Core.Timing;

public partial class TestScene : Node
{
	[Export]
	private Godot.Collections.Array<Entity> _entities = [];

	[Export]
	private EntitySwapSystem entitySwapSystem = null!;

	[Export]
	private InputHandler _inputHandler = null!;

	[Export]
	private Entity _enemy = null!;

	[Export]
	private SpiderAi _enemyAi = null!;

	[Export]
	private Godot.Collections.Array<Entity> _targetDummies = [];

	// Fresh instances per entity, otherwise every entity shares one Stat object
	private static Dictionary<StatType, Stat> NewEntityStats() => new()
	{
		[StatType.Health] = new ScaledStat(100, 0, 100),
		[StatType.Damage] = new ScaledStat(50, 0, 50),
		[StatType.MoveSpeed] = new LockedStat(30, 0, 30),
	};

	public override void _Ready()
	{
		LoggerService.SetLogLevel(LogLevel.Info);

		entitySwapSystem.InputHandler = _inputHandler;

		float spawnHeight = 5f;
		float spacing = 2f;
		int index = 0;

		foreach (Entity entity in _entities)
		{
			entity.Initialize(new EntityIdentitySpec());

			var statsComponent = entity.GetComponent<StatsComponent>();
			statsComponent?.AddStats(NewEntityStats());

			var characterComponent = entity.GetComponent<CharacterComponent>();

			if (characterComponent?.Character is { } character)
			{
				float xOffset = index % 5 * spacing;
				float zOffset = index / 5 * spacing;
				character.Position = new Vector3(xOffset, spawnHeight, zOffset);
			}

			entitySwapSystem.RegisterEntity(entity);
			index++;
		}

		// Stationary practice targets, lined up in front of the player spawn
		for (int i = 0; i < _targetDummies.Count; i++)
		{
			Entity dummy = _targetDummies[i];
			dummy.Initialize(new EntityIdentitySpec());
			dummy.GetComponent<StatsComponent>()?.AddStats(NewEntityStats());

			// Placed directly on the ground: dummies never move, so nothing applies gravity to them
			if (dummy.GetComponent<CharacterComponent>()?.Character is { } dummyCharacter)
				dummyCharacter.Position = new Vector3((i - 1) * 4f, 1f, -10f);
		}

		// Initialize enemy (spider) and place it slightly offset from targets
		_enemy.Initialize(new EntityIdentitySpec());
		_enemy.GetComponent<StatsComponent>()?.AddStats(NewEntityStats());

		var enemyCharacter = _enemy.GetComponent<CharacterComponent>()?.Character;

		if (enemyCharacter is not null)
		{
			// Move spider away so it has to chase
			enemyCharacter.Position = new Vector3(0f, spawnHeight, 20f);
		}

		// Build AI with proper stopping distance and cooldown
		// anything higher than 1 break easily for the stopping distance cant debug why
		// 0.1 -> 0.5 works for now tho for the test
		_enemyAi.BuildAi(_enemy, _entities, 0.1f, 5.0f);
	}

	public override void _Process(double delta)
	{
		TimerManager.UpdateTimers((float)delta);
	}
}
