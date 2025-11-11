using Godot;
using Core.ECS;
using Core.Inputs;
using Core.Utilities.Logging;

namespace Core.Systems.FSM;

/// <summary>
/// A component that drives a Finite State Machine for an entity.
/// It holds the current state, passes updates and input to it, and manages state transitions.
/// This component acts as the bridge between the lightweight state objects and the entity's other components.
/// </summary>
[GlobalClass]
public partial class FSMComponent : ComponentBase, IInputReceiver
{
	/// <summary>
	/// The currently active state in the FSM.
	/// </summary>
	public IState CurrentState { get; private set; } = null!;

	/// <summary>
	/// Initializes the FSM with a starting state. This must be called before the FSM can operate.
	/// </summary>
	/// <param name="startingState">The initial state for the FSM.</param>
	public void Initialize(IState startingState)
	{
		CurrentState = startingState;
		CurrentState.Enter();
		LoggerService.Info($"[{EntityId}] FSM initialized with state <{startingState.GetType().Name}>.");
	}

	/// <summary>
	/// Transitions the FSM to a new state.
	/// It calls Exit() on the current state and Enter() on the new state.
	/// </summary>
	/// <param name="newState">The new state to transition to.</param>
	public void ChangeState(IState newState)
	{
		if (CurrentState is not null && CurrentState.GetType() == newState.GetType())
		{
			return; // Do not transition to the same state type.
		}

		CurrentState?.Exit();
		CurrentState = newState;
		CurrentState.Enter();
		LoggerService.Debug($"[{EntityId}] FSM transitioned to <{newState.GetType().Name}>.");
	}

	/// <summary>
	/// Receives an input command and delegates it to the current state.
	/// </summary>
	public void ReceiveInput(InputCommand command)
	{
		CurrentState?.HandleInput(command);
	}

	public override void _Process(double delta)
	{
		CurrentState?.Process(delta);
	}

	public override void _PhysicsProcess(double delta)
	{
		CurrentState?.PhysicsProcess(delta);
	}
}