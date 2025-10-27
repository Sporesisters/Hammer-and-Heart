using System.Linq;
using Core.ECS;
using Godot;

namespace Core.Inputs.Internals;

/// <summary>
/// Base class for input providers (Player, AI, Network).
/// Produces InputCommands for a target entity.
/// </summary>
[GlobalClass]
public abstract partial class InputHandler : Node
{
	/// <summary>
	/// The entity to which this input handler is attached.
	/// </summary>
	[Export]
	public Entity? InputTarget { get; set; }

	public override void _Process(double delta)
	{
		DispatchInput();
	}

	/// <summary>
	/// Collects all relevant input for this frame and
	/// returns it as an immutable <see cref="InputCommand"/>.
	/// </summary>
	/// <returns>A snapshot of the input state this frame.</returns>
	public abstract InputCommand CollectInput();

	/// <summary>
	/// Dispatch the collected input to the target entity's components.
	/// </summary>
	public void DispatchInput()
	{
		if (InputTarget is null) return;

		InputCommand command = CollectInput();
		var inputReceivers = InputTarget.GetAllComponents().Values.OfType<IInputReceiver>();

		foreach (IInputReceiver component in inputReceivers)
			component.ReceiveInput(command);
	}
}
