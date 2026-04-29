using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlayerList : PanelContainer
{
    [Signal]
    public delegate void PlayerSelectedEventHandler(string playerName);

    private ItemList _list;
    private CheckBox _showHiddenCheck;

    public bool HidePlayersOnTeam { get; set; } = false;

    public override void _Ready()
    {
        _list = GetNode<ItemList>("%List");
        _showHiddenCheck = GetNode<CheckBox>("%ShowHiddenCheck");

        _showHiddenCheck.Toggled += (toggled) => Refresh();
        _list.ItemSelected += OnItemSelected;

        Players.TeamStateChanged += OnTeamStateChanged;

        Refresh();
    }

    public override void _ExitTree()
    {
        Players.TeamStateChanged -= OnTeamStateChanged;
    }

    private void OnTeamStateChanged()
    {
        Refresh();
    }

    public void Refresh()
    {
        _list.Clear();
        bool showHidden = _showHiddenCheck.ButtonPressed;

        // Sort: Favorites first (A-Z), then others (A-Z)
        var sortedPlayers = Players.PlayerDict.Values
            .Where(p => (showHidden || !p.IsHidden) && (!HidePlayersOnTeam || !p.IsOnTeam))
            .OrderByDescending(p => p.IsFav)
            .ThenBy(p => p.Name)
            .ToList();

        foreach (var player in sortedPlayers)
        {
            string displayName = ToCamelCase(player.Name);
            if (player.IsFav) displayName = "⭐ " + displayName;
            if (player.IsHidden) displayName += " (Hidden)";

            int idx = _list.AddItem(displayName);
            _list.SetItemMetadata(idx, player.Name);
        }
    }

    private void OnItemSelected(long index)
    {
        string playerName = _list.GetItemMetadata((int)index).AsString();
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
}
