using System;
using System.IO;
using System.Text.Json;
using Godot;

public static class AppSettings
{
    private const string SavePath = "user://settings.json";

    public class AlertSettings
    {
        public int Mode { get; set; } = 0; // 0: Sound, 1: TTS
        public string SoundPath { get; set; } = "";
        public float Volume { get; set; } = 1.0f;
        public string TTSMessage { get; set; } = "";

        public void ResetToDefaults(int mode, string soundPath, float volume, string ttsMessage)
        {
            Mode = mode;
            SoundPath = soundPath;
            Volume = volume;
            TTSMessage = ttsMessage;
        }
    }

    public class MoiTrackerSettings
    {
        public AlertSettings CraftAlert { get; set; } = new AlertSettings 
        { 
            SoundPath = "res://assets/sounds/craft.wav",
            TTSMessage = "Crafting interval reached."
        };
        public AlertSettings MoiAlert { get; set; } = new AlertSettings 
        { 
            SoundPath = "res://assets/sounds/moi.wav", 
            TTSMessage = "{player} has had an M.O.I." 
        };
        public string TtsVoiceId { get; set; } = "";

        public void ResetToDefaults()
        {
            CraftAlert.ResetToDefaults(0, "res://assets/sounds/craft.wav", 1.0f, "Crafting interval reached.");
            MoiAlert.ResetToDefaults(0, "res://assets/sounds/moi.wav", 1.0f, "{player} has had an M.O.I.");
            TtsVoiceId = "";
        }
    }

    public class SkillTrackerSettings
    {
        // Boilerplate for future settings
    }

    public enum TradeCategory { Any, WTS, WTB, PC, WTT }
    public enum MatchMode { SimpleText, ItemTemplate, Player }

    public class TradeFilter
    {
        public bool IsExclude { get; set; } = false;
        public TradeCategory Category { get; set; } = TradeCategory.Any;
        public MatchMode Mode { get; set; } = MatchMode.SimpleText;
        public string Pattern { get; set; } = "";
        public string TtsMessage { get; set; } = "";
    }

    public class TradeWatcherSettings
    {
        public bool EnableTts { get; set; } = true;
        public string TtsVoiceId { get; set; } = "";
        public System.Collections.Generic.List<TradeFilter> Filters { get; set; } = new();
    }

    public class STPCalculatorSettings
    {
        // Boilerplate for future settings
    }

    public class SkillCompareSettings
    {
        // Boilerplate for future settings
    }

    public class SermonWardenSettings
    {
        // Boilerplate for future settings
    }

    public class LogAlertSettings
    {
        // Boilerplate for future settings
    }

    public class LogSearchSettings
    {
        // Boilerplate for future settings
    }

    public class AffinityFoodPlannerSettings
    {
        // Boilerplate for future settings
    }

    public class DyeEstimatorSettings
    {
        // Boilerplate for future settings
    }

    public class SettlementPlannerSettings
    {
        // Boilerplate for future settings
    }

    public static MoiTrackerSettings MoiTracker { get; set; } = new();
    public static SkillTrackerSettings SkillTracker { get; set; } = new();
    public static TradeWatcherSettings TradeWatcher { get; set; } = new();
    public static STPCalculatorSettings STPCalculator { get; set; } = new();
    public static SkillCompareSettings SkillCompare { get; set; } = new();
    public static SermonWardenSettings SermonWarden { get; set; } = new();
    public static LogAlertSettings LogAlert { get; set; } = new();
    public static LogSearchSettings LogSearch { get; set; } = new();
    public static AffinityFoodPlannerSettings AffinityFoodPlanner { get; set; } = new();
    public static DyeEstimatorSettings DyeEstimator { get; set; } = new();
    public static SettlementPlannerSettings SettlementPlanner { get; set; } = new();

    public static void Save()
    {
        try
        {
            var data = new SaveData
            {
                MoiTracker = MoiTracker,
                SkillTracker = SkillTracker,
                TradeWatcher = TradeWatcher,
                STPCalculator = STPCalculator,
                SkillCompare = SkillCompare,
                SermonWarden = SermonWarden,
                LogAlert = LogAlert,
                LogSearch = LogSearch,
                AffinityFoodPlanner = AffinityFoodPlanner,
                DyeEstimator = DyeEstimator,
                SettlementPlanner = SettlementPlanner
            };

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            
            using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Write);
            if (file != null)
            {
                file.StoreString(json);
            }
        }
        catch (Exception e)
        {
            Terminal.WriteError($"Failed to save app settings: {e.Message}");
        }
    }

    public static void Load()
    {
        if (!Godot.FileAccess.FileExists(SavePath))
        {
            return;
        }

        try
        {
            using var file = Godot.FileAccess.Open(SavePath, Godot.FileAccess.ModeFlags.Read);
            if (file == null) return;

            string json = file.GetAsText();
            var data = JsonSerializer.Deserialize<SaveData>(json);

            if (data != null)
            {
                if (data.MoiTracker != null) MoiTracker = data.MoiTracker;
                if (data.SkillTracker != null) SkillTracker = data.SkillTracker;
                if (data.TradeWatcher != null) TradeWatcher = data.TradeWatcher;
                if (data.STPCalculator != null) STPCalculator = data.STPCalculator;
                if (data.SkillCompare != null) SkillCompare = data.SkillCompare;
                if (data.SermonWarden != null) SermonWarden = data.SermonWarden;
                if (data.LogAlert != null) LogAlert = data.LogAlert;
                if (data.LogSearch != null) LogSearch = data.LogSearch;
                if (data.AffinityFoodPlanner != null) AffinityFoodPlanner = data.AffinityFoodPlanner;
                if (data.DyeEstimator != null) DyeEstimator = data.DyeEstimator;
                if (data.SettlementPlanner != null) SettlementPlanner = data.SettlementPlanner;
            }
        }
        catch (Exception e)
        {
            Terminal.WriteError($"Failed to load app settings: {e.Message}");
        }
    }

    private class SaveData
    {
        public MoiTrackerSettings MoiTracker { get; set; }
        public SkillTrackerSettings SkillTracker { get; set; }
        public TradeWatcherSettings TradeWatcher { get; set; }
        public STPCalculatorSettings STPCalculator { get; set; }
        public SkillCompareSettings SkillCompare { get; set; }
        public SermonWardenSettings SermonWarden { get; set; }
        public LogAlertSettings LogAlert { get; set; }
        public LogSearchSettings LogSearch { get; set; }
        public AffinityFoodPlannerSettings AffinityFoodPlanner { get; set; }
        public DyeEstimatorSettings DyeEstimator { get; set; }
        public SettlementPlannerSettings SettlementPlanner { get; set; }
    }
}
