using Godot;

namespace Core.ECS.Components;

[GlobalClass]
public partial class BTVisualizerComponent : ComponentBase
{
	[Export]
	public Label3D? Label { get; private set; }

	public void ShowState(string text)
	{
		if (Label is null) return;

		Label.Text = text;
	}
}
