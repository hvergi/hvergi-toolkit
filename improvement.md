# Hvergi Toolkit — C# Code Improvement List

Full audit of all `.cs` files in the project. Organized by severity and category.

---

## 🔴 Bugs & Potential Crashes

### 1. `PlayerCard.cs` — Double-counting "you start" craft events (L91)
```csharp
if (lowerLine.Contains("you start") || lowerLine.Contains("you start improving"))
```
The condition `"you start"` is a **superset** of `"you start improving"` — `Contains("you start")` will **always** match when `"you start improving"` would, so the `||` right branch is dead code. Additionally, `"you start"` is overly broad and will trigger on any log line containing that text (e.g. "you start praying"), leading to inflated craft counts.

**Fix:** Use more specific patterns (e.g. `"you start creating"`, `"you start improving"`) as an `||` chain, or a regex.

---

### 2. `Player.cs` — `UpdateLogType()` re-parses config files on **every** `GetLogPath()` call (L83–118)
Every call to `GetLogPath()` (which happens once per second per log type during tracking) opens and reads `config.txt` and `gamesettings.txt` from disk. This is wasteful I/O on a hot path.

**Fix:** Cache `logType` after the first successful read. Add an explicit `InvalidateLogType()` method for when the user changes their config.

---

### 3. `Player.cs` — Possible `NullReferenceException` on `line` in `UpdateLogType()` (L103)
```csharp
if (line.StartsWith("event_log_rotation="))
```
`StreamReader.ReadLine()` can return `null` at end-of-stream, but the `while (!sr.EndOfStream)` check does **not** guarantee `ReadLine()` won't be null (race condition with file being written to).

**Fix:** Add a null check: `if (line != null && line.StartsWith(...))`.

---

### 4. `LogSearch.cs` — Unused variable `daySpan` (L264)
```csharp
var daySpan = line.AsSpan(line.Length - 2);
```
This variable is computed but never used. Also, `line.Length - 2` will throw `ArgumentOutOfRangeException` if the line is shorter than 2 characters.

**Fix:** Remove the dead code.

---

### 5. `LogSearch.cs` — Typo in field name `_lastChunckCount` (L38)
```csharp
private int _lastChunckCount;
```
Should be `_lastChunkCount`.

---

### 6. `HvergiToolkit.cs` — TTS Test button always uses MoiTracker voice ID (L211)
```csharp
DisplayServer.TtsSpeak(msg, AppSettings.MoiTracker.TtsVoiceId);
```
This is inside a generic `SetupAlertGroup(...)` method, but the TTS Test button hardcodes the MoiTracker voice. If the user configures a different voice for Trade or LogAlert, the test will play with the wrong voice.

**Fix:** Pass the voice ID getter into `SetupAlertGroup` as a parameter, or close over the correct settings object.

---

### 7. `HvergiToolkit.cs` — Empty `_Process()` override (L230–232)
```csharp
public override void _Process(double delta)
{
}
```
An empty `_Process()` override still gets called every frame by the engine. This is a minor performance concern.

**Fix:** Remove the empty override.

---

### 8. `LogAlertService.cs` — Filters treated as regex but UI collects plain text (L137)
```csharp
if (Regex.IsMatch(line, filter.Pattern, RegexOptions.IgnoreCase))
```
In `LogAlert.cs`, the `_patternEdit` line edit collects user input as plain text, but the service interprets it as a **regex pattern**. Entering text with regex special characters (e.g. `[`, `(`, `.`) will throw `RegexParseException`.

**Fix:** Either escape the pattern with `Regex.Escape()` before storing, or document and validate that users must enter valid regex. A toggle for "Regex / Plain text" mode would be ideal.

---

### 9. `TradeMonitorService.cs` — Regex compiled on every match call (L218–219, L222)
```csharp
string simpleRegex = Regex.Escape(filter.Pattern).Replace("\\*", ".*");
return Regex.IsMatch(message, simpleRegex, RegexOptions.IgnoreCase);
```
A new regex string is built and parsed on every single log line processed. For a poll-based service running every 2 seconds, this is inefficient.

**Fix:** Pre-compile `Regex` objects at filter creation time and cache them. Invalidate when filters change.

---

### 10. `UpdateManager.cs` — Mutating shared `DefaultRequestHeaders` on every call (L35)
```csharp
HttpClient.DefaultRequestHeaders.Add("User-Agent", "HvergiToolkit-Updater");
```
Each call to `CheckForUpdates()` **appends** another User-Agent header to the static `HttpClient`. Over time this accumulates duplicates and can cause HTTP 431 (Request Header Fields Too Large).

