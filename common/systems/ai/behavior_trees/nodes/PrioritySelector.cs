using System.Collections.Generic;
using System.Linq;
using Core.AI.BehaviourTrees.Internals;

namespace Core.AI.BehaviourTrees.Nodes;

/// <summary>
/// A composite node that ticks its children in order of descending priority.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="PrioritySelector"/> node evaluates its child nodes based on their
/// <see cref="Node.Priority"/> property, from highest to lowest.
/// </para>
/// <para>
/// It ticks each child sequentially until one returns <see cref="NodeStatus.Success"/>.
/// If a child returns <see cref="NodeStatus.Running"/>, the selector immediately
/// returns <see cref="NodeStatus.Running"/> and continues ticking that child on the next update.
/// If all children fail, the selector returns <see cref="NodeStatus.Failure"/>.
/// </para>
/// <para>
/// This node is useful when you want high-priority behaviors to preempt lower-priority ones.
/// </para>
/// </remarks>
/// <param name="name">
/// The name of this node, used for identification and debugging within the behavior tree.
/// </param>
/// <param name="priority">
/// The execution priority of this node relative to its siblings. Higher values indicate higher priority.
/// </param>
public class PrioritySelector(string name = "PrioritySelector", int priority = 1) : Selector(name, priority)
{
	private List<Node>? _sortedChildren;

	/// <summary>
	/// Gets the children sorted by descending priority. Cached for efficiency.
	/// </summary>
	protected List<Node> SortedChildren => _sortedChildren ??= SortChildren();

	/// <summary>
	/// Sorts the children by descending <see cref="Node.Priority"/>. Can be overridden in subclasses.
	/// </summary>
	/// <returns>A new list of child nodes sorted by priority.</returns>
	protected virtual List<Node> SortChildren() => Children.OrderByDescending(child => child.Priority).ToList();

	/// <summary>
	/// Ticks the children in priority order until one succeeds, one is running, or all fail.
	/// </summary>
	/// <param name="deltaTime">Time elapsed since last tick.</param>
	/// <returns>
	/// <see cref="NodeStatus.Success"/> if a child succeeds,
	/// <see cref="NodeStatus.Running"/> if a child is still running,
	/// or <see cref="NodeStatus.Failure"/> if all children fail.
	/// </returns>
	protected override NodeStatus OnTick(float deltaTime)
	{
		foreach (Node child in SortedChildren)
		{
			NodeStatus status = child.Tick(deltaTime);

			switch (status)
			{
				case NodeStatus.Running:
					return NodeStatus.Running;

				case NodeStatus.Success:
					Reset();
					return NodeStatus.Success;

				case NodeStatus.Failure:
				default:
					continue;
			}
		}

		Reset();
		return NodeStatus.Failure;
	}

	/// <summary>
	/// Resets the selector and clears the cached sorted children.
	/// </summary>
	public override void Reset()
	{
		base.Reset();
		_sortedChildren = null;
	}
}
