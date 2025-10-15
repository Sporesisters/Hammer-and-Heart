using Godot;
using System.Collections.Generic;

namespace Core.ECS.Components;

/// <summary>
/// Provides gravity for an entity, combining a base gravity
/// with optional modifiers from other nodes.
/// Use <see cref="SetModifier"/> / <see cref="RemoveModifier"/>
/// to adjust forces, and read <see cref="TotalGravity3D"/> or
/// <see cref="TotalGravity2D"/> for the effective gravity vector.
/// </summary>
[GlobalClass]
public partial class GravityComponent : ComponentBase
{
	/// <summary>
	/// The base gravity applied when no modifiers are active.
	/// Defaults to (0, -980, 0) in Godot units (cm/s²).
	/// </summary>
	[Export] public Vector3 BaseGravity { get; private set; } = new(0, -980f, 0);

	// Each source node can contribute a single modifier at a time.
	private readonly Dictionary<Node, Vector3> _modifiers = [];

	/// <summary>
	/// The effective gravity in 3D, combining <see cref="BaseGravity"/>
	/// with all active modifiers.
	/// </summary>
	public Vector3 TotalGravity3D()
	{
		Vector3 total = BaseGravity;

		foreach (Vector3 modifier in _modifiers.Values)
		{
			total += modifier;
		}

		return total;
	}

	/// <summary>
	/// The effective gravity in 2D (XY plane), derived from <see cref="TotalGravity3D"/>.
	/// </summary>
	public Vector2 TotalGravity2D()
	{
		Vector3 totalGravity = TotalGravity3D();
		return new(totalGravity.X, totalGravity.Y);
	}

	/// <summary>
	/// Assigns or updates a gravity modifier from a given source.
	/// Each source can only have one active modifier.
	/// </summary>
	public void SetModifier(Node source, Vector3 force) => _modifiers[source] = force;

	/// <summary>
	/// Removes the gravity modifier associated with the given source.
	/// </summary>
	public void RemoveModifier(Node source) => _modifiers.Remove(source);
}
