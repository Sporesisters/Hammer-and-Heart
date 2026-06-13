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

public partial class TestSwitchScene : Node
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

	private readonly Dictionary<StatType, Stat> _entityStatsMapping = new()
	{
		[StatType.Health] = new ScaledStat(100, 0, 100),
		[StatType.Damage] = new ScaledStat(50, 0, 50),
		[StatType.MoveSpeed] = new LockedStat(30, 0, 30),
	};

	public override void _Ready()
	{
		LoggerService.SetLogLevel(LogLevel.Info);

		float spawnHeight = 5f;
		float spacing = 2f;
		int index = 0;

		foreach (Entity entity in _entities)
		{
			entity.Initialize(new EntityIdentitySpec());

			var statsComponent = entity.GetComponent<StatsComponent>();
			statsComponent?.AddStats(_entityStatsMapping);

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

		entitySwapSystem.InputHandler = _inputHandler;

		// Initialize enemy (spider) and place it slightly offset from targets
		_enemy.Initialize(new EntityIdentitySpec());
		_enemy.GetComponent<StatsComponent>()?.AddStats(_entityStatsMapping);

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
