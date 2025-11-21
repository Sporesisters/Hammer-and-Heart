using Core.ECS.Events;
using Core.Inputs;

namespace Core.ECS.Components;

/// <summary>
/// Component that allows an entity to trigger a entity to swap with another entity.
///
/// When attached to an <see cref="Entity"/>, this component listens for swap input
/// (e.g., via <see cref="InputCommand.SwapCharacterPressed"/>). If swap input is detected,
/// it publishes a <see cref="PlayerSwapEvent"/> on the entity's <see cref="Entity.EventBus"/>.
/// </summary>
public partial class EntitySwapComponent : ComponentBase, IInputReceiver
{
	/// <inheritdoc/>
	public void ReceiveInput(InputCommand command)
	{
		if (!command.SwapCharacterPressed || IsDisabled) return;

		Entity?.World.EventBus.Publish(new PlayerSwapEvent());
		// EventBus?.Publish(new PlayerSwapEvent());
	}
}
