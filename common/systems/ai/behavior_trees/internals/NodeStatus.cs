namespace Core.AI.BehaviourTrees.Internals;

/// <summary>
/// Represents the possible execution outcomes of a behavior tree node.
/// </summary>
public enum NodeStatus
{
    /// <summary>
    /// Indicates that the node has completed its task successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Indicates that the node has failed to complete its task or that its condition was not met.
    /// </summary>
    Failure,

    /// <summary>
    /// Indicates that the node is still executing and should be processed again on the next tick.
    /// </summary>
    Running,
}
