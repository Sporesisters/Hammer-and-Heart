using Godot;
using Core.ECS;
using Core.Inputs;
using Core.ECS.Components;
using Core.Stats;
using Core.Stats.Types;
using Core.Systems;
using Core.Utilities.Logging;
using Core.ECS.Tags;

/// <summary>
/// Test scene for testing the game core and entity system.
/// </summary>
public partial class TestScene : Node
{
	/// <inheritdoc/>
	public override void _Ready()
	{
		// To stop log clutter, set the log level to Info. You can adjust this as needed.
		LoggerService.SetLogLevel(LogLevel.Info);

		var world = GameCore.Instance.EntityWorld;

		// Normally, these systems would be managed by a GameManager.
		// Here, we directly add them to the world to spawn and control a player.
		world.AddSystem<PlayerInputSystem>();
		world.AddSystem<MovementSystem>();
		world.AddSystem<EntitySwapSystem>();

		// Spawn test entities
		AnnabelleSpawnData annabelleSpawnData = new(world, EntityIdentitySpec.Empty, new Vector3(5, 2, 5));
		Entity p1 = EntityFactory.Spawn(annabelleSpawnData, this);

		ElaineSpawnData elaineSpawnData = new(world, EntityIdentitySpec.Empty, new Vector3(-5, 2, -5));
		EntityFactory.Spawn(elaineSpawnData, this);

		// Mark entity as player-controlled so input and swapping systems can act on it.
		p1.AddComponent<PlayerControlledTag>();

		// Add stats to entities.
		foreach (Entity entity in world.Query<StatsComponent>())
			entity.GetComponent<StatsComponent>()?.AddStat(StatType.MoveSpeed, new DynamicStat(25, 0, 25));
	}
}
