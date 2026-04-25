using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public partial class LogAlert : Window
{
    private PlayerList _playerList;
    private RichTextLabel _alertFeed;
    private CheckBox _enableTtsCheck;
    private OptionButton _logTypeSelect;
    private LineEdit _patternEdit;
    private LineEdit _ttsEdit;
    private Button _addButton;
    private VBoxContainer _filterList;

    private string _selectedPlayerName;
    private Timer _pollTimer;
    private Dictionary<LogReader.LogFileType, long> _lastPositions = new();

    public override void _Ready()
    {
        this.CloseRequested += OnCloseRequested;

        _playerList = GetNode<PlayerList>("%PlayerList");
        _alertFeed = GetNode<RichTextLabel>("%AlertFeed");
        _enableTtsCheck = GetNode<CheckBox>("%EnableTtsCheck");
        _logTypeSelect = GetNode<OptionButton>("%LogTypeSelect");
        _patternEdit = GetNode<LineEdit>("%PatternEdit");
        _ttsEdit = GetNode<LineEdit>("%TtsEdit");
        _addButton = GetNode<Button>("%AddButton");
        _filterList = GetNode<VBoxContainer>("%FilterList");

        _playerList.PlayerSelected += OnPlayerSelected;
        _addButton.Pressed += OnAddFilterPressed;
        _enableTtsCheck.Toggled += (toggled) => { AppSettings.LogAlert.EnableTts = toggled; AppSettings.Save(); };

        SetupDropdowns();
        LoadSettings();

        _pollTimer = new Timer();
        _pollTimer.WaitTime = 2.0f;
        _pollTimer.Autostart = true;
        _pollTimer.Timeout += OnPollTimerTimeout;
        AddChild(_pollTimer);
    }

    private void OnCloseRequested()
    {
        CallDeferred(MethodName.QueueFree);
    }

    private void SetupDropdowns()
    {
        _logTypeSelect.Clear();
        foreach (var type in Enum.GetValues<LogReader.LogFileType>())
        {
            if(type == LogReader.LogFileType.PM) continue;
            _logTypeSelect.AddItem(type.ToString());
            _logTypeSelect.SetItemMetadata(_logTypeSelect.ItemCount - 1, (int)type);
        }
    }

    private void LoadSettings()
    {
        _enableTtsCheck.ButtonPressed = AppSettings.LogAlert.EnableTts;
        RefreshFilterList();
    }

    private void RefreshFilterList()
    {
        foreach (Node child in _filterList.GetChildren()) child.QueueFree();

        foreach (var filter in AppSettings.LogAlert.Filters)
        {
            var hbox = new HBoxContainer();
            string ttsPart = string.IsNullOrEmpty(filter.TtsMessage) ? "" : $" (TTS: {filter.TtsMessage})";
            var label = new Label { 
                Text = $"[{filter.LogType}] {filter.Pattern}{ttsPart}",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                ClipText = true
            };
            var removeBtn = new Button { Text = "X" };
            removeBtn.Pressed += () => {
                AppSettings.LogAlert.Filters.Remove(filter);
                AppSettings.Save();
                RefreshFilterList();
            };

            hbox.AddChild(label);
            hbox.AddChild(removeBtn);
            _filterList.AddChild(hbox);
        }
    }

    private void OnAddFilterPressed()
    {
        string pattern = _patternEdit.Text.Trim();
        if (string.IsNullOrEmpty(pattern)) return;

        var filter = new AppSettings.LogFilter
        {
            LogType = (LogReader.LogFileType)_logTypeSelect.GetSelectedMetadata().AsInt32(),
            Pattern = pattern,
            TtsMessage = _ttsEdit.Text.Trim()
        };

        AppSettings.LogAlert.Filters.Add(filter);
        AppSettings.Save();
        RefreshFilterList();

        _patternEdit.Text = "";
        _ttsEdit.Text = "";
    }

    private void OnPlayerSelected(string playerName)
    {
        _selectedPlayerName = playerName;
        _lastPositions.Clear();
        _alertFeed.Clear();
        _alertFeed.AppendText($"[color=gray]Monitoring logs for {playerName}...[/color]\n");
        
        // Initial position setup for all active filter types
        if (Players.PlayerDict.TryGetValue(playerName, out Player player))
        {
            var activeTypes = AppSettings.LogAlert.Filters.Select(f => f.LogType).Distinct();
            foreach (var type in activeTypes)
            {
                string path = GetLatestLogPath(player, type);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    using var fs = new FileStream(path, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
                    _lastPositions[type] = fs.Length;
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

    private void OnPollTimerTimeout()
    {
        if (string.IsNullOrEmpty(_selectedPlayerName) || !Players.PlayerDict.TryGetValue(_selectedPlayerName, out Player player)) return;

        var activeTypes = AppSettings.LogAlert.Filters.Select(f => f.LogType).Distinct();

        foreach (var type in activeTypes)
        {
            string path = GetLatestLogPath(player, type);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;

            try
            {
                using var fs = new FileStream(path, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
                long lastPos = _lastPositions.ContainsKey(type) ? _lastPositions[type] : fs.Length;

                if (fs.Length < lastPos) lastPos = 0; 
                if (fs.Length == lastPos)
                {
                    _lastPositions[type] = lastPos;
                    continue;
                }

                fs.Seek(lastPos, SeekOrigin.Begin);
                using var sr = new StreamReader(fs);
                
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        ProcessLogLine(type, line);
                    }
                }
                _lastPositions[type] = fs.Position;
            }
            catch (Exception e)
            {
                Terminal.WriteError($"LogAlert: Poll error for {type}: {e.Message}");
            }
        }
    }

    private void ProcessLogLine(LogReader.LogFileType type, string line)
    {
        var filters = AppSettings.LogAlert.Filters.Where(f => f.LogType == type);

        foreach (var filter in filters)
        {
            try
            {
                if (Regex.IsMatch(line, filter.Pattern, RegexOptions.IgnoreCase))
                {
                    _alertFeed.AppendText($"[b][{type}][/b] {line}\n");
                    
                    if (AppSettings.LogAlert.EnableTts && !string.IsNullOrEmpty(filter.TtsMessage))
                    {
                        DisplayServer.TtsSpeak(filter.TtsMessage, AppSettings.LogAlert.TtsVoiceId);
                    }
                    break; // Only alert once per line
                }
            }
            catch (Exception e)
            {
                Terminal.WriteError($"LogAlert: Regex error in filter '{filter.Pattern}': {e.Message}");
            }
        }
    }
}
