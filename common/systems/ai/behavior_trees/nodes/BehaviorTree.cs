using System.Text;
using Core.AI.BehaviourTrees.Internals;
using Core.Utilities.Logging;

namespace Core.AI.BehaviourTrees.Nodes;

/// <summary>
/// A decorator node that wraps an entire Behavior Tree as a single reusable subtree.
/// <para>
/// This allows nesting of complete trees inside larger trees for modularity and reuse.
/// Only a single child is supported, consistent with the decorator pattern.
/// </para>
/// </summary>
/// <param name="name">
/// The name of this node, used for identification and debugging within the behavior tree.
/// </param>
/// <param name="priority">
/// The execution priority of this node relative to its siblings. Higher values indicate higher priority.
/// </param>
public class BehaviourTree(string name = "BehaviourTree", int priority = 1) : Decorator(name, priority)
{
	protected override NodeStatus OnTick(float deltaTime) => Children[0].Tick(deltaTime);

	/// <summary>
	/// Recursively prints the subtree starting from this node in a visually structured format.
	/// <para>
	/// Uses tree-branch characters (`├─`, `└─`, `│ `) to represent hierarchy and node relationships.
	/// Logs the structure via <see cref="LoggerService"/>.
	/// </para>
	/// </summary>
	public void PrintTree()
	{
		StringBuilder treeBuilder = new();
		PrintNodeRecursive(this, "", isLastChild: true, treeBuilder);
		LoggerService.Info(treeBuilder.ToString());
	}

	/// <summary>
	/// Helper method that prints a node and its children recursively with tree-branch formatting.
	/// </summary>
	/// <param name="node">The node to print.</param>
	/// <param name="prefix">The string prefix for the current level of indentation and branch.</param>
	/// <param name="isLastChild">Indicates whether this node is the last child of its parent, affects branch formatting.</param>
	/// <param name="treeBuilder">The StringBuilder used to accumulate the tree visualization.</param>
	private static void PrintNodeRecursive(Node node, string prefix, bool isLastChild, StringBuilder treeBuilder)
	{
		// Print current node with branch
		treeBuilder.Append(prefix);
		treeBuilder.Append(isLastChild ? "└─ " : "├─ ");
		treeBuilder.AppendLine(node.Name);

		// Build new prefix for children
		string childPrefix = prefix + (isLastChild ? "   " : "│  ");
		int totalChildren = node.ChildNodes.Count;

		for (int index = 0; index < totalChildren; index++)
		{
			Node child = node.ChildNodes[index];
			bool isChildLast = index == totalChildren - 1;
			PrintNodeRecursive(child, childPrefix, isChildLast, treeBuilder);
		}
	}
}
