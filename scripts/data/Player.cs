using Godot;

public class Player
{
    public string Name { get; set; }
    public string Path { get; set; }
    public bool IsHidden { get; set; }
    public bool IsFav { get; set; }

    public string GetWurmDir()
    {
        if (string.IsNullOrEmpty(Path)) return "";
        string[] parts = Path.Split("players/");
        return parts[0];
    }
}
