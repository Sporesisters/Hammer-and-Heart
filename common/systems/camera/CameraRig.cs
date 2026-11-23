using Godot;
using Core.ECS;
using Core.ECS.Components;

namespace Core.Cameras;

/// <summary>
/// Camera that follows a group of entities (the girls) with smooth damping and a fixed angle.
/// </summary>
/// <remarks>
/// <para>
/// The rig looks at the average position of all <see cref="Targets"/>. Swapping the active character
/// does not change that point, so the camera never jumps on a swap.
/// </para>
/// <para>
/// <see cref="CameraZone"/> areas in the scene override distance and angles while a target is inside them,
/// and the rig blends in and out of them using <see cref="ZoneBlendSpeed"/>.
/// </para>
/// </remarks>
[GlobalClass]
public partial class CameraRig : Node3D
{
	/// <summary>
	/// Entities the camera keeps on screen. Each needs a <see cref="CharacterComponent"/>.
	/// </summary>
	[Export]
	public Godot.Collections.Array<Entity> Targets { get; set; } = [];

	/// <summary>
	/// Default distance from the focus point to the camera.
	/// </summary>
	[ExportGroup("Framing")]
	[Export]
	public float Distance { get; set; } = 20f;

	/// <summary>
	/// Default vertical angle in degrees. Negative values look down.
	/// </summary>
	[Export(PropertyHint.Range, "-89,89,0.1")]
	public float PitchDegrees { get; set; } = -35f;

	/// <summary>
	/// Default horizontal angle in degrees.
	/// </summary>
	[Export(PropertyHint.Range, "-180,180,0.1")]
	public float YawDegrees { get; set; }

	/// <summary>
	/// Offset added to the focus point, e.g. to look slightly above the characters' feet.
	/// </summary>
	[Export]
	public Vector3 FocusOffset { get; set; } = new(0f, 1f, 0f);

	/// <summary>
	/// How fast the camera catches up with the targets. Higher is snappier.
	/// </summary>
	[ExportGroup("Smoothing")]
	[Export]
	public float FollowDamping { get; set; } = 5f;

	/// <summary>
	/// How fast distance and angles blend when entering or leaving a <see cref="CameraZone"/>.
	/// </summary>
	[Export]
	public float ZoneBlendSpeed { get; set; } = 3f;

	/// <summary>
	/// Clamps the focus point inside <see cref="BoundsMin"/> and <see cref="BoundsMax"/> on the X/Z plane.
	/// </summary>
	[ExportGroup("Limits")]
	[Export]
	public bool UseBounds { get; set; }

	/// <summary>
	/// Minimum X/Z of the focus point. Leave a margin from the level edge so the view stays inside the level.
	/// </summary>
	[Export]
	public Vector2 BoundsMin { get; set; } = new(-20f, -20f);

	/// <summary>
	/// Maximum X/Z of the focus point. Leave a margin from the level edge so the view stays inside the level.
	/// </summary>
	[Export]
	public Vector2 BoundsMax { get; set; } = new(20f, 20f);

	private Vector3 _focus;
	private float _currentDistance;
	private float _currentPitch;
	private float _currentYaw;
	private bool _hasSnapped;

	public override void _Ready()
	{
		_currentDistance = Distance;
		_currentPitch = PitchDegrees;
		_currentYaw = YawDegrees;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!TryGetTargetsCenter(out Vector3 center)) return;

		Vector3 desiredFocus = ClampToBounds(center + FocusOffset);
		(float distance, float pitch, float yaw) = GetDesiredFraming();

		if (!_hasSnapped)
		{
			SnapTo(desiredFocus, distance, pitch, yaw);
			return;
		}

		float dt = (float)delta;
		_focus = _focus.Lerp(desiredFocus, DampingWeight(FollowDamping, dt));

		float blend = DampingWeight(ZoneBlendSpeed, dt);
		_currentDistance = Mathf.Lerp(_currentDistance, distance, blend);
		_currentPitch = Mathf.Lerp(_currentPitch, pitch, blend);
		_currentYaw = Mathf.RadToDeg(Mathf.LerpAngle(Mathf.DegToRad(_currentYaw), Mathf.DegToRad(yaw), blend));

		ApplyTransform();
	}

	/// <summary>
	/// Adds an entity to the followed targets.
	/// </summary>
	/// <param name="entity">The entity to follow.</param>
	public void AddTarget(Entity entity)
	{
		if (!Targets.Contains(entity)) Targets.Add(entity);
	}

	/// <summary>
	/// Removes an entity from the followed targets.
	/// </summary>
	/// <param name="entity">The entity to stop following.</param>
	public void RemoveTarget(Entity entity) => Targets.Remove(entity);

	/// <summary>
	/// Moves the camera instantly to its desired position, skipping smoothing.
	/// Use after teleports or level loads.
	/// </summary>
	public void Snap() => _hasSnapped = false;

	private void SnapTo(Vector3 focus, float distance, float pitch, float yaw)
	{
		_focus = focus;
		_currentDistance = distance;
		_currentPitch = pitch;
		_currentYaw = yaw;
		_hasSnapped = true;
		ApplyTransform();
	}

	private void ApplyTransform()
	{
		Basis orientation = Basis.FromEuler(new Vector3(Mathf.DegToRad(_currentPitch), Mathf.DegToRad(_currentYaw), 0f));
		GlobalTransform = new Transform3D(orientation, _focus + orientation * new Vector3(0f, 0f, _currentDistance));
	}

	private bool TryGetTargetsCenter(out Vector3 center)
	{
		center = Vector3.Zero;
		int count = 0;

		foreach (Entity entity in Targets)
		{
			if (!IsInstanceValid(entity)) continue;
			if (entity.GetComponent<CharacterComponent>()?.Character is not { } character) continue;

			center += character.GlobalPosition;
			count++;
		}

		if (count is 0) return false;

		center /= count;
		return true;
	}

	private (float Distance, float Pitch, float Yaw) GetDesiredFraming()
	{
		CameraZone? zone = GetActiveZone();

		return zone is null
			? (Distance, PitchDegrees, YawDegrees)
			: (zone.Distance, zone.PitchDegrees, zone.YawDegrees);
	}

	private CameraZone? GetActiveZone()
	{
		CameraZone? best = null;

		foreach (Node node in GetTree().GetNodesInGroup(CameraZone.GroupName))
		{
			if (node is not CameraZone zone || !zone.Monitoring) continue;
			if (best is not null && zone.CameraPriority <= best.CameraPriority) continue;
			if (ContainsAnyTarget(zone)) best = zone;
		}

		return best;
	}

	private bool ContainsAnyTarget(CameraZone zone)
	{
		foreach (Entity entity in Targets)
		{
			if (!IsInstanceValid(entity)) continue;
			if (entity.GetComponent<CharacterComponent>()?.Character is { } character && zone.OverlapsBody(character))
				return true;
		}

		return false;
	}

	private Vector3 ClampToBounds(Vector3 point)
	{
		if (!UseBounds) return point;

		return new Vector3(
			Mathf.Clamp(point.X, BoundsMin.X, BoundsMax.X),
			point.Y,
			Mathf.Clamp(point.Z, BoundsMin.Y, BoundsMax.Y)
		);
	}

	/// <summary>
	/// Frame-rate independent lerp weight for exponential smoothing.
	/// </summary>
	private static float DampingWeight(float speed, float delta) => 1f - Mathf.Exp(-speed * delta);
}
