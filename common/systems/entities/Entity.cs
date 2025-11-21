using System;
using System.Collections.Generic;
using Core.Utilities.Logging;
using Core.Events;
using Core.ECS.Events;
using Core.ECS.Internals;

namespace Core.ECS;

/// <summary>
/// Represents an entity in the ECS system, capable of managing <see cref="ComponentBase"/> instances.
/// </summary>
public class Entity
{
	/// <summary>
	/// Stores all components attached to this entity, keyed by their type.
	/// Allows quick lookup and management of components.
	/// </summary>
	private readonly Dictionary<Type, ComponentBase> _components = [];

	/// <summary>
	/// Unique identifier for this entity.
	/// <para>
	/// <c>RuntimeId</c> is ephemeral and assigned by a registry or runtime system.
	/// <c>PersistentId</c> can be used for save/load or cross-scene references.
	/// </para>
	/// </summary>
	public EntityIdentity EntityIdentity { get; private set; } = null!;

	/// <summary>
	/// Event bus attached to this entity, used for decoupled event communication
	/// between components and systems.
	/// </summary>
	public EventBus EventBus { get; private set; } = null!;

	/// <summary>
	/// World this entity is part of. Used for querying and indexing components.
	/// </summary>
	public EntityWorld World { get; private set; } = null!;

	/// <summary>
	/// Root scene node of the entity's scene. Used for resolving components and systems.
	/// </summary>
	public EntityRoot Root { get; private set; } = null!;

	/// <summary>
	/// Initializes the entity with its identity and event bus.
	/// Safe to call multiple times; subsequent calls are ignored with a warning.
	/// </summary>
	/// <param name="spec">
	/// Entity specification containing type, subtype, and optional persistent ID.
	/// </param>
	/// <param name="entityWorld">The world this entity is part of.</param>
	/// <param name="entityRoot">The root scene node of the entity's scene.</param>
	public void Initialize(EntityIdentitySpec spec, EntityWorld entityWorld, EntityRoot entityRoot)
	{
		if (EntityIdentity is not null && EntityIdentity.RuntimeId is not 0)
		{
			LoggerService.Warning($"Entity already initialized: <{EntityIdentity.ShortId}>. Skipping.");
			return;
		}

		EntityIdentity ??= new EntityIdentity();
		EntityIdentity.Initialize(spec);

		EventBus ??= new EventBus();
		World = entityWorld;
		Root ??= entityRoot;
	}

	/// <summary>
	/// Destroys this entity, removing all components and unregistering from the world.
	/// Optionally frees the root scene node from the scene tree.
	/// </summary>
	/// <param name="freeRootNode">If <c>true</c>, the EntityRoot node is removed from the scene tree and freed.</param>
	public void Destroy(bool freeRootNode = true)
	{
		foreach (Type type in _components.Keys)
		{
			ComponentBase component = _components[type];
			component.DetachFromEntity();
			World.OnComponentRemoved(this, type);
		}

		_components.Clear();
		EventBus.Publish<EntityDestroyEvent>(new(this, freeRootNode));
	}

	/// <summary>
	/// Retrieves a component of the specified type attached to this entity.
	/// </summary>
	/// <typeparam name="T">Type of the component to retrieve.</typeparam>
	/// <returns>The component if found; otherwise, <c>null.</c></returns>
	public T? GetComponent<T>() where T : ComponentBase
	{
		Type type = typeof(T);

		if (_components.TryGetValue(type, out ComponentBase? component))
			return component as T;

		LoggerService.Debug($"Component <{type.Name}> not found.");
		return null;
	}

	/// <summary>
	/// Returns a read-only dictionary of all components attached to this entity.
	/// </summary>
	public IReadOnlyDictionary<Type, ComponentBase> GetAllComponents() => _components;

	/// <summary>
	/// Checks whether the entity has a component of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The component type to check for.</typeparam>
	/// <returns><c>True</c> if the component exists; otherwise, <c>false</c>.</returns>
	public bool HasComponent<T>() where T : ComponentBase => _components.ContainsKey(typeof(T));

	/// <summary>
	/// Adds a new instance of the specified component type to the entity.
	///
	/// This is a convenience method that constructs the component using its
	/// parameterless constructor and forwards it to <see cref="AddComponent{T}(T)"/>.
	/// </summary>
	/// <typeparam name="T">
	/// The component type to add. Must derive from <see cref="ComponentBase"/> and
	/// have a public parameterless constructor.
	/// </typeparam>
	/// <returns>
	/// <c>true</c> if the component was successfully added; <c>false</c> if a component
	/// of the same type already exists or registration was rejected.
	/// </returns>
	public bool AddComponent<T>() where T : ComponentBase, new()
		=> AddComponent(new T());

	/// <summary>
	/// Attempts to add the given component instance to the entity.
	///
	/// The component is registered only if no other component of the same type
	/// is already attached to the entity. If a duplicate exists, the operation
	/// fails and the component is not added.
	/// </summary>
	/// <typeparam name="T">
	/// The concrete type of the component being added.
	/// </typeparam>
	/// <param name="component">
	/// The component instance to attach to the entity.
	/// </param>
	/// <returns>
	/// <c>true</c> if the component was added; <c>false</c> if a duplicate type
	/// exists or registration was not permitted.
	/// </returns>
	public bool AddComponent<T>(T component) where T : ComponentBase
	{
		Type type = typeof(T);

		if (HasComponent<T>())
		{
			LoggerService.Error($"Cannot add duplicate component <{type.Name}>. Discarding new instance.");
			return false;
		}

		if (!CanRegisterComponent(component))
			return false;

		_components[type] = component;
		component.AttachToEntity(this);
		World.OnComponentAdded(this, type);

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

		_components.Remove(type);
		component.DetachFromEntity();
		World.OnComponentRemoved(this, type);

		return true;
	}

	/// <summary>
	/// Replaces a component of type <typeparamref name="TOld"/> with a new component
	/// of type <typeparamref name="TNew"/>. If the target component does not exist,
	/// the new component can optionally be added based on <paramref name="allowAddIfMissing"/>.
	/// </summary>
	/// <typeparam name="TNew">The type of the new component to add.</typeparam>
	/// <typeparam name="TOld">The type of the component to replace.</typeparam>
	/// <param name="allowAddIfMissing">
	/// If <c>true</c>, adds the new component even if no component of type <typeparamref name="TOld"/> exists.
	/// If <c>false</c>, the method will fail if the component to replace does not exist.
	/// </param>
	/// <returns>
	/// <c>true</c> if the component was successfully replaced or added; <c>false</c> otherwise.
	/// </returns>
	public bool ReplaceComponent<TNew, TOld>(bool allowAddIfMissing = false)
		where TNew : ComponentBase, new()
		where TOld : ComponentBase
	{
		string newTypeName = typeof(TNew).Name;
		string oldTypeName = typeof(TOld).Name;

		bool removed = RemoveComponent<TOld>();

		if (!removed && !allowAddIfMissing)
		{
			LoggerService.Warning($"Cannot replace component <{oldTypeName}>. It does not exist.");
			return false;
		}

		bool added = AddComponent<TNew>();

		if (added)
		{
			LoggerService.Info(removed
				? $"Component <{oldTypeName}> replaced successfully."
				: $"No existing <{oldTypeName}> found, added new <{newTypeName}> instead."
			);
		}
		else
		{
			LoggerService.Error($"Component <{newTypeName}> failed to add.");
		}

		return added;
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
