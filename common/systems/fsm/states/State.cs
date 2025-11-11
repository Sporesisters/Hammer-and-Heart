using Core.ECS;
using Core.Inputs;

namespace Core.Systems.FSM.States;

/// <summary>
/// Abstract base class for all states.
/// Provides a reference to the FSM controller and default empty implementations for IState methods,
/// allowing concrete states to only override the methods they need.
/// </summary>
public abstract class State : IState
{
	/// <summary>
	/// A reference to the FSM component that owns this state.
	/// Used to change states and access other entity components.
	/// </summary>
	protected readonly FSMComponent Fsm;

	/// <summary>
	/// A direct reference to the entity this state machine belongs to.
	/// </summary>
	protected Entity Entity => Fsm.Entity!;

	protected State(FSMComponent fsm)
	{
		Fsm = fsm;
	}

	public virtual void Enter() { }
	public virtual void Exit() { }
	public virtual void HandleInput(InputCommand command) { }
	public virtual void Process(double delta) { }
	public virtual void PhysicsProcess(double delta) { }
}