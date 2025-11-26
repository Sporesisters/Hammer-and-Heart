using Core.ECS;
using Core.ECS.Components;
using Core.ECS.Tags;
using Godot;

/// <summary>
/// Scene-level root for the Annabelle entity.
/// </summary>
[GlobalClass]
public partial class AnnabelleRoot : EntityRoot
{
	/// <summary>
	/// Character body component for the Annabelle entity.
	/// </summary>
	[Export]
	private CharacterBody3D _characterBody = null!;

	/// <inheritdoc/>
	public override void Setup(Entity entity, EntitySpawnData data)
	{
		AssertAssigned(_characterBody);

		var castData = RequireSpawnData<AnnabelleSpawnData>(data);

		entity.AddComponent<StatsComponent>();
		entity.AddComponent<MovementComponent>();
		entity.AddComponent<GravityComponent>();
		entity.AddComponent(new CharacterComponent(_characterBody, castData.Position));
		entity.AddComponent<EntitySwapComponent>();

		entity.AddComponent<PlayerTag>();
	}
}
