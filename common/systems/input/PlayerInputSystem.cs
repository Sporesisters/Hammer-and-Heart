using Core.ECS;
using Core.ECS.Components;
using Godot;
using System.Collections.Generic;
using System.Linq;

namespace Core.Inputs;

/// <summary>
/// Processes player input and dispatches it to all components implementing <see cref="IInputReceiver"/>.
/// <para>
/// This system queries entities tagged with <see cref="PlayerTagComponent"/> and converts the current
/// input state into an <see cref="InputCommand"/> snapshot each frame. Components can then react
/// to this input without directly accessing the Godot input API.
/// </para>
/// <para>
/// Designed to be flexible: you can extend it for AI or other input sources by implementing
/// similar systems without modifying component code.
/// </para>
/// </summary>
public class PlayerInputSystem : IProcessSystem
{
	/// <inheritdoc/>
	public EntityWorld? World { get; set; }

	/// <summary>
	/// Called every frame to update the system.
	/// Queries all player entities and sends their input snapshots to any input-receiving components.
	/// </summary>
	/// <param name="delta">Time in seconds since the last frame.</param>
	public void Update(double delta)
	{
		if (World is null) return;

		foreach (Entity entity in World.Query<PlayerTagComponent>())
		{
			var playerTag = entity.GetComponent<PlayerTagComponent>();

			if (playerTag is null || playerTag.IsDisabled)
				continue;

			IEnumerable<IInputReceiver> receivers = entity.GetAllComponents().Values.OfType<IInputReceiver>();

			if (!receivers.Any())
				continue;

			InputCommand command = CollectInput();

			foreach (IInputReceiver receiver in receivers)
				receiver.ReceiveInput(command);
		}
	}

	/// <summary>
	/// Captures the current player input from Godot's input system.
	/// </summary>
	/// <returns>An <see cref="InputCommand"/> snapshot representing the current input state.</returns>
	private static InputCommand CollectInput()
	{
		Vector2 moveDirection = Input.GetVector(
			PlayerInputActions.MOVE_LEFT,
			PlayerInputActions.MOVE_RIGHT,
			PlayerInputActions.MOVE_UP,
			PlayerInputActions.MOVE_DOWN
		);

		bool attackPressed = Input.IsActionPressed(PlayerInputActions.ATTACK);
		bool swapCharacterPressed = Input.IsActionJustPressed(PlayerInputActions.SWAP_CHARACTER);

		return new InputCommand(moveDirection, attackPressed, swapCharacterPressed);
	}
}
