using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using HvergiToolkit.Libs;

public partial class AffinityFoodPlanner : Window
{
    private PlayerSelectHorizontal _playerSelect;
    private PlayerSelectHorizontal _makerSelect;
    private OptionButton _customerOption;
    private LineEdit _customerEdit;
    private Button _saveCustomerBtn;
    private OptionButton _testSkillOption;
    private OptionButton _wantSkillOption;
    private Button _fetchMealsBtn;
    private Button _fetchMoonshineBtn;
    private Button _saveAffinityBtn;
    private RichTextLabel _resultsList;
    private Label _makerResultLabel;

    public override void _Ready()
    {
        _playerSelect = GetNode<PlayerSelectHorizontal>("%PlayerSelect");
        _makerSelect = GetNode<PlayerSelectHorizontal>("%MakerSelect");
        _customerOption = GetNode<OptionButton>("%CustomerOption");
        _customerEdit = GetNode<LineEdit>("%CustomerEdit");
        _saveCustomerBtn = GetNode<Button>("%SaveCustomerBtn");
        _testSkillOption = GetNode<OptionButton>("%TestSkillOption");
        _wantSkillOption = GetNode<OptionButton>("%WantSkillOption");
        _fetchMealsBtn = GetNode<Button>("%FetchMealsBtn");
        _fetchMoonshineBtn = GetNode<Button>("%FetchMoonshineBtn");
        _saveAffinityBtn = GetNode<Button>("%SaveAffinityBtn");
        _resultsList = GetNode<RichTextLabel>("%ResultsList");
        _makerResultLabel = GetNode<Label>("%MakerResultLabel");

        PopulateSkills();
        PopulateCustomers();

        _playerSelect.PlayerSelected += OnPlayerSelected;
        _makerSelect.PlayerSelected += (name) => UpdateMakerResult();
        _customerOption.ItemSelected += OnCustomerSelected;
        _saveCustomerBtn.Pressed += OnSaveCustomerPressed;
        _fetchMealsBtn.Pressed += OnFetchMealsPressed;
        _fetchMoonshineBtn.Pressed += OnFetchMoonshinePressed;
        _saveAffinityBtn.Pressed += OnSaveAffinityPressed;

        this.CloseRequested += () => CallDeferred(MethodName.QueueFree);
    }

    private void PopulateSkills()
    {
        _testSkillOption.Clear();
        _wantSkillOption.Clear();

        var skills = AffinityHelper.SkillIDs.OrderBy(kvp => kvp.Key).ToList();
        foreach (var skill in skills)
        {
            _testSkillOption.AddItem(skill.Key);
            _testSkillOption.SetItemMetadata(_testSkillOption.ItemCount - 1, skill.Value);

            _wantSkillOption.AddItem(skill.Key);
            _wantSkillOption.SetItemMetadata(_wantSkillOption.ItemCount - 1, skill.Value);
        }
    }

    private void PopulateCustomers()
    {
        _customerOption.Clear();
        _customerOption.AddItem("New Customer...");
        
        var sortedCustomers = Customers.CustomerAffinities.Keys.OrderBy(n => n).ToList();
        foreach (var name in sortedCustomers)
        {
            _customerOption.AddItem(name);
        }
    }

    private void OnPlayerSelected(string playerName)
    {
        if (Players.PlayerDict.TryGetValue(playerName, out Player player))
        {
            _customerOption.Selected = 0; // Reset customer select
            _customerEdit.Text = "";
            if (player.AffinitySkillID != -1)
            {
                SetSkillOptionByValue(_testSkillOption, player.AffinitySkillID);
            }
        }
    }

    private void OnCustomerSelected(long index)
    {
        if (index == 0) // New Customer...
        {
            _customerEdit.Text = "";
            return;
        }

        string name = _customerOption.GetItemText((int)index);
        _customerEdit.Text = name;
        _playerSelect.Refresh(); // De-select players visually if needed, though Select component doesn't have explicit reset yet.

        if (Customers.CustomerAffinities.TryGetValue(name, out int skillID))
        {
            SetSkillOptionByValue(_testSkillOption, skillID);
        }
    }

