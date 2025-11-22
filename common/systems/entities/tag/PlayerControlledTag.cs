namespace Core.ECS.Tags;

/// <summary>
/// Marker component that identifies an entity as currently controlled by a player.
/// Systems like input handling and movement will act on entities with this tag.
/// Contains no data; presence alone is sufficient.
/// </summary>
public class PlayerControlledTag : ComponentBase { }
