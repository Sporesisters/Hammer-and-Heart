using Core.Events;
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

		_eventBus.AddListener<GreetEvent>(OnTestEvent);
		_eventBus.Publish(new GreetEvent("Hello World"));
	}

	private void OnTestEvent(GreetEvent @event)
	{
		LoggerService.Info(@event.Name);
		LoggerService.Debug("Event Fired!!");
	}
}

public readonly struct GreetEvent(string Name) : IEvent
{
	public string Name { get; } = Name;
}
