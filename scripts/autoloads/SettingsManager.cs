using Godot;
using System;

namespace HvergiToolkit.Autoloads;

public partial class SettingsManager : Node
{
	
	public static SettingsManager Instance {get; private set;}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		Instance = null;
	}

}
