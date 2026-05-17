using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public partial class TradeWatcher : Window
{
    private PlayerList _playerList;
    private RichTextLabel _tradeFeed;
    private CheckBox _enableMonitorCheck;
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
    private TradeMonitorService _tradeService;

    public override void _Ready()
    {
        this.CloseRequested += OnCloseRequested;

        var toolkit = GetTree().Root.GetNode<HvergiToolkit.HvergiToolkit>("HvergiToolkit");
        _tradeService = toolkit.TradeMonitorService;
        _tradeService.TradeLineMatched += OnTradeLineMatched;

        _playerList = GetNode<PlayerList>("%PlayerList");
        _tradeFeed = GetNode<RichTextLabel>("%TradeFeed");
        _enableMonitorCheck = GetNode<CheckBox>("%EnableMonitorCheck");
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
        
        _enableMonitorCheck.Toggled += (toggled) => { 
            if (string.IsNullOrEmpty(_selectedPlayerName)) return;
            if (AppSettings.TradeWatcher.PlayerConfigs.TryGetValue(_selectedPlayerName, out var config)) {
                config.Enabled = toggled; 
                AppSettings.Save(); 
            }
        };

        _enableTtsCheck.Toggled += (toggled) => { 
            if (string.IsNullOrEmpty(_selectedPlayerName)) return;
            if (AppSettings.TradeWatcher.PlayerConfigs.TryGetValue(_selectedPlayerName, out var config)) {
                config.EnableTts = toggled; 
                AppSettings.Save(); 
            }
        };
        _modeSelect.ItemSelected += OnModeSelected;

        SetupDropdowns();
    }

    private void OnCloseRequested()
    {
        if (_tradeService != null)
        {
            _tradeService.TradeLineMatched -= OnTradeLineMatched;
        }
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

    private void OnModeSelected(long index)
    {
        bool isItem = (AppSettings.MatchMode)index == AppSettings.MatchMode.ItemTemplate;
        _patternEdit.Visible = !isItem;
        _itemBuilderHBox.Visible = isItem;
    }

    private void RefreshFilterList()
    {
        foreach (Node child in _filterList.GetChildren()) child.QueueFree();

        if (string.IsNullOrEmpty(_selectedPlayerName)) return;
        if (!AppSettings.TradeWatcher.PlayerConfigs.TryGetValue(_selectedPlayerName, out var config)) return;

        foreach (var filter in config.Filters)
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

        var config = AppSettings.TradeWatcher.PlayerConfigs[_selectedPlayerName];
        config.Filters.Add(filter);
        AppSettings.Save();
        RefreshFilterList();

        _ttsEdit.Text = "";
    }

    private void OnPlayerSelected(string playerName)
    {
        _selectedPlayerName = playerName;

        if (!AppSettings.TradeWatcher.PlayerConfigs.ContainsKey(playerName))
        {
            AppSettings.TradeWatcher.PlayerConfigs[playerName] = new AppSettings.PlayerTradeConfig();
            AppSettings.Save();
        }

        var config = AppSettings.TradeWatcher.PlayerConfigs[playerName];
        _enableMonitorCheck.SetPressedNoSignal(config.Enabled);
        _enableTtsCheck.SetPressedNoSignal(config.EnableTts);
        RefreshFilterList();

        _tradeFeed.Clear();
        _tradeFeed.AppendText($"[color=gray]Viewing trade feed for {playerName}...[/color]\n");
    }

    private void OnTradeLineMatched(string playerName, string formattedBbcode)
    {
        if (playerName == _selectedPlayerName)
        {
            _tradeFeed.AppendText(formattedBbcode);
        }
    }
}
