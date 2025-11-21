using Core.ECS;
using Godot;

public class ElaineSpawnData(EntityWorld world, EntityIdentitySpec spec) : EntitySpawnData(world, spec)
{
    public Vector3 Position { get; set; } = new(0, 0, 0);
}