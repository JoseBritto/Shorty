using Microsoft.EntityFrameworkCore;
using Shorty.Models;

namespace Shorty.Data;

public class AppDbContext : DbContext
{
    public DbSet<ShortUrl> ShortUrls { get; set; }
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}