using Godot;

namespace Core.ECS.Components;

/// <summary>
/// Component that attaches a <see cref="CharacterBody3D"/> to an entity.
/// Allows ECS systems and other components to access and manipulate the character's movement and physics.
/// </summary>
/// <remarks>
/// The <see cref="Character"/> property is exported, so it can be assigned directly in the Godot Editor.
/// The property is nullable to safely handle cases where a character may not yet be assigned.
/// </remarks>
[GlobalClass]
public partial class CharacterComponent : ComponentBase
{
	/// <summary>
	/// The <see cref="CharacterBody3D"/> node associated with this entity.
	/// Can be accessed by systems for movement, physics, and gameplay logic.
	/// </summary>
	[Export]
	public CharacterBody3D? Character { get; private set; }
}
