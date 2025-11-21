using Core.ECS.Internals;

namespace Core.ECS;

/// <summary>
/// Represents a system that performs regular (non-physics) updates in the component framework.
/// <para>
/// Implementing this interface designates a class as a process system, which
/// will be updated during the standard game loop update phase.
/// </para>
/// <para>
/// The <see cref="Update"/> method is called every frame with the delta time
/// since the last update, allowing the system to perform logic, AI, animations, or other
/// time-dependent operations.
/// </para>
/// </summary>
public interface IProcessSystem : ISystem
{
    /// <summary>
    /// Called each frame to update the system.
    /// </summary>
    /// <param name="delta">Time elapsed since the last frame update.</param>
    void Update(double delta);
}
