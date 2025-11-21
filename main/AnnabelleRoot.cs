using Core.ECS;
using Core.ECS.Components;
using Godot;

[GlobalClass]
public partial class AnnabelleRoot : EntityRoot
{
	[Export]
	private CharacterBody3D _characterBody = null!;

	public override void Setup(Entity entity, EntitySpawnData data)
	{
		entity.AddComponent<StatsComponent>();
		entity.AddComponent<MovementComponent>();
		entity.AddComponent<GravityComponent>();
		entity.AddComponent(new CharacterComponent(_characterBody));
		entity.AddComponent<EntitySwapComponent>();
	}
}
