using Core.Events;
using Core.Utilities.Logging;
using Godot;

namespace Core.Systems;

/// <summary>
/// <para>
/// The <see cref="GameCore"/> class is the central singleton for the game.
/// It acts as a service hub and entry point for accessing global systems.
/// </para>
/// <para>
/// <see cref="GameCore"/> is a <c>Node</c> that should be placed in the root scene
/// (autoload/singleton) and is accessible globally via <see cref="Instance"/>.
/// Other systems and gameplay code can draw functionality from it without needing
/// to duplicate references or bootstrap logic.
/// </para>
/// </summary>
[GlobalClass]
public partial class GameCore : Node
{
	/// <summary>
	/// Singleton instance of the <see cref="GameCore"/>.
	/// Accessible globally after it enters the scene tree.
	/// </summary>
	public static GameCore Instance { get; private set; } = null!;

	/// <summary>
	/// Global event bus used for decoupled communication between systems.
	/// </summary>
	public EventBus EventBus { get; private set; } = new();

	public override void _EnterTree()
	{
		Instance = this;

		SetupLoggingMode();
		SubscribeToEvents();
	}

	public override void _ExitTree()
	{
		GetTree().Root.ChildExitingTree -= HandleEventCleanups;
	}

	/// <summary>
	/// Subscribes the <see cref="GameCore"/> to global events such as crashes and cleanup.
	/// </summary>
	private void SubscribeToEvents()
	{
		GetTree().Root.ChildExitingTree += HandleEventCleanups;
	}

	/// <summary>
	/// Configures debug-only settings such as log level.
	/// </summary>
	private static void SetupLoggingMode()
	{
		if (!OS.HasFeature("debug")) return;

		LoggerService.SetLogLevel(LogLevel.Debug);
	}

	/// <summary>
	/// Clears all event subscriptions when nodes exit the tree.
	/// Prevents stale references and memory leaks.
	/// </summary>
	private void HandleEventCleanups(Node node)
	{
		EventBus.ClearAll();
	}
}
