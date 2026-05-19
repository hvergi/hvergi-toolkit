using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

public partial class LogSearch : Window
{
    private PlayerList _playerList;
    private OptionButton _logTypeSelect;
    private LineEdit _searchEdit;
    private Button _searchButton;
    private Button _cancelButton;
    private ItemList _resultsList;
    private TextEdit _fileViewer;

    private CheckBox _useDateFilterCheck;
    private SpinBox _startYear, _startMonth, _startDay;
    private SpinBox _endYear, _endMonth, _endDay;
    private Label _statusLabel;
    private ProgressBar _progressBar;

    private Button _loadPrevButton;
    private Button _loadNextButton;

    private string _selectedPlayerName;
    private CancellationTokenSource _searchCts;

    // View state
    private string _currentViewPath;
    private int _currentMatchLine;
    private int _viewStartLine;
    private int _viewEndLine;

    private int _lastChunckCount;

    public override void _Ready()
    {
        this.CloseRequested += OnCloseRequested;

        _playerList = GetNode<PlayerList>("%PlayerList");
        _logTypeSelect = GetNode<OptionButton>("%LogTypeSelect");
        _searchEdit = GetNode<LineEdit>("%SearchEdit");
        _searchButton = GetNode<Button>("%SearchButton");
        _cancelButton = GetNode<Button>("%CancelButton");
        _resultsList = GetNode<ItemList>("%ResultsList");
        _fileViewer = GetNode<TextEdit>("%FileViewer");

        _useDateFilterCheck = GetNode<CheckBox>("%UseDateFilterCheck");
        _startYear = GetNode<SpinBox>("%StartYear");
        _startMonth = GetNode<SpinBox>("%StartMonth");
        _startDay = GetNode<SpinBox>("%StartDay");
        _endYear = GetNode<SpinBox>("%EndYear");
        _endMonth = GetNode<SpinBox>("%EndMonth");
        _endDay = GetNode<SpinBox>("%EndDay");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _progressBar = GetNode<ProgressBar>("%ProgressBar");

        _loadPrevButton = GetNode<Button>("%LoadPrevButton");
        _loadNextButton = GetNode<Button>("%LoadNextButton");

        _playerList.PlayerSelected += OnPlayerSelected;
        _searchButton.Pressed += OnSearchPressed;
        _cancelButton.Pressed += OnCancelPressed;
        _resultsList.ItemSelected += OnResultSelected;

        _loadPrevButton.Pressed += OnLoadPrevPressed;
        _loadNextButton.Pressed += OnLoadNextPressed;

        // Initialize dates to current month
        var now = DateTime.Now;
        _startYear.Value = now.Year;
        _startMonth.Value = now.Month;
        _startDay.Value = 1;
        _endYear.Value = now.Year;
        _endMonth.Value = now.Month;
        _endDay.Value = now.Day;

        PopulateLogTypes();
    }

    private void OnCloseRequested()
    {
        _searchCts?.Cancel();
        CallDeferred(MethodName.QueueFree);
    }

    private void OnCancelPressed()
    {
        _searchCts?.Cancel();
    }

    private void PopulateLogTypes()
    {
        _logTypeSelect.Clear();
        foreach (var type in Enum.GetValues<LogReader.LogFileType>())
        {
            _logTypeSelect.AddItem(type.ToString());
            _logTypeSelect.SetItemMetadata(_logTypeSelect.ItemCount - 1, (int)type);
        }
    }

    private void OnPlayerSelected(string playerName)
    {
        _selectedPlayerName = playerName;
        Terminal.Write($"LogSearch: Selected player {playerName}");
    }

