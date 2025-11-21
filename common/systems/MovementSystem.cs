using Core.ECS;
using Core.ECS.Components;

public class MovementSystem : IPhysicsSystem
{
    public EntityWorld? World { get; set; }

    public void PhysicsUpdate(double delta)
    {
        if (World is null) return;

        foreach (var entity in World.Query<MovementComponent>())
        {
            entity.GetComponent<MovementComponent>()?.Move(delta);
        }
    }
}