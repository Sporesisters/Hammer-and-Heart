using System.Collections.Generic;
using Core.Utilities.Logging;

namespace Core.AI.BehaviourTrees.Internals;

/// <summary>
/// Represents the abstract base class for all Behavior Tree nodes.
/// <para>
/// A <see cref="Node"/> defines the structure and lifecycle shared by all
/// composite, decorator, and leaf nodes. It provides functionality for
/// child management, automatic validation, and execution flow.
/// </para>
/// </summary>
/// <remarks>
///
/// This class cannot be instantiated directly. Instead, it should be
/// inherited by specific node types (e.g., <c>Selector</c>, <c>Sequence</c>,
/// or custom leaf nodes).
/// </remarks>
/// <param name="name">The human-readable name of this node, used for debugging or visualization.</param>
/// <param name="priority">
/// The priority value used by certain composite nodes that order their children (e.g., priority selectors).
/// Defaults to <c>0</c>.
/// </param>
public abstract class Node(string name = "Node", int priority = 0)
{
	/// <summary>
	/// The human-readable name of this node, primarily for debugging or visualization purposes.
	/// </summary>
	public readonly string Name = name;

	/// <summary>
	/// The priority value of this node, used by specific node types that order children.
	/// </summary>
	public readonly int Priority = priority;

	/// <summary>
	/// The internal list of child nodes that belong to this node.
	/// <para>
	/// This list is protected so that only derived types can modify it.
	/// </para>
	/// </summary>
	protected readonly List<Node> Children = [];

	/// <summary>
	/// Provides a read-only view of this node’s child collection.
	/// </summary>
	public IReadOnlyList<Node> ChildNodes => Children.AsReadOnly();

	/// <summary>
	/// The index of the currently active child node.
	/// Used by composite nodes to track progress during execution.
	/// </summary>
	protected int currentChild;

	/// <summary>
	/// The maximum number of child nodes this node type supports.
	/// <para>
	/// A value of <c> -1</c> indicates there is no limit (default).
	/// </para>
	/// </summary>
	protected virtual int MaxChildren => -1;

	/// <summary>
	/// Indicates whether this node type requires at least one child to function properly.
	/// <para>
	/// Composite nodes (e.g., sequences, selectors) typically require children,
	/// while leaf nodes (e.g., actions, conditions) do not.
	/// </para>
	/// </summary>
	protected virtual bool RequiresChildren => true;

	/// <summary>
	/// Adds a child node to this node, ensuring that any child limit constraints are respected.
	/// </summary>
	/// <param name="child">The child node to add.</param>
	/// <remarks>
	/// If the maximum child count (<see cref="MaxChildren"/>) is exceeded,
	/// this method logs an error via <see cref="LoggerService"/> and ignores the addition.
	/// </remarks>
	public void AddChild(Node child)
	{
		if (MaxChildren != -1 && Children.Count >= MaxChildren)
		{
			LoggerService.Error(
				$"Cannot have more than {MaxChildren} child(ren). Attempted to add '{child.Name}' to '{Name}'."
			);
			return;
		}

		Children.Add(child);
	}

	/// <summary>
	/// Executes one tick of this node, automatically performing structural validation
	/// before delegating to the node's implementation.
	/// </summary>
	/// <returns>
	/// A <see cref="NodeStatus"/> value indicating the outcome of this tick.
	/// </returns>
	/// <remarks>
	/// This method serves as the primary entry point for ticking all nodes.
	/// It ensures consistent pre-validation and error handling across the behavior tree.
	/// </remarks>
	public NodeStatus Tick(float deltaTime)
	{
		if (!ValidateChildren())
			return NodeStatus.Failure;

		return OnTick(deltaTime);
	}

	/// <summary>
	/// Defines the core behavior of this node during execution.
	/// <para>
	/// Subclasses must override this method to implement their specific logic.
	/// </para>
	/// </summary>
	/// <returns>
	/// A <see cref="NodeStatus"/> value representing the execution result of this node.
	/// </returns>
	protected abstract NodeStatus OnTick(float deltaTime);

	/// <summary>
	/// Validates the structural integrity of this node before execution.
	/// Ensures that nodes which require children have at least one.
	/// </summary>
	/// <returns>
	/// <c>true</c> if validation passes; otherwise, <c>false</c>.
	/// </returns>
	/// <remarks>
	/// This method is invoked automatically by <see cref="Tick"/> before calling <see cref="OnTick"/>.
	/// Derived classes generally should not need to call it directly.
	/// </remarks>
	private bool ValidateChildren()
	{
		if (RequiresChildren && Children.Count == 0)
		{
			LoggerService.Error(
				$"Node '{Name}' requires at least one child but has none."
			);
			return false;
		}

		return true;
	}

	/// <summary>
	/// Resets this node and all of its children to their initial state.
	/// </summary>
	/// <remarks>
	/// This method is typically called after a behavior tree finishes execution,
	/// or when the system wants to restart the tree from a clean state.
	/// </remarks>
	public virtual void Reset()
	{
		currentChild = 0;

		foreach (Node child in Children)
			child.Reset();
	}
}
