using Microsoft.AspNetCore.Mvc;

namespace Shorty.Areas.Api;

[Area("Api")]
[Route("[area]/v1.0/[action]")]
[Route("[area]/[action]")]
public class V1Controller : Controller
{
    [HttpGet]
    public async Task<object> Create(string url, long? expiry, string? minimalResponse)
    {
        return new { url, expiry, minimalResponse };;
    }
}