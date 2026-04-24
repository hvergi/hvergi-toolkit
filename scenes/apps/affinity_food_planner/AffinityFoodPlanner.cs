using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using HvergiToolkit.Libs;

public partial class AffinityFoodPlanner : Window
{
    private PlayerSelectHorizontal _playerSelect;
    private PlayerSelectHorizontal _makerSelect;
    private TabContainer _targetTabs;
    private OptionButton _customerOption;
    private Button _addCustomerBtn;
    private Button _deleteCustomerBtn;
    private PopupPanel _customerPopup;
    private LineEdit _newCustomerName;
    private OptionButton _newCustomerSkill;
    private Button _popupSaveBtn;
    
    private OptionButton _stationOption;
    private OptionButton _rarityOption;
    private OptionButton _meatOption;
    private OptionButton _cheeseOption;
    private CheckBox _saltCheck;
    private GridContainer _herbGrid;
    private Button _allHerbsBtn;
    private Button _noHerbsBtn;
    private List<CheckBox> _herbCheckBoxes = new List<CheckBox>();

    private OptionButton _testSkillOption;
    private OptionButton _wantSkillOption;
    private Button _saveAffinityBtn;
    private RichTextLabel _moonshineResults;
    private RichTextLabel _mealResults;
    private RichTextLabel _makerResultLabel;

    public override void _Ready()
    {
        _playerSelect = GetNode<PlayerSelectHorizontal>("%PlayerSelect");
        _makerSelect = GetNode<PlayerSelectHorizontal>("%MakerSelect");
        _targetTabs = GetNode<TabContainer>("%TargetTabs");
        _customerOption = GetNode<OptionButton>("%CustomerOption");
        _addCustomerBtn = GetNode<Button>("%AddCustomerBtn");
        _deleteCustomerBtn = GetNode<Button>("%DeleteCustomerBtn");
        _customerPopup = GetNode<PopupPanel>("%CustomerPopup");
        _newCustomerName = GetNode<LineEdit>("%NewCustomerName");
        _newCustomerSkill = GetNode<OptionButton>("%NewCustomerSkill");
        _popupSaveBtn = GetNode<Button>("%PopupSaveBtn");

        _stationOption = GetNode<OptionButton>("%StationOption");
        _rarityOption = GetNode<OptionButton>("%RarityOption");
        _meatOption = GetNode<OptionButton>("%MeatOption");
        _cheeseOption = GetNode<OptionButton>("%CheeseOption");
        _saltCheck = GetNode<CheckBox>("%SaltCheck");
        _herbGrid = GetNode<GridContainer>("%HerbGrid");
        _allHerbsBtn = GetNode<Button>("%AllHerbsBtn");
        _noHerbsBtn = GetNode<Button>("%NoHerbsBtn");

        _testSkillOption = GetNode<OptionButton>("%TestSkillOption");
        _wantSkillOption = GetNode<OptionButton>("%WantSkillOption");
        _saveAffinityBtn = GetNode<Button>("%SaveAffinityBtn");
        _moonshineResults = GetNode<RichTextLabel>("%MoonshineResults");
        _mealResults = GetNode<RichTextLabel>("%MealResults");
        _makerResultLabel = GetNode<RichTextLabel>("%MakerResultLabel");

        PopulateSkills();
        PopulateCustomers();
        PopulateCheeses();
        PopulateMeats();
        PopulateHerbs();

        // Reactive Inputs
        _testSkillOption.ItemSelected += (idx) => Calculate();
        _wantSkillOption.ItemSelected += (idx) => Calculate();
        _makerSelect.PlayerSelected += (name) => Calculate();
        _stationOption.ItemSelected += (idx) => Calculate();
        _rarityOption.ItemSelected += (idx) => Calculate();
        _meatOption.ItemSelected += (idx) => Calculate();
        _cheeseOption.ItemSelected += (idx) => Calculate();
        _saltCheck.Toggled += (toggled) => Calculate();
        
        // Profile Loading
        _playerSelect.PlayerSelected += OnPlayerSelected;
        _customerOption.ItemSelected += OnCustomerSelected;
        _targetTabs.TabSelected += OnTargetTabSelected;
        
        // Explicit Actions
        _addCustomerBtn.Pressed += () => { _newCustomerName.Text = ""; _customerPopup.PopupCentered(); };
        _deleteCustomerBtn.Pressed += OnDeleteCustomerPressed;
        _popupSaveBtn.Pressed += OnPopupSaveCustomerPressed;
        _saveAffinityBtn.Pressed += OnSaveAffinityPressed;
        
        _allHerbsBtn.Pressed += () => ToggleAllHerbs(true);
        _noHerbsBtn.Pressed += () => ToggleAllHerbs(false);

        this.CloseRequested += () => CallDeferred(MethodName.QueueFree);
    }

