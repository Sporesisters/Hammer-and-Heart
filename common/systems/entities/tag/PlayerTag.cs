namespace Core.ECS.Tags;

/// <summary>
/// A marker component used to identify an entity as a player within the framework.
/// This component does not store any data; its presence alone is sufficient for systems
/// to recognize the entity as a player.
/// </summary>
public class PlayerTag : ComponentBase { }
