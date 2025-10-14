using Godot;
using System;
using System.Collections.Generic;
using Core.Utilities.Logging;
using Core.Events;

namespace Core.ECS;

/// <summary>
/// Represents an entity in the ECS system, capable of managing <see cref="ComponentBase"/> instances.
/// </summary>
[GlobalClass]
public partial class Entity : Node
{
	/// <summary>
	/// Unique identifier for this entity.
	/// Handles both persistence (Guid) and runtime (int) IDs.
	/// </summary>
	[Export] public EntityIdentity EntityIdentity { get; private set; } = null!;

	/// <summary>
	/// Gets the <see cref="Events.EventBus"/> instance associated with this entity.
	/// Used to enable decoupled communication between components via event publishing
	/// and subscription. Components can subscribe to entity-specific events and
	/// broadcast events without direct references to each other.
	/// </summary>
	/// <remarks>
	/// Exported for visibility in the Godot editor, but it is generally initialized
	/// and managed automatically by the <see cref="Entity"/> itself.
	/// </remarks>
	[Export] public EventBus EventBus { get; private set; } = null!;

	/// <summary>
	/// Stores all components attached to this entity, keyed by their type.
	/// Allows quick lookup and management of components.
	/// </summary>
	private readonly Dictionary<Type, ComponentBase> _components = [];

	/// <summary>
	/// Tracks exit handlers for components, keyed by the component instance.
	/// Each handler is invoked when the component exits the scene tree,
	/// allowing automatic cleanup and deregistration.
	/// </summary>
	private readonly Dictionary<ComponentBase, Action> _exitHandlers = [];

	public override void _EnterTree()
	{
		// Warn if entity lacks essential components.
		if (EntityIdentity.RuntimeId == 0)
		{
			LoggerService.Warning($"Entity '{Name}' entered tree uninitialized. Initialize the entity first.");
		}

		ChildEnteredTree += OnChildEntered;
	}

	public override void _ExitTree()
	{
		ChildEnteredTree -= OnChildEntered;

		foreach (ComponentBase component in _components.Values)
		{
			component.DetachFromEntity();
		}

		foreach ((ComponentBase component, Action handler) in _exitHandlers)
		{
			if (IsInstanceValid(component))
			{
				component.TreeExited -= handler;
			}
		}

		_components.Clear();
		_exitHandlers.Clear();
	}

	/// <summary>
	/// Initializes the entity with its core essentials, including a unique identity,
	/// an event bus, and any default components required for proper functionality.
	/// <para>
	/// This method should be called before adding the entity to the scene tree
	/// when instantiating through code. It ensures that all essential systems and
	/// components are ready for use by child components or gameplay logic.
	/// </para>
	/// <para>
	/// Subsequent calls are idempotent and will log a warning instead of re-initializing.
	/// </para>
	/// </summary>
	/// <param name="spec">
	/// Data used to configure the entity's type, optional subtype, and persistent ID.
	/// </param>
	public void Initialize(EntityIdentitySpec spec)
	{
		if (EntityIdentity is null)
		{
			EntityIdentity = new EntityIdentity();
			AddChild(EntityIdentity);
			LoggerService.Debug($"EntityIdentity created for type <{spec.Type}>.");
		}
		else if (EntityIdentity.RuntimeId != 0)
		{
			LoggerService.Warning($"Entity already initialized: <{EntityIdentity.ShortId}>. Skipping re-initialization.");
			return;
		}

		EntityIdentity.Initialize(spec);

		if (EventBus is null)
		{
			EventBus = new EventBus();
			AddChild(EventBus);
			LoggerService.Debug($"EventBus created for entity <{EntityIdentity.ShortId}>.");
		}

		LoggerService.Info($"Entity <{EntityIdentity}> initialized successfully.");
	}

	/// <summary>
	/// Handles the event when a child node enters the tree.
	/// Attempts to register it as a component if applicable.
	/// </summary>
	/// <param name="child">The child node that entered the tree.</param>
	private void OnChildEntered(Node child)
	{
		if (child.GetParent() != this) return;
		TryRegisterComponent(child);
	}

	/// <summary>
	/// Gets a component of type <typeparamref name="T"/> attached to this entity.
	/// </summary>
	/// <typeparam name="T">The type of component to retrieve.</typeparam>
	/// <returns>The component of type <typeparamref name="T"/>, or null if not found.</returns>
	public T? GetComponent<T>() where T : ComponentBase
	{
		Type type = typeof(T);

		if (_components.TryGetValue(type, out ComponentBase? component))
		{
			return component as T;
		}

		LoggerService.Debug($"Component <{type.Name}> not found.");
		return null;
	}

	/// <summary>
	/// Gets a read-only dictionary of all components attached to this entity.
	/// </summary>
	/// <returns>A read-only dictionary mapping component types to instances.</returns>
	public IReadOnlyDictionary<Type, ComponentBase> GetAllComponents() => _components;

	/// <summary>
	/// Checks whether the entity has a component of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The component type to check for.</typeparam>
	/// <returns><c>True</c> if the component exists; otherwise, <c>false</c>.</returns>
	public bool HasComponent<T>() where T : ComponentBase => _components.ContainsKey(typeof(T));

