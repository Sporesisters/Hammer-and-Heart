using Godot;
using System.Collections.Generic;
using Core.ECS;
using Core.ECS.Components;
using Core.Stats;
using Core.Systems;
using System.Linq;

public partial class TestScene : Node
{
	private readonly EntityWorld _world = new();

	public override void _Ready()
	{
		AnnabelleSpawnData annabelleSpawnData = new(_world, EntityIdentitySpec.Empty);
		EntityFactory.Spawn(annabelleSpawnData, this);
		EntityFactory.Spawn(annabelleSpawnData, this);
		EntityFactory.Spawn(annabelleSpawnData, this);

		ElaineSpawnData elaineSpawnData = new(_world, EntityIdentitySpec.Empty);
		EntityFactory.Spawn(elaineSpawnData, this);

		foreach (var item in _world.Query<CharacterComponent>())
		{
			GD.Print(item.EntityIdentity);
		}
		Entity node = _world.GetEntity(GetNode("AnnabelleRoot/CharacterBody3D/CollisionShape3D"));
		GD.Print(node.EntityIdentity);
	}

	// [Export] private Godot.Collections.Array<Entity> _entities = [];
	// [Export] private EntitySwapSystem entitySwapSystem = null!;
	// [Export] private InputHandler _inputHandler = null!;

	// private readonly Dictionary<StatType, Stat> _entityStatsMapping = new()
	// {
	// 	[StatType.Health] = new ScaledStat(100, 0, 100),
	// 	[StatType.Damage] = new ScaledStat(50, 0, 50),
	// 	[StatType.MoveSpeed] = new LockedStat(30, 0, 30),
	// };

	// public override void _Ready()
	// {
	// 	float spawnHeight = 5f;
	// 	float spacing = 2f;
	// 	int index = 0;

	// 	foreach (Entity entity in _entities)
	// 	{
	// 		entity.Initialize(new EntityIdentitySpec());

	// 		var statsComponent = entity.GetComponent<StatsComponent>();
	// 		statsComponent?.AddStats(_entityStatsMapping);

	// 		var characterComponent = entity.GetComponent<CharacterComponent>();

	// 		if (characterComponent is { Character: { } character })
	// 		{
	// 			float xOffset = index % 5 * spacing;
	// 			float zOffset = index / 5 * spacing;
	// 			character.Position = new Vector3(xOffset, spawnHeight, zOffset);
	// 		}

	// 		entitySwapSystem.RegisterEntity(entity);
	// 		index++;
	// 	}

	// 	entitySwapSystem.InputHandler = _inputHandler;
	// }
}
