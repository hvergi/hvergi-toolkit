using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HvergiToolkit.Libs;

public partial class SkillTracker : Window
{
    // Node references - Left Pane
    private PlayerSelectHorizontal _playerSelect;
    private OptionButton _primarySkillSelect;
    private LineEdit _actionFilterEdit;
    private Button _startButton;
    private Button _pauseButton;
    private Button _stopButton;
    private Button _resetButton;
    private Label _actionsLabel;
    private Label _sessionTimerLabel;
    private SpinBox _goalLevelSpinBox;
    private Label _stpToGoalLabel;
    private Label _estTimeToGoalLabel;

    // Node references - Right Pane
    private Label _skillNameLabel;
    private Label _ticksLabel;
    private Label _successRateLabel;
    private Label _gainedLabel;
    private Label _levelRangeLabel;
    private Label _sessionStpLabel;
    private Label _stpPerHourLabel;
    private Tree _otherSkillsTree;

    private double _sessionTime = 0;
    private bool _isRunning = false;
    private int _totalActions = 0;
    
    private Timer _logCheckTimer;
    private LogReader _logReader;
    private string _primarySkillName = "";
    private string _actionFilter = "";
    private double _goalLevel = 99.0;

    private class TrackedSkill
    {
        public string Name { get; set; }
        public int Ticks { get; set; }
        public double Gained { get; set; }
        public double StartLevel { get; set; }
        public double CurrentLevel { get; set; }
    }
    
    private Dictionary<string, TrackedSkill> _trackedSkills = new();

    public override void _Ready()
    {
        this.CloseRequested += OnCloseRequested;

        // Bind Nodes
        _playerSelect = GetNode<PlayerSelectHorizontal>("%PlayerSelect");
        _primarySkillSelect = GetNode<OptionButton>("%PrimarySkillSelect");
        _actionFilterEdit = GetNode<LineEdit>("%ActionFilterEdit");
        _startButton = GetNode<Button>("%StartButton");
        _pauseButton = GetNode<Button>("%PauseButton");
        _stopButton = GetNode<Button>("%StopButton");
        _resetButton = GetNode<Button>("%ResetButton");
        _actionsLabel = GetNode<Label>("%ActionsLabel");
        _sessionTimerLabel = GetNode<Label>("%SessionTimerLabel");
        _goalLevelSpinBox = GetNode<SpinBox>("%GoalLevelSpinBox");
        _stpToGoalLabel = GetNode<Label>("%StpToGoalLabel");
        _estTimeToGoalLabel = GetNode<Label>("%EstTimeToGoalLabel");

        _skillNameLabel = GetNode<Label>("%SkillNameLabel");
        _ticksLabel = GetNode<Label>("%TicksLabel");
        _successRateLabel = GetNode<Label>("%SuccessRateLabel");
        _gainedLabel = GetNode<Label>("%GainedLabel");
        _levelRangeLabel = GetNode<Label>("%LevelRangeLabel");
        _sessionStpLabel = GetNode<Label>("%SessionStpLabel");
        _stpPerHourLabel = GetNode<Label>("%StpPerHourLabel");
        _otherSkillsTree = GetNode<Tree>("%OtherSkillsTree");

        _logCheckTimer = new Timer();
        _logCheckTimer.WaitTime = 1.0;
        _logCheckTimer.Autostart = true;
        _logCheckTimer.Timeout += OnLogCheckTimerTimeout;
        AddChild(_logCheckTimer);

        InitializeUI();
        ConnectSignals();
    }

    private void InitializeUI()
    {
        // Populate Skills
        _primarySkillSelect.Clear();
        var sortedSkills = STPHelper.SkillDifficulty.Keys.OrderBy(s => s).ToList();
        foreach (var skill in sortedSkills)
        {
            _primarySkillSelect.AddItem(skill);
        }

        // Configure Goal SpinBox
        _goalLevelSpinBox.MaxValue = STPHelper.MaxLevel;
        _goalLevelSpinBox.Step = 0.000001;
        _goalLevelSpinBox.Value = 99.0;

        // Configure Tree
        _otherSkillsTree.Columns = 8;
        _otherSkillsTree.ColumnTitlesVisible = true;
        _otherSkillsTree.SetColumnTitle(0, "Skill");
        _otherSkillsTree.SetColumnTitle(1, "Ticks");
        _otherSkillsTree.SetColumnTitle(2, "Success %");
        _otherSkillsTree.SetColumnTitle(3, "Gained");
        _otherSkillsTree.SetColumnTitle(4, "Start");
        _otherSkillsTree.SetColumnTitle(5, "Current");
        _otherSkillsTree.SetColumnTitle(6, "S-STP");
        _otherSkillsTree.SetColumnTitle(7, "STP/hr");
        
        for (int i = 0; i < 8; i++)
        {
            _otherSkillsTree.SetColumnExpand(i, true);
        }

        ResetStats();
    }

