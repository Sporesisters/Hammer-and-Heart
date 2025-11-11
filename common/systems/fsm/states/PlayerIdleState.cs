using Core.Inputs;
using Core.ECS.Components;
using Godot;

namespace Core.Systems.FSM.States;

/// <summary>
/// The state for when the player-controlled character is standing still.
/// Transitions to moving or attacking states based on input.
/// </summary>
public class PlayerIdleState : State
{
	public PlayerIdleState(FSMComponent fsm) : base(fsm) { }

	public override void Enter()
	{
		// Ensure the character stops moving when entering idle state.
		Entity.GetComponent<MovementComponent>()?.Move(Vector2.Zero);
	}

	public override void HandleInput(InputCommand command)
	{
		// Transition to MoveState if there is movement input.
		if (command.MoveDirection != Vector2.Zero)
		{
			Fsm.ChangeState(new PlayerMoveState(Fsm));
			return;
		}

		// Transition to an attack state if the attack button is pressed.
		if (command.AttackPressed)
		{
			if (Entity.EntityIdentity.SubType == "Elaine")
			{
				Fsm.ChangeState(new ElaineAttackState(Fsm));
			}
			else if (Entity.EntityIdentity.SubType == "Annabelle")
			{
				Fsm.ChangeState(new AnnabelleKissState(Fsm));
			}
		}
	}
}