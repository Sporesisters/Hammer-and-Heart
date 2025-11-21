namespace Core.ECS.Internals;

/// <summary>
/// Base interface for all ECS systems.
/// <para>
/// Implementing this interface designates a class as a system, which can be a process system,
/// physics system, or reactive/event-driven system. Unlike a pure marker interface, this interface
/// now requires a reference to the <see cref="EntityWorld"/> the system is registered under.
/// </para>
/// <para>
/// Every system will automatically have its <see cref="World"/> property set when added to an
/// <see cref="EntityWorld"/>, giving it context to query entities, components, or interact with
/// the event bus if needed. Even if the system does not actively use the world, this ensures
/// consistent access and simplifies system registration and lifecycle management.
/// </para>
/// </summary>
public interface ISystem
{
    /// <summary>
    /// The <see cref="EntityWorld"/> this system is registered in.
    /// Automatically assigned by the world when the system is added.
    /// </summary>
    EntityWorld? World { get; set; }
}
