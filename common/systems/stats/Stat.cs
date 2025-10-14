using Core.Stats.Modifiers;
using Godot;
using System;

namespace Core.Stats;

/// <summary>
/// Represents a base gameplay stat with support for minimum, maximum, current value,
/// and pre/post modifiers.
/// </summary>
public abstract class Stat
{
	/// <summary>
	/// The base value of the stat before modifiers.
	/// </summary>
	protected float _baseStatValue = 0;

	/// <summary>
	/// The minimum allowed value for the stat.
	/// </summary>
	protected float _minimumValue = 0;

	/// <summary>
	/// The maximum allowed value for the stat.
	/// </summary>
	protected float _maximumValue = 0;

	/// <summary>
	/// The current value of the stat after applying modifiers.
	/// </summary>
	protected float _currentStatValue = 0;

	/// <summary>
	/// Modifiers applied before calculations.
	/// </summary>
	protected StatModifier _preModifiers = new();

	/// <summary>
	/// Modifiers applied after calculations.
	/// </summary>
	protected StatModifier _postModifiers = new();

	/// <summary>
	/// Event invoked when the current value changes.
	/// </summary>
	public event Action<Stat>? OnValueChanged;

	/// <summary>
	/// Event invoked when the current value reaches or falls below the minimum.
	/// </summary>
	public event Action<Stat>? OnValueDepleted;

	/// <summary>
	/// Initializes a new instance of the <see cref="Stat"/> class.
	/// </summary>
	/// <param name="baseStatValue">The starting base value of the stat.</param>
	/// <param name="minimumValue">The minimum allowed value.</param>
	/// <param name="maximumValue">The maximum allowed value.</param>
	public Stat(float baseStatValue, float minimumValue, float maximumValue)
	{
		_minimumValue = Mathf.Max(0, minimumValue);
		_maximumValue = Mathf.Max(minimumValue + 1, maximumValue);
		_baseStatValue = Mathf.Clamp(baseStatValue, _minimumValue, _maximumValue);
		_currentStatValue = _baseStatValue;
	}

	/// <summary>
	/// Gets the base (unmodified) value of the stat.
	/// </summary>
	public float BaseStatValue => _baseStatValue;

	/// <summary>
	/// Gets the minimum allowed value for the stat.
	/// </summary>
	public float MinimumValue => _minimumValue;

	/// <summary>
	/// Gets the maximum allowed value for the stat.
	/// </summary>
	public float MaximumValue => _maximumValue;

	/// <summary>
	/// Gets or sets the current value of the stat.
	/// Setting the value clamps it between the minimum and maximum and triggers events as appropriate.
	/// </summary>
	public virtual float CurrentStatValue
	{
		get => _currentStatValue;
		set => UpdateCurrentValue(value);
	}

	/// <summary>
	/// Gets the total flat modifier applied before calculations.
	/// </summary>
	public float PreFlatModifier => _preModifiers.FlatValueModifier;

	/// <summary>
	/// Gets the total percentage modifier applied before calculations.
	/// </summary>
	public float PrePercentModifier => _preModifiers.PercentageModifier;

	/// <summary>
	/// Gets the total flat modifier applied after calculations.
	/// </summary>
	public float PostFlatModifier => _postModifiers.FlatValueModifier;

	/// <summary>
	/// Gets the total percentage modifier applied after calculations.
	/// </summary>
	public float PostPercentModifier => _postModifiers.PercentageModifier;

	/// <summary>
	/// Raises the <see cref="OnValueChanged"/> event.
	/// </summary>
	protected void RaiseValueChanged() => OnValueChanged?.Invoke(this);

	/// <summary>
	/// Raises the <see cref="OnValueDepleted"/> event.
	/// </summary>
	protected void RaiseValueDepleted() => OnValueDepleted?.Invoke(this);

	/// <summary>
	/// Sets the modifiers for this stat.
	/// </summary>
	/// <param name="scaleType">Whether the modifier applies pre- or post-calculation.</param>
	/// <param name="flatValue">Flat value adjustment.</param>
	/// <param name="percentageValue">Percentage adjustment.</param>
	public void SetModifiers(ModifierScaleType scaleType, float flatValue, float percentageValue)
	{
		StatModifier? target = scaleType switch
		{
			ModifierScaleType.PreScaling => _preModifiers,
			ModifierScaleType.PostScaling => _postModifiers,
			_ => null
		};

		target?.SetModifiers(flatValue, percentageValue);
		RecalculateValueBounds();
	}

	/// <summary>
	/// Updates the current stat value while respecting the minimum and maximum bounds.
	/// Also triggers <see cref="OnValueChanged"/> and <see cref="OnValueDepleted"/> as appropriate.
	/// </summary>
	/// <param name="newValue">The new value to set.</param>
	protected void UpdateCurrentValue(float newValue)
	{
		float clampedValue = Mathf.Clamp(newValue, _minimumValue, _maximumValue);

		if (!Mathf.IsEqualApprox(_currentStatValue, clampedValue))
		{
			_currentStatValue = clampedValue;
			RaiseValueChanged();

			if (_currentStatValue <= _minimumValue)
			{
				RaiseValueDepleted();
			}
		}
	}

	/// <summary>
	/// Recalculates the maximum/current value bounds.
	/// Must be implemented by subclasses according to their behavior.
	/// </summary>
	protected abstract void RecalculateValueBounds();
}
