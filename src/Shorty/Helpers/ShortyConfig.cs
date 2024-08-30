namespace Shorty.Helpers;

public class ShortyConfig
{
    public int ShortUrlLength { get; set; }
    public string ShortUrlPrefix { get; set; }
    public long DefaultExpirationMinutes { get; set; }
    public long MaxExpirationMinutes { get; set; }
    public long MinExpirationMinutes { get; set; }
    public int DatabaseCleanupScheduleHours { get; set; }
    
    public ShortyConfig()
    {
        ShortUrlLength = 5;
        DefaultExpirationMinutes = 90 * 24 * 60; // 90 days
        MinExpirationMinutes = 10;
        MaxExpirationMinutes = 365 * 24 * 60;
        DatabaseCleanupScheduleHours = 24;
    }

    public bool TrySetShortUrlPrefix(string newPrefix)
    {
        if (!ConfigValidator.IsValidUrlPrefix(newPrefix))
            return false;
        if (!newPrefix.EndsWith('/'))
            ShortUrlPrefix = newPrefix + '/';
        return true;
    }
    
    public string? GetShortUrlPrefixWithTrailingSlash()
    {
        if(ShortUrlPrefix == null)
            return null;
        
        return ShortUrlPrefix.EndsWith('/') ? ShortUrlPrefix : ShortUrlPrefix + '/';
    }
}