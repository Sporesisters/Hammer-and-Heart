using Godot;

namespace Core.AI.BehaviourTrees;

public partial class BehaviorTree : Node
{
	[Export]
	public bool TickPhysics = false;

	public BTNode? Root { get; set; }

	[Export]
	public ProcessState PsrocessState { get; set; }

	public enum ProcessState
	{
		Process,
		Physics,
		Idle,
	}

	public override void _Process(double delta)
	{
		if (!TickPhysics && Root is not null)
			Root.Process();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (TickPhysics && Root is not null)
			Root.Process();
	}

	private void SetProcess(bool process)
	{
		if (process)
		{
			SetPhysicsProcess(true);
			SetProcess(false);
		}
	}
}
