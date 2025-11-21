using Godot;

namespace Core.ECS.Components;

/// <summary>
/// Component that attaches a <see cref="CharacterBody2D"/> to an entity.
/// Allows ECS systems and other components to access and manipulate the character's movement and physics.
/// </summary>
public class CharacterComponent : ComponentBase
{
	/// <summary>
	/// The <see cref="CharacterBody2D"/> node associated with this entity.
	/// Can be accessed by systems for movement, physics, and gameplay logic.
	/// </summary>
	public CharacterBody2D? Character { get; set; }
}
