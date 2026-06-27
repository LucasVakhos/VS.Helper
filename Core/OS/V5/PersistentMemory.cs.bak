using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace VS.Helper.Core.OS.V5;

public class PersistentMemory
{
    private static readonly string _filePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VS.Helper",
        "vshelper_memory.json");

    public Dictionary<string, string> Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return new();
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(_filePath)) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public void Save(string key, string value)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var data = Load();
            data[key] = value;
            File.WriteAllText(_filePath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // Memory persistence is best-effort.
        }
    }
}