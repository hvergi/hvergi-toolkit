using Godot;
using System;

public partial class SermonWarden : Window
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
