using System.Text;
using Core.AI.BehaviourTrees.Internals;
using Core.Utilities.Logging;

namespace Core.AI.BehaviourTrees.Nodes;

/// <summary>
/// A wrapper node that allows a whole Behavior Tree to be nested as a single node.
/// <para>
/// This lets you reuse trees or create modular subtrees inside larger trees.
/// </para>
/// </summary>
public class BehaviourTreeNode(string name = "BehaviourTree") : Node(name)
{
	protected override int MaxChildren => 1;

	protected override NodeStatus OnTick()
	{
		if (Children.Count == 0)
		{
			LoggerService.Warning($"BehaviourTreeNode '{Name}' has no children to tick.");
			return NodeStatus.Failure;
		}

		return Children[0].Tick();
	}

	/// <summary>
	/// Prints the tree starting from this node.
	/// </summary>
	public void PrintTree()
	{
		StringBuilder sb = new StringBuilder();
		PrintNode(this, 0, sb);
		LoggerService.Info(sb.ToString());
	}

	private static void PrintNode(Node node, int indentLevel, StringBuilder sb)
	{
		sb.Append(' ', indentLevel * 2).AppendLine(node.Name);
		foreach (Node child in node.ChildNodes)
		{
			PrintNode(child, indentLevel + 1, sb);
		}
	}
}
