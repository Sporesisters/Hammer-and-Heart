using Godot;
using System.Collections.Generic;

namespace Core.ECS.Components;

/// <summary>
/// Represents a component that applies gravity forces to an entity.
/// Supports a base gravity and multiple additive modifiers.
/// </summary>
public class GravityComponent : ComponentBase
{
	/// <summary>
	/// The base gravity vector applied to the entity.
	/// Default is (0, -980, 0), representing Earth's gravity in Godot units.
	/// </summary>
	public Vector3 BaseGravity { get; private set; } = new(0, -980f, 0);

	/// <summary>
	/// A collection of gravity modifiers added by other nodes or systems.
	/// </summary>
	private readonly Dictionary<Node, Vector3> _modifiers = [];

	/// <summary>
	/// Calculates the total gravity vector in 3D space, including
	/// the base gravity and all active modifiers.
	/// </summary>
	/// <returns>The resulting 3D gravity vector.</returns>
	public Vector3 TotalGravity3D()
	{
		Vector3 total = BaseGravity;

		foreach (Vector3 modifier in _modifiers.Values)
			total += modifier;

		return total;
	}

	/// <summary>
	/// Calculates the total gravity vector in 2D space (X and Y),
	/// ignoring the Z component.
	/// </summary>
	/// <returns>The resulting 2D gravity vector.</returns>
	public Vector2 TotalGravity2D()
	{
		Vector3 totalGravity = TotalGravity3D();
		return new Vector2(totalGravity.X, totalGravity.Y);
	}

	/// <summary>
	/// Adds or updates a gravity modifier from a specific source node.
	/// If a modifier from the same source already exists, it will be updated
	/// with the new force vector; otherwise, it will be added.
	/// </summary>
	/// <param name="source">The <see cref="Node"/> that provides the modifier.</param>
	/// <param name="force">The additional gravity vector applied by this source.</param>
	public void AddModifier(Node source, Vector3 force) => _modifiers[source] = force;

	/// <summary>
	/// Removes a gravity modifier previously added by a specific source node.
	/// If no modifier exists for the given source, this method does nothing.
	/// </summary>
	/// <param name="source">The <see cref="Node"/> whose modifier should be removed.</param>
	public void RemoveModifier(Node source) => _modifiers.Remove(source);

	/// <summary>
	/// Clears all gravity modifiers, leaving only the base gravity intact.
	/// </summary>
	public void ClearModifiers() => _modifiers.Clear();

	/// <summary>
	/// Sets the base gravity vector for this component, replacing the current value.
	/// </summary>
	/// <param name="gravity">The new base gravity vector.</param>
	public void SetBaseGravity(Vector3 gravity) => BaseGravity = gravity;
}