**Fix:** Set the header once in a static constructor, or use `TryAddWithoutValidation`, or check before adding.

---

### 11. `AudioHelper.cs` — WAV fallback to IMA-ADPCM for unexpected bit depths (L70)
```csharp
_ => AudioStreamWav.FormatEnum.ImaAdpcm // Fallback/default
```
If a WAV is 24-bit or 32-bit float, setting the format to IMA-ADPCM will produce garbled audio without any error message.

**Fix:** Return `null` and log an error for unsupported bit depths.

---

### 12. `LogSearch.cs` — Off-by-one in result display (L355)
```csharp
int idx = _resultsList.AddItem($"{datePart} [{fileName}:{lineNum + 1}] {text}");
```
`lineNum` is already 1-based (incremented before use in `SearchLogsAsync`), so `lineNum + 1` displays line 2 when the match is on line 1.

**Fix:** Use `lineNum` without `+ 1`, or keep `lineNum` zero-indexed and add 1 only for display.

---

## 🟡 Code Quality & Maintainability

### 13. Duplicated `ToCamelCase()` method (3 files)
The identical `ToCamelCase()` method exists in:
- `PlayerEditor.cs` (L457–473)
- `PlayerList.cs` (L68–80)
- `PlayerSelectHorizontal.cs` (L74–86)

**Fix:** Extract into a shared utility class (e.g. `StringHelper.ToCamelCase()`).

---

### 14. `ToCamelCase()` is misnamed — it produces **PascalCase**, not camelCase
```csharp
// "hello_world" → "HelloWorld" (PascalCase)
// camelCase would be "helloWorld"
```
**Fix:** Rename to `ToPascalCase()` or `ToTitleCase()`.

---

### 15. Duplicated `FormatTime()` method (2 files)
`MoiTracker.cs` (L319–325) and `PlayerCard.cs` (L118–124) have identical `FormatTime()` implementations.

**Fix:** Move to a shared utility class.

---

### 16. No namespace declarations on most classes
Only `AffinityHelper`, `STPHelper`, `UpdateManager`, and `HvergiToolkit` use namespaces. All other classes are in the **global namespace**, which can lead to name collisions (especially common names like `Player`, `Terminal`, `PlayerList`).

**Fix:** Adopt a consistent namespace (e.g. `HvergiToolkit.Apps`, `HvergiToolkit.Data`, `HvergiToolkit.UI`, `HvergiToolkit.Services`).

---

### 17. `AppSettings.cs` — Seven empty "boilerplate" settings classes (L48–133)
`SkillTrackerSettings`, `STPCalculatorSettings`, `SkillCompareSettings`, `SermonWardenSettings`, `LogSearchSettings`, `AffinityFoodPlannerSettings`, `DyeEstimatorSettings`, `SettlementPlannerSettings` are all empty. They add serialization overhead and clutter.

**Fix:** Remove until needed, or comment them out. Add them back when functionality is implemented.

---

### 18. `AppSettings.cs` — No save debouncing or throttling
`AppSettings.Save()` is called immediately on every single UI toggle (checkboxes, sliders). Each call serializes the entire settings object and writes to disk.

**Fix:** Implement a debounce timer (e.g. save at most once per second) or defer saves to `_Notification(WMCloseRequest)`.

---

### 19. `Customers.cs` — Static constructor calls `Load()` which uses `Terminal` (L12–15)
If `Customers` is referenced before the `Terminal` autoload is initialized, `Terminal.WriteError()` will fail silently (since `Output` is null, it falls back to `GD.Print`, but the log message format is misleading).

**Fix:** Move the initial load to an explicit `Initialize()` call orchestrated from `HvergiToolkit._Ready()`.

---

### 20. Inconsistent use of `using System;` — imported but unused in several files
`DyeEstimator.cs`, `SermonWarden.cs`, `SettlementPlanner.cs`, `DropZone.cs` all import `System` but don't use any types from it.

**Fix:** Remove unused `using` directives.

---

### 21. `HvergiToolkit.cs` — Method `onAppButtonPressed` violates C# naming conventions (L345)
```csharp
private void onAppButtonPressed(string scenePath)
```
Should be `OnAppButtonPressed` (PascalCase for methods).

---

