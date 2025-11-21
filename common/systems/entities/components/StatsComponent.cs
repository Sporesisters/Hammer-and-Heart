using System;
using System.Collections.Generic;
using Core.ECS.Events;
using Core.Stats;
using Core.Stats.Modifiers;
using Core.Utilities.Logging;

namespace Core.ECS.Components;

/// <summary>
/// Manages a collection of <see cref="Stat"/> instances for an entity.
/// Provides APIs for adding, querying, modifying stats, and registering stat-related events.
/// </summary>
public class StatsComponent : ComponentBase
{
	/// <summary>
	/// Internal mapping of stat types to their corresponding <see cref="Stat"/> objects.
	/// </summary>
	private readonly Dictionary<StatType, Stat> _statsMap = [];

	/// <summary>
	/// Stores the event handlers subscribed to each stat's <see cref="Stat.OnValueChanged"/> event.
	/// </summary>
	private readonly Dictionary<StatType, Action<Stat>> _onChangedHandlers = [];

	/// <summary>
	/// Stores the event handlers subscribed to each stat's <see cref="Stat.OnValueDepleted"/> event.
	/// </summary>
	private readonly Dictionary<StatType, Action<Stat>> _onDepletedHandlers = [];

	/// <summary>
	/// Adds multiple stats to the component. Existing stats are ignored and logged as warnings.
	/// </summary>
	/// <param name="newStatsMapping">The stats to add.</param>
	public void AddStats(Dictionary<StatType, Stat> newStatsMapping)
	{
		foreach ((StatType statId, Stat value) in newStatsMapping)
		{
			if (HasStat(statId))
			{
				LoggerService.Warning($"[{EntityId}] Duplicate stat entry ignored <{statId}>.");
				continue;
			}

			_statsMap.Add(statId, value);
			LoggerService.Info($"[{EntityId}] Added stat <{statId}> with value {value.CurrentStatValue}/{value.MaximumValue}.");
		}
	}

	/// <summary>
	/// Adds a single stat to the component. Existing stats are ignored and logged as warnings.
	/// </summary>
	/// <param name="statId">The type of stat to add.</param>
	/// <param name="stat">The stat object.</param>
	public void AddStat(StatType statId, Stat stat)
	{
		if (HasStat(statId))
		{
			LoggerService.Warning($"[{EntityId}] Duplicate stat entry ignored <{statId}>.");
			return;
		}

		_statsMap.Add(statId, stat);
		LoggerService.Info($"[{EntityId}] Added stat <{statId}> with value {stat.CurrentStatValue}/{stat.MaximumValue}.");
	}

	/// <summary>
	/// Checks whether the component has a stat of the specified type.
	/// </summary>
	/// <param name="statId">The stat type to check.</param>
	/// <returns><c>true</c> if the stat exists; otherwise, <c>false</c>.</returns>
	public bool HasStat(StatType statId) => _statsMap.ContainsKey(statId);

	/// <summary>
	/// Retrieves a stat by type.
	/// </summary>
	/// <param name="statId">The type of stat to retrieve.</param>
	/// <returns>The stat if found; otherwise, <c>null</c>.</returns>
	public Stat? GetStat(StatType statId)
	{
		if (_statsMap.TryGetValue(statId, out Stat? stat))
		{
			LoggerService.Debug($"[{EntityId}] Retrieved stat <{statId}> = {stat.CurrentStatValue}/{stat.MaximumValue}.");
			return stat;
		}

		LoggerService.Warning($"[{EntityId}] Requested <{statId}> but stat not found.");
		return null;
	}

	/// <summary>
	/// Applies modifiers to a stat, such as flat and percentage adjustments.
	/// Logs a warning if the stat type does not exist.
	/// </summary>
	/// <param name="statId">The stat type to modify.</param>
	/// <param name="modifierTarget">The target modifier scale.</param>
	/// <param name="flatValue">Flat adjustment value.</param>
	/// <param name="percentValue">Percentage adjustment.</param>
	public void SetModifiers(StatType statId, ModifierScaleType modifierTarget, float flatValue, float percentValue)
	{
		if (_statsMap.TryGetValue(statId, out Stat? stat))
		{
			stat.SetModifiers(modifierTarget, flatValue, percentValue);

			LoggerService.Debug(
				$"[{EntityId}] Updated <{statId}>: +{flatValue}, {percentValue}% (new value {stat.CurrentStatValue}/{stat.MaximumValue})."
			);
		}
		else
		{
			LoggerService.Warning($"[{EntityId}] Cannot set modifiers, <{statId}> stat not found.");
		}
	}

	/// <summary>
	/// Registers events for a specific stat, allowing external systems to respond
	/// when the stat changes or is depleted. Handlers are stored internally for safe unregistration.
	/// </summary>
	/// <param name="statId">The type of stat to register events for.</param>
	/// <param name="onChanged">
	/// If <c>true</c>, subscribes to the <see cref="Stat.OnValueChanged"/> event
	/// and publishes a <see cref="StatChangedEvent"/> to the <see cref="Entity.EventBus"/>.
	/// </param>
	/// <param name="onDepleted">
	/// If <c>true</c>, subscribes to the <see cref="Stat.OnValueDepleted"/> event
	/// and publishes a <see cref="StatDepletedEvent"/> to the <see cref="Entity.EventBus"/>.
	/// </param>
	public void RegisterEvents(StatType statId, bool onChanged = true, bool onDepleted = true)
	{
		if (!_statsMap.TryGetValue(statId, out Stat? stat))
			return;

		if (onChanged && !_onChangedHandlers.ContainsKey(statId))
		{
			void handler(Stat stat) => EventBus?.Publish(new StatChangedEvent(statId, stat));
			_onChangedHandlers[statId] = handler;
			stat.OnValueChanged += handler;
		}

		if (onDepleted && !_onDepletedHandlers.ContainsKey(statId))
		{
			void handler(Stat stat) => EventBus?.Publish(new StatDepletedEvent(statId, stat));
			_onDepletedHandlers[statId] = handler;
			stat.OnValueDepleted += handler;
		}
	}

	/// <summary>
	/// Unregisters previously registered stat events for a specific stat.
	/// This ensures event handlers are removed to prevent memory leaks or unwanted event firing.
	/// </summary>
	/// <param name="statId">The type of stat to unregister events for.</param>
	/// <param name="onChanged">
	/// If <c>true</c>, unsubscribes from the <see cref="Stat.OnValueChanged"/> event.
	/// </param>
	/// <param name="onDepleted">
	/// If <c>true</c>, unsubscribes from the <see cref="Stat.OnValueDepleted"/> event.
	/// </param>
	public void UnregisterEvents(StatType statId, bool onChanged = true, bool onDepleted = true)
	{
		if (!_statsMap.TryGetValue(statId, out Stat? stat))
			return;

		if (onChanged && _onChangedHandlers.TryGetValue(statId, out Action<Stat>? changedHandler))
		{
			stat.OnValueChanged -= changedHandler;
			_onChangedHandlers.Remove(statId);
		}

		if (onDepleted && _onDepletedHandlers.TryGetValue(statId, out Action<Stat>? depletedHandler))
		{
			stat.OnValueDepleted -= depletedHandler;
			_onDepletedHandlers.Remove(statId);
		}
	}
}
