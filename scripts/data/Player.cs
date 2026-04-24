using Godot;
using System;
using System.IO;
using System.Linq;

public class Player
{
    public string Name { get; set; }
    public string Path { get; set; }
    public bool IsHidden { get; set; }
    public bool IsFav { get; set; }
    public int AffinitySkillID { get; set; } = -1; // Default to -1 (none)
    
    public enum LogType
    {
        None,
        Single,
        Month,
        Day
    }
    
    private LogType logType;

    public string GetWurmDir()
    {
        if (string.IsNullOrEmpty(Path)) return "";
        // Path is usually .../players/playerName/
        // We want the part before /players/
        int index = Path.ToLower().LastIndexOf("players");
        if (index != -1)
        {
            return Path.Substring(0, index);
        }
        return "";
    }

    public string GetEventLog()
    {
        return GetLogPath("_Event");
    }

    public string GetSkillLog()
    {
        return GetLogPath("_Skills");
    }

    private string GetLogPath(string prefix)
    {
        UpdateLogType();

        if (logType == LogType.None) return "";

        string logsDir = System.IO.Path.Combine(Path, "logs");
        string fileName = "";

        var dateTime = Time.GetDatetimeDictFromSystem();
        int year = dateTime["year"].AsInt32();
        int month = dateTime["month"].AsInt32();
        int day = dateTime["day"].AsInt32();

        switch (logType)
        {
            case LogType.Single:
                fileName = $"{prefix}.txt";
                break;
            case LogType.Month:
                fileName = $"{prefix}.{year}-{month:D2}.txt";
                break;
            case LogType.Day:
                fileName = $"{prefix}.{year}-{month:D2}-{day:D2}.txt";
                break;
        }

        return System.IO.Path.Combine(logsDir, fileName);
    }

    private void UpdateLogType()
    {
        logType = LogType.None;
        string configTxtPath = System.IO.Path.Combine(Path, "config.txt");
        if (!File.Exists(configTxtPath)) return;

        try
        {
            string configName = File.ReadAllText(configTxtPath).Trim();
            string wurmDir = GetWurmDir();
            if (string.IsNullOrEmpty(wurmDir)) return;

            string gamesettingsPath = System.IO.Path.Combine(wurmDir, "configs", configName, "gamesettings.txt");
            if (!File.Exists(gamesettingsPath)) return;

            string[] lines = File.ReadAllLines(gamesettingsPath);
            foreach (string line in lines)
            {
                if (line.StartsWith("event_log_rotation="))
                {
                    string val = line.Split('=')[1].Trim();
                    if (int.TryParse(val, out int rotation))
                    {
                        logType = (LogType)rotation;
                    }
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Terminal.WriteError($"Error updating log type for {Name}: {e.Message}");
        }
    }
}
