using System.Text.Json;

namespace GH.VpnService.Win.Settings;

public static class WinClientSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GH.VpnService.Win");

    public static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static WinClientSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new WinClientSettings();

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<WinClientSettings>(json) ?? new WinClientSettings();
        }
        catch
        {
            return new WinClientSettings();
        }
    }

    public static void Save(WinClientSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsPath, json);
    }
}
