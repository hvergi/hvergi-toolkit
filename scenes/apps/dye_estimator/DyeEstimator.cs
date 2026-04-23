using Godot;
using System;

public partial class DyeEstimator : Window
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
