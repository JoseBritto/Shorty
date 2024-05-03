using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shorty.Data;
using Shorty.Models;

namespace Shorty.Areas.Api;

[Area("Api")]
[Route("[area]/v1.0/[action]")]
[Route("[area]/[action]")]
public class V1Controller : Controller
{
    
    private readonly AppDbContext _db;
    private readonly ILogger<V1Controller> _logger;

    public V1Controller(AppDbContext db, ILogger<V1Controller> logger)
    {
        _db = db;
        _logger = logger;
    }
    
    [HttpGet]
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
        var expiresAt = createdAt + TimeSpan.FromMinutes(expiry.Value);
        
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

        var shortLink = $"{Request.Scheme}://{Request.Host}/{shortUrlId}";
        if (minimal != null && minimal != "n")
        {
            return shortLink;
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

        return obj.OriginalUrl;
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
}