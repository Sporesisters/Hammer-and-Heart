using Godot;

namespace Core.Cameras;

/// <summary>
/// Trigger area that overrides the <see cref="CameraRig"/> framing while a followed character is inside it.
/// </summary>
/// <remarks>
/// Add a <see cref="CollisionShape3D"/> child to define the area. The rig blends to this zone's
/// values when a target enters and back to its default values when all targets leave.
/// If several zones overlap, the one with the highest <see cref="CameraPriority"/> wins.
/// </remarks>
[GlobalClass]
public partial class CameraZone : Area3D
{
	/// <summary>
	/// Group every zone joins so the rig can find them without manual wiring.
	/// </summary>
	public const string GroupName = "camera_zones";

	/// <summary>
	/// Distance from the focus point to the camera inside this zone.
	/// </summary>
	[Export]
	public float Distance { get; set; } = 20f;

	/// <summary>
	/// Vertical angle in degrees inside this zone. Negative values look down.
	/// </summary>
	[Export(PropertyHint.Range, "-89,89,0.1")]
	public float PitchDegrees { get; set; } = -35f;

	/// <summary>
	/// Horizontal angle in degrees inside this zone. Use 180 to flip the camera around.
	/// </summary>
	[Export(PropertyHint.Range, "-180,180,0.1")]
	public float YawDegrees { get; set; }

	/// <summary>
	/// Higher priority zones win when several zones overlap.
	/// </summary>
	[Export]
	public int CameraPriority { get; set; }

	public override void _EnterTree()
	{
		AddToGroup(GroupName);
	}
}
