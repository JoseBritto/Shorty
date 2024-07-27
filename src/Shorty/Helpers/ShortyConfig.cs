namespace Shorty.Helpers;

public class ShortyConfig
{
    public int ShortUrlLength { get; set; }
    public string ShortUrlCharacters { get; set; }
    public string? ShortUrlPrefix { get; set; }
    public int DefaultExpirationMinutes { get; set; }
    public int MaxExpirationMinutes { get; set; }
    public int MinExpirationMinutes { get; set; }

    public ShortyConfig()
    {
        ShortUrlLength = 5;
        ShortUrlCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-";
        DefaultExpirationMinutes = 90 * 24 * 60; // 90 days
        MinExpirationMinutes = 10;
        MaxExpirationMinutes = 365 * 24 * 60;
    }
}