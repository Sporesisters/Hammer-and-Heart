namespace Core.AI.BehaviourTrees.Nodes
{
	/// <summary>
	/// Executes all children in parallel, succeeds or fails depending on the policies.
	/// </summary>
	public enum ParallelPolicy { RequireAll, RequireOne }
}
