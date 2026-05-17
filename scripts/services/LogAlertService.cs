using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public partial class LogAlertService : Node
{
    [Signal]
    public delegate void LogAlertTriggeredEventHandler(string playerName, string logTypeName, string line);

    private Timer _pollTimer;
    private readonly Dictionary<string, LogAlertPlayerState> _playerStates = new();

    private class LogAlertPlayerState
    {
        public Dictionary<LogReader.LogFileType, long> LastPositions = new();
    }

    public override void _Ready()
    {
        _pollTimer = new Timer();
        _pollTimer.WaitTime = AppSettings.LogAlert.PollIntervalSeconds > 0 ? AppSettings.LogAlert.PollIntervalSeconds : 2.0f;
        _pollTimer.Autostart = true;
        _pollTimer.Timeout += OnPollTimeout;
        AddChild(_pollTimer);
    }

    public void Start()
    {
        _pollTimer.Start();
    }

    public void Stop()
    {
        _pollTimer.Stop();
    }

    public void SetPollInterval(float seconds)
    {
        if (seconds > 0)
        {
            _pollTimer.WaitTime = seconds;
        }
    }

    private void OnPollTimeout()
    {
        // Cleanup stale states for players that are no longer configured or enabled
        var activePlayers = AppSettings.LogAlert.PlayerConfigs
            .Where(kvp => kvp.Value.Enabled)
            .Select(kvp => kvp.Key)
            .ToHashSet();

        var statesToRemove = _playerStates.Keys.Where(k => !activePlayers.Contains(k)).ToList();
        foreach (var key in statesToRemove)
        {
            _playerStates.Remove(key);
        }

        foreach (var kvp in AppSettings.LogAlert.PlayerConfigs)
        {
            string playerName = kvp.Key;
            var config = kvp.Value;

            if (!config.Enabled) continue;

            if (!Players.PlayerDict.TryGetValue(playerName, out Player player)) continue;

            if (!_playerStates.TryGetValue(playerName, out var state))
            {
                state = new LogAlertPlayerState();
                _playerStates[playerName] = state;
            }

            // Determine active log types for this specific player
            var activeTypes = config.Filters.Select(f => f.LogType).Distinct();

            foreach (var type in activeTypes)
            {
                string currentLogPath = GetLatestLogPath(player, type);
                if (string.IsNullOrEmpty(currentLogPath) || !File.Exists(currentLogPath)) continue;

                try
                {
                    using var fs = new FileStream(currentLogPath, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
                    long lastPos = state.LastPositions.ContainsKey(type) ? state.LastPositions[type] : fs.Length;

                    if (fs.Length < lastPos) lastPos = 0; // File was truncated or rolled over
                    if (fs.Length == lastPos)
                    {
                        state.LastPositions[type] = lastPos;
                        continue;
                    }

                    fs.Seek(lastPos, SeekOrigin.Begin);
                    using var sr = new StreamReader(fs);
                    
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            ProcessLogLine(playerName, config, type, line);
                        }
                    }
                    state.LastPositions[type] = fs.Position;
                }
                catch (Exception e)
                {
                    Terminal.WriteError($"LogAlertService: Poll error for {playerName} on {type}: {e.Message}");
                }
            }
        }
    }

    private string GetLatestLogPath(Player player, LogReader.LogFileType type)
    {
        if (!LogReader.Prefixes.TryGetValue(type, out string prefix)) return null;
        string logsDir = Path.Combine(player.Path, "logs");
        if (!Directory.Exists(logsDir)) return null;

        return Directory.GetFiles(logsDir, prefix + "*.txt")
            .OrderByDescending(f => f)
            .FirstOrDefault();
    }

    private void ProcessLogLine(string playerName, AppSettings.PlayerLogAlertConfig config, LogReader.LogFileType type, string line)
    {
        var filters = config.Filters.Where(f => f.LogType == type);

        foreach (var filter in filters)
        {
            try
            {
                if (Regex.IsMatch(line, filter.Pattern, RegexOptions.IgnoreCase))
                {
                    EmitSignal(SignalName.LogAlertTriggered, playerName, type.ToString(), line);
                    
                    if (config.EnableTts && !string.IsNullOrEmpty(filter.TtsMessage))
                    {
                        DisplayServer.TtsSpeak(filter.TtsMessage, AppSettings.LogAlert.TtsVoiceId);
                    }
                    break; // Only alert once per line
                }
            }
            catch (Exception e)
            {
                Terminal.WriteError($"LogAlertService: Regex error in filter '{filter.Pattern}': {e.Message}");
            }
        }
    }
}
