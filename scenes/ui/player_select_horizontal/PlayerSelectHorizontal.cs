using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerSelectHorizontal : HBoxContainer
{
    [Signal]
    public delegate void PlayerSelectedEventHandler(string playerName);

    private OptionButton _optionButton;
    private CheckBox _showHiddenCheck;

    public override void _Ready()
    {
        _optionButton = GetNode<OptionButton>("%OptionButton");
        _showHiddenCheck = GetNode<CheckBox>("%ShowHiddenCheck");

        _showHiddenCheck.Toggled += (toggled) => Refresh();
        _optionButton.ItemSelected += OnItemSelected;

        Refresh();
    }

    public void Refresh()
    {
        _optionButton.Clear();
        bool showHidden = _showHiddenCheck.ButtonPressed;

        // Sort: Favorites first (A-Z), then others (A-Z)
        var sortedPlayers = Players.PlayerDict.Values
            .Where(p => showHidden || !p.IsHidden)
            .OrderByDescending(p => p.IsFav)
            .ThenBy(p => p.Name)
            .ToList();

        // Add a default "Select Player" option
        _optionButton.AddItem("Select Player...");
        _optionButton.SetItemDisabled(0, true);

        var popup = _optionButton.GetPopup();

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            var player = sortedPlayers[i];
            string displayName = ToCamelCase(player.Name);
            
            _optionButton.AddItem(displayName);
            int itemIdx = _optionButton.ItemCount - 1;
            _optionButton.SetItemMetadata(itemIdx, player.Name);

            if (player.IsFav)
            {
                _optionButton.SetItemText(itemIdx, "⭐ " + displayName);
            }
            
            if (player.IsHidden)
            {
                // Visual hint for hidden players in the list
                string currentText = _optionButton.GetItemText(itemIdx);
                _optionButton.SetItemText(itemIdx, currentText + " (Hidden)");
            }
        }
    }

    private void OnItemSelected(long index)
    {
        if (index == 0) return; // Ignore "Select Player..."
        
        string playerName = _optionButton.GetItemMetadata((int)index).AsString();
        EmitSignal(SignalName.PlayerSelected, playerName);
    }

    private string ToCamelCase(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var words = text.Split(new[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
        }
        return string.Join("", words);
    }

    public string GetSelectedPlayer()
    {
        int selectedIdx = _optionButton.Selected;
        if (selectedIdx <= 0) return null;
        return _optionButton.GetItemMetadata(selectedIdx).AsString();
    }
}
