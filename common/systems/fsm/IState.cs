using Core.Inputs;

namespace Core.Systems.FSM;

/// <summary>
/// Defines the contract for a state within a Finite State Machine.
/// Each state represents a specific behavior or condition of an entity.
/// </summary>
public interface IState
{
	/// <summary>
	/// Called once when the FSM enters this state.
	/// Use for setup logic, such as starting an animation or applying a status effect.
	/// </summary>
	void Enter();

	/// <summary>
	/// Called once when the FSM exits this state.
	/// Use for cleanup logic, such as stopping an animation or resetting variables.
	/// </summary>
	void Exit();

	/// <summary>
	/// Handles input commands passed from the FSM controller.
	/// Use for state transitions based on player input.
	/// </summary>
	/// <param name="command">The current input snapshot.</param>
	void HandleInput(InputCommand command);

	/// <summary>
	/// Called every frame. Use for time-based logic that is not physics-dependent.
	/// </summary>
	/// <param name="delta">The time elapsed since the last frame.</param>
	void Process(double delta);

	/// <summary>
	/// Called every physics frame. Use for logic that interacts with Godot's physics engine.
	/// </summary>
	/// <param name="delta">The time elapsed since the last physics frame.</param>
	void PhysicsProcess(double delta);
}