using Core.ECS.Events;
using Core.Inputs;
using Core.Utilities.Logging;
using Godot;

namespace Core.ECS.Components;

/// <summary>
/// Component that allows an entity to trigger a entity to swap with another entity.
///
/// When attached to an <see cref="Entity"/>, this component listens for swap input
/// (e.g., via <see cref="InputCommand.SwapCharacter"/>). If swap input is detected,
/// it publishes a <see cref="PlayerSwapEvent"/> on the entity's <see cref="Events.EventBus"/>.
/// </summary>
[GlobalClass]
public partial class EntitySwapComponent : ComponentBase, IInputReceiver
{
	public void ReceiveInput(InputCommand command)
	{
		if (command.SwapCharacter)
		{
			LoggerService.Debug($"<{EntityId}> Swap input detected on entity. Publishing PlayerSwapEvent...");
			EventBus?.Publish(new PlayerSwapEvent());
		}
	}
}
