using Core.Inputs;

namespace Core.ECS.Components;

/// <summary>
/// Component that enables an entity to participate in character swapping.
/// This component listens for swap input commands and tracks when a swap should occur.
/// </summary>
public partial class EntitySwapComponent : ComponentBase, IInputReceiver
{
	/// <summary>
	/// Indicates whether this entity has requested a swap.
	/// Set to <c>true</c> when a swap input is received, <c>false</c> after consumption.
	/// </summary>
	public bool ShouldSwap { get; private set; } = false;

	/// <summary>
	/// Consumes the swap request, resetting <see cref="ShouldSwap"/> to <c>false</c>.
	/// </summary>
	/// <returns>
	/// <c>true</c> if a swap was requested and is now consumed; otherwise, <c>false</c>.
	/// </returns>
	public bool ConsumeSwap()
	{
		if (ShouldSwap)
		{
			ShouldSwap = false;
			return true;
		}

		return false;
	}

	/// <inheritdoc/>
	public void ReceiveInput(InputCommand command)
	{
		if (!command.SwapCharacterPressed) return;

		ShouldSwap = true;
	}
}
