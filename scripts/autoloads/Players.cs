using System.Collections.Generic;

public static class Players
{
    public static Dictionary<string,Player> playerDict {get;}

    public static bool addPlayer(string player_name,string path)
    {
        if(playerDict.ContainsKey(player_name)) return false;
        Player player = new();
        player.Name = player_name;
        player.Path = path;
        playerDict.Add(player_name,player);
        return true;
    }

}

