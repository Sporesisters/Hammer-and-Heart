using Core.Inputs;
using Core.ECS.Components;
using Godot;

namespace Core.Systems.FSM.States;

/// <summary>
/// The state for when the player-controlled character is moving.
/// Reads movement input and applies it via the MovementComponent.
/// Transitions to Idle when movement stops or to an attack state.
/// </summary>
public class PlayerMoveState : State
{
	private readonly MovementComponent? _movementComponent;

	public PlayerMoveState(FSMComponent fsm) : base(fsm)
	{
		_movementComponent = Entity.GetComponent<MovementComponent>();
	}

	public override void HandleInput(InputCommand command)
	{
		// Pass movement direction to the MovementComponent.
		_movementComponent?.Move(command.MoveDirection);

		// Transition back to IdleState if movement stops.
		if (command.MoveDirection == Vector2.Zero)
		{
			Fsm.ChangeState(new PlayerIdleState(Fsm));
			return;
		}

		// Allow attacking while moving.
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