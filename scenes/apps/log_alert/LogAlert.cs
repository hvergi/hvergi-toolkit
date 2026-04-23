using Godot;
using System;

public partial class LogAlert : Window
{
	public override void _Ready()
	{
		 this.CloseRequested += OnCloseRequested;
	}

	private void OnCloseRequested()
    {
        CallDeferred(MethodName.QueueFree);
    }
}
