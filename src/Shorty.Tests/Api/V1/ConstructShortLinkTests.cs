using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ReceivedExtensions;
using Shorty.Areas.Api;
using Shorty.Constants;
using Shorty.Data;

namespace Shorty.Tests;

public class ConstructShortLinkTests
{
    private readonly V1Controller _v1Controller;
    private readonly IConfiguration _mockedConfig;
    private readonly AppDbContext _mockedDbContext;
    private readonly ILogger<V1Controller> _mockedLogger;

    public ConstructShortLinkTests()
    {
        var dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _mockedDbContext = new AppDbContext(dbContextOptions);
        _mockedLogger = Substitute.For<ILogger<V1Controller>>();
        _mockedConfig = Substitute.For<IConfiguration>();
        _v1Controller = new V1Controller(_mockedDbContext, _mockedLogger, _mockedConfig);
        
    }
    
    [Fact]
    public void FailsWhenShortUrlIdIsNullOrWhiteSpace()
    {
        // Arrange
        var shortUrlId1 = string.Empty;
        string shortUrlId2 = null!;
        string shortUrlId3 = " ";
        string shortUrlId4 = "  ";
        string shortUrlId5 = "\t";
        string shortUrlId6 = "\n";
        
        // Act
        Assert.Throws<ArgumentException>(() => _v1Controller.ConstructShortLink(shortUrlId1));
        Assert.Throws<ArgumentException>(() => _v1Controller.ConstructShortLink(shortUrlId2));
        Assert.Throws<ArgumentException>(() => _v1Controller.ConstructShortLink(shortUrlId3));
        Assert.Throws<ArgumentException>(() => _v1Controller.ConstructShortLink(shortUrlId4));
        Assert.Throws<ArgumentException>(() => _v1Controller.ConstructShortLink(shortUrlId5));
        Assert.Throws<ArgumentException>(() => _v1Controller.ConstructShortLink(shortUrlId6));
    }
    
    [Theory]
    [InlineData("https://example.com")]
    [InlineData("https://example.com:1125")]
    [InlineData("https://example.com:1125/")]
    [InlineData("https://us3r@example.com:1125/abc")]
    [InlineData("https://example.com:1125/abc/")]
    [InlineData(" https://example.com:1125/abc ")]
    public void UsesHttpsUrlPrefixCorrectly(string prefix)
    {
        var request = Substitute.For<HttpRequest>();
        var httpContext = Substitute.For<HttpContext>();
        httpContext.Request.Returns(request);
        _v1Controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        request.Scheme = "https";
        request.Host = new HostString("wrong");
        _mockedConfig[ConfigVar.SHORT_URL_PREFIX].Returns(prefix);
        
        var shortUrl = _v1Controller.ConstructShortLink("answer");
        
        Assert.StartsWith(prefix.Trim().EndsWith('/') ? prefix.Trim() : $"{prefix.Trim()}/", shortUrl);
        Assert.True(Uri.IsWellFormedUriString(shortUrl, UriKind.Absolute));
    }
    
    [Theory]
    [InlineData("http://example.com")]
    [InlineData("http://example.com:1125")]
    [InlineData("http://example.com:1125/")]
    [InlineData("http://my1site.com:1125/abc")]
    [InlineData("http://example.com:1125/abc/")]
    [InlineData("  http://example.com:1125/abc ")]
    [InlineData("http://example.com:1125/abc/\t")]
    public void UsesHttpUrlPrefixCorrectly(string prefix)
    {
        var request = Substitute.For<HttpRequest>();
        var httpContext = Substitute.For<HttpContext>();
        httpContext.Request.Returns(request);
        _v1Controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        request.Scheme = "http";
        request.Host = new HostString("wrong");
        _mockedConfig[ConfigVar.SHORT_URL_PREFIX].Returns(prefix);
        
        var shortUrl = _v1Controller.ConstructShortLink("answer");
        
        Assert.StartsWith(prefix.Trim().EndsWith('/') ? prefix.Trim() : $"{prefix.Trim()}/", shortUrl);
        Assert.True(Uri.IsWellFormedUriString(shortUrl, UriKind.Absolute));
    }
    
    
    
    
    [Theory]
    [InlineData("http")]
    [InlineData("https")]
    public void UseCurrentSchemeWhenNoSchemeInPrefix(string scheme)
    {
        var request = Substitute.For<HttpRequest>();
        var httpContext = Substitute.For<HttpContext>();
        httpContext.Request.Returns(request);
        _v1Controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        request.Scheme = scheme;
        request.Host = new HostString("wrong");
        request.IsHttps = scheme == "https";
        _mockedConfig[ConfigVar.SHORT_URL_PREFIX].Returns("example.com");
        
        var shortUrl = _v1Controller.ConstructShortLink("answer");
        
        Assert.Equal($"{scheme}://example.com/answer", shortUrl);
    }
}