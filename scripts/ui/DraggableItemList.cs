using Godot;
using System;

public partial class DraggableItemList : ItemList
{
    public override Variant _GetDragData(Vector2 atPosition)
    {
        int index = GetItemAtPosition(atPosition, true);
        if (index == -1) return default;

        string playerName = GetItemMetadata(index).AsString();
        if (string.IsNullOrEmpty(playerName)) return default;

        // Visual feedback during drag
        Label preview = new Label();
        preview.Text = GetItemText(index);
        SetDragPreview(preview);

        return playerName;
    }
}
