using System.Text.Json;

namespace Shorty.Helpers;

public class ConfigManager
{
    const string CONFIG_PATH = "./config.json";
    public readonly ShortyConfig Config;
        
    public ConfigManager(ShortyConfig config)
    {
        Config = config;
    }
    
    public async Task SaveConfigAsync()
    {
        var json = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(CONFIG_PATH, json);
    }
    
    public static async Task<ShortyConfig?> LoadConfigFromDiskAsync()
    {
        if (!File.Exists(CONFIG_PATH))
        {
            var config = new ShortyConfig();
            await new ConfigManager(config).SaveConfigAsync();
        }
        try
        {
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