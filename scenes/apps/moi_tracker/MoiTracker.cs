using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public partial class MoiTracker : Window
{
    public class SavedTeam
    {
        public string Name { get; set; }
        public string LeaderName { get; set; }
        public List<string> MemberNames { get; set; } = new();
    }

    private const string SavedTeamsPath = "user://moi_teams.json";

    private Label _activeMembersLabel;
    private Label _totalCraftsLabel;
    private Label _totalMoisLabel;
    private Label _globalCpmLabel;
    private Label _lastGlobalMoiLabel;
    
    private SpinBox _intervalSpinBox;
    private Button _startButton;
    private Button _pauseButton;
    private Button _stopButton;
    private Button _resetButton;
    private Label _countdownLabel;
    private ColorRect _alertOverlay;

    private OptionButton _teamSelect;
    private LineEdit _teamNameEdit;
    private Button _saveTeamButton;
    private Button _deleteTeamButton;
    private Button _clearTeamButton;

    private Control _leaderZone;
    private Control _membersGrid;
    private RichTextLabel _eventLog;
    private PlayerList _playerList;

    private Label _leaderPlaceholder;
    private Label _membersPlaceholder;

    private Timer _craftTimer;
    private Timer _logCheckTimer;
    private AudioStreamPlayer _audioPlayer;
    
    private int _totalCrafts = 0;
    private int _totalMois = 0;
    private double _secondsSinceLastGlobalMoi = 0;
    private bool _isRunning = false;
    
    private PackedScene _playerCardScene;

    private PlayerCard _leaderCard = null;
    private List<PlayerCard> _memberCards = new();
    private List<SavedTeam> _savedTeams = new();

    public override void _Ready()
    {
        this.CloseRequested += OnCloseRequested;
        
        _playerCardScene = GD.Load<PackedScene>("res://scenes/apps/moi_tracker/components/player_card.tscn");

        // UI References
        _activeMembersLabel = GetNode<Label>("%ActiveMembersLabel");
        _totalCraftsLabel = GetNode<Label>("%TotalCraftsLabel");
        _totalMoisLabel = GetNode<Label>("%TotalMoisLabel");
        _globalCpmLabel = GetNode<Label>("%GlobalCPMLabel");
        _lastGlobalMoiLabel = GetNode<Label>("%LastGlobalMoiLabel");

        _intervalSpinBox = GetNode<SpinBox>("%IntervalSpinBox");
        _startButton = GetNode<Button>("%StartButton");
        _pauseButton = GetNode<Button>("%PauseButton");
        _stopButton = GetNode<Button>("%StopButton");
        _resetButton = GetNode<Button>("%ResetButton");
        _countdownLabel = GetNode<Label>("%CountdownLabel");
        _alertOverlay = GetNode<ColorRect>("%AlertOverlay");

        _teamSelect = GetNode<OptionButton>("%TeamSelect");
        _teamNameEdit = GetNode<LineEdit>("%TeamNameEdit");
        _saveTeamButton = GetNode<Button>("%SaveTeamButton");
        _deleteTeamButton = GetNode<Button>("%DeleteTeamButton");
        _clearTeamButton = GetNode<Button>("%ClearTeamButton");

        _leaderZone = GetNode<Control>("%LeaderZone");
        _membersGrid = GetNode<Control>("%MembersGrid");
        _eventLog = GetNode<RichTextLabel>("%EventLog");
        _playerList = GetNode<PlayerList>("%PlayerList");
        _playerList.HidePlayersOnTeam = true;
        _playerList.Refresh();

        _leaderPlaceholder = GetNode<Label>("%LeaderPlaceholder");
        _membersPlaceholder = GetNode<Label>("%MembersPlaceholder");
        _audioPlayer = GetNode<AudioStreamPlayer>("%AudioPlayer");

        if (_leaderZone is DropZone dzLeader)
            dzLeader.PlayerDropped += (name) => OnPlayerDropped(name, true);
        
        var membersDropZone = GetNode<Control>("%MembersDropZone");
        if (membersDropZone is DropZone dzMembers)
            dzMembers.PlayerDropped += (name) => OnPlayerDropped(name, false);

        // Timer Setup - Now only for alerting the user to click again
        _craftTimer = new Timer();
        _craftTimer.OneShot = true;
        _craftTimer.Timeout += OnCraftTimerTimeout;
        AddChild(_craftTimer);

        _logCheckTimer = new Timer();
        _logCheckTimer.WaitTime = 1.0;
        _logCheckTimer.Autostart = true;
        _logCheckTimer.Timeout += OnLogCheckTimerTimeout;
        AddChild(_logCheckTimer);

        // Signal Connections
        _startButton.Pressed += StartTracking;
        _pauseButton.Pressed += PauseTracking;
        _stopButton.Pressed += StopTracking;
        _resetButton.Pressed += ResetTracking;

        _saveTeamButton.Pressed += SaveCurrentTeam;
        _deleteTeamButton.Pressed += DeleteSelectedTeam;
        _clearTeamButton.Pressed += ClearCurrentTeam;
        _teamSelect.ItemSelected += OnTeamSelected;

        LoadSavedTeams();
        UpdateUI();
        SetRunning(false);
    }

    private void OnCloseRequested()
    {
        ClearCurrentTeam();
        CallDeferred(MethodName.QueueFree);
    }

    private void ClearCurrentTeam()
    {
        if (_leaderCard != null && Players.PlayerDict.TryGetValue(_leaderCard.PlayerName, out Player leader))
        {
            leader.IsOnTeam = false;
        }

        foreach (var card in _memberCards.ToList())
        {
            if (Players.PlayerDict.TryGetValue(card.PlayerName, out Player member))
            {
                member.IsOnTeam = false;
            }
        }

        // Properly free nodes
        if (_leaderCard != null) { _leaderCard.QueueFree(); _leaderCard = null; }
        foreach (var card in _memberCards) { card.QueueFree(); }
        _memberCards.Clear();

        Players.NotifyTeamStateChanged();
        UpdateUI();
    }

    public override void _Process(double delta)
    {
        if (_isRunning && !_craftTimer.Paused)
        {
            _secondsSinceLastGlobalMoi += delta;
        }
        
        _lastGlobalMoiLabel.Text = $"Last Global MOI: {FormatTime(_secondsSinceLastGlobalMoi)}";

        if (!_craftTimer.IsStopped())
        {
            _countdownLabel.Text = $"Next Craft: {_craftTimer.TimeLeft:F1}s";
        }
    }

    private void StartTracking()
    {
        // Jump to end of logs to avoid catching up on events that happened while paused
        _leaderCard?.JumpToEnd();
        foreach (var card in _memberCards)
        {
            card.JumpToEnd();
        }

        if (_craftTimer.Paused)
        {
            _craftTimer.Paused = false;
        }

        SetRunning(true);
    }

    private void PauseTracking()
    {
        _craftTimer.Paused = true;
        SetRunning(false);
    }

    private void StopTracking()
    {
        _craftTimer.Stop();
        _craftTimer.Paused = false;
        SetRunning(false);
        _countdownLabel.Text = "Next Craft: --";
    }

    private void ResetTracking()
    {
        StopTracking();
        _totalCrafts = 0;
        _totalMois = 0;
        _secondsSinceLastGlobalMoi = 0;
        
        _leaderCard?.ResetStats();
        foreach (var card in _memberCards)
        {
            card.ResetStats();
        }
        
        UpdateUI();
    }

    private void SetRunning(bool running)
    {
        _isRunning = running;
        _startButton.Visible = !running;
        _pauseButton.Visible = running;
        _stopButton.Disabled = !running && _craftTimer.IsStopped();
        _intervalSpinBox.Editable = !running && _craftTimer.IsStopped();

        if (_leaderCard != null) _leaderCard.IsRunning = running;
        foreach (var card in _memberCards)
        {
            card.IsRunning = running;
        }
    }

    private void OnCraftTimerTimeout()
    {
        FlashAlert();
        _countdownLabel.Text = "Next Craft: NOW!";
        TriggerAlert(AppSettings.MoiTracker.CraftAlert, _leaderCard?.PlayerName ?? "Leader");
    }

    private void TriggerAlert(AppSettings.AlertSettings settings, string playerName)
    {
        if (settings.Mode == 0) // Sound
        {
            var stream = AudioHelper.LoadAudio(settings.SoundPath);
            if (stream != null)
            {
                _audioPlayer.Stream = stream;
                _audioPlayer.VolumeDb = Mathf.LinearToDb(settings.Volume);
                _audioPlayer.Play();
            }
        }
        else // TTS
        {
            string msg = settings.TTSMessage.Replace("{player}", playerName);
            DisplayServer.TtsSpeak(msg, AppSettings.MoiTracker.TtsVoiceId);
        }
    }

    private async void FlashAlert()
    {
        _alertOverlay.Visible = true;
        await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
        _alertOverlay.Visible = false;
    }

    private void OnLogCheckTimerTimeout()
    {
        if (!_isRunning || _craftTimer.Paused) return;

        _leaderCard?.CheckLogs();
        foreach (var card in _memberCards)
        {
            card.CheckLogs();
        }
    }

    private void OnMoiDetected(string playerName)
    {
        _totalMois++;
        _secondsSinceLastGlobalMoi = 0;
        
        string timestamp = Time.GetTimeStringFromSystem();
        _eventLog.AppendText($"[{timestamp}] [b]{playerName}[/b]: Moment of Inspiration!\n");
        
        TriggerAlert(AppSettings.MoiTracker.MoiAlert, playerName);
        UpdateUI();
    }

    private void OnCraftDetected(string playerName)
    {
        if (_leaderCard != null && playerName == _leaderCard.PlayerName)
        {
            _totalCrafts++;
            _craftTimer.Start((float)_intervalSpinBox.Value);
        }
        
        UpdateUI();
    }

    private void UpdateUI()
    {
        int memberCount = _memberCards.Count;
        int activeCount = (_leaderCard != null ? 1 : 0) + memberCount;
        _activeMembersLabel.Text = $"Active Members: {activeCount}";
        _totalCraftsLabel.Text = $"Total Crafts: {_totalCrafts}";
        _totalMoisLabel.Text = $"Total MOIs: {_totalMois}";
        
        double globalCpm = _totalMois > 0 ? (double)_totalCrafts / _totalMois : 0;
        _globalCpmLabel.Text = $"Global CPM: {globalCpm:F1}";

        if (_leaderPlaceholder != null)
            _leaderPlaceholder.Visible = _leaderCard == null;
        
        if (_membersPlaceholder != null)
            _membersPlaceholder.Visible = memberCount == 0;
    }

    private string FormatTime(double seconds)
    {
        TimeSpan t = TimeSpan.FromSeconds(seconds);
        if (t.TotalHours >= 1)
            return $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}";
        return $"{t.Minutes:D2}:{t.Seconds:D2}";
    }

    private void OnPlayerDropped(string playerName, bool isLeader)
    {
        if (Players.PlayerDict.TryGetValue(playerName, out Player player))
        {
            player.IsOnTeam = true;
            Players.NotifyTeamStateChanged();

            if (isLeader) AddLeader(player);
            else AddMember(player);
        }
    }

    private void AddLeader(Player player)
    {
        if (_leaderCard != null)
        {
            RemovePlayer(_leaderCard);
        }

        var card = _playerCardScene.Instantiate<PlayerCard>();
        _leaderZone.AddChild(card);
        card.Setup(player);
        card.IsRunning = _isRunning && !_craftTimer.Paused;
        card.Removed += () => RemovePlayer(card);
        card.MoiDetected += OnMoiDetected;
        card.CraftDetected += OnCraftDetected;
        _leaderCard = card;
        
        UpdateUI();
    }

    private void AddMember(Player player)
    {
        if (_leaderCard?.PlayerName == player.Name || _memberCards.Any(c => c.PlayerName == player.Name))
        {
            return;
        }

        var card = _playerCardScene.Instantiate<PlayerCard>();
        _membersGrid.AddChild(card);
        card.Setup(player);
        card.IsRunning = _isRunning && !_craftTimer.Paused;
        card.Removed += () => RemovePlayer(card);
        card.MoiDetected += OnMoiDetected;
        card.CraftDetected += OnCraftDetected;
        _memberCards.Add(card);

        UpdateUI();
    }

    private void RemovePlayer(PlayerCard card)
    {
        if (Players.PlayerDict.TryGetValue(card.PlayerName, out Player player))
        {
            player.IsOnTeam = false;
            Players.NotifyTeamStateChanged();
        }

        if (card == _leaderCard)
        {
            _leaderCard = null;
        }
        else
        {
            _memberCards.Remove(card);
        }
        card.QueueFree();
        UpdateUI();
    }

    // Saving and Loading Teams
    private void DeleteSelectedTeam()
    {
        int index = _teamSelect.Selected;
        if (index <= 0)
        {
            Terminal.WriteWarning("No team selected to delete.");
            return;
        }

        string teamName = _teamSelect.GetItemText(index);
        var team = _savedTeams.FirstOrDefault(t => t.Name == teamName);
        if (team != null)
        {
            _savedTeams.Remove(team);
            SaveTeamsToFile();
            RefreshTeamDropdown();
            Terminal.Write($"Deleted team preset: {teamName}");
        }
    }

    private void SaveCurrentTeam()
    {
        string teamName = _teamNameEdit.Text.Trim();
        if (string.IsNullOrEmpty(teamName))
        {
            Terminal.WriteWarning("Please enter a name for the team.");
            return;
        }

        if (_leaderCard == null && _memberCards.Count == 0)
        {
            Terminal.WriteWarning("Team is empty, nothing to save.");
            return;
        }

        var team = new SavedTeam
        {
            Name = teamName,
            LeaderName = _leaderCard?.PlayerName ?? "",
            MemberNames = _memberCards.Select(c => c.PlayerName).ToList()
        };

        // Update existing or add new
        var existing = _savedTeams.FirstOrDefault(t => t.Name == teamName);
        if (existing != null) _savedTeams.Remove(existing);
        _savedTeams.Add(team);

        SaveTeamsToFile();
        RefreshTeamDropdown();
        _teamNameEdit.Clear();
        Terminal.Write($"Saved team: {teamName}");
    }

    private void OnTeamSelected(long index)
    {
        if (index == 0) return; // Placeholder

        string teamName = _teamSelect.GetItemText((int)index);
        var team = _savedTeams.FirstOrDefault(t => t.Name == teamName);
        if (team == null) return;

        ClearCurrentTeam();

        if (!string.IsNullOrEmpty(team.LeaderName) && Players.PlayerDict.TryGetValue(team.LeaderName, out Player leader))
        {
            if (leader.IsOnTeam)
            {
                Terminal.WriteWarning($"Leader {leader.Name} is already on another team.");
            }
            else
            {
                OnPlayerDropped(leader.Name, true);
            }
        }

        foreach (var memberName in team.MemberNames)
        {
            if (Players.PlayerDict.TryGetValue(memberName, out Player member))
            {
                if (member.IsOnTeam)
                {
                    Terminal.WriteWarning($"Member {member.Name} is already on another team.");
                }
                else
                {
                    OnPlayerDropped(member.Name, false);
                }
            }
        }
    }

    private void LoadSavedTeams()
    {
        if (!Godot.FileAccess.FileExists(SavedTeamsPath)) return;

        try
        {
            using var file = Godot.FileAccess.Open(SavedTeamsPath, Godot.FileAccess.ModeFlags.Read);
            if (file != null)
            {
                string json = file.GetAsText();
                _savedTeams = JsonSerializer.Deserialize<List<SavedTeam>>(json) ?? new();
            }
        }
        catch (Exception e)
        {
            Terminal.WriteError($"Failed to load teams: {e.Message}");
        }

        RefreshTeamDropdown();
    }

    private void SaveTeamsToFile()
    {
        try
        {
            string json = JsonSerializer.Serialize(_savedTeams, new JsonSerializerOptions { WriteIndented = true });
            using var file = Godot.FileAccess.Open(SavedTeamsPath, Godot.FileAccess.ModeFlags.Write);
            file?.StoreString(json);
        }
        catch (Exception e)
        {
            Terminal.WriteError($"Failed to save teams: {e.Message}");
        }
    }

    private void RefreshTeamDropdown()
    {
        _teamSelect.Clear();
        _teamSelect.AddItem("Select a Team Preset...");
        foreach (var team in _savedTeams.OrderBy(t => t.Name))
        {
            _teamSelect.AddItem(team.Name);
        }
    }
}
