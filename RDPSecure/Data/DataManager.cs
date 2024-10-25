using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDPSecure.Data;

public class DataManager
{
    private static readonly object fileLock = new object();

    public static void SaveSettings(AppSettings settings)
    {
        SaveJsonData(AppDataManager.SettingsFile, settings);
    }

    public static AppSettings LoadSettings()
    {
        return LoadJsonData<AppSettings>(AppDataManager.SettingsFile) ?? new AppSettings();
    }

    public static void SaveBannedIPs(List<IPEntry> bannedIPs)
    {
        SaveJsonData(AppDataManager.BannedIPsFile, bannedIPs);
    }

    public static List<IPEntry> LoadBannedIPs()
    {
        return LoadJsonData<List<IPEntry>>(AppDataManager.BannedIPsFile) ?? new List<IPEntry>();
    }

    public static void SaveWhitelistedIPs(List<IPEntry> whitelistedIPs)
    {
        SaveJsonData(AppDataManager.WhitelistedIPsFile, whitelistedIPs);
    }

    public static List<IPEntry> LoadWhitelistedIPs()
    {
        return LoadJsonData<List<IPEntry>>(AppDataManager.WhitelistedIPsFile) ?? new List<IPEntry>();
    }

    private static void SaveJsonData<T>(string filePath, T data)
    {
        try
        {
            lock (fileLock)
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
        }
        catch (Exception ex)
        {
            LogError($"Error saving data to {filePath}: {ex.Message}");
            throw;
        }
    }

    private static T? LoadJsonData<T>(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                lock (fileLock)
                {
                    string json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<T>(json);
                }
            }
        }
        catch (Exception ex)
        {
            LogError($"Error loading data from {filePath}: {ex.Message}");
            throw;
        }
        return default;
    }

    private static void LogError(string message)
    {
        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - ERROR - {message}";
        File.AppendAllText(AppDataManager.LogFile, logEntry + Environment.NewLine);
    }
}