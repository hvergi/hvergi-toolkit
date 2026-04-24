using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerCard : PanelContainer
{
    [Signal]
    public delegate void RemovedEventHandler();
    [Signal]
    public delegate void MoiDetectedEventHandler(string playerName);
    [Signal]
    public delegate void CraftDetectedEventHandler(string playerName);

    private Player _player;
    private LogReader _logReader;
    
    private Label _nameLabel;
    private Label _moiLabel;
    private Label _craftsLabel;
    private Label _cpmLabel;
    private Label _lastMoiTimerLabel;
    private Button _removeButton;

    private int _moiCount = 0;
    private int _craftsCount = 0;
    private double _secondsSinceLastMoi = 0;

    public bool IsRunning { get; set; } = false;

    public string PlayerName => _player?.Name ?? "";

    public override void _Ready()
    {
        _nameLabel = GetNode<Label>("%NameLabel");
        _moiLabel = GetNode<Label>("%MoiLabel");
        _craftsLabel = GetNode<Label>("%CraftsLabel");
        _cpmLabel = GetNode<Label>("%CPMLabel");
        _lastMoiTimerLabel = GetNode<Label>("%LastMoiTimerLabel");
        _removeButton = GetNode<Button>("%RemoveButton");

        _removeButton.Pressed += () => EmitSignal(SignalName.Removed);
        
        UpdateUI();
        _lastMoiTimerLabel.Text = FormatTime(0);
    }

    public void Setup(Player player)
    {
        _player = player;
        _logReader = new LogReader(player);
        JumpToEnd();
        
        if (_nameLabel != null)
        {
            _nameLabel.Text = player.Name;
        }
    }

    public void JumpToEnd()
    {
        _logReader?.SetPosition(LogReader.LogFileType.Event, true);
    }

    public override void _Process(double delta)
    {
        if (!IsRunning) return;

        _secondsSinceLastMoi += delta;
        _lastMoiTimerLabel.Text = FormatTime(_secondsSinceLastMoi);
    }

    public void CheckLogs()
    {
        if (_logReader == null) return;

        var lines = _logReader.ReadLog(LogReader.LogFileType.Event);
        foreach (var line in lines)
        {
            string lowerLine = line.ToLower();
            
            // Check for MOI
            if (lowerLine.Contains("you have a moment of inspiration"))
            {
                _moiCount++;
                _secondsSinceLastMoi = 0;
                EmitSignal(SignalName.MoiDetected, _player.Name);
                UpdateUI();
            }
            
            // Check for Craft Start
            if (lowerLine.Contains("you start to") || lowerLine.Contains("you start improving"))
            {
                _craftsCount++;
                EmitSignal(SignalName.CraftDetected, _player.Name);
                UpdateUI();
            }
        }
    }

    public void ResetStats()
    {
        _moiCount = 0;
        _craftsCount = 0;
        _secondsSinceLastMoi = 0;
        UpdateUI();
        _lastMoiTimerLabel.Text = FormatTime(0);
    }

    private void UpdateUI()
    {
        _moiLabel.Text = $"{_moiCount}";
        _craftsLabel.Text = $"{_craftsCount}";
        
        double cpm = _moiCount > 0 ? (double)_craftsCount / _moiCount : 0;
        _cpmLabel.Text = $"{cpm:F1}";
    }

    private string FormatTime(double seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        if (t.TotalHours >= 1)
            return $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}";
        return $"{t.Minutes:D2}:{t.Seconds:D2}";
    }
}
