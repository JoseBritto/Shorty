using Microsoft.Extensions.Configuration;
using Shorty.Constants;
using Shorty.Helpers;

namespace Shorty.Tests;

public class ConfigValidationTests
{
    [Fact]
    public void EnsureUrlSettingsConfigValidity_ThrowsExceptionWhenShortUrlPrefixIsWhitespace()
    {
        // Arrange
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            {ConfigVar.SHORT_URL_PREFIX, " "}
        }).Build();
        
        // Act
        Assert.Throws<ArgumentException>(() => ConfigHelper.EnsureUrlSettingsConfigValidity(config));
    }
    
    [Theory]
    [InlineData("htt://example.com")]
    [InlineData("no://mehost")]
    [InlineData("ftp://example.com")]
    public void EnsureUrlSettingsConfigValidity_ThrowsExceptionWhenShortUrlPrefixIsNotHttporHttps(string prefix)
    {
        // Arrange
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            {ConfigVar.SHORT_URL_PREFIX, prefix}
        }).Build();
        
        // Act
        Assert.Throws<ConfigException>(() => ConfigHelper.EnsureUrlPrefixIsValid(config));
    }
    
    
}