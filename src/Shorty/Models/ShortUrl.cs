namespace Shorty.Models;

public class ShortUrl
{
    public string Id { get; set; }
    public string OriginalUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public ulong Clicks { get; set; }
    public bool IsDeleted { get; set; }
}