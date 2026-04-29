using Godot;
using System;

public partial class DropZone : Control
{
    [Signal]
    public delegate void PlayerDroppedEventHandler(string playerName);

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        return data.VariantType == Variant.Type.String;
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        EmitSignal(SignalName.PlayerDropped, data.AsString());
    }
}