    private void ConnectSignals()
    {
        _startButton.Pressed += StartTracking;
        _pauseButton.Pressed += PauseTracking;
        _stopButton.Pressed += StopTracking;
        _resetButton.Pressed += ResetStats;
        _goalLevelSpinBox.ValueChanged += (val) => _goalLevel = val;
    }

    public override void _Process(double delta)
    {
        if (_isRunning)
        {
            _sessionTime += delta;
            UpdateSessionTimer();
            UpdateGoalStats(); // Live estimations
        }
    }

    private void StartTracking()
    {
        string playerName = _playerSelect.GetSelectedPlayer();
        if (string.IsNullOrEmpty(playerName) || !Players.PlayerDict.TryGetValue(playerName, out Player player))
        {
            Terminal.WriteWarning("SkillTracker: Please select a valid player first.");
            return;
        }

        int selectedIdx = _primarySkillSelect.Selected;
        if (selectedIdx >= 0)
        {
            _primarySkillName = _primarySkillSelect.GetItemText(selectedIdx);
        }

        _actionFilter = _actionFilterEdit.Text.Trim();
        _goalLevel = _goalLevelSpinBox.Value;

        if (_logReader == null)
        {
            _logReader = new LogReader(player);
            _logReader.SetPosition(LogReader.LogFileType.Event, true);
            _logReader.SetPosition(LogReader.LogFileType.Skills, true);
        }

        _isRunning = true;
        UpdatePlaybackButtons();
        UpdateUI();
    }

    private void PauseTracking()
    {
        _isRunning = false;
        UpdatePlaybackButtons();
    }

    private void StopTracking()
    {
        _isRunning = false;
        _logReader = null;
        UpdatePlaybackButtons();
    }

    private void ResetStats()
    {
        StopTracking();
        _sessionTime = 0;
        _totalActions = 0;
        _actionsLabel.Text = "Actions: 0";
        _trackedSkills.Clear();
        UpdateSessionTimer();
        UpdateUI();
    }

    private void UpdatePlaybackButtons()
    {
        _startButton.Visible = !_isRunning;
        _pauseButton.Visible = _isRunning;
        
        _playerSelect.Modulate = _isRunning ? new Color(1, 1, 1, 0.5f) : new Color(1, 1, 1, 1);
        _primarySkillSelect.Disabled = _isRunning;
    }

    private void UpdateSessionTimer()
    {
        TimeSpan t = TimeSpan.FromSeconds(_sessionTime);
        _sessionTimerLabel.Text = $"Session: {t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
    }

    private void OnLogCheckTimerTimeout()
    {
        if (!_isRunning || _logReader == null) return;

        bool updated = false;

        // Process Event Log for Actions
        if (!string.IsNullOrEmpty(_actionFilter))
        {
            var eventLines = _logReader.ReadLog(LogReader.LogFileType.Event);
            foreach (var line in eventLines)
            {
                if (line.Contains(_actionFilter, StringComparison.OrdinalIgnoreCase))
                {
                    _totalActions++;
                    updated = true;
                }
            }
        }

        // Process Skills Log
        var skillLines = _logReader.ReadLog(LogReader.LogFileType.Skills);
        foreach (var line in skillLines)
        {
            if (line.Contains(" increased by "))
            {
                int bracketEnd = line.IndexOf(']');
                if (bracketEnd > -1 && line.Length > bracketEnd + 2)
                {
                    string content = line.Substring(bracketEnd + 2); // e.g. "Body strength increased by 0.000015 to 72.871201"
                    string[] parts = content.Split(new string[] { " increased by ", " to " }, StringSplitOptions.None);
                    if (parts.Length == 3)
                    {
                        string skillName = parts[0];
                        // Robust locale parsing: replace , with . then parse invariant
                        string gainedStr = parts[1].Replace(',', '.');
                        string levelStr = parts[2].Replace(',', '.');

                        if (double.TryParse(gainedStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double gained) && 
                            double.TryParse(levelStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double level))
                        {
                            HandleSkillGain(skillName, gained, level);
                            updated = true;
                        }
                    }
                }
            }
        }

        if (updated)
        {
            _actionsLabel.Text = $"Actions: {_totalActions}";
            UpdateUI();
        }
    }

    private void HandleSkillGain(string skillName, double gained, double level)
    {
        if (!_trackedSkills.TryGetValue(skillName, out TrackedSkill skill))
        {
            skill = new TrackedSkill
            {
                Name = skillName,
                StartLevel = level - gained,
                Ticks = 0,
                Gained = 0
            };
            _trackedSkills[skillName] = skill;
        }

        skill.Ticks++;
        skill.Gained += gained;
        skill.CurrentLevel = level;
    }

