using Godot;
using Core.ECS;
using Core.Inputs;

public partial class TestScene : Node
{
	private readonly EntityWorld _world = new();

	public override void _Ready()
	{
		_world.AddSystem<PlayerInputSystem>();

		AnnabelleSpawnData annabelleSpawnData = new(_world, EntityIdentitySpec.Empty, new Vector3(5, 2, 5));
		EntityFactory.Spawn(annabelleSpawnData, this);

		ElaineSpawnData elaineSpawnData = new(_world, EntityIdentitySpec.Empty, new Vector3(-5, 2, -5));
		EntityFactory.Spawn(elaineSpawnData, this);
	}
}
