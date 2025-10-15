namespace Core.Inputs;

/// <summary>
/// Interface for components that can receive InputCommand snapshots.
/// </summary>
public interface IInputReceiver
{
	/// <summary>
	/// Receives an InputCommand snapshot from the input handler.
	/// </summary>
	/// <param name="command">The input command snapshot.</param>
	void ReceiveInput(InputCommand command);
}
