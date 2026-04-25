using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HvergiToolkit.Libs;

public partial class SkillCompare : Window
{
    private class SkillData
    {
        public string Name;
        public double Level;
        public int AffinityCount;
        public int Depth;
        public List<SkillData> Children = new();
        public SkillData Parent;
    }

    private PlayerList _playerList;
    private OptionButton _dump1Select;
    private OptionButton _dump2Select;
    private Tree _skillsTree;
    private Tree _affinitiesTree;
    private Button _compareButton;

    private string _selectedPlayerName;
    private List<string> _dumpFiles = new();

    public override void _Ready()
    {
        this.CloseRequested += OnCloseRequested;

        _playerList = GetNode<PlayerList>("%PlayerList");
        _dump1Select = GetNode<OptionButton>("%Dump1Select");
        _dump2Select = GetNode<OptionButton>("%Dump2Select");
        _skillsTree = GetNode<Tree>("%Skills");
        _affinitiesTree = GetNode<Tree>("%Affinities");
        _compareButton = GetNode<Button>("%CompareButton");

        _playerList.PlayerSelected += OnPlayerSelected;
        _compareButton.Pressed += OnComparePressed;

        SetupTrees();
    }

    private void OnCloseRequested()
    {
        CallDeferred(MethodName.QueueFree);
    }

    private void SetupTrees()
    {
        // Skills Tree
        _skillsTree.Columns = 5;
        _skillsTree.SetColumnTitle(0, "Skill");
        _skillsTree.SetColumnTitle(1, "Dump 1");
        _skillsTree.SetColumnTitle(2, "Dump 2");
        _skillsTree.SetColumnTitle(3, "Change");
        _skillsTree.SetColumnTitle(4, "STP Gained");
        _skillsTree.SetColumnExpand(0, true);
        _skillsTree.SetColumnCustomMinimumWidth(0, 200);

        // Affinities Tree
        _affinitiesTree.Columns = 4;
        _affinitiesTree.SetColumnTitle(0, "Skill");
        _affinitiesTree.SetColumnTitle(1, "Dump 1 Aff.");
        _affinitiesTree.SetColumnTitle(2, "Dump 2 Aff.");
        _affinitiesTree.SetColumnTitle(3, "Change");
        _affinitiesTree.SetColumnExpand(0, true);
        _affinitiesTree.SetColumnCustomMinimumWidth(0, 200);
    }

    private void OnPlayerSelected(string playerName)
    {
        _selectedPlayerName = playerName;
        RefreshDumpLists();
    }

