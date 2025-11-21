namespace Core.ECS;

/// <summary>
/// Represents the data required to spawn an entity in the ECS world.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="EntitySpawnData"/> with the given world and entity specification.
/// </remarks>
/// <param name="world">The ECS world where the entity will exist.</param>
/// <param name="spec">The identity/specification of the entity.</param>
public abstract class EntitySpawnData(EntityWorld world, EntityIdentitySpec spec)
{
    /// <summary>
    /// The ECS world where the entity will be spawned.
    /// </summary>
    public EntityWorld World { get; } = world;

    /// <summary>
    /// The specification or identity of the entity.
    /// </summary>
    public EntityIdentitySpec Spec { get; } = spec;
}
