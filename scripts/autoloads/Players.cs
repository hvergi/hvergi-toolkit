using System.Collections.Generic;

public static class Players
{
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
}
