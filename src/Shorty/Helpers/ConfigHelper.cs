using Shorty.Constants;

namespace Shorty.Helpers;

public static class ConfigHelper
{
    /// <summary>
    /// All other methods assume this was called first for the config passed in
    /// </summary>
    /// <param name="config">Application config</param>
    public static void EnsureUrlSettingsConfigValidity(IConfiguration config)
    {
        EnsureUrlPrefixIsValid(config);
    }

    public static void EnsureUrlPrefixIsValid(IConfiguration config)
    {
        var currentPrefix = config[ConfigVar.SHORT_URL_PREFIX];
        if(currentPrefix == null)
            return;
        if(currentPrefix.Length == 0)
            return;
        if(string.IsNullOrWhiteSpace(currentPrefix))
            throw new ArgumentException("Short URL prefix cannot be a whitespace");
        if(!Uri.IsWellFormedUriString(currentPrefix, UriKind.Absolute))
        {
            var addon = currentPrefix.Trim().StartsWith("http") || currentPrefix.Trim().Contains("://") ? "" : "http://";
            if(Uri.IsWellFormedUriString(addon + currentPrefix, UriKind.Absolute))
            {
                var uri = new Uri(addon + currentPrefix);
                if(uri.Scheme == "http" || uri.Scheme == "https")
                    return;
                throw new ConfigException("Only HTTP and HTTPS schemes are allowed for short URL prefix");
            }
        }
        throw new ConfigException("Short URL prefix is not a valid URL");
    }


    public static string? GetShortUrlPrefix(this IConfiguration config, string currentScheme)
    {
        // No auto upgrade of url scheme. If no scheme if given it uses the current scheme
        var prefix = config[ConfigVar.SHORT_URL_PREFIX]?.Trim();
        if (string.IsNullOrEmpty(prefix))
            return null;
        if(!Uri.IsWellFormedUriString(prefix, UriKind.Absolute))
            prefix = currentScheme + "://" + prefix;
        if (!Uri.IsWellFormedUriString(prefix, UriKind.Absolute))
            return null;
        return prefix.TrimEnd('/') + "/";
    }
}