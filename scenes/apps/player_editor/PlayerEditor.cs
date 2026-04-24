using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

public partial class PlayerEditor : Window
{
    private ItemList _wurmPathsList;
    private ItemList _playerList;
    private PopupMenu _pathContextMenu;
    private FileDialog _folderDialog;
    private int _rightClickedIndex = -1;

    private Label _selectedPlayerLabel;
    private Control _settingsGrid;
    private CheckBox _favCheck;
    private CheckBox _hiddenCheck;
    private Player _selectedPlayer;

    public override void _Ready()
    {
        // Get node references
        _wurmPathsList = GetNode<ItemList>("%WurmPathsList");
        _playerList = GetNode<ItemList>("%PlayerList");
        _pathContextMenu = GetNode<PopupMenu>("%PathContextMenu");
        _folderDialog = GetNode<FileDialog>("%FolderDialog");

        _selectedPlayerLabel = GetNode<Label>("%SelectedPlayerLabel");
        _settingsGrid = GetNode<Control>("%SettingsGrid");
        _favCheck = GetNode<CheckBox>("%FavCheck");
        _hiddenCheck = GetNode<CheckBox>("%HiddenCheck");

        var autoFindWurmButton = GetNode<Button>("%AutoFindWurmButton");
        var autoFindSteamButton = GetNode<Button>("%AutoFindSteamButton");
        var manualAddButton = GetNode<Button>("%ManualAddButton");

        // Load existing paths
        foreach (var path in Players.WurmPaths)
        {
            _wurmPathsList.AddItem(path);
        }
        if (Players.WurmPaths.Count > 0)
        {
            RefreshPlayerList();
        }

        // Connect Button signals
        autoFindWurmButton.Pressed += OnAutoFindWurmPressed;
        autoFindSteamButton.Pressed += OnAutoFindSteamPressed;
        manualAddButton.Pressed += OnManualAddPressed;

        // Connect ItemList signals
        _wurmPathsList.ItemClicked += OnWurmPathItemClicked;
        _pathContextMenu.IdPressed += OnPathContextIdPressed;
        _playerList.ItemSelected += OnPlayerItemSelected;

        // Connect Settings signals
        _favCheck.Toggled += OnFavToggled;
        _hiddenCheck.Toggled += OnHiddenToggled;

        // Connect FileDialog signals
        _folderDialog.DirSelected += OnFolderSelected;

        // Window management
        this.CloseRequested += OnCloseRequested;
    }

    private void OnAutoFindWurmPressed()
    {
        string path = "";
        string os = OS.GetName();

        try 
        {
            if (os == "Windows")
            {
                path = FindWurmWindows();
            }
            else if (os == "Linux")
            {
                path = FindWurmLinux();
            }
            else if (os == "macOS")
            {
                path = FindWurmMac();
            }
        }
        catch (Exception e)
        {
            Terminal.WriteError($"Error during Auto-Find Wurm: {e.Message}");
        }

        if (!string.IsNullOrEmpty(path))
        {
            AddWurmPath(path);
        }
        else
        {
            Terminal.Write("Could not find Wurm installation path automatically.");
        }
    }

