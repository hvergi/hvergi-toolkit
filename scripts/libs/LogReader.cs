using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class LogReader
{
    public enum LogFileType
    {
        Event,
        Combat,
        Deaths,
        Friends,
        Trade,
        Local,
        Skills,
        PM,
        Alliance,
        Support,
        CAHelp,
        Freedom,
        GLFreedom,
        Village
    }

    public static readonly Dictionary<LogFileType, string> Prefixes = new()
    {
        { LogFileType.Event, "_Event" },
        { LogFileType.Combat, "_Combat" },
        { LogFileType.Deaths, "_Deaths" },
        { LogFileType.Friends, "_Friends" },
        { LogFileType.Trade, "Trade" },
        { LogFileType.Local, "_Local" },
        { LogFileType.Skills, "_Skills" },
        { LogFileType.PM, "PM__" },
        { LogFileType.Alliance, "Alliance" },
        { LogFileType.Support, "_Support" },
        { LogFileType.CAHelp, "CA_HELP" },
        { LogFileType.Freedom, "Freedom" },
        { LogFileType.GLFreedom, "GL-Freedom" },
        { LogFileType.Village, "Village" }
    };

    private readonly Player _player;
    private readonly Dictionary<LogFileType, long> _lastPositions = new();
    private readonly Dictionary<LogFileType, string> _lastFiles = new();
    private readonly Dictionary<LogFileType, int> _lastFileDay = new();
    //private int _lastDay = -1;

    public LogReader(Player player)
    {
        _player = player;
    }

    /// <summary>
    /// Reads the log file from the last position and returns new lines.
    /// Handles day rollover by reading the remainder of the old file if it changed.
    /// </summary>
    public List<string> ReadLog(LogFileType type)
    {
        List<string> lines = new();
        int currentDay = DateTime.Now.Day;

        _lastFiles.TryGetValue(type, out string lastPath);
        _lastFileDay.TryGetValue(type, out int lastDay);
        
        if(string.IsNullOrEmpty(lastPath)){
            lastPath = GetPath(type);
            _lastFiles[type] = lastPath;
            lastDay = currentDay;
            _lastFileDay[type] = lastDay;
            _lastPositions[type] = 0;
        }

        if (File.Exists(lastPath)){
            var (newLines, newPos) = ReadFrom(lastPath, _lastPositions.GetValueOrDefault(type, 0));
            lines.AddRange(newLines);
            _lastPositions[type] = newPos;
        }
        //Not all logs change daily, so we check for changes, and if it changed, we start reading from the begining
        if (currentDay != lastDay){
            var checkpath = GetPath(type);
            if(checkpath != lastPath){
                _lastPositions[type] = 0;
                _lastFiles[type] = checkpath;
                var (newLines, newPos) = ReadFrom(_lastFiles[type], _lastPositions.GetValueOrDefault(type, 0));
                lines.AddRange(newLines);
                _lastPositions[type] = newPos;
            }
            _lastFileDay[type] = currentDay;
        }
        return lines;
    }

    /// <summary>
    /// Sets the read position for a specific log type to either the start or the end of the current file.
    /// </summary>
    public void SetPosition(LogFileType type, bool toEnd)
    {
        string path = GetPath(type);
        if (string.IsNullOrEmpty(path)) return;

        _lastFiles[type] = path;
        if (toEnd && File.Exists(path))
        {
            try
            {
                _lastPositions[type] = new FileInfo(path).Length;
            }
            catch (Exception e)
            {
                Terminal.WriteError($"LogReader: Error getting file length for {path}: {e.Message}");
                _lastPositions[type] = 0;
            }
        }
        else
        {
            _lastPositions[type] = 0;
        }
    }

    private string GetPath(LogFileType type)
    {
        if (!Prefixes.TryGetValue(type, out string prefix)) return "";
        return _player.GetLogPath(prefix);
    }

    private (List<string> lines, long nextPos) ReadFrom(string path, long pos)
    {
        List<string> lines = new();
        long nextPos = pos;
        try
        {
            // Use System.IO explicitly to avoid ambiguity with Godot.FileAccess
            using var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
            
            // If the file is smaller than our last position, it might have been truncated or replaced
            if (pos > fs.Length) pos = 0;
            
            fs.Seek(pos, SeekOrigin.Begin);
            using var reader = new StreamReader(fs);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                lines.Add(line);
            }
            nextPos = fs.Position;
        }
        catch (Exception e)
        {
            Terminal.WriteError($"LogReader: Error reading {path}: {e.Message}");
        }
        return (lines, nextPos);
    }
}
