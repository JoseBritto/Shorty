using System.Text.Json;

namespace Shorty.Helpers;

public class ConfigManager
{
    const string CONFIG_PATH = "./config.json";
    private readonly ShortyConfig _config;
        
    public ConfigManager(ShortyConfig config, string configPath)
    {
        _config = config;
    }
    
    public async Task SaveConfigAsync()
    {
        var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(CONFIG_PATH, json);
    }
    
    public static async Task<ShortyConfig?> LoadConfigFromDiskAsync()
    {
        if (!File.Exists(CONFIG_PATH))
        {
            return new ShortyConfig();
        }
        try
        {
            if (!File.Exists(CONFIG_PATH))
                return new ShortyConfig();
            var json = await File.ReadAllTextAsync(CONFIG_PATH);
            var config = JsonSerializer.Deserialize<ShortyConfig>(json);
            if (config == null)
                throw new NullReferenceException("Config returned null when parsed!");
            return config;
        }
        catch
        {
            return null;
        }
    }
   
    
}