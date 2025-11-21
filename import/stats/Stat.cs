using Core.Stats.Modifiers;
using Godot;
using System;

namespace Core.Stats;

/// <summary>
/// Represents a base stat with a value that can be modified by pre- and post-scaling modifiers.
/// Provides events for value changes and depletion.
/// </summary>
public abstract class Stat
{
	/// <summary>
	/// Gets the original, unmodified base value of the stat.
	/// </summary>
	public float BaseStatValue { get; protected set; }

	/// <summary>
	/// Gets the minimum allowable value of the stat.
	/// </summary>
	public float MinimumValue { get; protected set; }

	/// <summary>
	/// Gets the maximum allowable value of the stat, which may be scaled by modifiers.
	/// </summary>
	public float MaximumValue { get; protected set; }

	/// <summary>
	/// The backing value of the current stat value, clamped between <see cref="MinimumValue"/> and <see cref="MaximumValue"/>.
	/// </summary>
	private float _currentStatValue;

	/// <summary>
	/// Gets or sets the current value of the stat, clamped between <see cref="MinimumValue"/> and <see cref="MaximumValue"/>.
	/// Setting the value triggers <see cref="OnValueChanged"/> and <see cref="OnValueDepleted"/> if applicable.
	/// </summary>
	public virtual float CurrentStatValue
	{
		get => _currentStatValue;
		set => UpdateCurrentValue(value);
	}

	/// <summary>
	/// Pre-scaling modifiers applied before the base value is scaled.
	/// </summary>
	public StatModifier PreModifiers { get; private init; } = new();

	/// <summary>
	/// Post-scaling modifiers applied after the base value is scaled.
	/// </summary>
	public StatModifier PostModifiers { get; private init; } = new();

	/// <summary>
	/// Triggered whenever the stat's <see cref="CurrentStatValue"/> changes.
	/// </summary>
	public event Action<Stat>? OnValueChanged;

	/// <summary>
	/// Triggered whenever the stat's <see cref="CurrentStatValue"/> reaches <see cref="MinimumValue"/>.
	/// </summary>
	public event Action<Stat>? OnValueDepleted;

	/// <summary>
	/// Initializes a new stat with a base value, minimum, and maximum.
	/// The current value is set to the clamped base value.
	/// </summary>
	/// <param name="baseValue">Initial base value.</param>
	/// <param name="min">Minimum allowed value.</param>
	/// <param name="max">Maximum allowed value.</param>
	protected Stat(float baseValue, float min, float max)
	{
		MinimumValue = Mathf.Max(0, min);
		MaximumValue = Mathf.Max(MinimumValue + 1, max);

		BaseStatValue = Mathf.Clamp(baseValue, MinimumValue, MaximumValue);
		UpdateCurrentValue(BaseStatValue);
	}

	/// <summary>
	/// Sets the modifiers for this stat.
	/// </summary>
	/// <param name="type">Whether the modifiers are pre- or post-scaling.</param>
	/// <param name="flat">Flat modifier value.</param>
	/// <param name="percent">Percentage modifier (fractional).</param>
	public virtual void SetModifiers(ModifierScaleType type, float flat, float percent)
	{
		StatModifier? target = type switch
		{
			ModifierScaleType.PreScaling => PreModifiers,
			ModifierScaleType.PostScaling => PostModifiers,
			_ => null
		};

		target?.SetModifiers(flat, percent);
		RecalculateValueBounds();
	}

	/// <summary>
	/// Updates the current stat value, clamped to valid bounds.
	/// Triggers events if the value changes or reaches minimum.
	/// </summary>
	/// <param name="newValue">New value to set.</param>
	protected void UpdateCurrentValue(float newValue)
	{
		float value = Mathf.Clamp(newValue, MinimumValue, MaximumValue);

		if (!Mathf.IsEqualApprox(_currentStatValue, value))
		{
			_currentStatValue = value;
			OnValueChanged?.Invoke(this);

			if (_currentStatValue <= MinimumValue)
				OnValueDepleted?.Invoke(this);
		}
	}

	/// <summary>
	/// Calculates the final scaled maximum value after applying pre- and post-scaling modifiers.
	/// </summary>
	/// <returns>The scaled maximum stat value.</returns>
	protected float EvaluateScaledMax()
	{
		float preCalculation = (BaseStatValue + PreModifiers.FlatValueModifier) * (1f + PreModifiers.PercentageModifier);
		float postCalculation = preCalculation * (1f + PostModifiers.PercentageModifier) + PostModifiers.FlatValueModifier;
		return postCalculation;
	}

	/// <summary>
	/// Recalculates the current value and maximum based on modifiers.
	/// Must be implemented by derived classes.
	/// </summary>
	protected abstract void RecalculateValueBounds();
}
