using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public partial class TradeMonitorService : Node
{
    [Signal]
    public delegate void TradeLineMatchedEventHandler(string playerName, string formattedBbcode);

    private Timer _pollTimer;
    private readonly Dictionary<string, TradePlayerState> _playerStates = new();

    private readonly Regex _logRegex = new(@"^\[(?<time>\d{2}:\d{2}:\d{2})\] <(?<name>.+?)>(?: \((?<server>\w+?)\))? (?<category>WTB|WTS|WTT|PC|@)\s*(?<msg>.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly Regex _itemRegex = new(@"\[(?<item>.+?)\]", RegexOptions.Compiled);

    private class TradePlayerState
    {
        public string LogPath;
        public long LastPos;
    }

    public override void _Ready()
    {
        _pollTimer = new Timer();
        _pollTimer.WaitTime = AppSettings.TradeWatcher.PollIntervalSeconds > 0 ? AppSettings.TradeWatcher.PollIntervalSeconds : 2.0f;
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
        var activePlayers = AppSettings.TradeWatcher.PlayerConfigs
            .Where(kvp => kvp.Value.Enabled)
            .Select(kvp => kvp.Key)
            .ToHashSet();

        var statesToRemove = _playerStates.Keys.Where(k => !activePlayers.Contains(k)).ToList();
        foreach (var key in statesToRemove)
        {
            _playerStates.Remove(key);
        }

        foreach (var kvp in AppSettings.TradeWatcher.PlayerConfigs)
        {
            string playerName = kvp.Key;
            var config = kvp.Value;

            if (!config.Enabled) continue;

            if (!Players.PlayerDict.TryGetValue(playerName, out Player player)) continue;

            string currentLogPath = player.GetLogPath("Trade");
            if (string.IsNullOrEmpty(currentLogPath) || !File.Exists(currentLogPath)) continue;

            if (!_playerStates.TryGetValue(playerName, out var state))
            {
                state = new TradePlayerState { LogPath = currentLogPath, LastPos = 0 };
                
                // If it's a new state but the file already has contents, don't read the whole backlog right away
                // Optionally could seek to end, but step 2 implies LastPos = 0 initially so it reads the day's history once.
                try
                {
                    using var fs = new FileStream(currentLogPath, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
                    state.LastPos = fs.Length;
                }
                catch (Exception e)
                {
                    Terminal.WriteError($"TradeMonitorService: Could not initialize LastPos for {playerName}. {e.Message}");
                }

                _playerStates[playerName] = state;
            }
            else if (state.LogPath != currentLogPath)
            {
                // Day rollover or path changed
                state.LogPath = currentLogPath;
                state.LastPos = 0;
            }

            try
            {
                using var fs = new FileStream(currentLogPath, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
                if (fs.Length < state.LastPos) state.LastPos = 0; // File was truncated
                if (fs.Length == state.LastPos) continue; // No new content

                fs.Seek(state.LastPos, SeekOrigin.Begin);
                using var sr = new StreamReader(fs);
                
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        ProcessLogLine(playerName, config, line);
                    }
                }
                state.LastPos = fs.Position;
            }
            catch (Exception e)
            {
                Terminal.WriteError($"TradeMonitorService: Poll error for {playerName}: {e.Message}");
            }
        }
    }

    private void ProcessLogLine(string playerName, AppSettings.PlayerTradeConfig config, string line)
    {
        var match = _logRegex.Match(line);
        if (!match.Success) return;

        string name = match.Groups["name"].Value;
        string categoryStr = match.Groups["category"].Value.ToUpper();
        string message = match.Groups["msg"].Value;
        string time = match.Groups["time"].Value;

        // 1. Priority: Mentions (@PlayerName)
        bool isMention = false;
        if (!string.IsNullOrEmpty(playerName))
        {
            if (categoryStr == "@" || message.Contains("@" + playerName, StringComparison.OrdinalIgnoreCase))
            {
                isMention = true;
            }
        }

        if (isMention)
        {
            string bbcode = $"[{time}] [color=yellow][b]<{name}> {categoryStr} {message}[/b][/color]\n";
            EmitSignal(SignalName.TradeLineMatched, playerName, bbcode);

            if (config.EnableTts)
            {
                DisplayServer.TtsSpeak($"You were mentioned in trade by {name}", AppSettings.TradeWatcher.TtsVoiceId);
            }
            return;
        }

        // 2. Standard Trade Logic
        if (!Enum.TryParse(categoryStr, out AppSettings.TradeCategory lineCategory))
        {
            lineCategory = AppSettings.TradeCategory.Any;
        }

        var items = new List<string>();
        var itemMatches = _itemRegex.Matches(message);
        foreach (Match im in itemMatches) items.Add(im.Groups["item"].Value);

        // 1. Exclude filters
        foreach (var filter in config.Filters.Where(f => f.IsExclude))
        {
            if (IsMatch(filter, name, lineCategory, message, items)) return;
        }

        // 2. Include filters
        bool passedInclude = true;
        AppSettings.TradeFilter matchedInclude = null;

        if (config.Filters.Any(f => !f.IsExclude))
        {
            passedInclude = false;
            foreach (var filter in config.Filters.Where(f => !f.IsExclude))
            {
                if (IsMatch(filter, name, lineCategory, message, items))
                {
                    passedInclude = true;
                    matchedInclude = filter;
                    break;
                }
            }
        }

        if (passedInclude)
        {
            string bbcode = $"[{time}] <{name}> [b]{categoryStr}[/b] {message}\n";
            EmitSignal(SignalName.TradeLineMatched, playerName, bbcode);
            
            if (config.EnableTts && matchedInclude != null && !string.IsNullOrEmpty(matchedInclude.TtsMessage))
            {
                string tts = matchedInclude.TtsMessage.Replace("{player}", name);
                DisplayServer.TtsSpeak(tts, AppSettings.TradeWatcher.TtsVoiceId);
            }
        }
    }

    private bool IsMatch(AppSettings.TradeFilter filter, string playerName, AppSettings.TradeCategory lineCategory, string message, List<string> items)
    {
        if (filter.Category != AppSettings.TradeCategory.Any && filter.Category != lineCategory) return false;

        switch (filter.Mode)
        {
            case AppSettings.MatchMode.Player:
                return playerName.Equals(filter.Pattern, StringComparison.OrdinalIgnoreCase);

            case AppSettings.MatchMode.SimpleText:
                string simpleRegex = Regex.Escape(filter.Pattern).Replace("\\*", ".*");
                return Regex.IsMatch(message, simpleRegex, RegexOptions.IgnoreCase);

            case AppSettings.MatchMode.ItemTemplate:
                string regexPattern = "^" + Regex.Escape(filter.Pattern).Replace("\\*", ".*") + "$";
                foreach (var item in items)
                {
                    if (Regex.IsMatch(item, regexPattern, RegexOptions.IgnoreCase)) return true;
                }
                return false;
        }

        return false;
    }
}
