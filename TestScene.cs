using Godot;
using System.Collections.Generic;
using Core.ECS;
using Core.ECS.Components;
using Core.Stats;
using Core.Stats.Types;
using Core.Utilities.Logging;
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
		[StatType.MoveSpeed] = new LockedStat(390, 0, 390),
	};

	public override void _Ready()
	{
		LoggerService.SetLogLevel(LogLevel.Debug);

		foreach (Entity entity in _entities)
		{
			entity.Initialize(new EntityIdentitySpec());
			var statsComponent = entity.GetComponent<StatsComponent>();
			statsComponent?.AddStats(_entityStatsMapping);

			entitySwapSystem.RegisterEntity(entity);
		}

		entitySwapSystem.InputHandler = _inputHandler;
	}
}
