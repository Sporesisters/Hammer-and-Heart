using Godot;
using Core.ECS;
using Core.Inputs;
using Core.ECS.Components;
using Core.Stats;
using Core.Stats.Types;
using Core.Systems;
using Core.Utilities.Logging;
using Core.ECS.Events;

public partial class TestScene : Node
{
	private readonly EntityWorld _world = new();

	public override void _Ready()
	{
		LoggerService.SetLogLevel(LogLevel.Info);

		AddChild(_world);
		_world.AddSystem<PlayerInputSystem>();
		_world.AddSystem<MovementSystem>();
		_world.AddSystem<EntitySwapSystem>();

		AnnabelleSpawnData annabelleSpawnData = new(_world, EntityIdentitySpec.Empty, new Vector3(5, 2, 5));
		Entity p1 = EntityFactory.Spawn(annabelleSpawnData, this);

		ElaineSpawnData elaineSpawnData = new(_world, EntityIdentitySpec.Empty, new Vector3(-5, 2, -5));
		EntityFactory.Spawn(elaineSpawnData, this);

		p1.AddComponent<ControlledByPlayerTag>();

		foreach (var entity in _world.Query<StatsComponent>())
		{
			entity.GetComponent<StatsComponent>()?.AddStat(StatType.MoveSpeed, new DynamicStat(25, 0, 25));
		}
	}
}

public class ControlledByPlayerTag : ComponentBase { }
