using Godot;
using System;
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

    public override void _Ready()
    {
        // Get node references
        _wurmPathsList = GetNode<ItemList>("%WurmPathsList");
        _playerList = GetNode<ItemList>("%PlayerList");
        _pathContextMenu = GetNode<PopupMenu>("%PathContextMenu");
        _folderDialog = GetNode<FileDialog>("%FolderDialog");

        var autoFindWurmButton = GetNode<Button>("%AutoFindWurmButton");
        var autoFindSteamButton = GetNode<Button>("%AutoFindSteamButton");
        var manualAddButton = GetNode<Button>("%ManualAddButton");

        // Connect Button signals
        autoFindWurmButton.Pressed += OnAutoFindWurmPressed;
        autoFindSteamButton.Pressed += OnAutoFindSteamPressed;
        manualAddButton.Pressed += OnManualAddPressed;

        // Connect ItemList signals
        _wurmPathsList.ItemClicked += OnWurmPathItemClicked;
        _pathContextMenu.IdPressed += OnPathContextIdPressed;

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

    private System.Collections.Generic.List<string> FindSteamWurmPaths()
    {
        var foundPaths = new System.Collections.Generic.List<string>();
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

    private System.Collections.Generic.List<string> GetSteamLibraryPaths(string steamPath)
    {
        var libraryPaths = new System.Collections.Generic.List<string> { steamPath };
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
                    if (Directory.Exists(path) && !libraryPaths.Contains(path))
                    {
                        libraryPaths.Add(path);
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

        for (int i = 0; i < _wurmPathsList.ItemCount; i++)
        {
            string existingPath = Path.GetFullPath(_wurmPathsList.GetItemText(i)).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            bool isDuplicate = (OS.GetName() == "Windows") 
                ? string.Equals(existingPath, normalizedPath, StringComparison.OrdinalIgnoreCase)
                : existingPath == normalizedPath;

            if (isDuplicate) return;
        }

        _wurmPathsList.AddItem(normalizedPath);
        Terminal.Write($"Path added: {normalizedPath}");
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
            Terminal.Write($"Removing path at index: {_rightClickedIndex}");
            _wurmPathsList.RemoveItem(_rightClickedIndex);
            _rightClickedIndex = -1;
        }
    }

    private void OnCloseRequested()
    {
        CallDeferred(MethodName.QueueFree);
    }
}
