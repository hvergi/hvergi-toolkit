using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public partial class TradeWatcher : Window
{
    private PlayerList _playerList;
    private RichTextLabel _tradeFeed;
    private CheckBox _enableTtsCheck;
    private OptionButton _typeSelect;
    private OptionButton _categorySelect;
    private OptionButton _modeSelect;
    private LineEdit _patternEdit;
    private LineEdit _ttsEdit;
    private Button _addButton;
    private VBoxContainer _filterList;

    // Item Builder UI
    private HBoxContainer _itemBuilderHBox;
    private LineEdit _itemRarityEdit;
    private LineEdit _itemNameEdit;
    private LineEdit _itemQlEdit;
    private LineEdit _itemDmgEdit;
    private LineEdit _itemWtEdit;

    private string _selectedPlayerName;
    private string _currentLogPath;
    private long _lastPos = 0;
    private Timer _pollTimer;

    private readonly Regex _logRegex = new(@"^\[(?<time>\d{2}:\d{2}:\d{2})\] <(?<name>.+?)>(?: \((?<server>\w+?)\))? (?<category>WTB|WTS|WTT|PC|@)\s*(?<msg>.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly Regex _itemRegex = new(@"\[(?<item>.+?)\]", RegexOptions.Compiled);

    public override void _Ready()
    {
        this.CloseRequested += OnCloseRequested;

        _playerList = GetNode<PlayerList>("%PlayerList");
        _tradeFeed = GetNode<RichTextLabel>("%TradeFeed");
        _enableTtsCheck = GetNode<CheckBox>("%EnableTtsCheck");
        _typeSelect = GetNode<OptionButton>("%TypeSelect");
        _categorySelect = GetNode<OptionButton>("%CategorySelect");
        _modeSelect = GetNode<OptionButton>("%ModeSelect");
        _patternEdit = GetNode<LineEdit>("%PatternEdit");
        _ttsEdit = GetNode<LineEdit>("%TtsEdit");
        _addButton = GetNode<Button>("%AddButton");
        _filterList = GetNode<VBoxContainer>("%FilterList");

        _itemBuilderHBox = GetNode<HBoxContainer>("%ItemBuilderHBox");
        _itemRarityEdit = GetNode<LineEdit>("%ItemRarityEdit");
        _itemNameEdit = GetNode<LineEdit>("%ItemNameEdit");
        _itemQlEdit = GetNode<LineEdit>("%ItemQlEdit");
        _itemDmgEdit = GetNode<LineEdit>("%ItemDmgEdit");
        _itemWtEdit = GetNode<LineEdit>("%ItemWtEdit");

        _playerList.PlayerSelected += OnPlayerSelected;
        _addButton.Pressed += OnAddFilterPressed;
        _enableTtsCheck.Toggled += (toggled) => { AppSettings.TradeWatcher.EnableTts = toggled; AppSettings.Save(); };
        _modeSelect.ItemSelected += OnModeSelected;

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
        _typeSelect.Clear();
        _typeSelect.AddItem("Include");
        _typeSelect.AddItem("Exclude");

        _categorySelect.Clear();
        foreach (var cat in Enum.GetValues<AppSettings.TradeCategory>())
        {
            _categorySelect.AddItem(cat.ToString());
        }

        _modeSelect.Clear();
        _modeSelect.AddItem("Simple Text");
        _modeSelect.AddItem("Item Template");
        _modeSelect.AddItem("Player Name");
    }

    private void LoadSettings()
    {
        _enableTtsCheck.ButtonPressed = AppSettings.TradeWatcher.EnableTts;
        RefreshFilterList();
    }

    private void OnModeSelected(long index)
    {
        bool isItem = (AppSettings.MatchMode)index == AppSettings.MatchMode.ItemTemplate;
        _patternEdit.Visible = !isItem;
        _itemBuilderHBox.Visible = isItem;
    }

    private void RefreshFilterList()
    {
        foreach (Node child in _filterList.GetChildren()) child.QueueFree();

        foreach (var filter in AppSettings.TradeWatcher.Filters)
        {
            var hbox = new HBoxContainer();
            string ttsPart = string.IsNullOrEmpty(filter.TtsMessage) ? "" : $" (TTS: {filter.TtsMessage})";
            var label = new Label { 
                Text = $"{(filter.IsExclude ? "[EXCL]" : "[INC]")} {filter.Category} | {filter.Mode}: {filter.Pattern}{ttsPart}",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                ClipText = true
            };
            var removeBtn = new Button { Text = "X" };
            removeBtn.Pressed += () => {
                AppSettings.TradeWatcher.Filters.Remove(filter);
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
        string pattern = "";
        var mode = (AppSettings.MatchMode)_modeSelect.Selected;

        if (mode == AppSettings.MatchMode.ItemTemplate)
        {
            string rarity = string.IsNullOrWhiteSpace(_itemRarityEdit.Text) ? "*" : _itemRarityEdit.Text.Trim();
            string name = string.IsNullOrWhiteSpace(_itemNameEdit.Text) ? "*" : _itemNameEdit.Text.Trim();
            string ql = string.IsNullOrWhiteSpace(_itemQlEdit.Text) ? "*" : _itemQlEdit.Text.Trim();
            string dmg = string.IsNullOrWhiteSpace(_itemDmgEdit.Text) ? "*" : _itemDmgEdit.Text.Trim();
            string wt = string.IsNullOrWhiteSpace(_itemWtEdit.Text) ? "*" : _itemWtEdit.Text.Trim();
            
            pattern = $"*{rarity}*{name}*QL:{ql}*DMG:{dmg}*WT:{wt}*";

            _itemRarityEdit.Text = "";
            _itemNameEdit.Text = "";
            _itemQlEdit.Text = "";
            _itemDmgEdit.Text = "";
            _itemWtEdit.Text = "";
        }
        else
        {
            pattern = _patternEdit.Text.Trim();
            if (string.IsNullOrEmpty(pattern)) return;
            _patternEdit.Text = "";
        }

        var filter = new AppSettings.TradeFilter
        {
            IsExclude = _typeSelect.Selected == 1,
            Category = (AppSettings.TradeCategory)_categorySelect.Selected,
            Mode = mode,
            Pattern = pattern,
            TtsMessage = _ttsEdit.Text.Trim()
        };

        AppSettings.TradeWatcher.Filters.Add(filter);
        AppSettings.Save();
        RefreshFilterList();

        _ttsEdit.Text = "";
    }

    private void OnPlayerSelected(string playerName)
    {
        _selectedPlayerName = playerName;
        if (Players.PlayerDict.TryGetValue(playerName, out Player player))
        {
            _currentLogPath = player.GetLogPath("Trade");
            _lastPos = 0;
            if (File.Exists(_currentLogPath))
            {
                using var fs = new FileStream(_currentLogPath, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
                _lastPos = fs.Length; 
            }
            _tradeFeed.Clear();
            _tradeFeed.AppendText($"[color=gray]Monitoring Trade log for {playerName}...[/color]\n");
        }
    }

    private void OnPollTimerTimeout()
    {
        if (string.IsNullOrEmpty(_currentLogPath) || !File.Exists(_currentLogPath)) return;

        try
        {
            using var fs = new FileStream(_currentLogPath, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
            if (fs.Length < _lastPos) _lastPos = 0; 
            if (fs.Length == _lastPos) return;

            fs.Seek(_lastPos, SeekOrigin.Begin);
            using var sr = new StreamReader(fs);
            
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    ProcessLogLine(line);
                }
            }
            _lastPos = fs.Position;
        }
        catch (Exception e)
        {
            Terminal.WriteError($"TradeWatcher: Poll error: {e.Message}");
        }
    }

    private void ProcessLogLine(string line)
    {
        var match = _logRegex.Match(line);
        if (!match.Success) return;

        string name = match.Groups["name"].Value;
        string categoryStr = match.Groups["category"].Value.ToUpper();
        string message = match.Groups["msg"].Value;
        string time = match.Groups["time"].Value;

        // 1. Priority: Mentions (@PlayerName)
        bool isMention = false;
        if (!string.IsNullOrEmpty(_selectedPlayerName))
        {
            // Check if the category itself is '@' or if the message contains the mention
            if (categoryStr == "@" || message.Contains("@" + _selectedPlayerName, StringComparison.OrdinalIgnoreCase))
            {
                isMention = true;
            }
        }

        if (isMention)
        {
            _tradeFeed.AppendText($"[{time}] [color=yellow][b]<{name}> {categoryStr} {message}[/b][/color]\n");
            if (AppSettings.TradeWatcher.EnableTts)
            {
                DisplayServer.TtsSpeak($"You were mentioned in trade by {name}", AppSettings.TradeWatcher.TtsVoiceId);
            }
            return;
        }

        // 2. Standard Trade Logic
        // Default to 'Any' if the category is non-standard, allowing it to pass 'Any' filters
        if (!Enum.TryParse(categoryStr, out AppSettings.TradeCategory lineCategory))
        {
            lineCategory = AppSettings.TradeCategory.Any;
        }

        var items = new List<string>();
        var itemMatches = _itemRegex.Matches(message);
        foreach (Match im in itemMatches) items.Add(im.Groups["item"].Value);

        // 1. Exclude filters
        foreach (var filter in AppSettings.TradeWatcher.Filters.Where(f => f.IsExclude))
        {
            if (IsMatch(filter, name, lineCategory, message, items)) return;
        }

        // 2. Include filters
        bool passedInclude = true;
        AppSettings.TradeFilter matchedInclude = null;

        if (AppSettings.TradeWatcher.Filters.Any(f => !f.IsExclude))
        {
            passedInclude = false;
            foreach (var filter in AppSettings.TradeWatcher.Filters.Where(f => !f.IsExclude))
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
            _tradeFeed.AppendText($"[{time}] <{name}> [b]{categoryStr}[/b] {message}\n");
            
            if (AppSettings.TradeWatcher.EnableTts && matchedInclude != null && !string.IsNullOrEmpty(matchedInclude.TtsMessage))
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
