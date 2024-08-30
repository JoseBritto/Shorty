namespace Shorty.Helpers;

public static class ConfigValidator
{
    public static bool ValidateAll(ShortyConfig config)
    {
        if (config.ShortUrlPrefix != null)
            return IsValidUrlPrefix(config.ShortUrlPrefix);
        return true;
    }

    public static bool IsValidUrlPrefix(string newPrefix)
    {
        return Uri.TryCreate(newPrefix, UriKind.Absolute, out var uri) 
            && (uri.Scheme == "http" || uri.Scheme == "https");
    }
}