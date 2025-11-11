using Core.Inputs;
using Core.ECS;
using Godot;

namespace Core.Systems.FSM.States;

/// <summary>
/// The state for when the player-controlled character is standing still.
/// Transitions to moving or attacking states based on input.
/// </summary>
public class PlayerIdleState : State
{
	public PlayerIdleState(FSMComponent fsm) : base(fsm) { }

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
			// Determine which character is active and transition to the correct attack state.
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