    private void RefreshDumpLists()
    {
        _dump1Select.Clear();
        _dump2Select.Clear();
        _dumpFiles.Clear();

        if (string.IsNullOrEmpty(_selectedPlayerName)) return;

        if (Players.PlayerDict.TryGetValue(_selectedPlayerName, out Player player))
        {
            string dumpsPath = player.GetDumpsPath();
            if (Directory.Exists(dumpsPath))
            {
                var files = Directory.GetFiles(dumpsPath, "skills.*.txt")
                    .OrderByDescending(f => f)
                    .ToList();

                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    _dumpFiles.Add(file);
                    _dump1Select.AddItem(fileName);
                    _dump2Select.AddItem(fileName);
                }

                if (_dump1Select.ItemCount > 1)
                {
                    _dump1Select.Selected = 1; // Default to second newest for older
                    _dump2Select.Selected = 0; // Default to newest for newer
                }
            }
        }
    }

    private void OnComparePressed()
    {
        if (_dump1Select.Selected == -1 || _dump2Select.Selected == -1) return;

        string path1 = _dumpFiles[_dump1Select.Selected];
        string path2 = _dumpFiles[_dump2Select.Selected];

        var skills1 = ParseDump(path1);
        var skills2 = ParseDump(path2);

        PopulateSkillsTree(skills1, skills2);
        PopulateAffinitiesTree(skills1, skills2);
    }

    private Dictionary<string, SkillData> ParseDump(string path)
    {
        var skillMap = new Dictionary<string, SkillData>();
        if (!File.Exists(path)) return skillMap;

        var lines = File.ReadAllLines(path);
        bool startParsing = false;
        
        Stack<SkillData> parentStack = new();

        foreach (var line in lines)
        {
            if (line.StartsWith("-----"))
            {
                startParsing = true;
                continue;
            }
            if (!startParsing) continue;
            if (string.IsNullOrWhiteSpace(line)) continue;

            int indent = 0;
            while (indent < line.Length && line[indent] == ' ') indent++;
            
            string content = line.Trim();
            if (string.IsNullOrEmpty(content)) continue;

            int colonIdx = content.IndexOf(':');
            if (colonIdx == -1) continue;

            string name = content.Substring(0, colonIdx).Trim();
            string valuesPart = content.Substring(colonIdx + 1).Trim();
            string[] values = valuesPart.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (values.Length > 0 && double.TryParse(values[0], out double level))
            {
                int depth = indent / 3;
                var data = new SkillData { Name = name, Level = level, Depth = depth };

                // Affinity Count (usually the 3rd value)
                if (values.Length >= 3 && int.TryParse(values[2], out int aff))
                {
                    data.AffinityCount = aff;
                }

                while (parentStack.Count > depth)
                {
                    parentStack.Pop();
                }

                if (parentStack.Count > 0)
                {
                    data.Parent = parentStack.Peek();
                    data.Parent.Children.Add(data);
                }

                parentStack.Push(data);
                
                if (level > 0 || !skillMap.ContainsKey(name))
                {
                    skillMap[name] = data;
                }
            }
        }

        return skillMap;
    }

    private void PopulateSkillsTree(Dictionary<string, SkillData> skills1, Dictionary<string, SkillData> skills2)
    {
        _skillsTree.Clear();
        var root = _skillsTree.CreateItem();

        var topLevel = skills2.Values.Where(s => s.Parent == null).OrderBy(s => s.Name).ToList();

        foreach (var skill in topLevel)
        {
            AddSkillToTree(root, skill, skills1, skills2);
        }
    }

    private void AddSkillToTree(TreeItem parent, SkillData s2, Dictionary<string, SkillData> skills1, Dictionary<string, SkillData> skills2)
    {
        var item = _skillsTree.CreateItem(parent);
        string name = s2.Name;
        double v2 = s2.Level;

        string displayName = name;
        if (s2.AffinityCount > 0)
        {
            displayName += new string('*', s2.AffinityCount);
        }

        item.SetText(0, displayName);
        item.SetCustomColor(0, GetLevelColor(v2));

        if (v2 > 0)
        {
            double v1 = skills1.TryGetValue(name, out var s1) ? s1.Level : 0;
            double change = v2 - v1;

            if (v1 > 0) item.SetText(1, v1.ToString("F4"));
            item.SetText(2, v2.ToString("F4"));
            
            if (change != 0)
            {
                item.SetText(3, (change > 0 ? "+" : "") + change.ToString("F4"));
                item.SetCustomColor(3, change > 0 ? Colors.Green : Colors.Red);
            }

            if (STPHelper.SkillDifficulty.TryGetValue(name, out double diff))
            {
                long stp = STPHelper.GetStpFromLevelToLevel(v1, v2, diff, false);
                if (stp != 0)
                {
                    item.SetText(4, stp.ToString("N0"));
                    item.SetCustomColor(4, stp > 0 ? Colors.Green : Colors.Red);
                }
            }
        }

        foreach (var child in s2.Children)
        {
            AddSkillToTree(item, child, skills1, skills2);
        }
    }

    private void PopulateAffinitiesTree(Dictionary<string, SkillData> skills1, Dictionary<string, SkillData> skills2)
    {
        _affinitiesTree.Clear();
        var root = _affinitiesTree.CreateItem();

        var allSkillNames = skills1.Keys.Union(skills2.Keys).OrderBy(n => n).ToList();
        
        int totalA1 = 0;
        int totalA2 = 0;

        // First pass: Calculate totals
        foreach (var name in allSkillNames)
        {
            totalA1 += skills1.TryGetValue(name, out var s1) ? s1.AffinityCount : 0;
            totalA2 += skills2.TryGetValue(name, out var s2) ? s2.AffinityCount : 0;
        }

        // Add Total Row
        var totalItem = _affinitiesTree.CreateItem(root);
        totalItem.SetText(0, "TOTAL AFFINITIES");
        totalItem.SetText(1, totalA1.ToString());
        totalItem.SetText(2, totalA2.ToString());
        int totalChange = totalA2 - totalA1;
        if (totalChange != 0)
        {
            totalItem.SetText(3, (totalChange > 0 ? "+" : "") + totalChange.ToString());
            totalItem.SetCustomColor(3, totalChange > 0 ? Colors.Green : Colors.Red);
        }
        totalItem.SetCustomBgColor(0, new Color(1, 1, 1, 0.1f));
        totalItem.SetCustomBgColor(1, new Color(1, 1, 1, 0.1f));
        totalItem.SetCustomBgColor(2, new Color(1, 1, 1, 0.1f));
        totalItem.SetCustomBgColor(3, new Color(1, 1, 1, 0.1f));

        // Second pass: Add individual skills
        foreach (var name in allSkillNames)
        {
            int a1 = skills1.TryGetValue(name, out var s1) ? s1.AffinityCount : 0;
            int a2 = skills2.TryGetValue(name, out var s2) ? s2.AffinityCount : 0;
            int change = a2 - a1;

            if (a1 > 0 || a2 > 0)
            {
                var item = _affinitiesTree.CreateItem(root);
                item.SetText(0, name);
                item.SetText(1, a1.ToString());
                item.SetText(2, a2.ToString());

                if (change != 0)
                {
                    item.SetText(3, (change > 0 ? "+" : "") + change.ToString());
                    item.SetCustomColor(3, change > 0 ? Colors.Green : Colors.Red);
                }
            }
        }
    }

    private Color GetLevelColor(double level)
    {
        if (level >= 100) return Colors.MediumPurple;
        if (level >= 99) return Colors.DarkRed;
        if (level >= 90) return Colors.Red;
        if (level >= 70) return Colors.Orange;
        if (level >= 50) return Colors.Yellow;
        return Colors.White;
    }
}
