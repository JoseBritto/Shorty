namespace Shorty.Helpers;

public class ConfigException : Exception
{
    /// <inheritdoc />
    public ConfigException(string message) : base(message)
    {
    }
}