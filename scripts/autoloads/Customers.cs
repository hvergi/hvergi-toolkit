using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

public static class Customers
{
    private const string SavePath = "user://customers_data.json";

    public static Dictionary<string, int> CustomerAffinities { get; } = new Dictionary<string, int>();

    static Customers()
    {
        Load();
    }

    public static void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(CustomerAffinities, new JsonSerializerOptions { WriteIndented = true });
            using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
            if (file != null)
            {
                file.StoreString(json);
            }
        }
        catch (Exception e)
        {
            Terminal.WriteError($"Failed to save customers data: {e.Message}");
        }
    }

    public static void Load()
    {
        if (!FileAccess.FileExists(SavePath)) return;

        try
        {
            using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
            if (file == null) return;

            string json = file.GetAsText();
            var data = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
            if (data != null)
            {
                CustomerAffinities.Clear();
                foreach (var kvp in data)
                {
                    CustomerAffinities[kvp.Key] = kvp.Value;
                }
            }
        }
        catch (Exception e)
        {
            Terminal.WriteError($"Failed to load customers data: {e.Message}");
        }
    }

    public static void SetAffinity(string name, int skillID)
    {
        CustomerAffinities[name] = skillID;
        Save();
    }
}
