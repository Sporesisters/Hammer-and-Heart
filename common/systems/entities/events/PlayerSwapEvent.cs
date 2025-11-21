using Core.Events;

namespace Core.ECS.Events;

/// <summary>
/// Event that signals the player wishes to swap control to the next entity.
///
/// This event is published by <see cref="Components.EntitySwapComponent"/>
/// when the swap input is detected, and is listened to by
/// <see cref="Systems.EntitySwapSystem"/> to perform the actual entity swap.
/// </summary>
public readonly struct PlayerSwapEvent : IEvent { }
