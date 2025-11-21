using Core.ECS.Internals;

namespace Core.ECS;

/// <summary>
/// Represents a system that performs physics-related updates in the component framework.
/// <para>
/// Implementing this interface designates a class as a physics system, which
/// will be updated during the physics step of the game loop.
/// </para>
/// <para>
/// The <see cref="PhysicsUpdate"/> method is called with the delta time between
/// physics ticks, allowing the system to perform movement, collision, or other
/// physics calculations.
/// </para>
/// </summary>
public interface IPhysicsSystem : ISystem
{
    /// <summary>
    /// Called every physics tick to update the system.
    /// </summary>
    /// <param name="delta">Time elapsed since the last physics update.</param>
    void PhysicsUpdate(double delta);
}
