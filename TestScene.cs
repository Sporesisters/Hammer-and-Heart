using System.Collections.Generic;
using Core.ECS;
using Core.ECS.Components;
using Core.Events;
using Core.Stats;
using Core.Stats.Types;
using Core.Utilities.Logging;
using Godot;

public partial class TestScene : Node
{
	private EventBus _eventBus = new();

	public override void _Ready()
	{
		LoggerService.SetLogLevel(LogLevel.Debug);
		LoggerService.Debug("Debug");
		LoggerService.Info("Info");
		LoggerService.Warning("Warning");
		LoggerService.Error("Error");
		LoggerService.Fatal("Fatal");

		AddChild(_eventBus);
		_eventBus.AddListener<GreetEvent>(OnTestEvent);
		_eventBus.Publish(new GreetEvent("Hello World"));

		CreateEntity();
	}

	private void OnTestEvent(GreetEvent @event)
	{
		LoggerService.Info(@event.Name);
		LoggerService.Debug("Event Fired!!");
	}

	private void CreateEntity()
	{
		Entity entity = new();
		entity.Initialize(new EntityIdentitySpec(EntityType.Player, "Elina"));
		AddChild(entity);

		entity.TryAddComponent(new StatsComponent());

		Dictionary<StatType, Stat> entityStatsMapping = new()
		{
			[StatType.Health] = new ScaledStat(100, 0, 100),
			[StatType.Damage] = new ScaledStat(50, 0, 50),
			[StatType.MoveSpeed] = new LockedStat(8, 0, 20),
		};

		var statsComponent = entity.GetComponent<StatsComponent>();
		statsComponent?.AddStats(entityStatsMapping);

		statsComponent?.ToString();
	}
}

public readonly struct GreetEvent(string Name) : IEvent
{
	public string Name { get; } = Name;
}