### 22. `HvergiToolkit.cs` — `[Export]` fields use `camelCase` instead of `PascalCase` (L12–48)
```csharp
public RichTextLabel terminalOutput;
public Button newsButton;
```
C# convention (and Godot C# best practice) is PascalCase for public fields/properties.

**Fix:** Rename to `TerminalOutput`, `NewsButton`, etc.

---

### 23. `DyeEstimator.cs`, `SermonWarden.cs`, `SettlementPlanner.cs` — Inconsistent indentation
These files use **tabs** while most other files use **spaces**. Also, the `_Ready()` method has an extra leading space on the body line.

**Fix:** Normalize to the project's chosen indentation style (spaces, based on majority).

---

### 24. `AffinityHelper.cs` — `GetSkillNameByID()` is O(n) linear scan (L350–357)
```csharp
foreach (var kvp in SkillIDs)
{
    if (kvp.Value == skillID) return kvp.Key;
}
```
**Fix:** Build a reverse-lookup dictionary (`Dictionary<int, string>`) at static init time.

---

### 25. `SkillCompare.cs` — `SafeReadAllLines()` reads entire file into memory (L320–330)
For large skill dump files this is fine, but the pattern could be improved for consistency with the streaming approach used elsewhere.

**Fix:** Consider using `File.ReadLines()` wrapped in the same shared-read `FileStream` pattern.

---

### 26. `STPHelper.cs` — Skill name casing inconsistencies
Skill names in `STPHelper.SkillDifficulty` use inconsistent casing (e.g. `"Body control"` lowercase-c, `"Small Axe"` uppercase-A). The `SkillTracker` parses skill names from log lines which may have different casing.

**Fix:** Use case-insensitive dictionary lookups (`StringComparer.OrdinalIgnoreCase`) or normalize casing.

---

### 27. `PlayerEditor.cs` — `ToCamelCase` joins words without spaces (L472)
```csharp
return string.Join("", words);
```
This concatenates words without any separator, so `"john_smith"` becomes `"JohnSmith"` instead of `"John Smith"`.

**Fix:** If the goal is display names, use `string.Join(" ", words)`.

---

## 🟢 Architecture & Design Improvements

### 28. No `IDisposable` / cleanup pattern for `LogReader`
`LogReader` is created in `SkillTracker` and `PlayerCard` but is never explicitly disposed. While it doesn't hold persistent file handles, a disposable pattern would be more robust.

**Fix:** Consider implementing `IDisposable` for clarity.

---

### 29. `TradeMonitorService` and `LogAlertService` have duplicated file-reading logic
Both services independently implement nearly identical file-polling patterns (check file size, seek, read new lines, handle truncation). 

**Fix:** Extract a shared `FileTailWatcher` utility class.

---

### 30. `LogAlertService.cs` — `GetLatestLogPath()` uses filename sorting, not timestamp (L124–126)
```csharp
return Directory.GetFiles(logsDir, prefix + "*.txt")
    .OrderByDescending(f => f)
    .FirstOrDefault();
```
String sorting works for `YYYY-MM-DD` formatted filenames but will break if file naming conventions change. The `Player.GetLogPath()` method already handles this correctly.

**Fix:** Use `Player.GetLogPath()` via the `LogReader.Prefixes` lookup, or sort by `File.GetLastWriteTime()`.

---

### 31. No signal/event cleanup in most Window apps
`AffinityFoodPlanner`, `STPCalculator`, `SkillCompare`, and `SkillTracker` connect signals in `_Ready()` but never disconnect them. While `QueueFree()` handles node-based signals, C# event handlers on static classes (`Players.TeamStateChanged`) can leak.

Only `PlayerList.cs` properly uses `_ExitTree()` for cleanup. Others should follow this pattern.

**Fix:** Add `_ExitTree()` overrides to unsubscribe from static events.

---

### 32. `MoiTracker.cs` — Manually creates `Timer` nodes instead of using scene-defined timers (L107–116)
Both `_craftTimer` and `_logCheckTimer` are created in code. Using scene-defined timers would be more maintainable and inspectable in the editor.

**Fix:** Define timers in the `.tscn` scene file and reference them with `GetNode<Timer>("%CraftTimer")`.

---

### 33. `HvergiToolkit.cs` — App launch logic has hardcoded scene paths (L80–91)
All 12 app button handlers hardcode `res://scenes/apps/...` paths as string literals. Adding a new app requires modifying this file.

