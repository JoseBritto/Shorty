using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
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
    private readonly ConfigManager _configMan;

    public V1Controller(AppDbContext db, ILogger<V1Controller> logger, ConfigManager configMan)
    {
        _db = db;
        _logger = logger;
        _configMan = configMan;

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
        if (uri.Host == "localhost" || (uri.Scheme != "http" && uri.Scheme != "https"))
            return BadRequest("Invalid URL: " + url);
        
        expiry ??= _configMan.Config.DefaultExpirationMinutes;
        
        if (expiry > _configMan.Config.MaxExpirationMinutes)
            return BadRequest($"Expiry cannot be more than {_configMan.Config.MaxExpirationMinutes} minutes");
        
        if (expiry < _configMan.Config.MinExpirationMinutes)
            return BadRequest($"Expiry cannot be less than {_configMan.Config.MinExpirationMinutes} minutes");
        
        // One character in base64 is 6 bits.
        // 1 byte = 8 bits
        var randomBytes = new byte[(int)Math.Ceiling(_configMan.Config.ShortUrlLength * 6.0 / 8.0)];
        _logger.LogDebug("Generating {n} random bytes for short URL of length {l}", randomBytes.Length, _configMan.Config.ShortUrlLength);
        using var rng = RandomNumberGenerator.Create();
        rng.GetNonZeroBytes(randomBytes);
        _logger.LogInformation("Generated random bytes for short URL: {randomB64}", Convert.ToBase64String(randomBytes));
        
        string shortUrlId;
        int tries = 15;
        while (true)
        {
            shortUrlId = Convert.ToBase64String(randomBytes).Substring(0, _configMan.Config.ShortUrlLength);
            shortUrlId = shortUrlId.Replace("/", "-").Replace("+", "_");
            // Check and warn about padding characters
            if (shortUrlId.Contains('='))
            {
                _logger.LogWarning("Padding characters detected in the first {urlLength} characters in generated short URL. Regenerating...", _configMan.Config.ShortUrlLength);
                rng.GetNonZeroBytes(randomBytes);
                if(tries > 0)
                {
                    tries--;
                    continue;
                }
            }

            if (await _db.ShortUrls.AnyAsync(s => s.Id == shortUrlId))
            {
                if(tries > 0)
                {
                    _logger.LogWarning("Short URL {shortUrlId} already exists. Regenerating...", shortUrlId);
                    rng.GetNonZeroBytes(randomBytes);
                    tries--;
                }
            }
            else
            {
                _logger.LogInformation("Generated unique short URL: {shortUrlId}", shortUrlId);
                break;
            }

            if (tries > 0) 
                continue;
            _logger.LogError("Failed to generate a unique short URL after 15 tries. Aborting...");
            return StatusCode(500, "Failed to generate a short URL. Please ask the admin to check the logs.");
        }
        
        var createdAt = DateTime.UtcNow;
        var expiresAt = createdAt + TimeSpan.FromMinutes((double)expiry);
        
        _logger.LogInformation("Short URL created: {ShortUrlId}\nExpires At: {expiresAt} UTC", shortUrlId, expiresAt);
        
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

    [HttpGet]
    public async Task<object> Resolve(string id)
    {
        var obj = await _db.ShortUrls.FindAsync(id);
        if (obj == null)
        {
            return BadRequest("Invalid Id");
        }
        await TrackClickAndExpiry(obj);
        if (obj.IsDeleted)
            return BadRequest("Link does not exist anymore");
        return obj.OriginalUrl + "\n";
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
        {
            return NotFound();
        }
        await TrackClickAndExpiry(shortUrl);
        if (shortUrl.IsDeleted)
        {
            return NotFound();
        }
        return RedirectPreserveMethod(shortUrl.OriginalUrl);
    }
    
    internal string ConstructShortLink(string shortUrlId)
    {
        if(string.IsNullOrWhiteSpace(shortUrlId))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(shortUrlId));
        var prefix = _configMan.Config.GetShortUrlPrefixWithTrailingSlash() ?? $"{Request.Scheme}://{Request.Host}/";
        return $"{prefix}{shortUrlId}";
    }

    private async Task TrackClickAndExpiry(ShortUrl shortUrl)
    {
        if(shortUrl.IsDeleted)
            return;
        
        if (shortUrl.ExpiresAtUtc < DateTime.UtcNow)
        {
            shortUrl.IsDeleted = true;
        }
        else
        {
            shortUrl.Clicks++;
        }
        _db.ShortUrls.Update(shortUrl);
        await _db.SaveChangesAsync();
    }

}