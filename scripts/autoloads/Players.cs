using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

public static class Players
{
    private const string SavePath = "user://players_data.json";

    public static List<string> WurmPaths { get; } = new List<string>();
    public static Dictionary<string, Player> PlayerDict { get; } = new Dictionary<string, Player>();

    public static bool AddPlayer(string playerName, string path)
    {
        if (PlayerDict.ContainsKey(playerName)) return false;
        Player player = new();
        player.Name = playerName;
        player.Path = path;
        PlayerDict.Add(playerName, player);
        return true;
    }

    public static void Clear()
    {
        PlayerDict.Clear();
    }

    public static void Save()
    {
        try
        {
            var data = new SaveData
            {
                WurmPaths = WurmPaths,
                Players = new List<Player>(PlayerDict.Values)
            };

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            
            using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Write);
            if (file != null)
            {
                file.StoreString(json);
            }
        }
        catch (Exception e)
        {
            Terminal.WriteError($"Failed to save players data: {e.Message}");
        }
    }

    public static bool Load()
    {
        if (!Godot.FileAccess.FileExists(SavePath))
        {
            return false;
        }

        try
        {
            using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Read);
            if (file == null) return false;

            string json = file.GetAsText();
            var data = JsonSerializer.Deserialize<SaveData>(json);

            if (data != null)
            {
                WurmPaths.Clear();
                if (data.WurmPaths != null) WurmPaths.AddRange(data.WurmPaths);

                PlayerDict.Clear();
                if (data.Players != null)
                {
                    foreach (var player in data.Players)
                    {
                        if (!string.IsNullOrEmpty(player.Name))
                        {
                            PlayerDict[player.Name] = player;
                        }
                    }
                }
                return true;
            }
        }
        catch (Exception e)
        {
            Terminal.WriteError($"Failed to load players data: {e.Message}");
        }

        return false;
    }

    private class SaveData
    {
        public List<string> WurmPaths { get; set; }
        public List<Player> Players { get; set; }
    }
}