    private string FindWurmWindows()
    {
        // Java Preferences on Windows are in the Registry
        // HKEY_CURRENT_USER\Software\JavaSoft\Prefs\com\wurmonline\client
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\JavaSoft\Prefs\com\wurmonline\client"))
                {
                    if (key != null)
                    {
                        object value = key.GetValue("wurm_dir");
                        if (value != null)
                        {
                            return DecodeJavaPref(value.ToString());
                        }
                    }
                }
            }
            catch { /* Registry might not be accessible */ }
        }
        return "";
    }

    private string FindWurmLinux()
    {
        // Java Preferences on Linux are in ~/.java/.userPrefs/com/wurmonline/client/prefs.xml
        string home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
        string prefsPath = Path.Combine(home, ".java/.userPrefs/com/wurmonline/client/prefs.xml");

        return ReadWurmDirFromXml(prefsPath);
    }

    private string FindWurmMac()
    {
        string home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
        
        // Check Linux-style path first as some Java setups use it
        string prefsPath = Path.Combine(home, ".java/.userPrefs/com/wurmonline/client/prefs.xml");
        string path = ReadWurmDirFromXml(prefsPath);
        
        if (string.IsNullOrEmpty(path))
        {
            // Fallback to common Mac locations
            string defaultMacPath = Path.Combine(home, "wurm");
            if (Directory.Exists(defaultMacPath)) return defaultMacPath;
        }

        return path;
    }

    private string ReadWurmDirFromXml(string xmlPath)
    {
        if (File.Exists(xmlPath))
        {
            try
            {
                var doc = XDocument.Load(xmlPath);
                var entry = doc.Descendants("entry")
                               .FirstOrDefault(e => e.Attribute("key")?.Value == "wurm_dir");
                
                if (entry != null)
                {
                    return DecodeJavaPref(entry.Attribute("value")?.Value);
                }
            }
            catch (Exception e)
            {
                Terminal.WriteError($"Failed to parse Java Prefs XML: {e.Message}");
            }
        }
        return "";
    }

    private string DecodeJavaPref(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Java Preferences encoding:
        // /C -> C (Capital letters are prefixed with /)
        // // -> /
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '/' && i + 1 < input.Length)
            {
                char next = input[i + 1];
                if (next == '/')
                {
                    sb.Append('/');
                    i++;
                }
                else
                {
                    sb.Append(char.ToUpper(next));
                    i++;
                }
            }
            else
            {
                sb.Append(input[i]);
            }
        }
        return sb.ToString();
    }

    private void OnAutoFindSteamPressed()
    {
        var paths = FindSteamWurmPaths();
        if (paths.Count > 0)
        {
            foreach (var path in paths)
            {
                AddWurmPath(path);
            }
        }
        else
        {
            Terminal.Write("Could not find Steam Wurm installation path automatically.");
        }
    }

    private List<string> FindSteamWurmPaths()
    {
        var foundPaths = new List<string>();
        string steamPath = GetSteamInstallPath();
        if (string.IsNullOrEmpty(steamPath)) return foundPaths;

        var libraryPaths = GetSteamLibraryPaths(steamPath);
        string appId = "1179680";

        foreach (var libPath in libraryPaths)
        {
            // Check for manifest
            string manifestPath = Path.Combine(libPath, "steamapps", $"appmanifest_{appId}.acf");
            if (File.Exists(manifestPath))
            {
                // Wurm Online Steam stores data in common/Wurm Online/gamedata
                string wurmPath = Path.Combine(libPath, "steamapps", "common", "Wurm Online", "gamedata");
                if (Directory.Exists(wurmPath))
                {
                    foundPaths.Add(wurmPath);
                }
            }
        }

        return foundPaths;
    }

    private string GetSteamInstallPath()
    {
        string os = OS.GetName();
        if (os == "Windows" && OperatingSystem.IsWindows())
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
                {
                    return key?.GetValue("SteamPath")?.ToString();
                }
            }
            catch { }
        }
        else if (os == "Linux")
        {
            string home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            string[] possiblePaths = {
                Path.Combine(home, ".local/share/Steam"),
                Path.Combine(home, ".steam/steam"),
                Path.Combine(home, ".var/app/com.valvesoftware.Steam/.local/share/Steam")
            };
            return possiblePaths.FirstOrDefault(Directory.Exists);
        }
        else if (os == "macOS")
        {
            string home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            string path = Path.Combine(home, "Library/Application Support/Steam");
            if (Directory.Exists(path)) return path;
        }
        return "";
    }

    private List<string> GetSteamLibraryPaths(string steamPath)
    {
        var libraryPaths = new List<string> { steamPath };
        string vdfPath = Path.Combine(steamPath, "config/libraryfolders.vdf");
        if (!File.Exists(vdfPath)) vdfPath = Path.Combine(steamPath, "steamapps/libraryfolders.vdf");

        if (File.Exists(vdfPath))
        {
            try
            {
                string content = File.ReadAllText(vdfPath);
                // Simple regex/manual parse for "path" entries in VDF
                var matches = System.Text.RegularExpressions.Regex.Matches(content, @"""path""\s+""([^""]+)""");
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    string path = match.Groups[1].Value.Replace(@"\\", @"\");
                    if (Directory.Exists(path))
                    {
                        bool exists = (OS.GetName() == "Windows") 
                            ? libraryPaths.Any(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase))
                            : libraryPaths.Contains(path);

                        if (!exists) libraryPaths.Add(path);
                    }
                }
            }
            catch (Exception e)
            {
                Terminal.WriteError($"Error parsing libraryfolders.vdf: {e.Message}");
            }
        }
        return libraryPaths;
    }

    private void OnManualAddPressed()
    {
        _folderDialog.PopupCentered();
    }

    private void OnFolderSelected(string dir)
    {
        AddWurmPath(dir);
    }

    public void AddWurmPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;

        // Normalize path to use consistent slashes and remove trailing ones
        string normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Verification: Ensure it's a valid Wurm directory (contains 'players' folder)
        string playersPath = Path.Combine(normalizedPath, "players");
        if (!Directory.Exists(playersPath))
        {
            Terminal.WriteError($"Invalid Wurm directory: '{normalizedPath}'. Missing 'players' folder.");
            return;
        }

        bool alreadyExists = (OS.GetName() == "Windows")
            ? Players.WurmPaths.Any(p => string.Equals(p, normalizedPath, StringComparison.OrdinalIgnoreCase))
            : Players.WurmPaths.Contains(normalizedPath);

        if (alreadyExists) return;

        _wurmPathsList.AddItem(normalizedPath);
        Players.WurmPaths.Add(normalizedPath);
        Terminal.Write($"Path added: {normalizedPath}");
        RefreshPlayerList();
    }

    private void RefreshPlayerList()
    {
        // Don't clear Players.PlayerDict as it contains saved metadata (Favorites, Hidden)
        _playerList.Clear();
        _selectedPlayer = null;
        _settingsGrid.Visible = false;
        _selectedPlayerLabel.Text = "Select a player to edit";

        HashSet<string> foundPlayers = new HashSet<string>();

        for (int i = 0; i < _wurmPathsList.ItemCount; i++)
        {
            string wurmPath = _wurmPathsList.GetItemText(i);
            string playersFolderPath = Path.Combine(wurmPath, "players");

            if (Directory.Exists(playersFolderPath))
            {
                var playerDirs = Directory.GetDirectories(playersFolderPath);
                foreach (var dir in playerDirs)
                {
                    // Verification: Ensure player directory contains a 'logs' folder
                    string logsPath = Path.Combine(dir, "logs");
                    if (!Directory.Exists(logsPath)) continue;

                    string playerName = Path.GetFileName(dir);
                    foundPlayers.Add(playerName);

                    if (!Players.PlayerDict.ContainsKey(playerName))
                    {
                        Players.AddPlayer(playerName, dir);
                    }
                    else
                    {
                        // Update path in case it changed/moved
                        Players.PlayerDict[playerName].Path = dir;
                    }
                }
            }
        }

        // Remove players that are no longer found in any configured path
        var playersToRemove = Players.PlayerDict.Keys.Where(name => !foundPlayers.Contains(name)).ToList();
        foreach (var name in playersToRemove)
        {
            Players.PlayerDict.Remove(name);
        }

        // Repopulate UI list from sorted keys
        foreach (var playerName in Players.PlayerDict.Keys.OrderBy(n => n))
        {
            int idx = _playerList.AddItem(ToCamelCase(playerName));
            _playerList.SetItemMetadata(idx, playerName);
        }
        
        Terminal.Write($"Detected {_playerList.ItemCount} players.");
        Players.Save();
    }

    private void OnPlayerItemSelected(long index)
    {
        string playerName = _playerList.GetItemMetadata((int)index).AsString();
        if (Players.PlayerDict.TryGetValue(playerName, out Player player))
        {
            _selectedPlayer = player;
            _selectedPlayerLabel.Text = ToCamelCase(playerName);
            _settingsGrid.Visible = true;

            // Update UI without triggering signals
            _favCheck.SetBlockSignals(true);
            _hiddenCheck.SetBlockSignals(true);

            _favCheck.ButtonPressed = player.IsFav;
            _hiddenCheck.ButtonPressed = player.IsHidden;

            _favCheck.SetBlockSignals(false);
            _hiddenCheck.SetBlockSignals(false);
        }
    }

    private void OnFavToggled(bool toggled)
    {
        if (_selectedPlayer != null)
        {
            _selectedPlayer.IsFav = toggled;
            Players.Save();
        }
    }

    private void OnHiddenToggled(bool toggled)
    {
        if (_selectedPlayer != null)
        {
            _selectedPlayer.IsHidden = toggled;
            Players.Save();
        }
    }

    private string ToCamelCase(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Split by common separators if they exist, or just treat as one word
        var words = text.Split(new[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
        }

        return string.Join("", words);
    }

    private void OnWurmPathItemClicked(long index, Vector2 atPosition, long mouseButtonIndex)
    {
        if (mouseButtonIndex == (long)MouseButton.Right)
        {
            _rightClickedIndex = (int)index;
            _pathContextMenu.Position = (Vector2I)(GetWindow().Position + (Vector2I)atPosition + (Vector2I)_wurmPathsList.GlobalPosition);
            _pathContextMenu.Popup();
        }
    }

    private void OnPathContextIdPressed(long id)
    {
        if (id == 0 && _rightClickedIndex != -1)
        {
            string pathToRemove = _wurmPathsList.GetItemText(_rightClickedIndex);
            Terminal.Write($"Removing path at index: {_rightClickedIndex}");
            _wurmPathsList.RemoveItem(_rightClickedIndex);
            Players.WurmPaths.Remove(pathToRemove);
            
            _rightClickedIndex = -1;
            RefreshPlayerList();
        }
    }

    private void OnCloseRequested()
    {
        CallDeferred(MethodName.QueueFree);
    }
}
