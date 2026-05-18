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
    private readonly Dictionary<string, LogReader> _logReaders = new();

    private readonly Regex _logRegex = new(@"^\[(?<time>\d{2}:\d{2}:\d{2})\] <(?<name>.+?)>(?: \((?<server>\w+?)\))? (?<category>WTB|WTS|WTT|PC|@)\s*(?<msg>.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly Regex _itemRegex = new(@"\[(?<item>.+?)\]", RegexOptions.Compiled);

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

        var statesToRemove = _logReaders.Keys.Where(k => !activePlayers.Contains(k)).ToList();
        foreach (var key in statesToRemove)
        {
            _logReaders.Remove(key);
        }

        foreach (var kvp in AppSettings.TradeWatcher.PlayerConfigs)
        {
            string playerName = kvp.Key;
            var config = kvp.Value;

            if (!config.Enabled) continue;

            if (!Players.PlayerDict.TryGetValue(playerName, out Player player)) continue;

            if (!_logReaders.TryGetValue(playerName, out var reader))
            {
                reader = new LogReader(player);
                reader.SetPosition(LogReader.LogFileType.Trade, true);
                _logReaders[playerName] = reader;
            }
            

            try
            {
                var newLines = reader.ReadLog(LogReader.LogFileType.Trade);
                
                foreach (var line in newLines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        ProcessLogLine(playerName, config, line);
                    }
                }
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
