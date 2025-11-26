using Core.ECS;
using Core.ECS.Components;

namespace Core.Systems;

/// <summary>
/// System responsible for updating movement of all entities that have a <see cref="MovementComponent"/>.
/// This system is executed during the physics update loop.
/// </summary>
public class MovementSystem : IPhysicsSystem
{
    /// <inheritdoc/>
    public EntityWorld? World { get; set; }

    /// <inheritdoc/>
    public void PhysicsUpdate(double delta)
    {
        if (World is null) return;

        foreach (Entity entity in World.Query<MovementComponent>())
            entity.GetComponent<MovementComponent>()?.Move(delta);
    }
}