    private void OnSaveCustomerPressed()
    {
        string name = _customerEdit.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        int skillID = (int)_testSkillOption.GetItemMetadata(_testSkillOption.Selected);
        Customers.SetAffinity(name, skillID);
        PopulateCustomers();
        
        // Select the newly saved/updated customer
        for (int i = 0; i < _customerOption.ItemCount; i++)
        {
            if (_customerOption.GetItemText(i) == name)
            {
                _customerOption.Selected = i;
                break;
            }
        }

        Terminal.Write($"Saved test affinity for customer '{name}'.");
    }

    private void OnSaveAffinityPressed()
    {
        string playerName = _playerSelect.GetSelectedPlayer();
        int skillID = (int)_testSkillOption.GetItemMetadata(_testSkillOption.Selected);

        if (!string.IsNullOrEmpty(playerName))
        {
            if (Players.PlayerDict.TryGetValue(playerName, out Player player))
            {
                player.AffinitySkillID = skillID;
                Players.Save();
                Terminal.Write($"Saved test affinity for player '{playerName}'.");
            }
        }
        else
        {
            OnSaveCustomerPressed();
        }
    }

    private void OnFetchMealsPressed()
    {
        int testID = (int)_testSkillOption.GetItemMetadata(_testSkillOption.Selected);
        int wantID = (int)_wantSkillOption.GetItemMetadata(_wantSkillOption.Selected);

        var recipes = AffinityHelper.GetMealsForAffinity(testID, wantID);
        DisplayResults(recipes);
        UpdateMakerResult();
    }

    private void OnFetchMoonshinePressed()
    {
        int testID = (int)_testSkillOption.GetItemMetadata(_testSkillOption.Selected);
        int wantID = (int)_wantSkillOption.GetItemMetadata(_wantSkillOption.Selected);

        string recipe = AffinityHelper.GetMoonshineForAffinity(testID, wantID);
        DisplayResults(new List<string> { recipe });
        UpdateMakerResult();
    }

    private void UpdateMakerResult()
    {
        string makerName = _makerSelect.GetSelectedPlayer();
        if (string.IsNullOrEmpty(makerName) || !Players.PlayerDict.ContainsKey(makerName))
        {
            _makerResultLabel.Text = "Maker Predicted Result: None";
            return;
        }

        Player maker = Players.PlayerDict[makerName];
        if (maker.AffinitySkillID == -1)
        {
            _makerResultLabel.Text = "Maker Predicted Result: Set Maker Test Affinity!";
            return;
        }

        int customerTestID = (int)_testSkillOption.GetItemMetadata(_testSkillOption.Selected);
        int customerWantID = (int)_wantSkillOption.GetItemMetadata(_wantSkillOption.Selected);
        
        int sTest = 175;
        int targetID = (customerWantID - customerTestID + sTest) % 138;
        if (targetID < 0) targetID += 138;

        int makerResultID = AffinityHelper.GetMakerAffinityForID(maker.AffinitySkillID, targetID);
        string skillName = AffinityHelper.GetSkillNameByID(makerResultID);
        
        // Since it's a standard Label, we'll set text without BBCode for now unless we change node type
        _makerResultLabel.Text = $"Maker Predicted Result: {skillName}";
    }

    private void DisplayResults(List<string> results)
    {
        _resultsList.Clear();
        if (results.Count == 0)
        {
            _resultsList.AppendText("[color=red]No recipes found for this combination.[/color]");
            return;
        }

        foreach (var r in results)
        {
            _resultsList.AppendText($"• {r}\n\n");
        }
    }

    private void SetSkillOptionByValue(OptionButton option, int value)
    {
        for (int i = 0; i < option.ItemCount; i++)
        {
            if ((int)option.GetItemMetadata(i) == value)
            {
                option.Selected = i;
                return;
            }
        }
    }
}
