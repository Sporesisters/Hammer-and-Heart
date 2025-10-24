using System.Collections.Generic;

namespace Core.AI.BehaviourTrees.Internals;

/// <summary>
/// Abstract base class for all Behavior Tree nodes.
///
/// A <see cref="Node"/> defines the structure and lifecycle
/// (tick/reset) shared by all composite, decorator, and leaf nodes.
/// It cannot be used directly — only extended by concrete node types.
/// </summary>
/// <param name="name">The human-readable name of this node, used for debugging or visualization.</param>
/// <param name="priority">
/// The execution priority used by certain composite nodes (e.g., priority selectors).
/// Defaults to 0.
/// </param>
public abstract class Node(string name = "Node", int priority = 0)
{
	/// <summary>
	/// The human-readable name of this node, primarily for debugging or tree visualization.
	/// </summary>
	public readonly string Name = name;

	/// <summary>
	/// The priority value of this node, used by specific node types that order
	/// children (for example, <c>PrioritySelector</c>).
	/// </summary>
	public readonly int Priority = priority;

	/// <summary>
	/// Child nodes that belong to this node.
	///
	/// This list is <c>protected</c> so that only derived types (composites)
	/// can modify their child structure. Use <see cref="AddChild(Node)"/> to
	/// attach a new child node.
	/// </summary>
	protected readonly List<Node> Children = [];

	/// <summary>
	/// The index of the currently active child node.
	/// Used by composite nodes to track execution progress.
	/// </summary>
	protected int currentChild;

	/// <summary>
	/// Adds a child node to this node’s list of children.
	/// </summary>
	/// <param name="child">The child <see cref="Node"/> to attach.</param>
	public void AddChild(Node child) => Children.Add(child);

	/// <summary>
	/// Ticks this node once, advancing its behavior by a single step.
	/// </summary>
	/// <returns>
	/// A <see cref="NodeStatus"/> value indicating whether the node
	/// succeeded, failed, or is still running.
	/// </returns>
	public abstract NodeStatus Tick();

	/// <summary>
	/// Resets this node and all its children to their initial state.
	/// </summary>
	public virtual void Reset()
	{
		currentChild = 0;

		foreach (Node child in Children)
			child.Reset();
	}
}
