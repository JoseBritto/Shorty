using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Shorty.Constants;
using Shorty.Data;
using Shorty.Helpers;
using Shorty.Models;

namespace Shorty.Areas.Api;

[Area("Api")]
[Route("[area]/v1/[action]")]
[Route("[area]/[action]")]
[EnableRateLimiting("unrestricted")]
public class V1Controller : Controller
{
    
    private readonly AppDbContext _db;
    private readonly ILogger<V1Controller> _logger;
    private readonly IConfiguration _config;

    public V1Controller(AppDbContext db, ILogger<V1Controller> logger, IConfiguration config)
    {
        _db = db;
        _logger = logger;
        _config = config;

        if(_db.Database.IsRelational())
            _db.Database.Migrate();
    }
    
    [HttpGet]
    [EnableRateLimiting("restricted")]
    public async Task<object> Create(string url, long? expiry, string? minimal)
    {
        Uri uri;
        try
        {
            uri = new Uri(url);
        }
        catch
        {
            return BadRequest("Invalid URL: " + url);
        }
        if (uri.Host == "localhost")
            return BadRequest("Invalid URL: " + url);
        
        expiry ??= (long)TimeSpan.FromDays(60).TotalMinutes;
        
        if (expiry > TimeSpan.FromDays(365).TotalMinutes)
            return BadRequest("Expiry cannot be more than 365 days");
        
        if (expiry < 10)
            return BadRequest("Expiry cannot be less than 10 minutes");
        
        var randNum = Random.Shared.Next(10, 64*64*64*64*64);
        _logger.LogInformation(Convert.ToBase64String(BitConverter.GetBytes(randNum)));

        
        string shortUrlId;
        do
        {
            shortUrlId = Convert.ToBase64String(BitConverter.GetBytes(randNum)).Substring(0, 5);
            shortUrlId = shortUrlId.Replace("/", "-").Replace("+", "_");
            // Check and remove if anu padding. Probably won't occur after the substring
            shortUrlId = shortUrlId.Replace("=", "");
        } while (await _db.ShortUrls.AnyAsync(s => s.Id == shortUrlId)); // If any has the same id. Loop again
        
        // This loop will go infinite if get closer to the max a 5 character b64 can hold in the db. But I don't think we'll reach that point anytime soon.
        //TODO: After Version 1 release: Break the loop in that case and error out.
        
        var createdAt = DateTime.UtcNow;
        var expiresAt = createdAt + CalculateExpiry(expiry);
        
        _logger.LogInformation("Short URL created: {ShortUrlId}\nExpires At: {expiresAt}", shortUrlId, expiresAt);
        
        var shortUrl = new ShortUrl
        {
            Id = shortUrlId,
            OriginalUrl = uri.AbsoluteUri,
            CreatedAtUtc = createdAt,
            ExpiresAtUtc = expiresAt,
            Clicks = 0,
            IsDeleted = false
        };
        
        _logger.LogInformation("Url Associated: {shortUrlId} -> {url}", shortUrlId, uri.AbsoluteUri);
        
        await _db.ShortUrls.AddAsync(shortUrl);
        await _db.SaveChangesAsync();

        _logger.LogDebug("Data Saved for {urlId}", shortUrlId);

        var shortLink = ConstructShortLink(shortUrlId);
        if (minimal != null && minimal != "n")
        {
            return shortLink + "\n";
        }
        
        return new
        {
            shortUrl = shortLink,
            originalUrl = uri.AbsoluteUri,
            expiresAt = new DateTimeOffset(expiresAt, TimeSpan.Zero).ToUnixTimeSeconds()
        };
    }

    private TimeSpan CalculateExpiry(long? expiry)
    {
        var defaultExpiry = TimeSpan.FromDays(10);
        if (_config[ConfigVar.SHORT_URL_DEFAULT_EXPIRY_MINUTES] != null)
        {
            if (long.TryParse(_config[ConfigVar.SHORT_URL_DEFAULT_EXPIRY_MINUTES], out var val) && val > 0)
                defaultExpiry = TimeSpan.FromMinutes(val);
            _logger.LogWarning("Invalid value for {configVar}. Using default value of {value}.", 
                ConfigVar.SHORT_URL_DEFAULT_EXPIRY_MINUTES, defaultExpiry.TotalMinutes);
        }

        if (_config[ConfigVar.SHORT_URL_ALLOW_CUSTOM_EXPIRY] == null) 
            return defaultExpiry;
        
        if (bool.TryParse(_config[ConfigVar.SHORT_URL_ALLOW_CUSTOM_EXPIRY], out var allow))
        {
            if (!allow)
            {
                if (expiry.HasValue)
                    _logger.LogInformation("Custom expiry is disabled. Ignoring the expiry value.");
                return defaultExpiry;
            }
            if (_config[ConfigVar.MAX_EXPIRY_MINUTES] != null)
            {
                if (long.TryParse(_config[ConfigVar.MAX_EXPIRY_MINUTES], out var maxExpiry) && maxExpiry > 0)
                {
                    if (expiry > maxExpiry)
                    {
                        _logger.LogWarning("Expiry value is more than the maximum allowed. Setting to maximum.");
                        return TimeSpan.FromMinutes(maxExpiry);
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid value for {configVar}. Assuming no limit.",
                        ConfigVar.MAX_EXPIRY_MINUTES);
                }
            }
            if (_config[ConfigVar.MIN_EXPIRY_MINUTES] != null)
            {
                if (long.TryParse(_config[ConfigVar.MIN_EXPIRY_MINUTES], out var minExpiry) && minExpiry > 0)
                {
                    if (expiry < minExpiry)
                    {
                        _logger.LogWarning("Expiry value is less than the minimum allowed. Setting to minimum.");
                        return TimeSpan.FromMinutes(minExpiry);
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid value for {configVar}. Assuming no limit.",
                        ConfigVar.MIN_EXPIRY_MINUTES);
                }

            }
            return expiry.HasValue ? TimeSpan.FromMinutes(expiry.Value) : defaultExpiry;
        }

        _logger.LogWarning("Invalid value for {configVar}. Assuming false.",
            ConfigVar.SHORT_URL_ALLOW_CUSTOM_EXPIRY);
        
        return defaultExpiry;
    }


    [HttpGet]
    public async Task<object> Resolve(string id)
    {
        var obj = await _db.ShortUrls.FindAsync(id);

        if (obj == null)
        {
            return BadRequest("Invalid Id");
        }

        return obj.OriginalUrl;
    }

    public async Task<IActionResult> Docs()
    {
        return View();
    }

    [Route("/{shortUrlId}")]
    [HttpGet]
    public async Task<IActionResult> Redirect(string shortUrlId)
    {
        var shortUrl = await _db.ShortUrls.FindAsync(shortUrlId);
        if (shortUrl == null)
            return NotFound();
        return RedirectPreserveMethod(shortUrl.OriginalUrl);
    }
    
    internal string ConstructShortLink(string shortUrlId)
    {
        if(string.IsNullOrWhiteSpace(shortUrlId))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(shortUrlId));
        var prefix = _config.GetShortUrlPrefix(Request.Scheme);
        return prefix != null ? $"{prefix}{shortUrlId}" : $"{Request.Scheme}://{Request.Host}/{shortUrlId}";
    }

}