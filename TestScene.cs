using Core.Utilities.Logging;
using Godot;

public partial class TestScene : Node
{
	public override void _Ready()
	{
		LoggerService.SetLogLevel(LogLevel.Debug);
		LoggerService.Debug("Debug");
		LoggerService.Info("Info");
		LoggerService.Warning("Warning");
		LoggerService.Error("Error");
		LoggerService.Fatal("Fatal");
	}
}
