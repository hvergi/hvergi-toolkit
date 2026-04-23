using Godot;
using System;

public partial class PlayerEditor : Window
{
    private ItemList _wurmPathsList;
    private ItemList _playerList;
    private PopupMenu _pathContextMenu;
    private int _rightClickedIndex = -1;

    public override void _Ready()
    {
        // Get node references
        _wurmPathsList = GetNode<ItemList>("%WurmPathsList");
        _playerList = GetNode<ItemList>("%PlayerList");
        _pathContextMenu = GetNode<PopupMenu>("%PathContextMenu");

        var autoFindWurmButton = GetNode<Button>("%AutoFindWurmButton");
        var autoFindSteamButton = GetNode<Button>("%AutoFindSteamButton");
        var manualAddButton = GetNode<Button>("%ManualAddButton");

        // Connect Button signals
        autoFindWurmButton.Pressed += OnAutoFindWurmPressed;
        autoFindSteamButton.Pressed += OnAutoFindSteamPressed;
        manualAddButton.Pressed += OnManualAddPressed;

        // Connect ItemList signals
        _wurmPathsList.ItemClicked += OnWurmPathItemClicked;
        _pathContextMenu.IdPressed += OnPathContextIdPressed;

        // Window management
        this.CloseRequested += OnCloseRequested;
    }

    private void OnAutoFindWurmPressed()
    {
        Terminal.Write("Test");
		Terminal.WriteError("Test");
		Terminal.WriteWarning("Test");
        AddWurmPath("Testing if this works.");
    }

    private void OnAutoFindSteamPressed()
    {
        GD.Print("Auto-Find Steam pressed");
        // Implementation for finding Steam Wurm paths
    }

    private void OnManualAddPressed()
    {
        GD.Print("Manual Add pressed");
        // Implementation for manual path addition
    }

    public void AddWurmPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        
        // Optional: Check for duplicates before adding
        for (int i = 0; i < _wurmPathsList.ItemCount; i++)
        {
            if (_wurmPathsList.GetItemText(i) == path) return;
        }

        _wurmPathsList.AddItem(path);
        GD.Print($"Path added: {path}");
    }

    private void OnWurmPathItemClicked(long index, Vector2 atPosition, long mouseButtonIndex)
    {
        if (mouseButtonIndex == (long)MouseButton.Right)
        {
            _rightClickedIndex = (int)index;
            _pathContextMenu.Position = (Vector2I)(GetWindow().Position + (Vector2I)atPosition + (Vector2I)_wurmPathsList.GlobalPosition);
            _pathContextMenu.Popup();
        }
    }

    private void OnPathContextIdPressed(long id)
    {
        if (id == 0 && _rightClickedIndex != -1)
        {
            GD.Print($"Removing path at index: {_rightClickedIndex}");
            _wurmPathsList.RemoveItem(_rightClickedIndex);
            _rightClickedIndex = -1;
        }
    }

    private void OnCloseRequested()
    {
        CallDeferred(MethodName.QueueFree);
    }
}
