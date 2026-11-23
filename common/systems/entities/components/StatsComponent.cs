using System.Collections.Generic;
using Core.Stats;
using Core.Stats.Modifiers;
using Core.Timing.Types;
using Godot;
using Core.Utilities.Logging;

namespace Core.ECS.Components;

/// <summary>
/// A component that manages a set of <see cref="Stat"/> values for an <see cref="Entity"/>.
/// Provides APIs to add, query, and modify stats dynamically.
/// </summary>
[GlobalClass]
public partial class StatsComponent : ComponentBase
{
	/// <summary>Seconds of protection after taking health damage.</summary>
	[Export(PropertyHint.Range, "0,5,0.05")]
	public float DamageInvulnerabilitySeconds { get; set; }

	private readonly CountdownTimer _damageInvulnerability = new(0f);

	/// <summary>
	/// The internal mapping of stat types to their corresponding <see cref="Stat"/> instances.
	/// </summary>
	private readonly Dictionary<StatType, Stat> _statsMap = [];

	/// <summary>
	/// Adds multiple stats to this component.
	/// If a stat type already exists, it will be skipped and logged as a warning.
	/// </summary>
	/// <param name="newStatsMapping">The new stats to add.</param>
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
	/// Checks if a stat of a given type exists on this component.
	/// </summary>
	/// <param name="statId">The type of stat to check for.</param>
	/// <returns><c>true</c> if the stat exists, otherwise <c>false</c>.</returns>
	public bool HasStat(StatType statId) => _statsMap.ContainsKey(statId);

	/// <summary>Whether a registered stat has reached its minimum value.</summary>
	public bool IsDepleted(StatType statId) => _statsMap.TryGetValue(statId, out Stat? stat)
		&& stat.CurrentStatValue <= stat.MinimumValue;

	/// <summary>Applies a hit and returns the updated Health stat if damage was accepted.</summary>
	public Stat? ApplyDamage(float amount)
	{
		if (!float.IsFinite(amount) || amount <= 0f || _damageInvulnerability.IsRunning) return null;
		if (!_statsMap.TryGetValue(StatType.Health, out Stat? health) || health.CurrentStatValue <= health.MinimumValue)
			return null;

		// Start protection before changing health, which synchronously notifies listeners.
		if (DamageInvulnerabilitySeconds > 0f)
		{
			_damageInvulnerability.ResetWithNewDuration(DamageInvulnerabilitySeconds);
			_damageInvulnerability.Start();
		}

		health.CurrentStatValue -= amount;
		return health;
	}

	public override void _ExitTree() => _damageInvulnerability.Stop();

	/// <summary>
	/// Retrieves a stat by type.
	/// </summary>
	/// <param name="statId">The type of stat to retrieve.</param>
	/// <returns>The <see cref="Stat"/> if found, otherwise <c>null</c>.</returns>
	public Stat? GetStat(StatType statId)
	{
		if (_statsMap.TryGetValue(statId, out Stat? stat))
		{
			LoggerService.Debug($"[{EntityId}] Retrieved stat <{statId}> = {stat.CurrentStatValue}/{stat.MaximumValue}.");
			return stat;
		}

		LoggerService.Debug($"[{EntityId}] Requested <{statId}> but stat not found.");
		return null;
	}

	/// <summary>
	/// Sets modifiers for a given stat, such as flat or percentage adjustments.
	/// Logs a warning if the stat type does not exist.
	/// </summary>
	/// <param name="statId">The stat type to modify.</param>
	/// <param name="modifierTarget">The modifier scale type (e.g., base, bonus).</param>
	/// <param name="flatValue">Flat value adjustment.</param>
	/// <param name="percentValue">Percentage adjustment.</param>
	public void SetModifiers(StatType statId, ModifierScaleType modifierTarget, float flatValue, float percentValue)
	{
		if (_statsMap.TryGetValue(statId, out Stat? stat))
		{
			stat.SetModifiers(modifierTarget, flatValue, percentValue);
			LoggerService.Debug($"[{EntityId}] Updated <{statId}>: +{flatValue}, {percentValue}% (new value {stat.CurrentStatValue}/{stat.MaximumValue}).");
		}
		else
		{
			LoggerService.Warning($"[{EntityId}] Cannot set modifiers, <{statId}> stat not found.");
		}
	}

	/// <summary>
	/// Returns a string representation of all stats for debugging.
	/// </summary>
	public override string ToString()
	{
		if (_statsMap.Count is 0)
			return $"[{EntityId}] {GetType().Name}: (empty)";

		string debugOutput = $"[{EntityId}] {GetType().Name}:";

		foreach ((StatType key, Stat stat) in _statsMap)
			debugOutput += $"\n- {key}: {stat.CurrentStatValue} / {stat.MaximumValue}";

		LoggerService.Info(debugOutput);
		return debugOutput;
	}
}