    private void PopulateSkills()
    {
        _testSkillOption.Clear();
        _wantSkillOption.Clear();
        _newCustomerSkill.Clear();

        var skills = AffinityHelper.SkillIDs.OrderBy(kvp => kvp.Key).ToList();
        foreach (var skill in skills)
        {
            _testSkillOption.AddItem(skill.Key);
            _testSkillOption.SetItemMetadata(_testSkillOption.ItemCount - 1, skill.Value);

            _wantSkillOption.AddItem(skill.Key);
            _wantSkillOption.SetItemMetadata(_wantSkillOption.ItemCount - 1, skill.Value);

            _newCustomerSkill.AddItem(skill.Key);
            _newCustomerSkill.SetItemMetadata(_newCustomerSkill.ItemCount - 1, skill.Value);
        }
    }

    private void PopulateCustomers()
    {
        _customerOption.Clear();
        _customerOption.AddItem("Load Customer...");
        
        var sortedCustomers = Customers.CustomerAffinities.Keys.OrderBy(n => n).ToList();
        foreach (var name in sortedCustomers)
        {
            _customerOption.AddItem(name);
        }
    }

    private void PopulateCheeses()
    {
        _cheeseOption.Clear();
        for (int i = 0; i < AffinityHelper.CheeseNames.Length; i++)
        {
            _cheeseOption.AddItem(AffinityHelper.CheeseNames[i]);
            _cheeseOption.SetItemMetadata(i, AffinityHelper.Cheeses[i]);
        }
        _cheeseOption.Selected = 0;
    }

    private void PopulateMeats()
    {
        _meatOption.Clear();
        _meatOption.AddItem("Any Meat");
        _meatOption.SetItemMetadata(0, -1);
        for (int i = 0; i < AffinityHelper.MeatNames.Length; i++)
        {
            _meatOption.AddItem(AffinityHelper.MeatNames[i]);
            _meatOption.SetItemMetadata(i + 1, i);
        }
        _meatOption.Selected = 0;
    }

    private void PopulateHerbs()
    {
        foreach (var child in _herbGrid.GetChildren()) child.QueueFree();
        _herbCheckBoxes.Clear();

        for (int i = 0; i < AffinityHelper.HerbNames.Length; i++)
        {
            var cb = new CheckBox();
            cb.Text = AffinityHelper.HerbNames[i].Replace("Chopped ", "").Replace("Ground ", "");
            cb.ButtonPressed = false; // Default to NO exclusions
            cb.Toggled += (toggled) => Calculate();
            _herbGrid.AddChild(cb);
            _herbCheckBoxes.Add(cb);
        }
    }

    private void ToggleAllHerbs(bool toggled)
    {
        foreach (var cb in _herbCheckBoxes)
        {
            cb.SetBlockSignals(true);
            cb.ButtonPressed = toggled;
            cb.SetBlockSignals(false);
        }
        Calculate();
    }

    private void OnTargetTabSelected(long tab)
    {
        if (tab == 0) // Players
        {
            _customerOption.Selected = 0;
        }
    }

    private void OnPlayerSelected(string playerName)
    {
        if (Players.PlayerDict.TryGetValue(playerName, out Player player))
        {
            if (player.AffinitySkillID != -1)
            {
                SetSkillOptionByValue(_testSkillOption, player.AffinitySkillID);
                Calculate();
            }
        }
    }

    private void OnCustomerSelected(long index)
    {
        if (index == 0) return;

        string name = _customerOption.GetItemText((int)index);
        if (Customers.CustomerAffinities.TryGetValue(name, out int skillID))
        {
            SetSkillOptionByValue(_testSkillOption, skillID);
            Calculate();
        }
    }

    private void OnDeleteCustomerPressed()
    {
        if (_customerOption.Selected <= 0) return;

        string name = _customerOption.GetItemText(_customerOption.Selected);
        if (Customers.CustomerAffinities.ContainsKey(name))
        {
            Customers.CustomerAffinities.Remove(name);
            Customers.Save();
            PopulateCustomers();
            _moonshineResults.Clear();
            _mealResults.Clear();
            Terminal.Write($"Deleted customer '{name}'.");
        }
    }