	/// <summary>
	/// Attempts to add a component to the entity.
	/// </summary>
	/// <typeparam name="T">The type of the component to add.</typeparam>
	/// <param name="component">The component instance to add.</param>
	/// <returns><c>True</c> if successfully added; <c>false</c> if a duplicate exists or registration failed.</returns>
	public bool TryAddComponent<T>(T component) where T : ComponentBase
	{
		Type type = typeof(T);

		if (HasComponent<T>())
		{
			LoggerService.Error($"Cannot add duplicate component <{type.Name}>. Discarding new instance.");
			component.Free();
			return false;
		}

		if (!CanRegisterComponent(component))
		{
			component.Free();
			return false;
		}

		if (component.GetParent() != this)
		{
			AddChild(component);
		}

		_components[type] = component;
		component.AttachToEntity(this);

		void OnExit()
		{
			if (_components.TryGetValue(type, out ComponentBase? c) && c == component)
			{
				component.DetachFromEntity();
				_components.Remove(type);
				LoggerService.Info($"Component <{type.Name}> auto-removed on TreeExit.");
			}

			if (_exitHandlers.Remove(component))
			{
				component.TreeExited -= OnExit;
			}
		}

		component.TreeExited += OnExit;
		_exitHandlers[component] = OnExit;

		return true;
	}

	/// <summary>
	/// Removes a component of type <typeparamref name="T"/> from the entity.
	/// </summary>
	/// <typeparam name="T">The type of the component to remove.</typeparam>
	/// <returns><c>True</c> if removed successfully; otherwise, <c>false</c>.</returns>
	public bool RemoveComponent<T>() where T : ComponentBase
	{
		Type type = typeof(T);

		if (!_components.TryGetValue(type, out ComponentBase? component))
		{
			LoggerService.Warning($"Cannot remove component <{type.Name}>. Not found.");
			return false;
		}

		component.DetachFromEntity();
		_components.Remove(type);

		if (_exitHandlers.Remove(component, out Action? handler))
		{
			component.TreeExited -= handler;
		}

		component.QueueFree();
		return true;
	}

	/// <summary>
	/// Replaces the component of type <typeparamref name="T"/> with a new one.
	/// Optionally adds it if none exists.
	/// </summary>
	/// <typeparam name="T">The type of the component to replace.</typeparam>
	/// <param name="newComponent">
	/// The new component to add. It will be freed automatically if not used.
	/// </param>
	/// <param name="allowAddIfMissing">
	/// If true, adds the component when none exists; otherwise fails.
	/// </param>
	/// <returns>
	/// <c>True</c> if the component was replaced or added; <c>false</c> otherwise.
	/// </returns>
	public bool ReplaceComponent<T>(ComponentBase newComponent, bool allowAddIfMissing = false) where T : ComponentBase
	{
		string typeName = typeof(T).Name;
		bool removed = RemoveComponent<T>();

		if (!removed && !allowAddIfMissing)
		{
			LoggerService.Warning($"Cannot replace component <{typeName}>. It does not exist.");
			newComponent.Free();
			return false;
		}

		string newTypeName = newComponent.GetType().Name;
		bool added = TryAddComponent(newComponent);

		if (added)
		{
			LoggerService.Info(removed
				? $"Component <{typeName}> replaced successfully."
				: $"No existing <{typeName}> found, added new <{newTypeName}> instead."
			);
		}
		else
		{
			LoggerService.Error($"Component <{typeName}> failed to add.");
		}

		return added;
	}

	/// <summary>
	/// Attempts to register a node as a component if it is a <see cref="ComponentBase"/>.
	/// </summary>
	/// <param name="node">The node to register.</param>
	private void TryRegisterComponent(Node node)
	{
		if (node is not ComponentBase component) return;

		Type type = component.GetType();

		if (_components.ContainsKey(type))
		{
			LoggerService.Error($"Duplicate component <{type.Name}> detected as child. Destroying it.");
			node.QueueFree();
			return;
		}

		if (!CanRegisterComponent(component))
		{
			node.QueueFree();
			return;
		}

		_components[type] = component;
		component.AttachToEntity(this);

		void OnExit()
		{
			if (_components.TryGetValue(type, out ComponentBase? c) && c == component)
			{
				component.DetachFromEntity();
				_components.Remove(type);
				LoggerService.Info($"Component <{type.Name}> auto-removed on TreeExit.");
			}

			if (_exitHandlers.Remove(component))
			{
				component.TreeExited -= OnExit;
			}
		}

		component.TreeExited += OnExit;
		_exitHandlers[component] = OnExit;
	}

	/// <summary>
	/// Determines whether a component can be registered on this entity.
	/// Checks for conflicts with existing components.
	/// </summary>
	/// <param name="newComponent">The component to check.</param>
	/// <returns><c>True</c> if the component can be added; otherwise, <c>false</c>.</returns>
	private bool CanRegisterComponent(ComponentBase newComponent)
	{
		foreach (ComponentBase existingComponent in _components.Values)
		{
			if (!existingComponent.CanBeAddedToEntity(this) || !newComponent.CanBeAddedToEntity(this))
			{
				LoggerService.Error($"Cannot add <{newComponent.GetType().Name}>. Blocked by <{existingComponent.GetType().Name}>.");
				return false;
			}
		}

		return true;
	}
}
