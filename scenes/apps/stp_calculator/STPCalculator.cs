using Godot;
using System;
using System.Linq;
using HvergiToolkit.Libs;

public partial class STPCalculator : Window
{
    private OptionButton _skillOption;
    private CheckBox _isCreationCheck;
    private SpinBox _currentLevelSpin;
    private SpinBox _targetLevelSpin;
    private Button _submitSTPBtn;

    private SpinBox _targetTicksSpin;
    private Button _submitTicksBtn;

    private Label _difficultyLabel;
    private Label _currentSTPLabel;
    private Label _outLVLLabel;

    private Label _targetSTP, _targetPer, _targetLeft;
    private Label _stp50, _per50, _left50;
    private Label _stp70, _per70, _left70;
    private Label _stp90, _per90, _left90;
    private Label _stp100, _per100, _left100;

    public override void _Ready()
    {
        _skillOption = GetNode<OptionButton>("%SkillOption");
        _isCreationCheck = GetNode<CheckBox>("%IsCreationCheck");
        _currentLevelSpin = GetNode<SpinBox>("%CurrentLevelSpin");
        _targetLevelSpin = GetNode<SpinBox>("%TargetLevelSpin");
        _submitSTPBtn = GetNode<Button>("%SubmitSTPBtn");

        _targetTicksSpin = GetNode<SpinBox>("%TargetTicksSpin");
        _submitTicksBtn = GetNode<Button>("%SubmitTicksBtn");

        _difficultyLabel = GetNode<Label>("%DifficultyLabel");
        _currentSTPLabel = GetNode<Label>("%CurrentSTPLabel");
        _outLVLLabel = GetNode<Label>("%OutLVLLabel");

        _targetSTP = GetNode<Label>("%TargetSTP");
        _targetPer = GetNode<Label>("%TargetPer");
        _targetLeft = GetNode<Label>("%TargetLeft");

        _stp50 = GetNode<Label>("%STP50");
        _per50 = GetNode<Label>("%Per50");
        _left50 = GetNode<Label>("%Left50");

        _stp70 = GetNode<Label>("%STP70");
        _per70 = GetNode<Label>("%Per70");
        _left70 = GetNode<Label>("%Left70");

        _stp90 = GetNode<Label>("%STP90");
        _per90 = GetNode<Label>("%Per90");
        _left90 = GetNode<Label>("%Left90");

        _stp100 = GetNode<Label>("%STP100");
        _per100 = GetNode<Label>("%Per100");
        _left100 = GetNode<Label>("%Left100");

        PopulateSkills();

        _submitSTPBtn.Pressed += OnSubmitSTPPressed;
        _submitTicksBtn.Pressed += OnSubmitTicksPressed;
        this.CloseRequested += () => CallDeferred(MethodName.QueueFree);
    }

    private void PopulateSkills()
    {
        var skills = STPHelper.SkillDifficulty.Keys.OrderBy(s => s).ToList();
        foreach (var skill in skills)
        {
            _skillOption.AddItem(skill);
        }
    }

    private void OnSubmitSTPPressed()
    {
        string skillName = _skillOption.GetItemText(_skillOption.Selected);
        double diff = STPHelper.SkillDifficulty[skillName];
        bool isCreation = _isCreationCheck.ButtonPressed;
        double currentLvl = _currentLevelSpin.Value;
        double targetLvl = _targetLevelSpin.Value;

        _difficultyLabel.Text = $"Difficulty: {diff}";

        long currentSTP = STPHelper.GetStpToLevel(currentLvl, diff, isCreation);
        _currentSTPLabel.Text = $"Current STP: {currentSTP:N0}";

        UpdateTargetRow(targetLvl, currentSTP, diff, isCreation, _targetSTP, _targetPer, _targetLeft);
        UpdateTargetRow(50.0, currentSTP, diff, isCreation, _stp50, _per50, _left50);
        UpdateTargetRow(70.0, currentSTP, diff, isCreation, _stp70, _per70, _left70);
        UpdateTargetRow(90.0, currentSTP, diff, isCreation, _stp90, _per90, _left90);
        UpdateTargetRow(100.0, currentSTP, diff, isCreation, _stp100, _per100, _left100);
    }

    private void UpdateTargetRow(double targetLvl, long currentSTP, double diff, bool isCreation, Label stpLabel, Label perLabel, Label leftLabel)
    {
        long targetSTP = STPHelper.GetStpToLevel(targetLvl, diff, isCreation);
        stpLabel.Text = targetSTP.ToString("N0");

        if (targetSTP > 0)
        {
            double percent = (double)currentSTP / targetSTP * 100.0;
            perLabel.Text = $"{percent:F4}%";
        }
        else
        {
            perLabel.Text = "0%";
        }

        long left = Math.Max(0, targetSTP - currentSTP);
        leftLabel.Text = left.ToString("N0");
    }

    private void OnSubmitTicksPressed()
    {
        string skillName = _skillOption.GetItemText(_skillOption.Selected);
        double diff = STPHelper.SkillDifficulty[skillName];
        bool isCreation = _isCreationCheck.ButtonPressed;
        double currentLvl = _currentLevelSpin.Value;
        long ticks = (long)_targetTicksSpin.Value;

        double newLvl = STPHelper.GetLevelAfterTicks(currentLvl, ticks, diff, isCreation);
        _outLVLLabel.Text = $"New Level: {newLvl:F12}";
        
        // Also update current STP for context
        long currentSTP = STPHelper.GetStpToLevel(currentLvl, diff, isCreation);
        _currentSTPLabel.Text = $"Current STP: {currentSTP:N0}";
        _difficultyLabel.Text = $"Difficulty: {diff}";
    }
}
