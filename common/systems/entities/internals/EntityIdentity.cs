using System;

namespace Core.ECS.Internals;

/// <summary>
/// Represents a unique identity for an entity within the ECS.
/// RuntimeId is ephemeral and assigned by the <see cref="EntityRegistry"/>.
/// PersistentId is optional and can be used for save/load or cross-scene reference.
/// </summary>
public class EntityIdentity
{
	/// <summary>
	/// Runtime-unique ID, assigned by the registry when the entity is added.
	/// Only meaningful within a specific registry and session.
	/// </summary>
	public int RuntimeId { get; private set; }

	/// <summary>
	/// Optional persistent GUID, survives scene reloads or save/load.
	/// </summary>
	public Guid PersistentId { get; private set; } = Guid.NewGuid();

	/// <summary>
	/// Broad category of the entity (Player, Enemy, Item, etc.)
	/// </summary>
	public EntityType Type { get; private set; } = EntityType.Unknown;

	/// <summary>
	/// Optional subtype (e.g., "Goblin", "Potion").
	/// </summary>
	public string SubType { get; private set; } = string.Empty;

	/// <summary>
	/// Shortened persistent ID (first 8 characters) for debugging.
	/// </summary>
	private string ShortGuid => PersistentId.ToString("N")[..8];

	/// <summary>
	/// Compact ID for logs and quick reference: Type#RuntimeId.
	/// </summary>
	public string ShortId => $"{Type}#{RuntimeId}";

	/// <summary>
	/// Initializes the identity. Must be called after creation.
	/// </summary>
	/// <param name="spec">Entity specification containing type, subtype, and optional persistent ID.</param>
	public void Initialize(EntityIdentitySpec spec)
	{
		PersistentId = spec.PersistentId ?? (PersistentId != Guid.Empty ? PersistentId : Guid.NewGuid());
		Type = spec.Type ?? Type;
		SubType = string.IsNullOrWhiteSpace(spec.SubType) ? SubType : spec.SubType;
	}

	/// <summary>
	/// Assigns or reassigns the ephemeral RuntimeId for this entity.
	/// Managed by the <see cref="EntityRegistry"/>; should not be set externally.
	/// RuntimeId is session-specific and not persisted across scenes or saves.
	/// </summary>
	/// <param name="runtimeId">The new runtime ID.</param>
	public void SetRuntimeId(int runtimeId) => RuntimeId = runtimeId;

	/// <summary>
	/// Resets the runtime ID for reuse. Called by the registry when re-adding to a new registry/scene.
	/// </summary>
	/// <param name="newRuntimeId">The new runtime ID assigned by the registry.</param>
	public void ResetRuntimeId(int newRuntimeId) => RuntimeId = newRuntimeId;

	/// <summary>
	/// Returns a full string representation for debugging.
	/// </summary>
	public override string ToString()
	{
		string typeName = string.IsNullOrEmpty(SubType) ? Type.ToString() : $"{Type}:{SubType}";
		return $"ID|{RuntimeId:D8}|{ShortGuid}|{typeName}";
	}
}