    private async void OnSearchPressed()
    {
        if (string.IsNullOrEmpty(_selectedPlayerName))
        {
            Terminal.WriteWarning("LogSearch: Please select a player first.");
            return;
        }

        string query = _searchEdit.Text.Trim();
        if (string.IsNullOrEmpty(query))
        {
            Terminal.WriteWarning("LogSearch: Please enter a search string.");
            return;
        }

        if (_logTypeSelect.Selected == -1) return;
        LogReader.LogFileType selectedType = (LogReader.LogFileType)_logTypeSelect.GetSelectedMetadata().AsInt32();

        if (Players.PlayerDict.TryGetValue(_selectedPlayerName, out Player player))
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            SetSearching(true);
            _resultsList.Clear();
            _fileViewer.Text = "";
            _loadPrevButton.Visible = false;
            _loadNextButton.Visible = false;

            try
            {
                DateTime startDate = new DateTime((int)_startYear.Value, (int)_startMonth.Value, (int)_startDay.Value);
                DateTime endDate = new DateTime((int)_endYear.Value, (int)_endMonth.Value, (int)_endDay.Value).AddDays(1).AddSeconds(-1);
                bool useFilter = _useDateFilterCheck.ButtonPressed;

                await Task.Run(() => SearchLogsAsync(player, selectedType, query, useFilter, startDate, endDate, token), token);
            }
            catch (OperationCanceledException)
            {
                Terminal.Write("LogSearch: Search cancelled.");
                _statusLabel.Text = "Search cancelled.";
            }
            catch (Exception e)
            {
                Terminal.WriteError($"LogSearch: Search failed: {e.Message}");
                _statusLabel.Text = "Search failed.";
            }
            finally
            {
                SetSearching(false);
            }
        }
    }

    private void SetSearching(bool searching)
    {
        _searchButton.Visible = !searching;
        _cancelButton.Visible = searching;
        _playerList.MouseFilter = searching ? Control.MouseFilterEnum.Ignore : Control.MouseFilterEnum.Stop;
        _playerList.Modulate = searching ? new Color(0.5f, 0.5f, 0.5f) : new Color(1, 1, 1);
        _logTypeSelect.Disabled = searching;
        _searchEdit.Editable = !searching;
        _useDateFilterCheck.Disabled = searching;
        
        if (!searching)
        {
            _progressBar.Value = 0;
        }
    }

    private struct MatchInfo
    {
        public string FileName;
        public string FilePath;
        public int LineNumber;
        public string LineText;
        public DateTime TimeStamp;
    }

    private const int MaxResultsToDisplay = 500;

    private void SearchLogsAsync(Player player, LogReader.LogFileType type, string query, bool useFilter, DateTime start, DateTime end, CancellationToken token)
    {
        string logsDir = Path.Combine(player.Path, "logs");
        if (!Directory.Exists(logsDir))
        {
            UpdateStatus("Logs directory not found.");
            return;
        }

        if (!LogReader.Prefixes.TryGetValue(type, out string prefix)) return;

        var allFiles = Directory.GetFiles(logsDir, prefix + "*.txt")
            .Select(f => new FileInfo(f))
            .OrderByDescending(fi => fi.LastWriteTime)
            .Select(fi => fi.FullName) // Convert back to string paths
            .ToList();

        List<string> filteredFiles = new();
        foreach (var file in allFiles)
        {
            if (!useFilter)
            {
                filteredFiles.Add(file);
                continue;
            }

            var fileDate = GetDateFromFileName(Path.GetFileName(file), prefix);
            if (fileDate.HasValue)
            {
                if (Path.GetFileNameWithoutExtension(file).Split('.').Length == 2)
                {
                    DateTime monthStart = fileDate.Value;
                    DateTime monthEnd = monthStart.AddMonths(1).AddSeconds(-1);
                    if (monthStart <= end && monthEnd >= start)
                        filteredFiles.Add(file);
                }
                else if (fileDate.Value >= start && fileDate.Value <= end)
                {
                    filteredFiles.Add(file);
                }
            }
        }

        UpdateStatus($"Searching {filteredFiles.Count} files...");

        int totalMatches = 0;
        int displayedMatches = 0;
        DateTime currentLogDate = DateTime.MinValue;
        List<MatchInfo> matchBuffer = new();

        for (int fIdx = 0; fIdx < filteredFiles.Count; fIdx++)
        {
            token.ThrowIfCancellationRequested();
            string file = filteredFiles[fIdx];
            UpdateProgress((float)fIdx / filteredFiles.Count, Path.GetFileName(file));

            try
            {
                using var fs = new FileStream(file, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                int lineIdx = 0;

                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    lineIdx++;
                    if (lineIdx % 2000 == 0) token.ThrowIfCancellationRequested();
                    
                    if (line != null && line.Contains("Logging started"))
                    {
                        if (DateTime.TryParseExact(line.Substring(line.Length - 10), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                        {
                            currentLogDate = parsedDate;
                            continue;
                        }

                    }

                    if (useFilter && !(currentLogDate >= start && currentLogDate <= end))
                    {
                        continue;
                    }                   
                   
                    if (line != null && line.Contains(query, StringComparison.OrdinalIgnoreCase))
                    {
                        totalMatches++;
                        
                        if (displayedMatches < MaxResultsToDisplay)
                        {
                            matchBuffer.Add(new MatchInfo
                            {
                                FileName = Path.GetFileName(file),
                                FilePath = file,
                                LineNumber = lineIdx,
                                LineText = line.Trim(),
                                TimeStamp = currentLogDate
                            });
                            displayedMatches++;

                            if (matchBuffer.Count >= 100)
                            {
                                var godotChunk = new Godot.Collections.Array<Godot.Collections.Dictionary>();
                                foreach (var m in matchBuffer)
                                {
                                    var d = new Godot.Collections.Dictionary();
                                    d["fileName"] = m.FileName;
                                    d["path"] = m.FilePath;
                                    d["line"] = m.LineNumber;
                                    d["text"] = m.LineText;
                                    d["timestamp"] = m.TimeStamp.ToShortDateString();
                                    godotChunk.Add(d);
                                }
                                matchBuffer.Clear();
                                CallDeferred(nameof(AddResultsChunk), godotChunk);
                            }
                        }
                    }
                    

                }
            }
            catch (Exception e)
            {
                Terminal.WriteError($"LogSearch: Error reading {file}: {e.Message}");
            }
        }

        if (matchBuffer.Count > 0)
        {
            var godotChunk = new Godot.Collections.Array<Godot.Collections.Dictionary>();
            foreach (var m in matchBuffer)
            {
                var d = new Godot.Collections.Dictionary();
                d["fileName"] = m.FileName;
                d["path"] = m.FilePath;
                d["line"] = m.LineNumber;
                d["text"] = m.LineText;
                d["timestamp"] = m.TimeStamp.ToShortDateString();
                godotChunk.Add(d);
            }
            CallDeferred(nameof(AddResultsChunk), godotChunk);
        }

        string resultMsg = $"Found {totalMatches} matches.";
        if (totalMatches > MaxResultsToDisplay)
        {
            resultMsg += $" (Showing first {MaxResultsToDisplay})";
        }
        UpdateStatus(resultMsg);
    }

    private void AddResultsChunk(Godot.Collections.Array<Godot.Collections.Dictionary> chunk)
    {
        foreach (var m in chunk)
        {
            string fileName = m["fileName"].AsString();
            int lineNum = m["line"].AsInt32();
            string text = m["text"].AsString();
            string datePart = m["timestamp"].AsString();
            
            int idx = _resultsList.AddItem($"{datePart} [{fileName}:{lineNum + 1}] {text}");
            
            var metadata = new Godot.Collections.Dictionary
            {
                { "path", m["path"] },
                { "line", lineNum }
            };
            _resultsList.SetItemMetadata(idx, metadata);
        }
    }

    private void UpdateStatus(string status)
    {
        _statusLabel.CallDeferred(Label.MethodName.SetText, status);
    }

    private void UpdateProgress(float progress, string fileName)
    {
        _progressBar.CallDeferred(ProgressBar.MethodName.SetValue, progress * 100);
        _statusLabel.CallDeferred(Label.MethodName.SetText, $"Searching {fileName}...");
    }

    private DateTime? GetDateFromFileName(string fileName, string prefix)
    {
        string nameWithoutExt = fileName.Substring(0, fileName.Length - 4);
        if (nameWithoutExt == prefix) return DateTime.Now; // Current log
        
        string datePart = nameWithoutExt.Substring(prefix.Length).Trim('.');
        string[] parts = datePart.Split('-');
        
        if (parts.Length == 3) // YYYY-MM-DD
        {
            if (int.TryParse(parts[0], out int y) && int.TryParse(parts[1], out int m) && int.TryParse(parts[2], out int d))
            {
                try { return new DateTime(y, m, d); } catch { return null; }
            }
        }
        else if (parts.Length == 2) // YYYY-MM
        {
            if (int.TryParse(parts[0], out int y) && int.TryParse(parts[1], out int m))
            {
                try { return new DateTime(y, m, 1); } catch { return null; }
            }
        }
        
        return null;
    }

    private void OnResultSelected(long index)
    {
        var metadata = _resultsList.GetItemMetadata((int)index).AsGodotDictionary();
        _currentViewPath = metadata["path"].AsString();
        _currentMatchLine = metadata["line"].AsInt32();

        if (!File.Exists(_currentViewPath))
        {
            Terminal.WriteError($"LogSearch: File not found: {_currentViewPath}");
            return;
        }

        _viewStartLine = Math.Max(0, _currentMatchLine - 30);
        _viewEndLine = _currentMatchLine + 30;

        UpdateFileViewer(true);
    }

    private void OnLoadPrevPressed()
    {
        if (string.IsNullOrEmpty(_currentViewPath)) return;
        _viewStartLine = Math.Max(0, _viewStartLine - 50);
        UpdateFileViewer(false,true);
    }

    private void OnLoadNextPressed()
    {
        if (string.IsNullOrEmpty(_currentViewPath)) return;
        _viewEndLine += 50;
        UpdateFileViewer(false);
    }

    private void UpdateFileViewer(bool initialLoad, bool prev=false )
    {
        try
        {
            string query = _searchEdit.Text.Trim();
            
            // Capture state to prevent jumping
            int oldStart = _viewStartLine;
            double oldVScroll = _fileViewer.ScrollVertical;
            int oldCaret = _fileViewer.GetCaretLine();

            List<string> chunkLines = SafeReadLinesChunk(_currentViewPath, _viewStartLine, out int totalLinesInFile);
            _viewEndLine = Math.Min(_viewEndLine, totalLinesInFile - 1);
            
            System.Text.StringBuilder sb = new();
            for (int i = 0; i < chunkLines.Count; i++)
            {
                int actualLineIdx = _viewStartLine + i;
                sb.AppendLine($"[L{actualLineIdx + 1}] {chunkLines[i]}");
            }

            _fileViewer.Text = sb.ToString();
            _fileViewer.SetSearchText(query);
            _fileViewer.SetSearchFlags(0); 

            _loadPrevButton.Visible = _viewStartLine > 0;
            _loadNextButton.Visible = _viewEndLine < totalLinesInFile - 1;

            if (initialLoad)
            {
                int relativeLine = _currentMatchLine - _viewStartLine;
                _fileViewer.SetCaretLine(relativeLine);
                _fileViewer.CenterViewportToCaret();
            }
            else
            {
                if (prev)
                {
                    int diff = chunkLines.Count - _lastChunckCount;
                    _fileViewer.SetCaretLine(oldCaret + diff);
                    _fileViewer.ScrollVertical = oldVScroll + diff;
                }
                else
                {
                    _fileViewer.SetCaretLine(oldCaret);
                    _fileViewer.ScrollVertical = oldVScroll;
                }

            }
            _lastChunckCount = chunkLines.Count;
        }
        catch (Exception e)
        {
            Terminal.WriteError($"LogSearch: Error updating viewer: {e.Message}");
        }
    }

    private List<string> SafeReadLinesChunk(string path, int startLine, out int totalLines)
    {
        var lines = new List<string>();
        totalLines = 0;

        try
        {
            using var fs = new FileStream(path, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs);
            
            int currentLine = 0;
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine();
                if (currentLine >= startLine && currentLine <= _viewEndLine)
                {
                    lines.Add(line);
                }
                currentLine++;
            }
            totalLines = currentLine;
        }
        catch (Exception e)
        {
            Terminal.WriteError($"LogSearch: Error streaming lines: {e.Message}");
        }

        return lines;
    }
}
