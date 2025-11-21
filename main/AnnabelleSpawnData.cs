using Core.ECS;
using Godot;

public class AnnabelleSpawnData(EntityWorld world, EntityIdentitySpec spec) : EntitySpawnData(world, spec)
{
    public Vector3 Position { get; set; } = new(0, 0, 0);
}