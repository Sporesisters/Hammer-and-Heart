using System;
using System.Collections.Generic;

namespace Core.ECS.Internals;

/// <summary>
/// Provides a reverse lookup index mapping component types to the entities that contain them.
/// This supports efficient queries over component composition, such as retrieving all entities
/// that contain a given component or a set of components.
/// </summary>
public class ComponentIndex
{
	/// <summary>
	/// Maps a component type to the set of entity IDs that currently contain that type.
	/// The key includes both specific component types and their base component types
	/// (excluding <see cref="ComponentBase"/>).
	/// </summary>
	private readonly Dictionary<Type, HashSet<int>> _componentsToEntitiesId = [];

	/// <summary>
	/// Registers that the specified entity has acquired a component of the given type.
	/// The component type and all of its ancestors up to (but excluding) <see cref="ComponentBase"/>
	/// are indexed. This enables systems to query using base component types.
	/// </summary>
	/// <param name="entityId">The ID of the entity that now contains the component.</param>
	/// <param name="componentType">The exact runtime type of the component being added.</param>
	/// <exception cref="ArgumentException">
	/// Thrown if <paramref name="componentType"/> does not derive from <see cref="ComponentBase"/>.
	/// </exception>
	public void AddComponentReference(int entityId, Type componentType)
	{
		if (!typeof(ComponentBase).IsAssignableFrom(componentType))
			throw new ArgumentException($"Type '{componentType}' does not derive from ComponentBase and cannot be indexed.");

		foreach (Type type in GetComponentTypeHierarchy(componentType))
		{
			if (!_componentsToEntitiesId.TryGetValue(type, out HashSet<int>? entities))
			{
				entities = [];
				_componentsToEntitiesId[type] = entities;
			}

			entities.Add(entityId);
		}
	}

	/// <summary>
	/// Removes the association between the specified entity and a component type.
	/// The component type and all of its ancestor component types (up to <see cref="ComponentBase"/>) are unindexed.
	/// If a type no longer has any entities referencing it, the type entry is removed entirely.
	/// </summary>
	/// <param name="entityId">The ID of the entity losing the component.</param>
	/// <param name="componentType">The exact runtime type of the component being removed.</param>
	/// <exception cref="ArgumentException">
	/// Thrown if <paramref name="componentType"/> does not derive from <see cref="ComponentBase"/>.
	/// </exception>
	public void RemoveComponentReference(int entityId, Type componentType)
	{
		if (!typeof(ComponentBase).IsAssignableFrom(componentType))
			throw new ArgumentException($"Type '{componentType}' does not derive from ComponentBase and cannot be unindexed.");

		foreach (Type type in GetComponentTypeHierarchy(componentType))
		{
			if (_componentsToEntitiesId.TryGetValue(type, out HashSet<int>? entities))
			{
				entities.Remove(entityId);

				if (entities.Count is 0)
					_componentsToEntitiesId.Remove(type);
			}
		}
	}

	/// <summary>
	/// Returns all entity IDs that contain every component type specified.
	/// If any component type is not present in the index, no entities are returned.
	/// </summary>
	/// <param name="componentTypes">The component types that an entity must contain to be included.</param>
	/// <returns>
	/// A sequence of entity IDs that have all component types in <paramref name="componentTypes"/>.
	/// </returns>
	public IEnumerable<int> GetEntitiesWith(params Type[] componentTypes)
	{
		if (componentTypes.Length is 0)
			yield break;

		SortTypesByEntityCount(componentTypes);

		if (!_componentsToEntitiesId.TryGetValue(componentTypes[0], out HashSet<int>? baseSet))
			yield break;

		HashSet<int> result = [.. baseSet];

		for (int i = 1; i < componentTypes.Length; i++)
		{
			if (!_componentsToEntitiesId.TryGetValue(componentTypes[i], out HashSet<int>? nextSet))
				yield break;

			result.IntersectWith(nextSet);
		}

		foreach (int entityId in result)
			yield return entityId;
	}

	/// <summary>
	/// Returns whether the specified entity currently contains a component of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The component type to check for. Must derive from <see cref="ComponentBase"/>.</typeparam>
	/// <param name="entityId">The ID of the entity being checked.</param>
	/// <returns><c>true</c> if the entity contains the component; otherwise, <c>false</c>.</returns>
	public bool HasComponent<T>(int entityId) where T : ComponentBase =>
		_componentsToEntitiesId.TryGetValue(typeof(T), out HashSet<int>? entities) && entities.Contains(entityId);

	/// <summary>
	/// Removes all index data, leaving the index empty.
	/// </summary>
	public void Clear() => _componentsToEntitiesId.Clear();

	/// <summary>
	/// Enumerates the component type and all of its inherited component types,
	/// stopping before reaching <see cref="ComponentBase"/>.
	/// </summary>
	/// <param name="type">The component implementation type.</param>
	/// <returns>An enumeration of that type followed by its ancestor component types.</returns>
	private static IEnumerable<Type> GetComponentTypeHierarchy(Type type)
	{
		while (type is not null && type != typeof(ComponentBase))
		{
			yield return type;
			type = type.BaseType!;
		}
	}

	/// <summary>
	/// Orders the provided component types so that component types referenced by fewer entities come first.
	/// This minimizes cost when intersecting indexed sets.
	/// </summary>
	/// <param name="componentTypes">An array of component types to reorder.</param>
	private void SortTypesByEntityCount(Type[] componentTypes)
	{
		Array.Sort(componentTypes, (a, b) =>
		{
			_componentsToEntitiesId.TryGetValue(a, out HashSet<int>? setA);
			_componentsToEntitiesId.TryGetValue(b, out HashSet<int>? setB);
			return (setA?.Count ?? 0).CompareTo(setB?.Count ?? 0);
		});
	}
}
