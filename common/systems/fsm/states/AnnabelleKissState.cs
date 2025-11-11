using Core.ECS.Components;
using Core.Utilities.Logging;
using Godot;

namespace Core.Systems.FSM.States;

/// <summary>
/// A state representing Annabelle blowing a projectile kiss to calm a monster.
/// This would be expanded to include projectile spawning logic.
/// </summary>
public class AnnabelleKissState : State
{
	public AnnabelleKissState(FSMComponent fsm) : base(fsm) { }

	public override void Enter()
	{
		LoggerService.Info($"[{Fsm.EntityId}] Annabelle blows a kiss.");
		Entity.GetComponent<MovementComponent>()?.Move(Vector2.Zero);
	}

	public override void Process(double delta)
	{
		// Like the attack state, this would typically be timed with an animation.
		// We transition back to Idle for this example.
		LoggerService.Debug($"[{Fsm.EntityId}] Kiss animation finished. Returning to Idle.");
		Fsm.ChangeState(new PlayerIdleState(Fsm));
	}

	public override void Exit()
	{
		LoggerService.Info($"[{Fsm.EntityId}] Annabelle's action is complete.");
	}
}