    private void OnPopupSaveCustomerPressed()
    {
        string name = _newCustomerName.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        int skillID = (int)_newCustomerSkill.GetItemMetadata(_newCustomerSkill.Selected);
        Customers.SetAffinity(name, skillID);
        PopulateCustomers();
        
        // Select it
        for (int i = 0; i < _customerOption.ItemCount; i++)
        {
            if (_customerOption.GetItemText(i) == name)
            {
                _customerOption.Selected = i;
                OnCustomerSelected(i);
                break;
            }
        }

        _customerPopup.Hide();
        Terminal.Write($"Added and saved new customer '{name}'.");
    }

    private void OnSaveAffinityPressed()
    {
        if (_targetTabs.CurrentTab == 0) // Players
        {
            string playerName = _playerSelect.GetSelectedPlayer();
            if (!string.IsNullOrEmpty(playerName) && Players.PlayerDict.TryGetValue(playerName, out Player player))
            {
                int skillID = (int)_testSkillOption.GetItemMetadata(_testSkillOption.Selected);
                player.AffinitySkillID = skillID;
                Players.Save();
                Terminal.Write($"Saved test affinity for player '{playerName}'.");

                if (_makerSelect.GetSelectedPlayer() == playerName)
                {
                    Calculate();
                }
            }
        }
        else // Customers
        {
            if (_customerOption.Selected > 0)
            {
                string name = _customerOption.GetItemText(_customerOption.Selected);
                int skillID = (int)_testSkillOption.GetItemMetadata(_testSkillOption.Selected);
                Customers.SetAffinity(name, skillID);
                Terminal.Write($"Updated test affinity for customer '{name}'.");
            }
            else
            {
                _customerPopup.PopupCentered();
            }
        }
    }

    private void Calculate()
    {
        if (_testSkillOption.Selected < 0 || _wantSkillOption.Selected < 0) return;

        int testID = (int)_testSkillOption.GetItemMetadata(_testSkillOption.Selected);
        int wantID = (int)_wantSkillOption.GetItemMetadata(_wantSkillOption.Selected);

        // Update Moonshine
        string moonshineRecipe = AffinityHelper.GetMoonshineForAffinity(testID, wantID);
        _moonshineResults.Clear();
        _moonshineResults.AppendText(moonshineRecipe);

        // Meal Calculation with Preferences
        int stationBaseIdx = _stationOption.Selected * 4;
        int stationIdx = stationBaseIdx + _rarityOption.Selected;
        int cheeseIdx = _cheeseOption.Selected;
        int meatIdx = (int)_meatOption.GetItemMetadata(_meatOption.Selected);
        bool useSalt = _saltCheck.ButtonPressed;

        HashSet<int> excludedHerbs = new HashSet<int>();
        for (int i = 0; i < _herbCheckBoxes.Count; i++)
        {
            if (_herbCheckBoxes[i].ButtonPressed) excludedHerbs.Add(i);
        }

        var mealRecipes = AffinityHelper.GetMealsForAffinity(testID, wantID, stationIdx, cheeseIdx, useSalt, meatIdx, excludedHerbs);
        _mealResults.Clear();
        if (mealRecipes.Count == 0)
        {
            _mealResults.AppendText("[color=red]No meal recipes found for these preferences.[/color]");
        }
        else
        {
            foreach (var r in mealRecipes)
            {
                _mealResults.AppendText($"• {r}\n\n");
            }
        }

        UpdateMakerResult(testID, wantID);
    }

    private void UpdateMakerResult(int customerTestID, int customerWantID)
    {
        string makerName = _makerSelect.GetSelectedPlayer();
        if (string.IsNullOrEmpty(makerName) || !Players.PlayerDict.ContainsKey(makerName))
        {
            _makerResultLabel.Text = "[center][color=gray]Select a Maker to see predicted result[/color][/center]";
            return;
        }

        Player maker = Players.PlayerDict[makerName];
        if (maker.AffinitySkillID == -1)
        {
            _makerResultLabel.Text = "[center][color=yellow]Set Maker's Test Affinity in their profile first![/color][/center]";
            return;
        }

        string makerTestSkillName = AffinityHelper.GetSkillNameByID(maker.AffinitySkillID);
        
        int sTest = 175;
        int targetID = (customerWantID - customerTestID + sTest) % 138;
        if (targetID < 0) targetID += 138;

        int makerResultID = AffinityHelper.GetMakerAffinityForID(maker.AffinitySkillID, targetID);
        string predictedSkillName = AffinityHelper.GetSkillNameByID(makerResultID);
        
        _makerResultLabel.Text = $"[center]Current Test Affinity: [b][color=yellow]{makerTestSkillName}[/color][/b]\n\nMaker Predicted Taste Result:\n[b][color=cyan]{predictedSkillName}[/color][/b][/center]";
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
