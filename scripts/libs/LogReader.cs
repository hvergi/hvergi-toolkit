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

    private static readonly Dictionary<LogFileType, string> _prefixes = new()
    {
        { LogFileType.Event, "_Event" },
        { LogFileType.Combat, "_Combat" },
        { LogFileType.Deaths, "_Deaths" },
        { LogFileType.Friends, "_Friends" },
        { LogFileType.Trade, "Trade" },
        { LogFileType.Local, "_Local" },
        { LogFileType.Skills, "_Skills" },
        { LogFileType.PM, "PM_" },
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
    private int _lastDay = -1;

    public LogReader(Player player)
    {
        _player = player;
        var dateTime = Time.GetDatetimeDictFromSystem();
        _lastDay = dateTime["day"].AsInt32();
    }

    /// <summary>
    /// Reads the log file from the last position and returns new lines.
    /// Handles day rollover by reading the remainder of the old file if it changed.
    /// </summary>
    public List<string> ReadLog(LogFileType type)
    {
        List<string> lines = new();
        string currentPath = GetPath(type);
        
        var dateTime = Time.GetDatetimeDictFromSystem();
        int currentDay = dateTime["day"].AsInt32();

        if (string.IsNullOrEmpty(currentPath)) 
        {
            _lastDay = currentDay;
            return lines;
        }

        // Check if the file path has changed (e.g., due to day rollover in daily rotation)
        if (_lastFiles.TryGetValue(type, out string lastPath) && lastPath != currentPath)
        {
            if (File.Exists(lastPath))
            {
                // Read remaining lines from the old file
                lines.AddRange(ReadFrom(lastPath, _lastPositions.GetValueOrDefault(type, 0)).lines);
            }
            // Reset position for the new file
            _lastPositions[type] = 0;
        }

        if (File.Exists(currentPath))
        {
            long pos = _lastPositions.GetValueOrDefault(type, 0);
            var (newLines, newPos) = ReadFrom(currentPath, pos);
            lines.AddRange(newLines);
            _lastPositions[type] = newPos;
            _lastFiles[type] = currentPath;
        }

        _lastDay = currentDay;
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
        if (!_prefixes.TryGetValue(type, out string prefix)) return "";
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
