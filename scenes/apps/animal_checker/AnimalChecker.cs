using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class AnimalChecker : Window
{
    private PlayerSelectHorizontal _playerSelect;
    private Button _startButton;
    private Button _stopButton;
    private RichTextLabel _matchDisplay;
    
    private LogReader _logReader;
    private Timer _pollTimer;
    
    /// <summary>
    /// Dictionary where Key is the Match Type, and Value is an array of strings to count matches for in each log line.
    /// </summary>
    public Dictionary<string, string[]> MatchDictionary { get; set; } = new()
    {
        { "Output",["It gives more resources.","It seems prize winning.","It looks plump and ready to butcher.","It seems to pick stuff up.","It seems vibrant."]}
    };

    public override void _Ready()
    {
        this.CloseRequested += OnCloseRequested;
        
        _playerSelect = GetNode<PlayerSelectHorizontal>("%PlayerSelect");
        _startButton = GetNode<Button>("%StartButton");
        _stopButton = GetNode<Button>("%StopButton");
        _matchDisplay = GetNode<RichTextLabel>("%MatchDisplay");
        
        _startButton.Pressed += OnStartPressed;
        _stopButton.Pressed += OnStopPressed;
        
        _pollTimer = new Timer();
        _pollTimer.WaitTime = 1.0f; // Poll every second
        _pollTimer.OneShot = false;
        _pollTimer.Timeout += OnPollTimeout;
        AddChild(_pollTimer);
        
        // Disable stop button initially
        _stopButton.Disabled = true;
    }

    private void OnStartPressed()
    {
        string playerName = _playerSelect.GetSelectedPlayer();
        if (string.IsNullOrEmpty(playerName))
        {
            Terminal.WriteError("AnimalChecker: No player selected.");
            return;
        }

        if (!Players.PlayerDict.TryGetValue(playerName, out Player player))
        {
            Terminal.WriteError($"AnimalChecker: Player '{playerName}' not found.");
            return;
        }

        _logReader = new LogReader(player);
        // Start reading from the end of the file to ignore old logs
        _logReader.SetPosition(LogReader.LogFileType.Event, true);
        
        _startButton.Disabled = true;
        _stopButton.Disabled = false;
        
        _pollTimer.Start();
        Terminal.Write($"AnimalChecker: Started tracking for {playerName}");
        _matchDisplay.AppendText($"[i]Started tracking for {playerName}...[/i]\n");
    }

    private void OnStopPressed()
    {
        _pollTimer.Stop();
        _startButton.Disabled = false;
        _stopButton.Disabled = true;
        Terminal.Write("AnimalChecker: Stopped tracking.");
        _matchDisplay.AppendText("[i]Stopped tracking.[/i]\n");
    }

    private void OnPollTimeout()
    {
        if (_logReader == null) return;
        
        var newLines = _logReader.ReadLog(LogReader.LogFileType.Event);
        foreach (var line in newLines)
        {
            ProcessLine(line);
        }
    }

    private void ProcessLine(string line)
    {
        // Iterate through each type in the dictionary
        foreach (var entry in MatchDictionary)
        {
            string type = entry.Key;
            string[] matchStrings = entry.Value;
            int count = 0;
            
            // Count occurrences of each string in the array for this specific line
            foreach (var match in matchStrings)
            {
                if (line.Contains(match, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }
            
            // Only display if at least one match was found
            if (count > 0)
            {
                _matchDisplay.AppendText($"{type}: {count}\n");
            }
        }
    }

    private void OnCloseRequested()
    {
        _pollTimer?.Stop();
        CallDeferred(MethodName.QueueFree);
    }
}
