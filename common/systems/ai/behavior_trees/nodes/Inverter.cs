using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes;

/// <summary>
/// A decorator node that inverts the result of its single child node.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Inverter"/> node ticks its single child and returns the opposite
/// status:
/// </para>
/// <list type="bullet">
/// <item><description>
/// If the child returns <see cref="NodeStatus.Success"/>, the inverter returns <see cref="NodeStatus.Failure"/>.
/// </description></item>
/// <item><description>
/// If the child returns <see cref="NodeStatus.Failure"/>, the inverter returns <see cref="NodeStatus.Success"/>.
/// </description></item>
/// <item><description>
/// If the child returns <see cref="NodeStatus.Running"/>, the inverter also returns <see cref="NodeStatus.Running"/>.
/// </description></item>
/// </list>
/// </remarks>
/// <param name="name">
/// The name of this node, used for identification and debugging within the behavior tree.
/// </param>
/// <param name="priority">
/// The execution priority of this node relative to its siblings. Higher values indicate higher priority.
/// </param>
public class Inverter(string name = "Inverter", int priority = 1) : Decorator(name, priority)
{
	protected override NodeStatus OnTick(float deltaTime)
	{
		NodeStatus status = Children[0].Tick(deltaTime);

		return status switch
		{
			NodeStatus.Success => NodeStatus.Failure,
			NodeStatus.Failure => NodeStatus.Success,
			_ => NodeStatus.Running
		};
	}
}
