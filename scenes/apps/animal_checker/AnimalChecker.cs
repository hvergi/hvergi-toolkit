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
        { "Output",["It gives more resources.","It seems prize winning.","It looks plump and ready to butcher.","It seems to pick stuff up.","It seems vibrant."]},
        {"Combat",["It seems more friendly.","It will fight fiercely.","It looks more friendly than normal","It is a tough bugger.","It seems especially loyal"]},
        {"Draft",["It is easy on its gear","It has a strong body","It can carry more than average","It has strong legs"]},
        {"Speed",["It has fleeter movement than normal.","It has lightning movement.","It has very strong leg muscles.","It seems accustomed to water"]}
    };

    public Dictionary<string, string[]> RareDictionary { get; set; } = new(){
        { "Misc",["It seems immortal.","It has a chance to produce twins"]},
        {"Combat",["It seems extremely tame"]},
        {"Draft",["It seems stronger than normal","It seems more nimble than normal"]},
        {"Speed",["It is unbelievably fast"]},
        {"Output",["It has very good genes"]},
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
        if (newLines.Count == 0) return;

        // Group lines by their [HH:MM:SS] timestamp prefix.
        // Lines without a recognisable timestamp fall into an empty-string group.
        var groups = new Dictionary<string, List<string>>();
        var groupOrder = new List<string>(); // preserve insertion order

        foreach (var line in newLines)
        {
            string ts = string.Empty;
            if (line.Length >= 10 && line[0] == '[' && line[9] == ']')
                ts = line[..10]; // e.g. "[12:34:56]"

            if (!groups.ContainsKey(ts))
            {
                groups[ts] = new List<string>();
                groupOrder.Add(ts);
            }
            groups[ts].Add(line);
        }

        foreach (var ts in groupOrder)
        {
            ProcessGroup(groups[ts]);
        }
    }

    /// <summary>
    /// Processes all lines that share the same timestamp as a single animal
    /// inspection and emits one combined output block in the order:
    /// gender → rare traits → regular traits.
    /// </summary>
    private void ProcessGroup(List<string> lines)
    {
        // --- Gender ---
        string gender = null;
        foreach (var line in lines)
        {
            if (!line.Contains("trait points")) continue;
            gender = line.Contains("She") ? "Female" : "Male";
            break;
        }

        // --- Rare trait counts (accumulated across all lines) ---
        var rareCounts = new Dictionary<string, int>();
        foreach (var key in RareDictionary.Keys)
            rareCounts[key] = 0;

        foreach (var line in lines)
        {
            foreach (var entry in RareDictionary)
            {
                foreach (var match in entry.Value)
                {
                    if (line.Contains(match, StringComparison.OrdinalIgnoreCase))
                        rareCounts[entry.Key]++;
                }
            }
        }

        // --- Regular trait counts (accumulated across all lines) ---
        var traitCounts = new Dictionary<string, int>();
        foreach (var key in MatchDictionary.Keys)
            traitCounts[key] = 0;

        foreach (var line in lines)
        {
            foreach (var entry in MatchDictionary)
            {
                foreach (var match in entry.Value)
                {
                    if (line.Contains(match, StringComparison.OrdinalIgnoreCase))
                        traitCounts[entry.Key]++;
                }
            }
        }

        // --- Bail out early if there is nothing animal-related in this group ---
        bool hasGender = gender != null;
        bool hasRare   = rareCounts.Values.Any(c => c > 0);
        bool hasTrait  = traitCounts.Values.Any(c => c > 0);
        if (!hasGender && !hasRare && !hasTrait) return;

        // --- Emit output in order: gender → rare → traits ---
        if (hasGender)
        {
            _matchDisplay.AppendText($"[b]{gender}[/b] ");
            DisplayServer.TtsSpeak(gender, AppSettings.AnimalChecker.TtsVoiceId);
        }

        foreach (var entry in rareCounts.Where(kv => kv.Value > 0))
        {
            _matchDisplay.AppendText($"Rare {entry.Key}: {entry.Value} ");
            DisplayServer.TtsSpeak($"Rare {entry.Key}: {entry.Value}", AppSettings.AnimalChecker.TtsVoiceId);
        }

        foreach (var entry in traitCounts.Where(kv => kv.Value > 0))
        {
            _matchDisplay.AppendText($"{entry.Key}: {entry.Value} ");
            DisplayServer.TtsSpeak($"{entry.Key}: {entry.Value}", AppSettings.AnimalChecker.TtsVoiceId);
        }

        _matchDisplay.AppendText("\n");
    }

    private void OnCloseRequested()
    {
        _pollTimer?.Stop();
        CallDeferred(MethodName.QueueFree);
    }
}
