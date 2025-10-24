using Godot;

namespace Core.BehaviourTrees
{
	// -------------------------
	// Example: Tick Driver Node
	// -------------------------
	public class BehaviorTreeComponent : Node
	{
		public BTNode Root;
		[Export] public bool TickPhysics = false;

		public override void _Process(double delta)
		{
			if (!TickPhysics && Root != null)
				Root.Process();
		}

		public override void _PhysicsProcess(double delta)
		{
			if (TickPhysics && Root != null)
				Root.Process();
		}
	}
}
