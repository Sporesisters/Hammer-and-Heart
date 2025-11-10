using Godot;
using System;

using Core.ECS;
using Core.ECS.Components;

/* Partly AI Generated for debugging level*/

public partial class SimpleFollowCam : Camera3D
{
	[Export] public Entity TargetPlayableEntity;
	private CharacterBody3D TargetPlayerCharacter;
	
	[Export] public Vector3 Offset = new Vector3(0, 6, 8);
	[Export] public float Smoothness = 1.0f;

	public override void _Ready()
	{
		if (TargetPlayableEntity != null){
			var characterComp = TargetPlayableEntity.GetComponent<CharacterComponent>();
			TargetPlayerCharacter = characterComp.Character;
		}
	}
	public override void _Process(double delta)
	{
		if (TargetPlayerCharacter == null)
			return;

		// Desired position behind and above the player
		Vector3 targetPos = TargetPlayerCharacter.GlobalPosition + TargetPlayerCharacter.Transform.Basis.Z * Offset.Z;
		targetPos.Y += Offset.Y;
		targetPos.X += Offset.X;

		// Smoothly interpolate camera position
		GlobalPosition = GlobalPosition.Lerp(targetPos, (float)(Smoothness * delta));

		// Make the camera look at the player
		LookAt(TargetPlayerCharacter.GlobalPosition, Vector3.Up);
	}
}
