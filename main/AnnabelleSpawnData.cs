using Core.ECS;
using Godot;

/// <summary>
/// Spawn data for creating an Annabelle entity.
/// </summary>
/// <param name="world">The ECS world the new entity will belong to.</param>
/// <param name="spec">The identity specification defining which entity type is being created.</param>
/// <param name="spawnPosition">The world-space position where Annabelle should be spawned.</param>
public class AnnabelleSpawnData(EntityWorld world, EntityIdentitySpec spec, Vector3 spawnPosition)
    : EntitySpawnData(world, spec)
{
    /// <summary>
    /// The world-space position Annabelle should be placed at when spawned.
    /// </summary>
    public readonly Vector3 Position = spawnPosition;
}
