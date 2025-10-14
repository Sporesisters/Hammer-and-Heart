using System;
using Godot;
using Core.Utilities.Logging;

namespace Core.ECS;

/// <summary>
/// Immutable identifier for an entity.
/// Provides both a runtime-unique ID (sequential) and a persistent GUID.
/// </summary>
[GlobalClass]
public sealed partial class EntityIdentity : Node
{
	/// <summary>
	/// Runtime-unique ID, incremented sequentially.
	/// </summary>
	private static int _runtimeCounter = 0;

	/// <summary>
	/// Runtime-unique ID, incremented sequentially.
	/// Useful for debugging and in-memory references.
	/// </summary>
	public int RuntimeId { get; private set; }

	/// <summary>
	/// Persistent GUID, survives saves/loads.
	/// </summary>
	public Guid PersistentId { get; private set; }

	/// <summary>
	/// Broad category of the entity (Player, Enemy, Item, etc.).
	/// </summary>
	[Export] public EntityType Type { get; private set; } = EntityType.Unknown;

	/// <summary>
	/// Optional subtype (e.g., "Goblin", "Potion").
	/// </summary>
	[Export] public string SubType { get; private set; } = string.Empty;

	/// <summary>
	/// Initialize the EntityIdentity using a specification object.
	/// </summary>
	/// <param name="spec">Specification data including type, subtype, and optional persistent ID.</param>
	public void Initialize(EntityIdentitySpec spec)
	{
		if (RuntimeId != 0)
		{
			LoggerService.Error("EntityIdentity is already initialized.");
			return;
		}

		RuntimeId = ++_runtimeCounter;
		PersistentId = spec.PersistentId
			?? (PersistentId != Guid.Empty ? PersistentId : Guid.NewGuid());

		Type = spec.Type ?? Type;

		if (!string.IsNullOrWhiteSpace(spec.SubType))
		{
			SubType = spec.SubType;
		}
	}

	/// <summary>
	/// Shortened Persistent ID for debugging (first 8 characters).
	/// </summary>
	private string ShortGuid => PersistentId.ToString("N")[..8];

	/// <summary>
	/// Compact ID in the form Type#RuntimeId for logs and quick reference.
	/// </summary>
	public string ShortId => $"{Type}#{RuntimeId}";

	/// <summary>
	/// Resets the runtime ID counter to zero.
	/// Should be called at the end of gameplay/session to prevent large values.
	/// </summary>
	public static void ResetRuntimeCounter()
	{
		_runtimeCounter = 0;
		LoggerService.Debug("EntityIdentity runtime counter has been reset.");
	}

	/// <summary>
	/// Returns a full string representation for debugging.
	/// </summary>
	public override string ToString()
	{
		string typeName = string.IsNullOrEmpty(SubType) ? Type.ToString() : $"{Type}:{SubType}";
		return $"ID|{RuntimeId:D8}|{ShortGuid}|{typeName}";
	}
}