**Fix:** Use an `[Export]` dictionary or a data-driven approach (e.g., `[Export] PackedScene[] appScenes`).

---

### 34. `SkillTracker.cs` — `StopTracking()` nulls `_logReader` but `StartTracking` doesn't re-seek (L182–187)
After stop and re-start, a brand new `LogReader` is created. If the user pauses briefly, they lose position context. But if they use Pause → Start, the original reader is reused. Behavior is inconsistent.

**Fix:** Keep the `LogReader` alive and re-seek to end on Start, matching the MoiTracker pattern.

---

### 35. No centralized error handling or crash recovery
If `AppSettings.Load()` encounters corrupt JSON, it silently fails and uses defaults. The user has no indication their settings were lost.

**Fix:** Back up the previous settings file before overwriting. Log a visible warning when loading fails.

---

## 🔵 Performance

### 36. `AffinityHelper.cs` — Static constructor precalculates a massive meal table
`PrecalculateMeals()` runs nested loops (8 × 14 × 5 × 2 × 4 × C(14,3) = ~1.6M iterations) at startup. This adds a noticeable startup delay.

**Fix:** Consider lazy initialization (calculate on first use) or background initialization.

---

### 37. `LogSearch.cs` — `SafeReadLinesChunk()` reads the entire file even when only a slice is needed (L492–520)
The method reads all lines sequentially from the beginning to find the requested chunk, discarding lines before `startLine`.

**Fix:** For large files, consider using a `StreamReader` with byte-offset seeking, or caching the file line index.

---

### 38. `SkillTracker.cs` — `UpdateOtherSkillsTree()` rebuilds the entire Tree on every update (L362–388)
Every skill tick clears and repopulates all tree items. For sessions with many secondary skills, this causes UI churn.

**Fix:** Update existing tree items in-place, only adding new rows when new skills appear.

---

## ⚪ Minor / Cosmetic

### 39. `Terminal.cs` — Missing space after timestamp in Warning/Error (L34, L45)
```csharp
$"[color=yellow][{timestamp}]WARNING: {text}[/color]\n"
// Should be:
$"[color=yellow][{timestamp}] WARNING: {text}[/color]\n"
```

---

### 40. `LogSearch.cs` — Fully qualified `System.IO.FileAccess.Read` used inconsistently
Some files use `System.IO.FileAccess.Read` (to disambiguate from `Godot.FileAccess`), while others just use `FileAccess`. This inconsistency is confusing.

**Fix:** Add `using FileAccess = System.IO.FileAccess;` at the top of files that need it, or consistently use the full qualifier.

---

### 41. `PlayerEditor.cs` — VDF path regex uses double-escaped backslashes (L301, L304)
```csharp
var matches = Regex.Matches(content, @"""path""\s+""([^""]+)""");
string path = match.Groups[1].Value.Replace(@"\\", @"\");
```
The replacement `@"\\" → @"\"` only replaces literal double-backslashes. This may not correctly handle all VDF escape sequences.

**Fix:** Test with actual Steam library VDF files across installations.

---

### 42. `SkillCompare.cs` — No null check on `SkillData.Parent` when popping stack (L172–175)
```csharp
while (parentStack.Count > depth)
{
    parentStack.Pop();
}
```
This is safe but could silently produce incorrect tree hierarchies if the dump file is malformed. A warning log would help debugging.

---

### 43. Missing XML documentation on all public APIs
No public classes or methods have `<summary>` documentation comments. This makes the codebase harder to navigate in IDEs and for contributors.

**Fix:** Add XML doc comments to at least all public classes and their key methods.

---

### 44. `HvergiToolkit.cs` — Stale comment at L229
```csharp
// Called every frame. 'delta' is the elapsed time since the previous frame.
```
This is a Godot template comment and should be removed along with the empty `_Process()`.

---

### 45. `MoiTracker.cs` — Magic number `0` for alert mode (L249)
```csharp
if (settings.Mode == 0) // Sound
```
The comment helps, but using a named constant or enum would be clearer.

**Fix:** Define an `AlertMode` enum (`Sound = 0, TTS = 1`) in `AppSettings`.

---

## Summary

| Severity | Count |
|:---------|:-----:|
| 🔴 Bugs / Crashes | 12 |
| 🟡 Code Quality | 15 |
| 🟢 Architecture | 8 |
| 🔵 Performance | 3 |
| ⚪ Cosmetic / Minor | 7 |
| **Total** | **45** |
