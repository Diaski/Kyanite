using Kyanite.Models;
using System;
using System.IO;
using System.Text.Json;

namespace Kyanite.Services;

internal class AppSettingsService
{
    readonly string AppDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Kyanite"
        );

    readonly string FullPathToSettingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Kyanite",
            "settings.json"
        );

    static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public AppSettings? CurrentAppSettings { get; private set; }

    public AppSettingsService()
    {
        Load();
    }

    void Load()
    {
        if (!File.Exists(FullPathToSettingsFile))
        {
            CurrentAppSettings = new AppSettings();
            return;
        }

        try
        {
            string json = File.ReadAllText(FullPathToSettingsFile);
            CurrentAppSettings = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions) ?? new AppSettings();
        }
        catch (Exception)
        {
            // Keep the old file for debugging and start from a clean configuration.
            CurrentAppSettings = new AppSettings();
        }
    }

    public void Save(AppSettings appSettings)
    {
        Directory.CreateDirectory(AppDataDirectory);

        string json = JsonSerializer.Serialize(appSettings, SerializerOptions);

        // Write to a temporary file first so a crash mid-save cannot corrupt the existing settings.
        string temporaryFile = FullPathToSettingsFile + ".tmp";

        File.WriteAllText(temporaryFile, json);
        File.Move(temporaryFile, FullPathToSettingsFile, overwrite: true);

        CurrentAppSettings = appSettings;
    }
}