    private void UpdateUI()
    {
        _skillNameLabel.Text = string.IsNullOrEmpty(_primarySkillName) ? "None Selected" : _primarySkillName;

        if (!string.IsNullOrEmpty(_primarySkillName) && _trackedSkills.TryGetValue(_primarySkillName, out TrackedSkill primary))
        {
            double diff = STPHelper.SkillDifficulty.GetValueOrDefault(_primarySkillName, 4000.0);
            double sessionStp = STPHelper.GetStpFromLevelToLevel(primary.StartLevel, primary.CurrentLevel, diff, false);
            double stpPerHr = _sessionTime > 0 ? sessionStp / (_sessionTime / 3600.0) : 0;
            double successRate = _totalActions > 0 ? ((double)primary.Ticks / _totalActions) * 100.0 : 0;

            _ticksLabel.Text = primary.Ticks.ToString();
            _successRateLabel.Text = $"{successRate:F1}%";
            _gainedLabel.Text = $"{primary.Gained:F6}";
            _levelRangeLabel.Text = $"{primary.StartLevel:F4} -> {primary.CurrentLevel:F4}";
            _sessionStpLabel.Text = $"{sessionStp:F1}";
            _stpPerHourLabel.Text = $"{stpPerHr:F1}";
        }
        else
        {
            _ticksLabel.Text = "0";
            _successRateLabel.Text = "0.0%";
            _gainedLabel.Text = "0.0000";
            _levelRangeLabel.Text = "0.0 -> 0.0";
            _sessionStpLabel.Text = "0.0";
            _stpPerHourLabel.Text = "0.0";
        }

        UpdateOtherSkillsTree();
        UpdateGoalStats();
    }

    private void UpdateGoalStats()
    {
        if (string.IsNullOrEmpty(_primarySkillName) || !_trackedSkills.TryGetValue(_primarySkillName, out TrackedSkill primary))
        {
            _stpToGoalLabel.Text = "STP to Goal: --";
            _estTimeToGoalLabel.Text = "Est. Time: --";
            return;
        }

        double diff = STPHelper.SkillDifficulty.GetValueOrDefault(_primarySkillName, 4000.0);
        double sessionStp = STPHelper.GetStpFromLevelToLevel(primary.StartLevel, primary.CurrentLevel, diff, false);
        double stpPerHr = _sessionTime > 0 ? sessionStp / (_sessionTime / 3600.0) : 0;
        
        long stpToGoal = 0;
        if (primary.CurrentLevel < _goalLevel)
        {
            stpToGoal = STPHelper.GetStpFromLevelToLevel(primary.CurrentLevel, _goalLevel, diff, false);
        }

        _stpToGoalLabel.Text = $"STP to Goal: {stpToGoal}";

        if (stpPerHr > 0 && stpToGoal > 0)
        {
            double hrs = stpToGoal / stpPerHr;
            TimeSpan t = TimeSpan.FromHours(hrs);
            
            string timeStr;
            if (t.TotalDays >= 1.0)
                timeStr = $"{(int)t.TotalDays}d {t.Hours}h {t.Minutes}m";
            else
                timeStr = $"{t.Hours}h {t.Minutes}m {t.Seconds}s";
                
            _estTimeToGoalLabel.Text = $"Est. Time: {timeStr}";
        }
        else
        {
            _estTimeToGoalLabel.Text = "Est. Time: --";
        }
    }

    private void UpdateOtherSkillsTree()
    {
        _otherSkillsTree.Clear();
        TreeItem root = _otherSkillsTree.CreateItem();

        // Sorted alphabetically by Name
        foreach (var kvp in _trackedSkills.OrderBy(x => x.Key))
        {
            if (kvp.Key == _primarySkillName) continue; // Skip primary

            TrackedSkill skill = kvp.Value;
            double diff = STPHelper.SkillDifficulty.GetValueOrDefault(skill.Name, 4000.0);
            double sessionStp = STPHelper.GetStpFromLevelToLevel(skill.StartLevel, skill.CurrentLevel, diff, false);
            double stpPerHr = _sessionTime > 0 ? sessionStp / (_sessionTime / 3600.0) : 0;
            double successRate = _totalActions > 0 ? ((double)skill.Ticks / _totalActions) * 100.0 : 0;

            TreeItem item = _otherSkillsTree.CreateItem(root);
            item.SetText(0, skill.Name);
            item.SetText(1, skill.Ticks.ToString());
            item.SetText(2, $"{successRate:F1}%");
            item.SetText(3, $"{skill.Gained:F6}");
            item.SetText(4, $"{skill.StartLevel:F4}");
            item.SetText(5, $"{skill.CurrentLevel:F4}");
            item.SetText(6, $"{sessionStp:F1}");
            item.SetText(7, $"{stpPerHr:F1}");
        }
    }

    private void OnCloseRequested()
    {
        CallDeferred(MethodName.QueueFree);
    }
}
