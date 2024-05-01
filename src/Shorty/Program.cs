using System.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

Dictionary<string, string> shortUrls = new();

const string DOMAIN = "localhost:5122";
const string HTTP = "http";

/*
app.MapGet("/create", (Func<string, long?, string?, object>)((url, expiry, minimalResponse) =>
{
    if (!Uri.TryCreate(HttpUtility.UrlDecode(url), UriKind.Absolute, out var uri) || uri.Host == DOMAIN)
        return Results.BadRequest("Invalid URL: "+url);
    
    expiry ??= (long)TimeSpan.FromDays(1).TotalMinutes;
    
    if (expiry > TimeSpan.FromDays(30).TotalMinutes)
        return Results.BadRequest("Expiry cannot be more than 30 days");
    if (expiry < 10)
        return Results.BadRequest("Expiry cannot be less than 10 minutes");
    
    // Create a random number between 10 and max that a 5 character base64 string can represent
    var randNum= Random.Shared.Next(10, 64*64*64*64);
    
    string shortUrlData;
    do
    {
        shortUrlData = Convert.ToBase64String(BitConverter.GetBytes(randNum)).Substring(0, 5);
        shortUrlData = shortUrlData.Replace("/", "-").Replace("+", "_");
        //TODO: Set max tries
    }while (shortUrls.ContainsKey(shortUrlData));
    shortUrls[shortUrlData] = uri.AbsoluteUri;
    if (minimalResponse is not null && minimalResponse != "false")
        return $"{HTTP}://{DOMAIN}/" + shortUrlData;
    var shortUrl = $"{HTTP}://{DOMAIN}/" + shortUrlData;
    return Results.Created(shortUrl, new
    {
        shortUrl = shortUrl,
        originalUrl = uri.AbsoluteUri,
        expiresAt = (DateTimeOffset.UtcNow + TimeSpan.FromMinutes((double)expiry)).ToUnixTimeSeconds()
    });
}));
*/

app.MapGet("/{shortUrl}", (string shortUrl) =>
{
    if(shortUrls.TryGetValue(shortUrl, out var url))
        return Results.Redirect(url, false, true);
    return Results.NotFound();
});

app.Use(async (HttpContext context, Func<Task> next) =>
{
    try
    {
        await next();
    }
    catch (BadHttpRequestException ex)
    {
        context.Response.StatusCode = ex.StatusCode;
        await context.Response.WriteAsync("Bad Request: "+ex.Message);
    }
});


app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "areaRoute",
    pattern: "{area:exists}/{controller}/{action}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();