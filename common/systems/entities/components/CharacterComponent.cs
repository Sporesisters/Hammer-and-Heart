using Godot;

namespace Core.ECS.Components;

/// <summary>
/// Component that attaches a <see cref="CharacterBody3D"/> to an entity.
/// Allows ECS systems and other components to access and manipulate the character's movement and physics.
/// </summary>
public class CharacterComponent : ComponentBase
{
    /// <summary>
    /// The <see cref="CharacterBody3D"/> node associated with this entity.
    /// Can be accessed by systems for movement, physics, and gameplay logic.
    /// </summary>
    public CharacterBody3D Character { get; set; }

    /// <summary>
    /// Creates a new character component and initializes the character's transform.
    /// </summary>
    /// <param name="character">The CharacterBody3D instance to bind to the component.</param>
    /// <param name="position">The initial world position to place the character at.</param>
    public CharacterComponent(CharacterBody3D character, Vector3 position)
    {
        Character = character;
        Character.Position = position;
    }
}
