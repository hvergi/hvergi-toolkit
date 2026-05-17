using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public partial class LogAlert : Window
{
    private PlayerList _playerList;
    private RichTextLabel _alertFeed;
    private CheckBox _enableMonitorCheck;
    private CheckBox _enableTtsCheck;
    private OptionButton _logTypeSelect;
    private LineEdit _patternEdit;
    private LineEdit _ttsEdit;
    private Button _addButton;
    private VBoxContainer _filterList;

    private string _selectedPlayerName;
    private LogAlertService _logAlertService;

    public override void _Ready()
    {
        this.CloseRequested += OnCloseRequested;

        var toolkit = GetTree().Root.GetNode<HvergiToolkit.HvergiToolkit>("HvergiToolkit");
        _logAlertService = toolkit.LogAlertService;
        _logAlertService.LogAlertTriggered += OnLogAlertTriggered;

        _playerList = GetNode<PlayerList>("%PlayerList");
        _alertFeed = GetNode<RichTextLabel>("%AlertFeed");
        _enableMonitorCheck = GetNode<CheckBox>("%EnableMonitorCheck");
        _enableTtsCheck = GetNode<CheckBox>("%EnableTtsCheck");
        _logTypeSelect = GetNode<OptionButton>("%LogTypeSelect");
        _patternEdit = GetNode<LineEdit>("%PatternEdit");
        _ttsEdit = GetNode<LineEdit>("%TtsEdit");
        _addButton = GetNode<Button>("%AddButton");
        _filterList = GetNode<VBoxContainer>("%FilterList");

        _playerList.PlayerSelected += OnPlayerSelected;
        _addButton.Pressed += OnAddFilterPressed;
        
        _enableMonitorCheck.Toggled += (toggled) => { 
            if (string.IsNullOrEmpty(_selectedPlayerName)) return;
            if (AppSettings.LogAlert.PlayerConfigs.TryGetValue(_selectedPlayerName, out var config)) {
                config.Enabled = toggled; 
                AppSettings.Save(); 
            }
        };

        _enableTtsCheck.Toggled += (toggled) => { 
            if (string.IsNullOrEmpty(_selectedPlayerName)) return;
            if (AppSettings.LogAlert.PlayerConfigs.TryGetValue(_selectedPlayerName, out var config)) {
                config.EnableTts = toggled; 
                AppSettings.Save(); 
            }
        };

        SetupDropdowns();
    }

    private void OnCloseRequested()
    {
        if (_logAlertService != null)
        {
            _logAlertService.LogAlertTriggered -= OnLogAlertTriggered;
        }
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

    private void RefreshFilterList()
    {
        foreach (Node child in _filterList.GetChildren()) child.QueueFree();

        if (string.IsNullOrEmpty(_selectedPlayerName)) return;
        if (!AppSettings.LogAlert.PlayerConfigs.TryGetValue(_selectedPlayerName, out var config)) return;

        foreach (var filter in config.Filters)
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
                config.Filters.Remove(filter);
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
        if (string.IsNullOrEmpty(_selectedPlayerName)) return;

        string pattern = _patternEdit.Text.Trim();
        if (string.IsNullOrEmpty(pattern)) return;

        var filter = new AppSettings.LogFilter
        {
            LogType = (LogReader.LogFileType)_logTypeSelect.GetSelectedMetadata().AsInt32(),
            Pattern = pattern,
            TtsMessage = _ttsEdit.Text.Trim()
        };

        var config = AppSettings.LogAlert.PlayerConfigs[_selectedPlayerName];
        config.Filters.Add(filter);
        AppSettings.Save();
        RefreshFilterList();

        _patternEdit.Text = "";
        _ttsEdit.Text = "";
    }

    private void OnPlayerSelected(string playerName)
    {
        _selectedPlayerName = playerName;

        if (!AppSettings.LogAlert.PlayerConfigs.ContainsKey(playerName))
        {
            AppSettings.LogAlert.PlayerConfigs[playerName] = new AppSettings.PlayerLogAlertConfig();
            AppSettings.Save();
        }

        var config = AppSettings.LogAlert.PlayerConfigs[playerName];
        _enableMonitorCheck.SetPressedNoSignal(config.Enabled);
        _enableTtsCheck.SetPressedNoSignal(config.EnableTts);
        RefreshFilterList();

        _alertFeed.Clear();
        _alertFeed.AppendText($"[color=gray]Viewing alerts for {playerName}...[/color]\n");
    }

    private void OnLogAlertTriggered(string playerName, string logTypeName, string line)
    {
        if (playerName == _selectedPlayerName)
        {
            _alertFeed.AppendText($"[b][{logTypeName}][/b] {line}\n");
        }
    }
}
