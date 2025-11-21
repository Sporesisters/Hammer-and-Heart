using Godot;

namespace Core.ECS.Components;

/// <summary>
/// Component that attaches a <see cref="CharacterBody3D"/> to an entity.
/// Allows ECS systems and other components to access and manipulate the character's movement and physics.
/// </summary>
/// <param name="character">The <see cref="CharacterBody3D"/> node to attach to the entity.</param>
public class CharacterComponent(CharacterBody3D character) : ComponentBase
{
	/// <summary>
	/// The <see cref="CharacterBody3D"/> node associated with this entity.
	/// Can be accessed by systems for movement, physics, and gameplay logic.
	/// </summary>
	public CharacterBody3D? Character { get; set; } = character;
}
