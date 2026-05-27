using Godot;
using Core.Utilities.Logging;
using Core.Events;

namespace Core.ECS;

/// <summary>
/// Abstract base class for all components in the ECS system.
/// Components extend <see cref="Node"/> and can be attached to an <see cref="Entity"/>.
/// </summary>
[GlobalClass]
public abstract partial class ComponentBase : Node
{
	/// <summary>
	/// The <see cref="Entity"/> this component is currently attached to.
	/// Will be <c>null</c> if the component is not attached.
	/// </summary>
	public Entity? Entity { get; private set; }

	/// <summary>
	/// The <see cref="EntityId"/> associated with the entity that owns this component.
	/// </summary>
	public string EntityId => Entity?.EntityIdentity.ShortId ?? "Unknown";

	/// <summary>
	/// The <see cref="EventBus"/> associated with the entity that owns this component.
	/// </summary>
	public EventBus? EventBus => Entity?.EventBus;

	/// <summary>
	/// Determines whether this component can be added to the specified <see cref="Entity"/>.
	/// Logs a warning if the entity is null.
	/// </summary>
	/// <param name="entity">The entity to check against.</param>
	/// <returns><c>true</c> if this component can be added; otherwise, <c>false</c>.</returns>
	public bool CanBeAddedToEntity(Entity? entity)
	{
		if (entity is null)
		{
			LoggerService.Warning($"<{GetType().Name}> cannot be added: target entity is null.");
			return false;
		}

		return EvaluateCoexistenceRule(entity);
	}

	/// <summary>
	/// Evaluates whether this component can coexist with other components in the entity.
	/// Override in derived components to define custom rules (e.g., exclusivity).
	/// </summary>
	/// <param name="entity">The entity to evaluate against.</param>
	/// <returns><c>true</c> if coexistence is allowed; otherwise, <c>false</c>.</returns>
	protected virtual bool EvaluateCoexistenceRule(Entity entity) => true;

	/// <summary>
	/// Attaches this component to the given entity.
	/// Called internally by the entity when the component is added.
	/// </summary>
	/// <param name="entity">The entity this component is being attached to.</param>
	internal void AttachToEntity(Entity entity)
	{
		Entity = entity;
		OnAddedToEntity();
	}

	/// <summary>
	/// Detaches this component from its current entity.
	/// Called internally by the entity when the component is removed.
	/// </summary>
	internal void DetachFromEntity()
	{
		Entity = null;
		OnRemovedFromEntity();
	}

	/// <summary>
	/// Called when the component is added to an entity.
	/// Override to implement custom initialization logic.
	/// </summary>
	protected virtual void OnAddedToEntity() { }

	/// <summary>
	/// Called when the component is removed from an entity.
	/// Override to implement custom cleanup logic.
	/// </summary>
	protected virtual void OnRemovedFromEntity() { }
}
