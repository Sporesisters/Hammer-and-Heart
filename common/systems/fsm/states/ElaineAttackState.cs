using Core.ECS.Components;
using Core.Utilities.Logging;
using Godot;

namespace Core.Systems.FSM.States;

/// <summary>
/// A state representing Elaine performing a hammer attack.
/// This is a placeholder and would be expanded with combo logic, timers, and animation events.
/// </summary>
public class ElaineAttackState : State
{
	public ElaineAttackState(FSMComponent fsm) : base(fsm) { }

	public override void Enter()
	{
		LoggerService.Info($"[{Fsm.EntityId}] Elaine begins her hammer attack!");
		Entity.GetComponent<MovementComponent>()?.Move(Vector2.Zero);
	}

	public override void Process(double delta)
	{
		// In a real implementation, we would wait for the attack animation to finish...
		// or for a timer to expire before transitioning back to Idle :D
		// For this example, we'll transition back immediately for demonstration.
		LoggerService.Debug($"[{Fsm.EntityId}] Attack finished. Returning to Idle.");
		Fsm.ChangeState(new PlayerIdleState(Fsm));
	}

	public override void Exit()
	{
		LoggerService.Info($"[{Fsm.EntityId}] Elaine's attack concludes.");
	}
}